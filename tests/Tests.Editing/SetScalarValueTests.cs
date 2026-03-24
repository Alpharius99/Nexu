using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class SetScalarValueTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static string Apply(string json, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, json);
        return patch.ApplyTo(json);
    }

    [Fact]
    public void SetScalar_StringToString()
    {
        // {"v":"old"}  — "old" at positions 5..10
        var json = "{\"v\":\"old\"}";
        var intent = new SetScalarValue(AnyId, 5, 10, "\"old\"", "\"new\"");
        Assert.Equal("{\"v\":\"new\"}", Apply(json, intent));
    }

    [Fact]
    public void SetScalar_NumberToNumber()
    {
        // {"n":42}  — 42 at positions 5..7
        var json = "{\"n\":42}";
        var intent = new SetScalarValue(AnyId, 5, 7, "42", "100");
        Assert.Equal("{\"n\":100}", Apply(json, intent));
    }

    [Fact]
    public void SetScalar_StringToNumber()
    {
        var json = "{\"v\":\"old\"}";
        var intent = new SetScalarValue(AnyId, 5, 10, "\"old\"", "123");
        Assert.Equal("{\"v\":123}", Apply(json, intent));
    }

    [Fact]
    public void SetScalar_BoolToNull()
    {
        // {"f":true}  — true at 5..9
        var json = "{\"f\":true}";
        var intent = new SetScalarValue(AnyId, 5, 9, "true", "null");
        Assert.Equal("{\"f\":null}", Apply(json, intent));
    }

    [Fact]
    public void SetScalar_RoundTrip_NoParseErrors()
    {
        var json = "{\"n\":42}";
        var intent = new SetScalarValue(AnyId, 5, 7, "42", "100");
        var result = Apply(json, intent);
        var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
        Assert.False(parseResult.HasErrors);
    }

    [Fact]
    public void SetScalar_OldTextMismatch_Throws()
    {
        var json = "{\"n\":42}";
        var intent = new SetScalarValue(AnyId, 5, 7, "99", "100");
        var patch = PatchGenerator.Generate(intent, json);
        Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(json));
    }
}
