using System.Collections.Immutable;

namespace Nexu.Parsing.Json;

public sealed record ParseResult(CstNode Root, ImmutableArray<Diagnostic> Diagnostics)
{
    public bool HasErrors => !Diagnostics.IsEmpty;
}
