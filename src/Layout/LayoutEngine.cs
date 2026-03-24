using System.Collections.Immutable;

using Nexu.Domain;

namespace Nexu.Layout;

public static class LayoutEngine
{
    public const double NodeWidth = 120.0;
    public const double NodeHeight = 40.0;
    public const double HorizontalGap = 60.0;
    public const double VerticalGap = 20.0;

    public static LayoutResult Compute(NodeGraph graph)
    {
        var positioned = new Dictionary<NodeId, PositionedNode>();
        var edges = ImmutableArray.CreateBuilder<Edge>();

        double yOffset = 0.0;
        PositionSubtree(graph.RootId, 0, graph, positioned, edges, ref yOffset);

        return new LayoutResult(positioned.Values.ToImmutableArray(), edges.ToImmutable());
    }

    private static double PositionSubtree(
        NodeId nodeId,
        int depth,
        NodeGraph graph,
        Dictionary<NodeId, PositionedNode> positioned,
        ImmutableArray<Edge>.Builder edges,
        ref double yOffset)
    {
        var node = graph.Nodes[nodeId];
        double x = depth * (NodeWidth + HorizontalGap);

        double centerY;
        if (node.ChildIds.IsEmpty)
        {
            centerY = yOffset + NodeHeight / 2.0;
            yOffset += NodeHeight + VerticalGap;
        }
        else
        {
            double firstCenter = double.MaxValue;
            double lastCenter = double.MinValue;

            foreach (var childId in node.ChildIds)
            {
                double childCenter = PositionSubtree(childId, depth + 1, graph, positioned, edges, ref yOffset);
                if (childCenter < firstCenter) firstCenter = childCenter;
                if (childCenter > lastCenter) lastCenter = childCenter;

                edges.Add(new Edge(nodeId, childId, ImmutableArray<LayoutPoint>.Empty));
            }

            centerY = (firstCenter + lastCenter) / 2.0;
        }

        double y = centerY - NodeHeight / 2.0;
        positioned[nodeId] = new PositionedNode(nodeId, x, y, NodeWidth, NodeHeight, node.Label);
        return centerY;
    }
}
