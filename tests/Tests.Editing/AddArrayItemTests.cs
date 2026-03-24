using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class AddArrayItemTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static string Apply(string json, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, json);
        return patch.ApplyTo(json);
    }

    [Fact]
    public void AddArrayItem_ToEmptyArray_Compact()
    {
        // [] — array spans 0..2
        var json = "[]";
        var intent = new AddArrayItem(AnyId, 0, 2, -1, "  ", "1");
        var result = Apply(json, intent);
        Assert.Equal("[1]", result);
    }

    [Fact]
    public void AddArrayItem_ToNonEmpty_Compact()
    {
        // [1,2] — last element ends at 4, array ends at 5
        var json = "[1,2]";
        var intent = new AddArrayItem(AnyId, 0, 5, 4, "  ", "3");
        var result = Apply(json, intent);
        Assert.Equal("[1,2, 3]", result);
    }

    [Fact]
    public void AddArrayItem_ToNonEmpty_Multiline()
    {
        var json = "[\n  1,\n  2\n]";
        // last element '2' is at index 9, End=10, array ends at 12
        var intent = new AddArrayItem(AnyId, 0, 12, 10, "  ", "3");
        var result = Apply(json, intent);
        Assert.Equal("[\n  1,\n  2,\n  3\n]", result);
    }

    [Fact]
    public void AddArrayItem_StringValue()
    {
        // ["a"] — "a" ends at 4, array ends at 5
        var json = "[\"a\"]";
        var intent = new AddArrayItem(AnyId, 0, 5, 4, "  ", "\"b\"");
        var result = Apply(json, intent);
        Assert.Equal("[\"a\", \"b\"]", result);
    }

    [Fact]
    public void AddArrayItem_RoundTrip_NoParseErrors()
    {
        var json = "[1,2]";
        var intent = new AddArrayItem(AnyId, 0, 5, 4, "  ", "3");
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void AddArrayItem_RoundTrip_LastElementCorrect()
    {
        var json = "[1,2]";
        var intent = new AddArrayItem(AnyId, 0, 5, 4, "  ", "99");
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        var arr = Assert.IsType<CstArray>(parseResult.Root);
        var last = Assert.IsType<CstValue>(arr.Elements[^1]);
        Assert.Equal("99", last.RawText);
    }
}
