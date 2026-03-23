using System.Collections.Immutable;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Parsing.Json;

public sealed class JsonParserTests
{
    private static ParseResult Parse(string json) =>
        JsonParser.Parse(new RawDocument(json, 0, null));

    [Fact]
    public void EmptyObject_ParsesCorrectly()
    {
        var result = Parse("{}");

        var obj = Assert.IsType<CstObject>(result.Root);
        Assert.Empty(obj.Properties);
        Assert.Equal(0, obj.Start);
        Assert.Equal(2, obj.End);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SimpleProperty_ParsesCorrectly()
    {
        var result = Parse("{\"name\":\"Alice\"}");

        var obj = Assert.IsType<CstObject>(result.Root);
        Assert.Single(obj.Properties);
        var prop = obj.Properties[0];
        Assert.Equal("name", prop.Key);
        var val = Assert.IsType<CstValue>(prop.Value);
        Assert.Equal("Alice", val.RawText);
        Assert.Equal(CstValueKind.String, val.Kind);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NestedObject_ParsesCorrectly()
    {
        var result = Parse("{\"a\":{\"b\":1}}");

        Assert.Empty(result.Diagnostics);
        var outer = Assert.IsType<CstObject>(result.Root);
        Assert.Single(outer.Properties);
        var inner = Assert.IsType<CstObject>(outer.Properties[0].Value);
        Assert.Single(inner.Properties);
        Assert.Equal("b", inner.Properties[0].Key);
    }

    [Fact]
    public void EmptyArray_ParsesCorrectly()
    {
        var result = Parse("[]");

        var arr = Assert.IsType<CstArray>(result.Root);
        Assert.Empty(arr.Elements);
        Assert.Equal(0, arr.Start);
        Assert.Equal(2, arr.End);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void ArrayWithElements_ParsesCorrectly()
    {
        var result = Parse("[1,2,3]");

        Assert.Empty(result.Diagnostics);
        var arr = Assert.IsType<CstArray>(result.Root);
        Assert.Equal(3, arr.Elements.Length);
        var e0 = Assert.IsType<CstValue>(arr.Elements[0]);
        Assert.Equal(CstValueKind.Number, e0.Kind);
        Assert.Equal("1", e0.RawText);
    }

    [Fact]
    public void StringValue_ParsesCorrectly()
    {
        var result = Parse("\"hello\"");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.String, val.Kind);
        Assert.Equal("hello", val.RawText);
        Assert.Equal(0, val.Start);
        Assert.Equal(7, val.End);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NumberValue_ParsesCorrectly()
    {
        var result = Parse("42");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.Number, val.Kind);
        Assert.Equal("42", val.RawText);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void FloatNumber_ParsesCorrectly()
    {
        var result = Parse("3.14");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.Number, val.Kind);
        Assert.Equal("3.14", val.RawText);
    }

    [Fact]
    public void BooleanTrue_ParsesCorrectly()
    {
        var result = Parse("true");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.True, val.Kind);
        Assert.Equal("true", val.RawText);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void BooleanFalse_ParsesCorrectly()
    {
        var result = Parse("false");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.False, val.Kind);
        Assert.Equal("false", val.RawText);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void NullValue_ParsesCorrectly()
    {
        var result = Parse("null");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.Null, val.Kind);
        Assert.Equal("null", val.RawText);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void DuplicateKeys_ReturnsDiagnostic()
    {
        var result = Parse("{\"a\":1,\"a\":2}");

        Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticKind.DuplicateKey, result.Diagnostics[0].Kind);
        Assert.Contains("\"a\"", result.Diagnostics[0].Message);
    }

    [Fact]
    public void SyntaxError_MissingColon_ReturnsDiagnostic()
    {
        var result = Parse("{\"key\" 42}");

        Assert.NotEmpty(result.Diagnostics);
        Assert.Contains(result.Diagnostics, d => d.Kind == DiagnosticKind.SyntaxError);
    }

    [Fact]
    public void CharacterIndices_AreCorrect_ForSimpleProperty()
    {
        // {"x":1}
        // 01234567
        var result = Parse("{\"x\":1}");

        var obj = Assert.IsType<CstObject>(result.Root);
        Assert.Equal(0, obj.Start);
        Assert.Equal(7, obj.End);
        var prop = obj.Properties[0];
        Assert.Equal(1, prop.KeyStart);
        var val = Assert.IsType<CstValue>(prop.Value);
        Assert.Equal(CstValueKind.Number, val.Kind);
        Assert.Equal("1", val.RawText);
    }

    [Fact]
    public void WhitespaceTolerant_ParsesCorrectly()
    {
        var result = Parse("  {  \"key\"  :  \"value\"  }  ");

        Assert.Empty(result.Diagnostics);
        var obj = Assert.IsType<CstObject>(result.Root);
        Assert.Single(obj.Properties);
        Assert.Equal("key", obj.Properties[0].Key);
    }

    [Fact]
    public void MultipleProperties_ParsesCorrectly()
    {
        var result = Parse("{\"a\":1,\"b\":true,\"c\":null}");

        Assert.Empty(result.Diagnostics);
        var obj = Assert.IsType<CstObject>(result.Root);
        Assert.Equal(3, obj.Properties.Length);
        Assert.Equal("a", obj.Properties[0].Key);
        Assert.Equal("b", obj.Properties[1].Key);
        Assert.Equal("c", obj.Properties[2].Key);
    }

    [Fact]
    public void NegativeNumber_ParsesCorrectly()
    {
        var result = Parse("-42");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal(CstValueKind.Number, val.Kind);
        Assert.Equal("-42", val.RawText);
    }

    [Fact]
    public void EscapedString_ParsesCorrectly()
    {
        var result = Parse("\"hello\\nworld\"");

        var val = Assert.IsType<CstValue>(result.Root);
        Assert.Equal("hello\nworld", val.RawText);
    }
}
