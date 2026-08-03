namespace CalradiaForge.Tests.Core.Logging;

using CalradiaForge.Core.Infra.Logging;

using Serilog.Events;
using Serilog.Parsing;

public sealed class SerilogTextFormatterTests {
	[Fact]
	public void Format_RendersMessageContextThreadPropertiesAndException() {
		SerilogTextFormatter formatter = new();
		InvalidOperationException exception = new("formatter failure");
		LogEvent logEvent = new(
			new DateTimeOffset(2026, 7, 15, 12, 34, 56, 789, TimeSpan.Zero),
			LogEventLevel.Warning,
			exception,
			new MessageTemplateParser().Parse("Processed {Count} modules"),
			[
				new LogEventProperty("Count", new ScalarValue(3)),
				new LogEventProperty("SourceContext", new ScalarValue("CalradiaForge.Tests")),
				new LogEventProperty("ThreadId", new ScalarValue(7)),
				new LogEventProperty("Path", new ScalarValue("X:\\Fake\\Modules"))
			]);
		using StringWriter output = new();

		formatter.Format(logEvent, output);
		string text = output.ToString();

		Assert.Contains("Warning Processed 3 modules", text);
		Assert.Contains("SourceContext=\"CalradiaForge.Tests\"", text);
		Assert.Contains("ThreadId=7", text);
		Assert.Contains("Path=\"X:\\Fake\\Modules\"", text);
		Assert.Contains("formatter failure", text);
	}
}
