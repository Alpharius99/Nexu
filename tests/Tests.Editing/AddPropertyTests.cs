using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class AddPropertyTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static string Apply(string json, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, json);
        return patch.ApplyTo(json);
    }

    [Fact]
    public void AddProperty_ToEmptyObject_Compact()
    {
        // {} — object spans 0..2, ParentObjectEnd=2 (position of '}' is 1, so End=2)
        var json = "{}";
        var intent = new AddProperty(AnyId, 0, 2, -1, "  ", "x", "1");
        var result = Apply(json, intent);
        Assert.Equal("{\n  \"x\": 1\n}", result);
    }

    [Fact]
    public void AddProperty_ToEmptyObject_Multiline()
    {
        var json = "{\n}";
        var intent = new AddProperty(AnyId, 0, 3, -1, "  ", "x", "1");
        var result = Apply(json, intent);
        Assert.Equal("{\n  \"x\": 1\n}", result);
    }

    [Fact]
    public void AddProperty_ToNonEmpty_Compact()
    {
        // {"a":1} — last property end is after '1' at position 6, object end is 7
        var json = "{\"a\":1}";
        // "a":1 — property ends at 6 (after '1'), object end = 7
        var intent = new AddProperty(AnyId, 0, 7, 6, "  ", "x", "2");
        var result = Apply(json, intent);
        Assert.Equal("{\"a\":1,\n  \"x\": 2\n}", result);
    }

    [Fact]
    public void AddProperty_ToNonEmpty_Multiline()
    {
        var json = "{\n  \"a\": 1\n}";
        // "a": 1 ends at position 10, object ends at 12
        var intent = new AddProperty(AnyId, 0, 12, 10, "  ", "x", "2");
        var result = Apply(json, intent);
        Assert.Equal("{\n  \"a\": 1,\n  \"x\": 2\n}", result);
    }

    [Fact]
    public void AddProperty_KeyWithSpecialChars()
    {
        var json = "{}";
        var intent = new AddProperty(AnyId, 0, 2, -1, "  ", "a\\b\"c", "1");
        var result = Apply(json, intent);
        Assert.Equal("{\n  \"a\\\\b\\\"c\": 1\n}", result);
    }

    [Fact]
    public void AddProperty_RoundTrip_NoParseErrors()
    {
        var json = "{\"a\":1}";
        var intent = new AddProperty(AnyId, 0, 7, 6, "  ", "b", "2");
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void AddProperty_RoundTrip_NewKeyPresent()
    {
        var json = "{\"a\":1}";
        var intent = new AddProperty(AnyId, 0, 7, 6, "  ", "newKey", "99");
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        var obj = Assert.IsType<CstObject>(parseResult.Root);
        Assert.Contains(obj.Properties, p => p.Key == "newKey");
    }
}
