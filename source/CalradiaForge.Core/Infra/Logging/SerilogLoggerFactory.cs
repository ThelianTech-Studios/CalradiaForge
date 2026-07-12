namespace CalradiaForge.Core.Infra.Logging;

using CalradiaForge.Core.Infra.Config;
using CalradiaForge.Core.Infra.Paths;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

/// <summary>
/// Creates the future Serilog logging pipeline without initializing it during WPF startup.
/// Callers may add source context with <c>ForContext&lt;T&gt;()</c> after composition is introduced.
/// </summary>
public sealed class SerilogLoggerFactory {

	/// <summary>
	/// Private Readonly fields to hold the AppConfig and Log Directory path.
	/// </summary>
	private readonly AppConfigSettings _configInstance;
	private readonly string _logDirectory;
	private readonly string _logFilePath;


	/// <summary>
	/// Initializes a new instance of the <see cref="SerilogLoggerFactory"/> class.
	/// </summary>
	/// <param name="configSettings"></param>
	public SerilogLoggerFactory(AppConfigSettings configSettings) {
		_configInstance = configSettings;
		_logDirectory = AppPaths.LogsDirectory;
		_logFilePath = AppPaths.LogsFilePath;
	}

	/// <summary>Creates an isolated logger for later DI composition or smoke testing.</summary>
	public Serilog.ILogger Create() {
		LogRetentionPolicy.Cleanup(_logDirectory, _configInstance.LogFileDaysToKeep);
		var formatter = new RedactingTextFormatter();
		var configuration = new LoggerConfiguration()
			.MinimumLevel.Is(_configInstance.DebugMode ? LogEventLevel.Debug : LogEventLevel.Information)
			.Enrich.FromLogContext()
			.Enrich.WithThreadId()
			.Enrich.WithExceptionDetails()
			.WriteTo.Async(sinks => sinks.File(
				formatter,
				_logFilePath,
				rollingInterval: RollingInterval.Infinite,
				retainedFileCountLimit: default,
				shared: false));

#if DEBUG
		configuration = configuration.WriteTo.Debug(formatter: formatter);
#endif
		return configuration.CreateLogger();
	}
}
