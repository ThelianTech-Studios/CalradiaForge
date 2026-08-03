namespace CalradiaForge.Core.Infra.Launch;

using System.Diagnostics;

/// <summary>Starts the Steam protocol or configured game executable for a launch request.</summary>
public interface ILaunchProcessStarter {
	void Start(ProcessStartInfo startInfo);
}

internal sealed class LaunchProcessStarter : ILaunchProcessStarter {
	public void Start(ProcessStartInfo startInfo) {
		Process.Start(startInfo);
	}
}
