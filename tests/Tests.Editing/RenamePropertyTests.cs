using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class RenamePropertyTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static string Apply(string json, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, json);
        return patch.ApplyTo(json);
    }

    [Fact]
    public void Rename_Simple()
    {
        // {"name":"Alice"}
        //  ^    ^  = positions 1..7 (includes quotes: "name" = 1..7)
        var json = "{\"name\":\"Alice\"}";
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = Apply(json, intent);
        Assert.Equal("{\"fullName\":\"Alice\"}", result);
    }

    [Fact]
    public void Rename_FirstOfMultiple()
    {
        var json = "{\"a\":1,\"b\":2}";
        // "a" is at 1..4
        var intent = new RenameProperty(AnyId, 1, 4, "a", "x");
        var result = Apply(json, intent);
        Assert.Equal("{\"x\":1,\"b\":2}", result);
    }

    [Fact]
    public void Rename_LastOfMultiple()
    {
        var json = "{\"a\":1,\"b\":2}";
        // "b" is at 7..10
        var intent = new RenameProperty(AnyId, 7, 10, "b", "y");
        var result = Apply(json, intent);
        Assert.Equal("{\"a\":1,\"y\":2}", result);
    }

    [Fact]
    public void Rename_KeyWithSpecialChars()
    {
        var json = "{\"name\":\"Alice\"}";
        var intent = new RenameProperty(AnyId, 1, 7, "name", "he said \"hi\"");
        var result = Apply(json, intent);
        Assert.Equal("{\"he said \\\"hi\\\"\":\"Alice\"}", result);
    }

    [Fact]
    public void Rename_RoundTrip_NoParseErrors()
    {
        var json = "{\"name\":\"Alice\"}";
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = Apply(json, intent);
        var doc = new RawDocument(result, 1, null);
        var parseResult = JsonParser.Parse(doc);
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void Rename_OldTextMismatch_Throws()
    {
        var json = "{\"name\":\"Alice\"}";
        // Stale positions pointing to wrong content
        var intent = new RenameProperty(AnyId, 2, 8, "wrong", "x");
        var patch = PatchGenerator.Generate(intent, json);
        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(json));
    }
}
