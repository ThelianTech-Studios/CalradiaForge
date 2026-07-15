namespace CalradiaForge.Core.Infra.GamePlatform.Steam;

using System.Text;

/// <summary>
/// Minimal KeyValues1 text parser used for local Steam VDF and ACF metadata.
/// </summary>
internal static class ValveKeyValuesParser {
	internal sealed record Node(string Name, string? Value, IReadOnlyList<Node> Children) {
		public Node? Child(string name) {
			return Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
		}
	}

	public static bool TryParse(string content, out Node root, out string error) {
		try {
			Parser parser = new(content);
			root = new Node(string.Empty, null, parser.ParseEntries(expectClosingBrace: false));
			error = string.Empty;
			return true;
		} catch (FormatException ex) {
			root = new Node(string.Empty, null, []);
			error = ex.Message;
			return false;
		}
	}

	private sealed class Parser {
		private readonly Tokenizer _tokenizer;

		public Parser(string content) {
			_tokenizer = new Tokenizer(content ?? string.Empty);
		}

		public IReadOnlyList<Node> ParseEntries(bool expectClosingBrace) {
			List<Node> nodes = [];
			while (true) {
				Token token = _tokenizer.Read();
				if (token.Kind == TokenKind.End) {
					if (expectClosingBrace) {
						throw new FormatException("KeyValues object is missing a closing brace.");
					}
					return nodes;
				}
				if (token.Kind == TokenKind.CloseBrace) {
					if (!expectClosingBrace) {
						throw new FormatException("KeyValues input contains an unexpected closing brace.");
					}
					return nodes;
				}
				if (token.Kind != TokenKind.Text) {
					throw new FormatException("KeyValues entry is missing a key.");
				}

				Token value = _tokenizer.Read();
				if (value.Kind == TokenKind.OpenBrace) {
					nodes.Add(new Node(token.Value, null, ParseEntries(expectClosingBrace: true)));
				} else if (value.Kind == TokenKind.Text) {
					nodes.Add(new Node(token.Value, value.Value, []));
				} else {
					throw new FormatException($"KeyValues key '{token.Value}' is missing a value or object.");
				}
			}
		}
	}

	private enum TokenKind {
		Text,
		OpenBrace,
		CloseBrace,
		End
	}

	private readonly record struct Token(TokenKind Kind, string Value = "");

	private sealed class Tokenizer {
		private readonly string _content;
		private int _position;

		public Tokenizer(string content) {
			_content = content;
		}

		public Token Read() {
			SkipTrivia();
			if (_position >= _content.Length) {
				return new Token(TokenKind.End);
			}

			char current = _content[_position++];
			if (current == '{') {
				return new Token(TokenKind.OpenBrace);
			}
			if (current == '}') {
				return new Token(TokenKind.CloseBrace);
			}
			if (current == '"') {
				return new Token(TokenKind.Text, ReadQuoted());
			}

			StringBuilder value = new();
			value.Append(current);
			while (_position < _content.Length) {
				current = _content[_position];
				if (char.IsWhiteSpace(current) || current is '{' or '}') {
					break;
				}
				value.Append(current);
				_position++;
			}
			return new Token(TokenKind.Text, value.ToString());
		}

		private string ReadQuoted() {
			StringBuilder value = new();
			while (_position < _content.Length) {
				char current = _content[_position++];
				if (current == '"') {
					return value.ToString();
				}
				if (current == '\\' && _position < _content.Length) {
					char escaped = _content[_position];
					if (escaped is '\\' or '"') {
						value.Append(escaped);
						_position++;
						continue;
					}
				}
				value.Append(current);
			}
			throw new FormatException("KeyValues quoted string is not terminated.");
		}

		private void SkipTrivia() {
			while (_position < _content.Length) {
				if (char.IsWhiteSpace(_content[_position]) || _content[_position] == '\uFEFF') {
					_position++;
					continue;
				}
				if (_content[_position] == '/'
					&& _position + 1 < _content.Length
					&& _content[_position + 1] == '/') {
					_position += 2;
					while (_position < _content.Length && _content[_position] is not '\r' and not '\n') {
						_position++;
					}
					continue;
				}
				break;
			}
		}
	}
}
