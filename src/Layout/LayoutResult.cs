using System.Collections.Immutable;

namespace Nexu.Layout;

public sealed record LayoutResult(
    ImmutableArray<PositionedNode> Nodes,
    ImmutableArray<Edge> Edges);
