namespace Nexu.Parsing.Json;

public enum DiagnosticKind { DuplicateKey, SyntaxError }

public sealed record Diagnostic(DiagnosticKind Kind, int Line, int Column, string Message);
