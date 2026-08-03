namespace CalradiaForge.UI.Toasts;

using System.Globalization;
using System.IO;

using CalradiaForge.Core.Infra.Localization;
using CalradiaForge.Core.Infra.Mods;
using CalradiaForge.Core.Models;

using Serilog;

/// <summary>
/// Explicitly activated application-lifetime presentation boundary for manual
/// mod-install progress and terminal notifications.
/// </summary>
public interface IInstallNotificationPresenter {
	void Activate();
	void ReportLauncherCompletion(LauncherInstallPresentationCompletion completion);
}

/// <summary>
/// Reports that the temporary launcher presentation has reconciled the accepted
/// snapshot and recalculated launcher state for one admitted operation.
/// </summary>
public sealed record LauncherInstallPresentationCompletion(
	Guid OperationId,
	bool Succeeded,
	string? ErrorMessage = null);

/// <summary>
/// Maps UI-neutral install notifications into one correlated toast lifecycle.
/// It observes work only; admission, cancellation, sequencing, and quiescence
/// remain owned by <see cref="ModPipelineManager"/>.
/// </summary>
public sealed class InstallNotificationPresenter : IInstallNotificationPresenter, IDisposable {
	private static readonly TimeSpan ProgressThrottleInterval = TimeSpan.FromMilliseconds(150);
	private const int RememberedTerminalLimit = 32;

	private readonly object _gate = new();
	private readonly IModInstallOperationNotificationSource _source;
	private readonly IToastNotificationSink _notifications;
	private readonly TranslationService _translator;
	private readonly ILogger _logger;
	private readonly INotificationDelayScheduler _scheduler;
	private readonly Queue<Guid> _rememberedTerminalOrder = new();
	private readonly HashSet<Guid> _rememberedTerminalIds = [];
	private OperationState? _current;
	private bool _activated;
	private bool _disposed;

	public InstallNotificationPresenter(
		IModInstallOperationNotificationSource source,
		IToastNotificationSink notifications,
		TranslationService translator,
		ILogger logger)
		: this(source, notifications, translator, logger, new TimerNotificationDelayScheduler()) { }

