namespace CalradiaForge.UI;

using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

using CalradiaForge.Core.Infra.DependencyInjection;
using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Logging;
using CalradiaForge.UI.Composition;
using CalradiaForge.UI.Dialogs;
using CalradiaForge.UI.Lifecycle;
using CalradiaForge.UI.Views;

using Microsoft.Extensions.DependencyInjection;

using Serilog;

/// <summary>
/// WPF application host and sole owner of the root provider and process lifecycle.
/// </summary>
public partial class App : Application, IApplicationLifetime {
	private static readonly TimeSpan ShutdownInterval = TimeSpan.FromSeconds(15);
	private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
	private ServiceProvider? _provider;
	private Task? _committedLifecycleTask;
	private int _providerDisposed;

	public App() {
		InitializeComponent();
	}

	protected override async void OnStartup(StartupEventArgs e) {
		base.OnStartup(e);
		SubscribeGlobalExceptionHandlers();

		try {
			ServiceCollection services = new();
			services.AddSingleton<IApplicationLifetime>(this);
			services.AddCalradiaForgeCore();
			services.AddCalradiaForgeUi(Dispatcher);

			_provider = services.BuildServiceProvider(new ServiceProviderOptions {
				ValidateOnBuild = true,
				ValidateScopes = true
			});

			ApplicationStartupCoordinator startup =
				_provider.GetRequiredService<ApplicationStartupCoordinator>();
			bool shellStarted = await startup.StartAsync();
			if (!shellStarted) {
				await StartCommittedLifecycleAsync(
					ShutdownReason.UserRequest,
					restartReason: null,
					interactive: false);
			}
		} catch (Exception ex) {
			await HandleStartupFailureAsync(ex);
		}
	}

	public async Task RequestShutdownAsync(ShutdownReason reason) {
		await StartCommittedLifecycleAsync(
			reason,
			restartReason: null,
			interactive: reason == ShutdownReason.UserRequest,
			confirmCommit: reason == ShutdownReason.UserRequest
				? () => ShowOnUiThread(
					provider => ApplicationLifecycleConfirmationPolicy.ConfirmShutdown(
						provider.GetRequiredService<IApplicationWorkController>(),
						provider.GetRequiredService<IApplicationDialogService>(),
						provider.GetRequiredService<TranslationService>().Strings),
					fallback: false)
				: null);
	}

	public async Task RequestRestartAsync(RestartReason reason) {
		await StartCommittedLifecycleAsync(
			ShutdownReason.UserRequest,
			reason,
			interactive: true,
			confirmCommit: () => ShowOnUiThread(
				provider => ApplicationLifecycleConfirmationPolicy.ConfirmRestart(
					reason,
					provider.GetRequiredService<IApplicationWorkController>(),
					provider.GetRequiredService<IApplicationDialogService>(),
					provider.GetRequiredService<TranslationService>().Strings),
				fallback: false));
	}

	protected override void OnSessionEnding(SessionEndingCancelEventArgs e) {
		try {
			ServiceProvider? provider = _provider;
			if (provider is not null && Volatile.Read(ref _providerDisposed) == 0) {
				ApplicationShutdownCoordinator shutdown =
					provider.GetRequiredService<ApplicationShutdownCoordinator>();
				IReadOnlyList<Exception> failures = shutdown.CompleteOperatingSystemShutdown();
				if (failures.Count > 0) {
					Log.Warning(
						"Operating-system shutdown preparation completed with {FailureCount} recoverable failure(s).",
						failures.Count);
				}
			}
		} catch (Exception ex) {
			Debug.WriteLine($"[App] Operating-system shutdown preparation failed: {ex}");
		} finally {
			// SessionEnding is synchronous. Do not cancel Windows logoff/shutdown to await
			// the ordinary async lifecycle; OnExit performs fallback provider disposal.
			base.OnSessionEnding(e);
		}
	}

	protected override void OnExit(ExitEventArgs e) {
		// Normal shutdown disposes asynchronously before calling Application.Shutdown.
		// This synchronous path is a last-resort fallback only.
		if (Interlocked.Exchange(ref _providerDisposed, 1) == 0) {
			try {
				_provider?.Dispose();
			} catch (Exception ex) {
				Debug.WriteLine($"[App] Fallback provider disposal failed: {ex}");
			} finally {
				_provider = null;
			}
		}

		UnsubscribeGlobalExceptionHandlers();
		base.OnExit(e);
	}

	private async Task StartCommittedLifecycleAsync(
		ShutdownReason reason,
		RestartReason? restartReason,
		bool interactive,
		Func<bool>? confirmCommit = null) {
		Task? lifecycleTask;
		await _lifecycleGate.WaitAsync();
		try {
			if (_committedLifecycleTask is null) {
				if (confirmCommit is not null && !confirmCommit()) {
					return;
				}
				_committedLifecycleTask = RunCommittedLifecycleAsync(reason, restartReason, interactive);
			}
			lifecycleTask = _committedLifecycleTask;
		} finally {
			_lifecycleGate.Release();
		}

		if (lifecycleTask is not null) {
			await lifecycleTask;
		}
	}

