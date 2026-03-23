using Nexu.Domain;
using Nexu.Layout;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Layout;

public sealed class LayoutEngineTests
{
    private static LayoutResult Compute(string json)
    {
        var parseResult = JsonParser.Parse(new RawDocument(json, 0, null));
        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        return LayoutEngine.Compute(graph);
    }

    [Fact]
    public void Compute_SingleNode_PlacedAtOriginX()
    {
        var result = Compute("42");

        Assert.Single(result.Nodes);
        Assert.Equal(0.0, result.Nodes[0].X);
        Assert.Empty(result.Edges);
    }

    [Fact]
    public void Compute_ChildNode_PlacedRightOfParent()
    {
        var result = Compute("{\"key\":\"value\"}");

        // object -> property -> scalar
        // depth 0, 1, 2
        var xs = result.Nodes.Select(n => n.X).Distinct().OrderBy(x => x).ToList();
        Assert.Equal(3, xs.Count);
        Assert.Equal(0.0, xs[0]);
        Assert.True(xs[1] > xs[0]);
        Assert.True(xs[2] > xs[1]);
    }

    [Fact]
    public void Compute_MultipleLeaves_StackedVertically()
    {
        var result = Compute("{\"a\":1,\"b\":2,\"c\":3}");

        // 3 scalar leaf nodes (at depth 2) should have distinct Y positions
        var leafNodes = result.Nodes
            .Where(n => Math.Abs(n.X - 2.0 * (LayoutEngine.NodeWidth + LayoutEngine.HorizontalGap)) < 0.001)
            .OrderBy(n => n.Y)
            .ToList();

        Assert.Equal(3, leafNodes.Count);
        // each leaf is offset by NodeHeight + VerticalGap
        for (int i = 1; i < leafNodes.Count; i++)
            Assert.True(leafNodes[i].Y > leafNodes[i - 1].Y);
    }

    [Fact]
    public void Compute_GeneratesEdges_OnePerParentChildPair()
    {
        // object -> 2 properties -> 2 scalars = 4 edges total
        var result = Compute("{\"a\":1,\"b\":2}");

        // nodes: root obj + 2 props + 2 scalars = 5
        // edges: obj->propA, obj->propB, propA->scalar1, propB->scalar2 = 4
        Assert.Equal(5, result.Nodes.Length);
        Assert.Equal(4, result.Edges.Length);
    }

    [Fact]
    public void Compute_IsDeterministic()
    {
        var json = "{\"x\":1,\"y\":2,\"z\":[1,2,3]}";
        var result1 = Compute(json);
        var result2 = Compute(json);

        // Same number of nodes and edges
        Assert.Equal(result1.Nodes.Length, result2.Nodes.Length);
        Assert.Equal(result1.Edges.Length, result2.Edges.Length);
    }

    [Fact]
    public void Compute_ParentCenteredOnChildren()
    {
        // With 2 children, parent Y should be midpoint between them
        var result = Compute("{\"a\":1,\"b\":2}");

        // find the root (X=0)
        var root = result.Nodes.Single(n => n.X == 0.0);
        // find depth-1 nodes (properties)
        double depth1X = LayoutEngine.NodeWidth + LayoutEngine.HorizontalGap;
        var props = result.Nodes.Where(n => Math.Abs(n.X - depth1X) < 0.001).OrderBy(n => n.Y).ToList();
        Assert.Equal(2, props.Count);

        double expectedCenter = (props[0].Y + LayoutEngine.NodeHeight / 2.0 +
                                  props[1].Y + LayoutEngine.NodeHeight / 2.0) / 2.0;
        double rootCenter = root.Y + LayoutEngine.NodeHeight / 2.0;
        Assert.Equal(expectedCenter, rootCenter, 3);
    }

    [Fact]
    public void Compute_AllNodesHaveCorrectDimensions()
    {
        var result = Compute("{\"name\":\"test\"}");

        foreach (var node in result.Nodes)
        {
            Assert.Equal(LayoutEngine.NodeWidth, node.Width);
            Assert.Equal(LayoutEngine.NodeHeight, node.Height);
        }
    }
}
