using System.Collections.Immutable;

namespace Nexu.Parsing.Json;

public abstract record CstNode(int Start, int End);

public sealed record CstObject(int Start, int End, ImmutableArray<CstProperty> Properties)
    : CstNode(Start, End);

public sealed record CstProperty(int Start, int End, int KeyStart, int KeyEnd, string Key, CstNode Value)
    : CstNode(Start, End);

public sealed record CstArray(int Start, int End, ImmutableArray<CstNode> Elements)
    : CstNode(Start, End);

public enum CstValueKind { String, Number, True, False, Null }

public sealed record CstValue(int Start, int End, CstValueKind Kind, string RawText)
    : CstNode(Start, End);

public sealed record CstError(int Start, int End, string Message)
    : CstNode(Start, End);
