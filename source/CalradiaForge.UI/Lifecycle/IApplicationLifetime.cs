namespace CalradiaForge.UI.Lifecycle;

/// <summary>
/// Identifies why the current application process is shutting down.
/// </summary>
public enum ShutdownReason {
	UserRequest,
	FatalStartup,
	FatalDispatcher,
	OperatingSystem
}

/// <summary>
/// Identifies why the current application process is being replaced.
/// </summary>
public enum RestartReason {
	DebugModeChanged,
	UserRequest
}

/// <summary>
/// Narrow application-host boundary exposed to UI consumers.
/// </summary>
public interface IApplicationLifetime {
	Task RequestShutdownAsync(ShutdownReason reason);
	Task RequestRestartAsync(RestartReason reason);
}
