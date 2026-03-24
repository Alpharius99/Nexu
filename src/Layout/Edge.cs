using System.Collections.Immutable;

using Nexu.Domain;

namespace Nexu.Layout;

public sealed record Edge(NodeId From, NodeId To, ImmutableArray<LayoutPoint> Waypoints);
