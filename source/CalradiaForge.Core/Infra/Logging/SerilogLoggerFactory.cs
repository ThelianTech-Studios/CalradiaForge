namespace CalradiaForge.Core.Infra.Logging;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Paths;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

/// <summary>
/// Creates the provider-owned Serilog logging pipeline used by application composition.
/// Callers may add source context with <c>ForContext&lt;T&gt;()</c>.
/// </summary>
public sealed class SerilogLoggerFactory {

	/// <summary>
	/// Private readonly fields used to construct the process logger once.
	/// </summary>
	private readonly LoggingSettings _loggingSettings;
	private readonly LogFileLifecycle _logFileLifecycle;
	private readonly string _logFilePath;
	private int _created;

	/// <summary>
	/// Initializes a new instance of the <see cref="SerilogLoggerFactory"/> class.
	/// </summary>
	public SerilogLoggerFactory(LoggingSettings loggingSettings)
		: this(
			loggingSettings,
			new LogFileLifecycle(AppPaths.LogsDirectory, AppPaths.LogsFilePath),
			AppPaths.LogsFilePath) {
	}

	internal SerilogLoggerFactory(
		LoggingSettings loggingSettings,
		LogFileLifecycle logFileLifecycle,
		string logFilePath) {
		_loggingSettings = loggingSettings ?? throw new ArgumentNullException(nameof(loggingSettings));
		_logFileLifecycle = logFileLifecycle ?? throw new ArgumentNullException(nameof(logFileLifecycle));
		ArgumentException.ThrowIfNullOrWhiteSpace(logFilePath);
		_logFilePath = logFilePath;
	}

	/// <summary>Creates the single provider-owned Serilog logger for the current application graph.</summary>
	public Serilog.ILogger Create() {
		if (Interlocked.Exchange(ref _created, 1) != 0) {
			throw new InvalidOperationException("The Serilog logger factory can create only one logger instance.");
		}

		_logFileLifecycle.PrepareForStartup();
		var formatter = new SerilogTextFormatter();
		var configuration = new LoggerConfiguration()
			.MinimumLevel.Is(_loggingSettings.DebugMode ? LogEventLevel.Debug : LogEventLevel.Information)
			.Enrich.FromLogContext()
			.Enrich.WithThreadId()
			.Enrich.WithExceptionDetails()
			.WriteTo.Async(sinks => sinks.File(
				formatter,
				_logFilePath,
				rollingInterval: RollingInterval.Infinite,
				retainedFileCountLimit: null,
				shared: false));

#if DEBUG
		configuration = configuration.WriteTo.Debug(formatter: formatter);
#endif
		return configuration.CreateLogger();
	}
}
