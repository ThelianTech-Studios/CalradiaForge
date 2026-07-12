namespace CalradiaForge.Core.Infra.Logging;

using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using Serilog.Events;

/// <summary>
/// Applies the shared secret and user-data redaction rules before values reach a log sink.
/// </summary>
public static partial class LogRedactor {
	private static readonly string[] SensitiveKeyFragments = [
		"authorization", "bearer", "token", "apikey", "api-key", "password", "passwd",
		"secret", "credential", "clientsecret", "accesskey", "refreshkey"
	];

	/// <summary>Redacts secret-like values in a plain message, URL, or exception string.</summary>
	public static string Redact(string? value) {
		if (string.IsNullOrEmpty(value)) {
			return value ?? string.Empty;
		}

		string redacted = BearerTokenRegex().Replace(value, "$1[REDACTED]");
		redacted = SecretAssignmentRegex().Replace(redacted, "$1[REDACTED]");
		redacted = SecretUrlParameterRegex().Replace(redacted, "$1[REDACTED]");
		return redacted;
	}

	/// <summary>Redacts a structured Serilog property recursively.</summary>
	public static object? RedactValue(object? value, string? propertyName = null) {
		if (IsSensitiveKey(propertyName)) {
			return "[REDACTED]";
		}
		if (value is null) {
			return null;
		}
		if (value is string text) {
			return Redact(text);
		}
		if (value is Exception exception) {
			return Redact(exception.ToString());
		}
		if (value is IDictionary dictionary) {
			var result = new Dictionary<string, object?>();
			foreach (DictionaryEntry entry in dictionary) {
				string key = entry.Key?.ToString() ?? "Value";
				result[key] = RedactValue(entry.Value, key);
			}
			return result;
		}
		if (value is IEnumerable sequence && value is not byte[]) {
			return sequence.Cast<object?>().Select(item => RedactValue(item)).ToArray();
		}
		if (value.GetType().IsPrimitive || value is decimal || value is Guid || value is DateTime || value is DateTimeOffset) {
			return value;
		}
		if (value.GetType().IsClass) {
			var result = new Dictionary<string, object?>();
			foreach (PropertyInfo property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
				if (!property.CanRead || property.GetIndexParameters().Length != 0) {
					continue;
				}
				try {
					result[property.Name] = RedactValue(property.GetValue(value), property.Name);
				} catch {
					result[property.Name] = "[UNAVAILABLE]";
				}
			}
			return result;
		}
		return Redact(value.ToString());
	}

	/// <summary>
	/// Renders a structured Serilog property after recursively applying the shared redaction rules.
	/// </summary>
	internal static string RenderProperty(string propertyName, LogEventPropertyValue propertyValue) =>
		$"{propertyName}={RenderPropertyValue(propertyValue, propertyName)}";

	private static string RenderPropertyValue(LogEventPropertyValue propertyValue, string? propertyName = null) {
		if (IsSensitiveKey(propertyName)) {
			return "[REDACTED]";
		}

		return propertyValue switch {
			ScalarValue scalar => RenderScalarValue(scalar, propertyName),
			SequenceValue sequence => $"[{string.Join(", ", sequence.Elements.Select(element => RenderPropertyValue(element)))}]",
			StructureValue structure => RenderStructureValue(structure),
			DictionaryValue dictionary => RenderDictionaryValue(dictionary),
			_ => Redact(propertyValue.ToString())
		};
	}

	private static string RenderScalarValue(ScalarValue scalar, string? propertyName) {
		object? redacted = RedactValue(scalar.Value, propertyName);
		if (redacted is null) {
			return "null";
		}
		if (redacted is string text) {
			return $"\"{text.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
		}
		return Redact(scalar.ToString());
	}

	private static string RenderStructureValue(StructureValue structure) {
		string typeTag = string.IsNullOrWhiteSpace(structure.TypeTag) ? string.Empty : $"{structure.TypeTag} ";
		string properties = string.Join(", ", structure.Properties.Select(property =>
			$"{property.Name}={RenderPropertyValue(property.Value, property.Name)}"));
		return $"{typeTag}{{ {properties} }}";
	}

	private static string RenderDictionaryValue(DictionaryValue dictionary) {
		string entries = string.Join(", ", dictionary.Elements.Select(entry => {
			string key = entry.Key.Value?.ToString() ?? "Value";
			return $"{RenderPropertyValue(entry.Key)}={RenderPropertyValue(entry.Value, key)}";
		}));
		return $"{{ {entries} }}";
	}

	private static bool IsSensitiveKey(string? key) => !string.IsNullOrWhiteSpace(key)
		&& SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

	[GeneratedRegex(@"(Bearer\s+)[^\s,;]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex BearerTokenRegex();

	[GeneratedRegex(@"(\b(?:authorization|token|api[-_]?key|password|secret|credential)\s*[:=]\s*)[^\s,;]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex SecretAssignmentRegex();

	[GeneratedRegex(@"([?&](?:access_token|api_key|apikey|token|password|secret)=)[^&\s]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
	private static partial Regex SecretUrlParameterRegex();
}
