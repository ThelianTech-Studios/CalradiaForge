namespace CalradiaForge.Core.Infra.Launch;

using System.Diagnostics;

using Serilog;

/// <summary>Inspects the local process table for the Steam client.</summary>
internal sealed class SteamProcessInspector : ISteamProcessInspector {
	private const string _steamProcessName = "steam";
	private readonly Func<string, Process[]> _getProcessesByName;

	public SteamProcessInspector()
		: this(Process.GetProcessesByName) {
	}

	internal SteamProcessInspector(Func<string, Process[]> getProcessesByName) {
		_getProcessesByName = getProcessesByName
			?? throw new ArgumentNullException(nameof(getProcessesByName));
	}

	public SteamProcessStatus Inspect() {
		Process[] processes;
		try {
			processes = _getProcessesByName(_steamProcessName);
		} catch (Exception ex) {
			Log.Warning(ex, "Steam process inspection failed; the Steam state is unknown.");
			return SteamProcessStatus.Unknown;
		}

		Exception? disposalFailure = null;
		foreach (Process process in processes) {
			try {
				process.Dispose();
			} catch (Exception ex) {
				disposalFailure ??= ex;
			}
		}

		if (disposalFailure is not null) {
			Log.Warning(
				disposalFailure,
				"Steam process inspection could not release every process handle; the Steam state is unknown.");
			return SteamProcessStatus.Unknown;
		}

		SteamProcessStatus status = processes.Length > 0
			? SteamProcessStatus.Running
			: SteamProcessStatus.NotRunning;
		Log.Debug(
			"Steam process inspection completed. Status={SteamProcessStatus} Count={ProcessCount}",
			status,
			processes.Length);
		return status;
	}
}
