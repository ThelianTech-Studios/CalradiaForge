namespace CalradiaForge.Core.Infra.Launch;

/// <summary>Provides bounded launch delays without coupling launch policy to the system clock.</summary>
public interface ILaunchDelay {
	Task DelayAsync(TimeSpan delay);
}

internal sealed class LaunchDelay : ILaunchDelay {
	public Task DelayAsync(TimeSpan delay) => Task.Delay(delay);
}
