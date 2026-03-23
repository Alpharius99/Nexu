using System.Collections.Immutable;

namespace Nexu.Domain;

public sealed record NodeGraph(
    NodeId RootId,
    ImmutableDictionary<NodeId, Node> Nodes)
{
    public Node Root => Nodes[RootId];
}