	internal InstallNotificationPresenter(
		IModInstallOperationNotificationSource source,
		IToastNotificationSink notifications,
		TranslationService translator,
		ILogger logger,
		INotificationDelayScheduler scheduler) {
		_source = source ?? throw new ArgumentNullException(nameof(source));
		_notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
		_translator = translator ?? throw new ArgumentNullException(nameof(translator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
	}

	/// <summary>
	/// Subscribes to the manager notification boundary exactly once.
	/// </summary>
	public void Activate() {
		lock (_gate) {
			ObjectDisposedException.ThrowIf(_disposed, this);
			if (_activated) {
				return;
			}
			_source.InstallProgressChanged += OnInstallProgressChanged;
			_source.InstallOperationCompleted += OnInstallOperationCompleted;
			_activated = true;
		}
	}

	/// <summary>
	/// Supplies the presentation half of the terminal handshake. The toast is
	/// finalized only after both this signal and the Core terminal result arrive.
	/// </summary>
	public void ReportLauncherCompletion(LauncherInstallPresentationCompletion completion) {
		ArgumentNullException.ThrowIfNull(completion);
		Finalization? finalization = null;
		lock (_gate) {
			if (_disposed || !_activated) {
				return;
			}
			if (_current is null || _current.OperationId != completion.OperationId) {
				_logger.Debug(
					"Ignored stale launcher install completion for {OperationId}.",
					completion.OperationId);
				return;
			}
			if (_current.Completion is not null) {
				_logger.Debug(
					"Ignored duplicate launcher install completion for {OperationId}.",
					completion.OperationId);
				return;
			}
			_current.Completion = completion;
			finalization = TryTakeFinalizationLocked(_current);
		}
		FinalizeNotification(finalization);
	}

	private void OnInstallProgressChanged(ModInstallProgress progress) {
		if (progress.Stage == ModInstallProgressStage.OperationStarted) {
			BeginOperation(progress);
			return;
		}

		IToastNotificationHandle? handle = null;
		ModInstallProgress? dispatch = null;
		TimeSpan? scheduleDelay = null;
		bool cancelPending = false;
		lock (_gate) {
			if (_disposed || !_activated
				|| _current is null
				|| _current.OperationId != progress.OperationId
				|| _current.TerminalResult is not null) {
				return;
			}

			DateTime timestamp = progress.TimestampUtc == default
				? DateTime.UtcNow
				: progress.TimestampUtc;
			bool preserveTransition = progress.Stage != ModInstallProgressStage.Extracting;
			TimeSpan elapsed = timestamp - _current.LastProgressDispatchUtc;
			if (preserveTransition
				|| _current.LastProgressDispatchUtc == DateTime.MinValue
				|| elapsed >= ProgressThrottleInterval) {
				_current.PendingProgress = null;
				_current.LastProgressDispatchUtc = timestamp;
				handle = _current.Handle;
				dispatch = progress;
				cancelPending = true;
			} else {
				_current.PendingProgress = progress;
				scheduleDelay = ProgressThrottleInterval - elapsed;
			}
		}

		if (cancelPending) {
			_scheduler.Cancel();
		}
		if (dispatch is not null && handle is not null) {
			UpdateHandle(handle, dispatch);
		}
		if (scheduleDelay is not null) {
			_scheduler.Schedule(scheduleDelay.Value, FlushPendingProgress);
		}
	}

	private void BeginOperation(ModInstallProgress progress) {
		OperationState state;
		IToastNotificationHandle? previousHandle;
		lock (_gate) {
			if (_disposed || !_activated) {
				return;
			}
			if (_current?.OperationId == progress.OperationId) {
				return;
			}

			previousHandle = _current?.Handle;
			state = new OperationState(progress.OperationId) {
				LastProgressDispatchUtc = progress.TimestampUtc == default
					? DateTime.UtcNow
					: progress.TimestampUtc
			};
			_current = state;
		}

		_scheduler.Cancel();
		previousHandle?.Close();
		IToastNotificationHandle opened = _notifications.Open(CreateProgressRequest(progress));
		bool retain;
		ModInstallProgress? pending;
		lock (_gate) {
			retain = !_disposed && ReferenceEquals(_current, state);
			if (retain) {
				state.Handle = opened;
				pending = state.PendingProgress;
				state.PendingProgress = null;
			} else {
				pending = null;
			}
		}
		if (!retain) {
			opened.Close();
			return;
		}
		if (pending is not null) {
			UpdateHandle(opened, pending);
		}
	}

	private void FlushPendingProgress() {
		IToastNotificationHandle? handle;
		ModInstallProgress? progress;
		lock (_gate) {
			if (_disposed || !_activated || _current?.TerminalResult is not null) {
				return;
			}
			handle = _current?.Handle;
			progress = _current?.PendingProgress;
			if (_current is not null) {
				_current.PendingProgress = null;
				if (progress is not null) {
					_current.LastProgressDispatchUtc = progress.TimestampUtc == default
						? DateTime.UtcNow
						: progress.TimestampUtc;
				}
			}
		}
		if (handle is not null && progress is not null) {
			UpdateHandle(handle, progress);
		}
	}

	private void OnInstallOperationCompleted(ModInstallOperationResult result) {
		Finalization? finalization = null;
		bool showStandalone = false;
		lock (_gate) {
			if (_disposed || !_activated || !RememberTerminalLocked(result.OperationId)) {
				return;
			}

			if (!result.WasAdmitted) {
				showStandalone = true;
			} else {
				if (_current is null || _current.OperationId != result.OperationId) {
					_current = new OperationState(result.OperationId);
				}
				if (_current.TerminalResult is not null) {
					return;
				}
				_current.TerminalResult = result;
				_current.PendingProgress = null;
				finalization = TryTakeFinalizationLocked(_current);
			}
		}

		if (showStandalone) {
			_notifications.Open(CreateTerminalRequest(result, completion: null));
			return;
		}
		_scheduler.Cancel();
		FinalizeNotification(finalization);
	}

	private Finalization? TryTakeFinalizationLocked(OperationState state) {
		if (state.TerminalResult is null || state.Completion is null) {
			return null;
		}
		_current = null;
		return new Finalization(state.Handle, state.TerminalResult, state.Completion);
	}

	private void FinalizeNotification(Finalization? finalization) {
		if (finalization is null) {
			return;
		}

		_scheduler.Cancel();
		ToastRequest request = CreateTerminalRequest(
			finalization.Result,
			finalization.Completion);
		if (finalization.Handle is not null) {
			finalization.Handle.Transition(request);
		} else {
			_notifications.Open(request);
		}
	}

	private void UpdateHandle(IToastNotificationHandle handle, ModInstallProgress progress) {
		try {
			double value;
			double maximum;
			if (progress.Stage == ModInstallProgressStage.Extracting) {
				value = progress.BatchFilesProcessed;
				maximum = Math.Max(1, progress.EstimatedTotalFiles);
			} else {
				value = progress.ProcessedArchiveCount;
				maximum = Math.Max(1, progress.TotalArchives);
			}
			handle.UpdateProgress(value, maximum, FormatProgress(progress));
		} catch (Exception ex) {
			_logger.Warning(
				ex,
				"Install notification progress update failed for {OperationId}.",
				progress.OperationId);
		}
	}

	private ToastRequest CreateProgressRequest(ModInstallProgress progress) => new() {
		Title = _translator.Strings.Toast_InstallingMods,
		Message = _translator.Strings.Toast_InstallInProgress,
		Severity = ToastSeverity.Info,
		TemplateKey = ToastTemplateKeys.InstallProgress,
		IsPersistent = true,
		AllowClickDismiss = false,
		ShowCloseButton = false,
		ProgressValue = 0,
		ProgressMax = Math.Max(1, progress.TotalArchives)
	};

	private ToastRequest CreateTerminalRequest(
		ModInstallOperationResult result,
		LauncherInstallPresentationCompletion? completion) {
		TranslationStrings strings = _translator.Strings;
		if (completion is { Succeeded: false }) {
			if (!string.IsNullOrWhiteSpace(completion.ErrorMessage)) {
				_logger.Error(
					"Launcher presentation reconciliation failed for {OperationId}: {PresentationError}",
					result.OperationId,
					completion.ErrorMessage);
			}
			return TerminalRequest(
				strings.Toast_InstallFailed,
				strings.Toast_InstallReconciliationFailed,
				ToastSeverity.Error);
		}

		(string title, ToastSeverity severity) = result.Status switch {
			ModInstallOperationStatus.Succeeded =>
				(strings.Toast_InstallComplete, ToastSeverity.Success),
			ModInstallOperationStatus.SucceededWithWarnings =>
				(strings.Toast_InstallSucceededWithWarnings, ToastSeverity.Warning),
			ModInstallOperationStatus.PartiallyFailed =>
				(strings.Toast_InstallCompleteWithErrors, ToastSeverity.Error),
			ModInstallOperationStatus.Cancelled when HasCompletedChanges(result.Summary) =>
				(strings.Toast_InstallCancelled, ToastSeverity.Warning),
			ModInstallOperationStatus.Cancelled =>
				(strings.Toast_InstallCancelled, ToastSeverity.Info),
			ModInstallOperationStatus.RejectedBusy =>
				(strings.Toast_InstallRejectedBusy, ToastSeverity.Warning),
			ModInstallOperationStatus.RejectedAdmissionStopped =>
				(strings.Toast_InstallRejectedAdmissionStopped, ToastSeverity.Info),
			ModInstallOperationStatus.ValidationFailed =>
				(strings.Toast_InstallValidationFailed, ToastSeverity.Warning),
			_ => (strings.Toast_InstallFailed, ToastSeverity.Error)
		};

		return TerminalRequest(title, FormatTerminalMessage(result), severity);
	}

	private ToastRequest TerminalRequest(string title, string message, ToastSeverity severity) {
		return new ToastRequest {
			Title = title,
			Message = message,
			Severity = severity,
			AllowClickDismiss = true,
			ShowCloseButton = true
		};
	}

	private string FormatProgress(ModInstallProgress progress) {
		TranslationStrings strings = _translator.Strings;
		if (!string.IsNullOrWhiteSpace(progress.ArchiveFileName)) {
			return string.Format(
				CultureInfo.CurrentCulture,
				strings.Toast_InstallProgressFormat,
				progress.ArchiveIndex,
				progress.TotalArchives,
				Path.GetFileNameWithoutExtension(progress.ArchiveFileName),
				progress.BatchFilesProcessed,
				progress.EstimatedTotalFiles);
		}
		return string.Format(
			CultureInfo.CurrentCulture,
			strings.Toast_InstallSummaryFormat,
			progress.InstalledCount,
			progress.UpgradedCount,
			progress.SkippedCount,
			progress.FailedCount);
	}

	private string FormatTerminalMessage(ModInstallOperationResult result) {
		TranslationStrings strings = _translator.Strings;
		if (!result.WasAdmitted) {
			if (result.Status == ModInstallOperationStatus.RejectedBusy) {
				return strings.Toast_InstallRejectedBusy;
			}
			if (result.Status == ModInstallOperationStatus.RejectedAdmissionStopped) {
				return strings.Toast_InstallRejectedAdmissionStopped;
			}

			List<string> validationMessages = FormatDiagnosticMessages(result);
			return validationMessages.Count > 0
				? string.Join(" ", validationMessages)
				: strings.Toast_InstallDiagnosticGeneric;
		}

		List<string> messages = [];
		if (result.Status == ModInstallOperationStatus.Cancelled) {
			messages.Add(HasCompletedChanges(result.Summary)
				? strings.Toast_InstallCancelledWithChanges
				: strings.Toast_InstallCancelledWithoutChanges);
		}
		messages.Add(string.Format(
			CultureInfo.CurrentCulture,
			strings.Toast_InstallSummaryFormat,
			result.Summary.InstalledCount,
			result.Summary.UpgradedCount,
			result.Summary.SkippedCount,
			result.Summary.FailedCount));

		if (result.Summary.BLSEResult is ModInstallResult blse) {
			messages.Add(blse.Status is ModInstallStatus.Installed or ModInstallStatus.Upgraded
				? strings.Toast_InstallBlseSucceeded
				: strings.Toast_InstallBlseFailed);
		}
		if (result.UnblockResult is { FailedCount: > 0 } unblock) {
			messages.Add(string.Format(
				CultureInfo.CurrentCulture,
				strings.Toast_InstallUnblockWarningFormat,
				unblock.FailedCount));
		}
		if (result.ReconciliationResult is { Success: false }) {
			messages.Add(strings.Toast_InstallReconciliationFailed);
		}
		foreach (string diagnosticMessage in FormatDiagnosticMessages(result)) {
			if (!messages.Contains(diagnosticMessage, StringComparer.CurrentCulture)) {
				messages.Add(diagnosticMessage);
			}
		}
		return string.Join(" ", messages);
	}

	private List<string> FormatDiagnosticMessages(ModInstallOperationResult result) {
		TranslationStrings strings = _translator.Strings;
		List<string> messages = [];
		foreach (ModInstallDiagnosticCode code in result.DiagnosticCodes) {
			string? message = code switch {
				ModInstallDiagnosticCode.None
					or ModInstallDiagnosticCode.Busy
					or ModInstallDiagnosticCode.AdmissionStopped
					or ModInstallDiagnosticCode.Cancelled => null,
				ModInstallDiagnosticCode.ArchiveSelectionEmpty =>
					strings.Toast_InstallValidationArchiveSelectionEmpty,
				ModInstallDiagnosticCode.GameDirectoryNotConfigured =>
					strings.Toast_InstallValidationGameDirectoryNotConfigured,
				ModInstallDiagnosticCode.GameDirectoryMissing =>
					strings.Toast_InstallValidationGameDirectoryMissing,
				ModInstallDiagnosticCode.ModulesDirectoryMissing =>
					strings.Toast_InstallValidationModulesDirectoryMissing,
				ModInstallDiagnosticCode.UnblockCompletedWithFailures
					when result.UnblockResult is { FailedCount: > 0 } => null,
				ModInstallDiagnosticCode.UnblockCompletedWithFailures
					or ModInstallDiagnosticCode.UnblockFailed =>
					strings.Toast_InstallUnblockFailed,
				ModInstallDiagnosticCode.ReconciliationIncomplete
					or ModInstallDiagnosticCode.ReconciliationFailed
					when result.ReconciliationResult is { Success: false } => null,
				ModInstallDiagnosticCode.ReconciliationIncomplete
					or ModInstallDiagnosticCode.ReconciliationFailed =>
					strings.Toast_InstallReconciliationFailed,
				ModInstallDiagnosticCode.ReconciliationCompletedWithWarnings =>
					strings.Toast_InstallReconciliationWarnings,
				_ => strings.Toast_InstallDiagnosticGeneric
			};
			if (message is not null
				&& !messages.Contains(message, StringComparer.CurrentCulture)) {
				messages.Add(message);
			}
		}
		return messages;
	}

	private bool RememberTerminalLocked(Guid operationId) {
		if (!_rememberedTerminalIds.Add(operationId)) {
			_logger.Debug(
				"Ignored duplicate install terminal result for {OperationId}.",
				operationId);
			return false;
		}
		_rememberedTerminalOrder.Enqueue(operationId);
		while (_rememberedTerminalOrder.Count > RememberedTerminalLimit) {
			_rememberedTerminalIds.Remove(_rememberedTerminalOrder.Dequeue());
		}
		return true;
	}

	private static bool HasCompletedChanges(ModInstallSummary summary) =>
		summary.InstalledCount > 0
		|| summary.UpgradedCount > 0
		|| summary.BLSEResult?.Status is ModInstallStatus.Installed or ModInstallStatus.Upgraded;

	public void Dispose() {
		IToastNotificationHandle? handle;
		lock (_gate) {
			if (_disposed) {
				return;
			}
			_disposed = true;
			if (_activated) {
				_source.InstallProgressChanged -= OnInstallProgressChanged;
				_source.InstallOperationCompleted -= OnInstallOperationCompleted;
				_activated = false;
			}
			handle = _current?.Handle;
			_current = null;
		}
		_scheduler.Dispose();
		handle?.Close();
	}

	private sealed class OperationState {
		public OperationState(Guid operationId) {
			OperationId = operationId;
		}

		public Guid OperationId { get; }
		public IToastNotificationHandle? Handle { get; set; }
		public ModInstallProgress? PendingProgress { get; set; }
		public ModInstallOperationResult? TerminalResult { get; set; }
		public LauncherInstallPresentationCompletion? Completion { get; set; }
		public DateTime LastProgressDispatchUtc { get; set; } = DateTime.MinValue;
	}

	private sealed record Finalization(
		IToastNotificationHandle? Handle,
		ModInstallOperationResult Result,
		LauncherInstallPresentationCompletion Completion);
}

internal interface INotificationDelayScheduler : IDisposable {
	void Schedule(TimeSpan delay, Action callback);
	void Cancel();
}

internal sealed class TimerNotificationDelayScheduler : INotificationDelayScheduler {
	private readonly object _gate = new();
	private Timer? _timer;
	private Action? _callback;
	private bool _disposed;

	public void Schedule(TimeSpan delay, Action callback) {
		ArgumentNullException.ThrowIfNull(callback);
		lock (_gate) {
			ObjectDisposedException.ThrowIf(_disposed, this);
			_callback = callback;
			_timer ??= new Timer(OnTimer);
			_timer.Change(delay < TimeSpan.Zero ? TimeSpan.Zero : delay, Timeout.InfiniteTimeSpan);
		}
	}

	public void Cancel() {
		lock (_gate) {
			if (_disposed) {
				return;
			}
			_callback = null;
			_timer?.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
		}
	}

	private void OnTimer(object? state) {
		Action? callback;
		lock (_gate) {
			if (_disposed) {
				return;
			}
			callback = _callback;
			_callback = null;
		}
		callback?.Invoke();
	}

	public void Dispose() {
		lock (_gate) {
			if (_disposed) {
				return;
			}
			_disposed = true;
			_callback = null;
			_timer?.Dispose();
			_timer = null;
		}
	}
}
