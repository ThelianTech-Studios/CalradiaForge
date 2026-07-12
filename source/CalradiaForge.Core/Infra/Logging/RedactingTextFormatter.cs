namespace CalradiaForge.Core.Infra.Logging;

using System.Text;

using Serilog.Events;
using Serilog.Formatting;

/// <summary>Formats Serilog events after redacting messages, exceptions, and structured properties.</summary>
internal sealed class RedactingTextFormatter : ITextFormatter {
	public void Format(LogEvent logEvent, TextWriter output) {
		var builder = new StringBuilder()
			.Append('[').Append(logEvent.Timestamp.ToString("MM/dd/yy HH:mm:ss.fff")).Append("] ")
			.Append(logEvent.Level).Append(' ')
			.Append(LogRedactor.Redact(logEvent.RenderMessage()));

		if (logEvent.Exception is not null) {
			builder.AppendLine().Append(LogRedactor.Redact(logEvent.Exception.ToString()));
		}
		foreach (KeyValuePair<string, LogEventPropertyValue> property in logEvent.Properties) {
			builder.Append(' ').Append(LogRedactor.RenderProperty(property.Key, property.Value));
		}
		output.WriteLine(builder.ToString());
	}
}
