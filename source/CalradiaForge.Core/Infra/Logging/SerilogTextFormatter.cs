namespace CalradiaForge.Core.Infra.Logging;

using System.Text;

using Serilog.Events;
using Serilog.Formatting;

/// <summary>Formats Serilog events for the local text sinks without changing event values.</summary>
internal sealed class SerilogTextFormatter : ITextFormatter {
	public void Format(LogEvent logEvent, TextWriter output) {
		var builder = new StringBuilder()
			.Append('[').Append(logEvent.Timestamp.ToString("MM/dd/yy HH:mm:ss.fff")).Append("] ")
			.Append(logEvent.Level).Append(' ')
			.Append(logEvent.RenderMessage());

		AppendPropertyIfPresent(builder, logEvent, "SourceContext");
		AppendPropertyIfPresent(builder, logEvent, "ThreadId");

		foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties) {
			if (property.Key is "SourceContext" or "ThreadId") {
				continue;
			}
			builder.Append(' ').Append(property.Key).Append('=').Append(property.Value);
		}

		if (logEvent.Exception is not null) {
			builder.AppendLine().Append(logEvent.Exception);
		}

		output.WriteLine(builder.ToString());
	}

	private static void AppendPropertyIfPresent(StringBuilder builder, LogEvent logEvent, string propertyName) {
		if (logEvent.Properties.TryGetValue(propertyName, out LogEventPropertyValue? value)) {
			builder.Append(' ').Append(propertyName).Append('=').Append(value);
		}
	}
}
