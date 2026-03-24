using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class RemoveNodeTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static string Apply(string json, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, json);
        return patch.ApplyTo(json);
    }

    [Fact]
    public void RemoveNode_OnlyProperty()
    {
        // {"a":1}
        // "a":1 spans 1..6
        var json = "{\"a\":1}";
        var intent = new RemoveNode(AnyId, 1, 6, -1, -1, 1);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.Empty(obj.Properties);
    }

    [Fact]
    public void RemoveNode_FirstProperty()
    {
        // {"a":1,"b":2}
        // "a":1 spans 1..6, "b":2 starts at 7
        var json = "{\"a\":1,\"b\":2}";
        var intent = new RemoveNode(AnyId, 1, 6, -1, 7, 2);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.Single(obj.Properties);
        Assert.Equal("b", obj.Properties[0].Key);
    }

    [Fact]
    public void RemoveNode_LastProperty()
    {
        // {"a":1,"b":2}
        // "b":2 spans 7..12
        var json = "{\"a\":1,\"b\":2}";
        var intent = new RemoveNode(AnyId, 7, 12, 6, -1, 2);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.Single(obj.Properties);
        Assert.Equal("a", obj.Properties[0].Key);
    }

    [Fact]
    public void RemoveNode_MiddleProperty()
    {
        // {"a":1,"b":2,"c":3}
        // "b":2 spans 7..12, preceded by comma at 6, followed by "c" at 13
        var json = "{\"a\":1,\"b\":2,\"c\":3}";
        var intent = new RemoveNode(AnyId, 7, 12, 6, 13, 3);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.Equal(2, obj.Properties.Length);
        Assert.Equal("a", obj.Properties[0].Key);
        Assert.Equal("c", obj.Properties[1].Key);
    }

    [Fact]
    public void RemoveNode_OnlyArrayElement()
    {
        // [1] — element '1' spans 1..2
        var json = "[1]";
        var intent = new RemoveNode(AnyId, 1, 2, -1, -1, 1);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var arr = Assert.IsType<CstArray>(parseResult.Root);
        Assert.Empty(arr.Elements);
    }

    [Fact]
    public void RemoveNode_FirstArrayElement()
    {
        // [1,2,3] — '1' spans 1..2, next sibling starts at 3
        var json = "[1,2,3]";
        var intent = new RemoveNode(AnyId, 1, 2, -1, 3, 3);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var arr = Assert.IsType<CstArray>(parseResult.Root);
        Assert.Equal(2, arr.Elements.Length);
    }

    [Fact]
    public void RemoveNode_LastArrayElement()
    {
        // [1,2,3] — '3' spans 5..6, prevSiblingEnd=4
        var json = "[1,2,3]";
        var intent = new RemoveNode(AnyId, 5, 6, 4, -1, 3);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var arr = Assert.IsType<CstArray>(parseResult.Root);
        Assert.Equal(2, arr.Elements.Length);
    }

    [Fact]
    public void RemoveNode_MiddleArrayElement()
    {
        // [1,2,3] — '2' spans 3..4, prevSiblingEnd=2, nextSiblingStart=5
        var json = "[1,2,3]";
        var intent = new RemoveNode(AnyId, 3, 4, 2, 5, 3);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
        var arr = Assert.IsType<CstArray>(parseResult.Root);
        Assert.Equal(2, arr.Elements.Length);
    }

    [Fact]
    public void RemoveNode_Multiline_NoBlankLine()
    {
        var json = "{\n  \"a\": 1,\n  \"b\": 2\n}";
        // "b": 2 — find positions by inspection:
        // 0: {, 1: \n, 2-3: "  ", 4: ", 5: a, 6: ", 7: :, 8: ' ', 9: 1, 10: ,, 11: \n
        // 12-13: "  ", 14: ", 15: b, 16: ", 17: :, 18: ' ', 19: 2, 20: \n, 21: }
        // "b": 2 property spans 14..20
        var intent = new RemoveNode(AnyId, 14, 20, 10, -1, 2);
        var result = Apply(json, intent);
        Assert.DoesNotContain("\n\n", result);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void RemoveNode_RoundTrip_NoParseErrors()
    {
        var json = "{\"a\":1,\"b\":2}";
        var intent = new RemoveNode(AnyId, 7, 12, 6, -1, 2);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void RemoveNode_RoundTrip_KeyAbsent()
    {
        var json = "{\"a\":1,\"b\":2}";
        var intent = new RemoveNode(AnyId, 7, 12, 6, -1, 2);
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.DoesNotContain(obj.Properties, p => p.Key == "b");
    }
}
