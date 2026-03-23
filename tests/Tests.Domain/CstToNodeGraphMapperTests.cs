using Nexu.Domain;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Domain;

public sealed class CstToNodeGraphMapperTests
{
    private static NodeGraph Map(string json)
    {
        var result = JsonParser.Parse(new RawDocument(json, 0, null));
        return CstToNodeGraphMapper.Map(result.Root);
    }

    [Fact]
    public void Map_EmptyObject_ReturnsSingleObjectNode()
    {
        var graph = Map("{}");

        Assert.Single(graph.Nodes);
        Assert.Equal(NodeType.Object, graph.Root.Type);
        Assert.Empty(graph.Root.ChildIds);
        Assert.Null(graph.Root.ParentId);
    }

    [Fact]
    public void Map_SimpleProperty_ReturnsPropertyWithScalarChild()
    {
        var graph = Map("{\"name\":\"Alice\"}");

        // root object + 1 property + 1 scalar = 3 nodes
        Assert.Equal(3, graph.Nodes.Count);

        var root = graph.Root;
        Assert.Equal(NodeType.Object, root.Type);
        Assert.Single(root.ChildIds);

        var prop = graph.Nodes[root.ChildIds[0]];
        Assert.Equal(NodeType.Property, prop.Type);
        Assert.Equal("name", prop.Label);
        Assert.Equal(root.Id, prop.ParentId);
        Assert.Single(prop.ChildIds);

        var scalar = graph.Nodes[prop.ChildIds[0]];
        Assert.Equal(NodeType.Scalar, scalar.Type);
        Assert.Equal("Alice", scalar.Label);
        Assert.Equal(prop.Id, scalar.ParentId);
    }

    [Fact]
    public void Map_NestedObject_BuildsCorrectHierarchy()
    {
        var graph = Map("{\"a\":{\"b\":1}}");

        var root = graph.Root;
        Assert.Equal(NodeType.Object, root.Type);

        var propA = graph.Nodes[root.ChildIds[0]];
        Assert.Equal("a", propA.Label);
        Assert.Equal(NodeType.Property, propA.Type);

        var innerObj = graph.Nodes[propA.ChildIds[0]];
        Assert.Equal(NodeType.Object, innerObj.Type);
        Assert.Equal(propA.Id, innerObj.ParentId);

        var propB = graph.Nodes[innerObj.ChildIds[0]];
        Assert.Equal("b", propB.Label);
        Assert.Equal(NodeType.Property, propB.Type);
    }

    [Fact]
    public void Map_Array_BuildsArrayNodeWithElementChildren()
    {
        var graph = Map("[1,2,3]");

        var root = graph.Root;
        Assert.Equal(NodeType.Array, root.Type);
        Assert.Equal(3, root.ChildIds.Length);

        foreach (var childId in root.ChildIds)
        {
            var child = graph.Nodes[childId];
            Assert.Equal(NodeType.Scalar, child.Type);
            Assert.Equal(root.Id, child.ParentId);
        }
    }

    [Fact]
    public void Map_AllParentReferences_AreCorrect()
    {
        var graph = Map("{\"a\":1,\"b\":true}");

        var root = graph.Root;
        foreach (var childId in root.ChildIds)
        {
            var prop = graph.Nodes[childId];
            Assert.Equal(root.Id, prop.ParentId);

            foreach (var grandchildId in prop.ChildIds)
            {
                var val = graph.Nodes[grandchildId];
                Assert.Equal(prop.Id, val.ParentId);
            }
        }
    }

    [Fact]
    public void Map_ScalarRoot_ReturnsSingleNode()
    {
        var graph = Map("42");

        Assert.Single(graph.Nodes);
        Assert.Equal(NodeType.Scalar, graph.Root.Type);
        Assert.Equal("42", graph.Root.Label);
    }
}
