using System.Collections.Immutable;

using Nexu.Parsing.Json;

namespace Nexu.Domain;

public static class CstToNodeGraphMapper
{
    public static NodeGraph Map(CstNode root)
    {
        var nodes = ImmutableDictionary.CreateBuilder<NodeId, Node>();
        var rootId = MapNode(root, null, nodes);
        return new NodeGraph(rootId, nodes.ToImmutable());
    }

    private static NodeId MapNode(
        CstNode cst,
        NodeId? parentId,
        ImmutableDictionary<NodeId, Node>.Builder nodes)
    {
        var id = NodeId.New();

        switch (cst)
        {
            case CstObject obj:
            {
                var childIds = ImmutableArray.CreateBuilder<NodeId>(obj.Properties.Length);
                // Register placeholder first so children can reference parent
                nodes[id] = new Node(id, NodeType.Object, null, parentId, ImmutableArray<NodeId>.Empty);
                foreach (var prop in obj.Properties)
                {
                    var childId = MapNode(prop, id, nodes);
                    childIds.Add(childId);
                }
                nodes[id] = new Node(id, NodeType.Object, null, parentId, childIds.ToImmutable());
                break;
            }
            case CstProperty prop:
            {
                nodes[id] = new Node(id, NodeType.Property, prop.Key, parentId, ImmutableArray<NodeId>.Empty);
                var valueId = MapNode(prop.Value, id, nodes);
                nodes[id] = new Node(id, NodeType.Property, prop.Key, parentId, ImmutableArray.Create(valueId));
                break;
            }
            case CstArray arr:
            {
                var childIds = ImmutableArray.CreateBuilder<NodeId>(arr.Elements.Length);
                nodes[id] = new Node(id, NodeType.Array, null, parentId, ImmutableArray<NodeId>.Empty);
                for (int i = 0; i < arr.Elements.Length; i++)
                {
                    var childId = MapNode(arr.Elements[i], id, nodes);
                    childIds.Add(childId);
                }
                nodes[id] = new Node(id, NodeType.Array, null, parentId, childIds.ToImmutable());
                break;
            }
            case CstValue val:
            {
                nodes[id] = new Node(id, NodeType.Scalar, val.RawText, parentId, ImmutableArray<NodeId>.Empty);
                break;
            }
            default:
            {
                // CstError or unknown
                nodes[id] = new Node(id, NodeType.Scalar, "?", parentId, ImmutableArray<NodeId>.Empty);
                break;
            }
        }

        return id;
    }
}
