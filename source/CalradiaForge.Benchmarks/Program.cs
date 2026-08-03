namespace CalradiaForge.Benchmarks;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

internal static class Program {
	private static void Main(string[] args) {
		BenchmarkSwitcher
			.FromAssembly(typeof(Program).Assembly)
			.Run(args, DefaultConfig.Instance);
	}
}