	private async Task RunCommittedLifecycleAsync(
		ShutdownReason reason,
		RestartReason? restartReason,
		bool interactive) {
		bool replacementAuthorized = false;
		try {
			ServiceProvider? provider = _provider;
			if (provider is null) {
				return;
			}

			ApplicationShutdownCoordinator shutdown =
				provider.GetRequiredService<ApplicationShutdownCoordinator>();
			shutdown.BeginShutdown();

			bool quiescent;
			do {
				quiescent = await shutdown.WaitForQuiescenceAsync(ShutdownInterval);
				if (quiescent || !interactive) {
					break;
				}
			} while (ShowOnUiThread(
				provider => provider
					.GetRequiredService<IApplicationDialogService>()
					.ChooseDelayedShutdown() == DelayedShutdownChoice.ContinueWaiting,
				fallback: false));

			IReadOnlyList<Exception> failures = await shutdown.CompleteShutdownAsync();
			if (failures.Count > 0) {
				Log.Warning(
					"Application shutdown completed with {FailureCount} recoverable cleanup failure(s).",
					failures.Count);
			}

			Log.Information(
				"Application lifecycle committed. ShutdownReason={ShutdownReason}, RestartReason={RestartReason}, Quiescent={Quiescent}.",
				reason,
				restartReason,
				quiescent);
			replacementAuthorized = restartReason is not null;
		} catch (Exception ex) {
			try {
				Log.Error(ex, "Committed application lifecycle encountered an unexpected failure.");
			} catch {
				Debug.WriteLine($"[App] Committed lifecycle failure: {ex}");
			}
		} finally {
			if (Current.MainWindow is MainWindow mainWindow) {
				mainWindow.PrepareForApplicationShutdown();
			}
			await ApplicationExitFinalizer.CompleteAsync(
				DisposeProviderOnceAsync,
				replacementAuthorized ? StartReplacementProcess : null,
				Shutdown,
				ex => Debug.WriteLine($"[App] Final lifecycle step failed: {ex}"));
		}
	}

	private async Task DisposeProviderOnceAsync() {
		if (Interlocked.Exchange(ref _providerDisposed, 1) != 0) {
			return;
		}

		ServiceProvider? provider = _provider;
		_provider = null;
		if (provider is not null) {
			await provider.DisposeAsync();
		}
	}

	private void StartReplacementProcess() {
		string executablePath = Environment.ProcessPath
			?? Process.GetCurrentProcess().MainModule?.FileName
			?? throw new InvalidOperationException("The current executable path is unavailable.");
		Process.Start(new ProcessStartInfo {
			FileName = executablePath,
			UseShellExecute = true
		});
	}

	private async Task HandleStartupFailureAsync(Exception exception) {
		try {
			if (_provider is not null) {
				Log.Fatal(exception, "Fatal application startup failure.");
				ShowFatalErrorOnUiThread("CalradiaForge could not start and will now close.");
				await StartCommittedLifecycleAsync(
					ShutdownReason.FatalStartup,
					restartReason: null,
					interactive: false);
				return;
			}
		} catch (Exception cleanupException) {
			Debug.WriteLine($"[App] Controlled startup cleanup failed: {cleanupException}");
		}

		Logger.Instance.Error(exception, "App: Fatal failure before provider/logger availability.");
		MessageBox.Show(
			"CalradiaForge could not initialize and will now close.",
			"CalradiaForge Fatal Error",
			MessageBoxButton.OK,
			MessageBoxImage.Error);
		await ApplicationExitFinalizer.CompleteAsync(
			DisposeProviderOnceAsync,
			startReplacement: null,
			Shutdown,
			ex => Debug.WriteLine($"[App] Bootstrap cleanup failed: {ex}"));
	}

	private T ShowOnUiThread<T>(Func<ServiceProvider, T> show, T fallback) {
		ServiceProvider? provider = _provider;
		if (provider is null || Volatile.Read(ref _providerDisposed) != 0) {
			return fallback;
		}

		T Invoke() => show(provider);
		return Dispatcher.CheckAccess() ? Invoke() : Dispatcher.Invoke(Invoke);
	}

	private void ShowFatalErrorOnUiThread(string message) {
		void Show() => MessageBox.Show(
			message,
			"CalradiaForge Fatal Error",
			MessageBoxButton.OK,
			MessageBoxImage.Error);

		if (Dispatcher.CheckAccess()) {
			Show();
		} else {
			Dispatcher.Invoke(Show);
		}
	}

	private void SubscribeGlobalExceptionHandlers() {
		DispatcherUnhandledException += OnDispatcherUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
	}

	private void UnsubscribeGlobalExceptionHandlers() {
		DispatcherUnhandledException -= OnDispatcherUnhandledException;
		TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
		AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
	}

	private async void OnDispatcherUnhandledException(
		object sender,
		DispatcherUnhandledExceptionEventArgs e) {
		e.Handled = true;
		try {
			Log.Fatal(e.Exception, "Fatal WPF dispatcher exception.");
			ShowFatalErrorOnUiThread("CalradiaForge encountered a fatal error and will close.");
			await StartCommittedLifecycleAsync(
				ShutdownReason.FatalDispatcher,
				restartReason: null,
				interactive: false);
		} catch (Exception cleanupException) {
			Debug.WriteLine($"[App] Dispatcher fatal cleanup failed: {cleanupException}");
			Shutdown();
		}
	}

	private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e) {
		try {
			Log.Error(e.Exception, "Unobserved task exception.");
		} catch {
			Debug.WriteLine($"[App] Unobserved task exception: {e.Exception}");
		} finally {
			e.SetObserved();
		}
	}

	private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
		Exception exception = e.ExceptionObject as Exception
			?? new InvalidOperationException("AppDomain raised a non-Exception failure object.");
		try {
			Log.Fatal(exception, "Unhandled AppDomain exception. IsTerminating={IsTerminating}.", e.IsTerminating);
		} catch {
			Debug.WriteLine($"[App] AppDomain unhandled exception: {exception}");
		}

		if (!e.IsTerminating && Volatile.Read(ref _providerDisposed) == 0) {
			_ = Dispatcher.InvokeAsync(
				() => RequestShutdownAsync(ShutdownReason.FatalStartup));
		}
	}
}
