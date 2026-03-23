using System.Collections.Immutable;

namespace Nexu.Domain;

public sealed record Node(
    NodeId Id,
    NodeType Type,
    string? Label,
    NodeId? ParentId,
    ImmutableArray<NodeId> ChildIds);
