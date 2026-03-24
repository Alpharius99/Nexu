using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace Nexu.Parsing.Json;

public static class JsonParser
{
    public static ParseResult Parse(RawDocument document)
    {
        var impl = new ParserImpl(document.Text);
        var root = impl.ParseValue();
        return new ParseResult(root, impl.Diagnostics.ToImmutableArray());
    }

    private sealed class ParserImpl
    {
        private readonly string _text;
        private int _pos;
        private readonly List<Diagnostic> _diagnostics = new();

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        public ParserImpl(string text) => _text = text;

        public CstNode ParseValue()
        {
            SkipWhitespace();
            if (_pos >= _text.Length)
                return MakeError(_pos, "Unexpected end of input");

            return _text[_pos] switch
            {
                '{' => ParseObject(),
                '[' => ParseArray(),
                '"' => ParseString(),
                't' => ParseLiteral("true", CstValueKind.True),
                'f' => ParseLiteral("false", CstValueKind.False),
                'n' => ParseLiteral("null", CstValueKind.Null),
                '-' => ParseNumber(),
                var c when c >= '0' && c <= '9' => ParseNumber(),
                var c => MakeError(_pos, $"Unexpected character '{c}'")
            };
        }

        private CstObject ParseObject()
        {
            int start = _pos;
            _pos++; // consume '{'
            SkipWhitespace();

            var properties = ImmutableArray.CreateBuilder<CstProperty>();
            var seenKeys = new HashSet<string>(StringComparer.Ordinal);

            if (_pos < _text.Length && _text[_pos] == '}')
            {
                _pos++;
                return new CstObject(start, _pos, properties.ToImmutable());
            }

            while (_pos < _text.Length)
            {
                SkipWhitespace();
                if (_pos >= _text.Length) break;

                if (_text[_pos] != '"')
                {
                    AddError(DiagnosticKind.SyntaxError, _pos, "Expected property key (string)");
                    break;
                }

                var prop = ParseProperty();
                if (!seenKeys.Add(prop.Key))
                {
                    var (line, col) = GetLineCol(prop.KeyStart);
                    _diagnostics.Add(new Diagnostic(DiagnosticKind.DuplicateKey, line, col,
                        $"Duplicate key \"{prop.Key}\""));
                }
                properties.Add(prop);

                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == ',')
                {
                    _pos++;
                    SkipWhitespace();
                    // allow trailing comma gracefully
                    if (_pos < _text.Length && _text[_pos] == '}')
                        break;
                    continue;
                }
                break;
            }

            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == '}')
                _pos++;
            else
                AddError(DiagnosticKind.SyntaxError, _pos, "Expected '}'");

            return new CstObject(start, _pos, properties.ToImmutable());
        }

        private CstProperty ParseProperty()
        {
            int start = _pos;
            int keyStart = _pos;
            var keyValue = ParseStringRaw();
            int keyEnd = _pos;
            string key = keyValue.RawText;

            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == ':')
                _pos++;
            else
                AddError(DiagnosticKind.SyntaxError, _pos, "Expected ':'");

            SkipWhitespace();
            var value = ParseValue();

            return new CstProperty(start, _pos, keyStart, keyEnd, key, value);
        }

        private CstArray ParseArray()
        {
            int start = _pos;
            _pos++; // consume '['
            SkipWhitespace();

            var elements = ImmutableArray.CreateBuilder<CstNode>();

            if (_pos < _text.Length && _text[_pos] == ']')
            {
                _pos++;
                return new CstArray(start, _pos, elements.ToImmutable());
            }

            while (_pos < _text.Length)
            {
                SkipWhitespace();
                if (_pos >= _text.Length) break;

                var element = ParseValue();
                elements.Add(element);

                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == ',')
                {
                    _pos++;
                    SkipWhitespace();
                    if (_pos < _text.Length && _text[_pos] == ']')
                        break;
                    continue;
                }
                break;
            }

            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == ']')
                _pos++;
            else
                AddError(DiagnosticKind.SyntaxError, _pos, "Expected ']'");

            return new CstArray(start, _pos, elements.ToImmutable());
        }

        private CstValue ParseString()
        {
            int start = _pos;
            string value = ParseStringRaw().RawText;
            return new CstValue(start, _pos, CstValueKind.String, value);
        }

        // Returns a CstValue whose RawText is the unescaped string content (no quotes)
        private CstValue ParseStringRaw()
        {
            int start = _pos;
            if (_pos >= _text.Length || _text[_pos] != '"')
            {
                AddError(DiagnosticKind.SyntaxError, _pos, "Expected '\"'");
                return new CstValue(start, _pos, CstValueKind.String, "");
            }
            _pos++; // consume opening '"'

            var sb = new StringBuilder();
            while (_pos < _text.Length && _text[_pos] != '"')
            {
                if (_text[_pos] == '\\')
                {
                    _pos++;
                    if (_pos >= _text.Length) break;
                    switch (_text[_pos])
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u' when _pos + 4 < _text.Length:
                            var hex = _text.Substring(_pos + 1, 4);
                            if (int.TryParse(hex, NumberStyles.HexNumber, null, out int cp))
                                sb.Append((char)cp);
                            _pos += 4;
                            break;
                        default:
                            sb.Append(_text[_pos]);
                            break;
                    }
                    _pos++;
                }
                else
                {
                    sb.Append(_text[_pos++]);
                }
            }

            if (_pos < _text.Length && _text[_pos] == '"')
                _pos++;
            else
                AddError(DiagnosticKind.SyntaxError, start, "Unterminated string");

            return new CstValue(start, _pos, CstValueKind.String, sb.ToString());
        }

        private CstValue ParseNumber()
        {
            int start = _pos;
            if (_pos < _text.Length && _text[_pos] == '-') _pos++;
            while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
            if (_pos < _text.Length && _text[_pos] == '.')
            {
                _pos++;
                while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
            }
            if (_pos < _text.Length && (_text[_pos] == 'e' || _text[_pos] == 'E'))
            {
                _pos++;
                if (_pos < _text.Length && (_text[_pos] == '+' || _text[_pos] == '-')) _pos++;
                while (_pos < _text.Length && _text[_pos] >= '0' && _text[_pos] <= '9') _pos++;
            }
            return new CstValue(start, _pos, CstValueKind.Number, _text.Substring(start, _pos - start));
        }

        private CstValue ParseLiteral(string literal, CstValueKind kind)
        {
            int start = _pos;
            if (_pos + literal.Length <= _text.Length &&
                _text.Substring(_pos, literal.Length) == literal)
            {
                _pos += literal.Length;
                return new CstValue(start, _pos, kind, literal);
            }
            AddError(DiagnosticKind.SyntaxError, _pos, $"Expected '{literal}'");
            return new CstValue(start, _pos, kind, literal);
        }

        private CstError MakeError(int pos, string message)
        {
            AddError(DiagnosticKind.SyntaxError, pos, message);
            return new CstError(pos, pos, message);
        }

        private void AddError(DiagnosticKind kind, int pos, string message)
        {
            var (line, col) = GetLineCol(pos);
            _diagnostics.Add(new Diagnostic(kind, line, col, message));
        }

        private (int Line, int Col) GetLineCol(int pos)
        {
            int line = 1, col = 1;
            for (int i = 0; i < pos && i < _text.Length; i++)
            {
                if (_text[i] == '\n') { line++; col = 1; }
                else col++;
            }
            return (line, col);
        }

        private void SkipWhitespace()
        {
            while (_pos < _text.Length && _text[_pos] is ' ' or '\t' or '\r' or '\n')
                _pos++;
        }
    }
}
