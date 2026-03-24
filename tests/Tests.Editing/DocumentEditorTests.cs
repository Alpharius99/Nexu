using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class DocumentEditorTests
{
    private static readonly NodeId AnyId = NodeId.New();

    private static RawDocument Doc(string text) => new(text, 1, null);

    [Fact]
    public void Apply_IncreasesRevision()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = DocumentEditor.Apply(doc, intent);
        Assert.Equal(2, result.Document.Revision);
    }

    [Fact]
    public void Apply_Rename_ParseResultHasNoErrors()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = DocumentEditor.Apply(doc, intent);
        Assert.False(result.ParseResult.HasErrors);
    }

    [Fact]
    public void Apply_Rename_GraphContainsNewKey()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = DocumentEditor.Apply(doc, intent);
        Assert.Contains(result.Graph.Nodes.Values, n => n.Label == "fullName");
    }

    [Fact]
    public void Apply_AddProperty_NodeCountIncremented()
    {
        var doc = Doc("{\"a\":1}");
        var originalDoc = doc;
        var originalGraph = CstToNodeGraphMapper.Map(JsonParser.Parse(originalDoc).Root);
        var originalCount = originalGraph.Nodes.Count;

        var intent = new AddProperty(AnyId, 0, 7, 6, "  ", "b", "2");
        var result = DocumentEditor.Apply(doc, intent);
        Assert.True(result.Graph.Nodes.Count > originalCount);
    }

    [Fact]
    public void Apply_Remove_NodeCountDecremented()
    {
        var doc = Doc("{\"a\":1,\"b\":2}");
        var originalGraph = CstToNodeGraphMapper.Map(JsonParser.Parse(doc).Root);
        var originalCount = originalGraph.Nodes.Count;

        // Remove "b":2 which spans 7..12
        var intent = new RemoveNode(AnyId, 7, 12, 6, -1, 2);
        var result = DocumentEditor.Apply(doc, intent);
        Assert.True(result.Graph.Nodes.Count < originalCount);
    }

    [Fact]
    public void Apply_PatchIsInvertible()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = DocumentEditor.Apply(doc, intent);
        var restored = result.Patch.Invert().ApplyTo(result.Document.Text);
        Assert.Equal(doc.Text, restored);
    }

    [Fact]
    public void Apply_FullRoundTrip_LayoutIsNonEmpty()
    {
        var doc = Doc("{\"name\":\"Alice\",\"age\":30}");
        var intent = new RenameProperty(AnyId, 1, 7, "name", "fullName");
        var result = DocumentEditor.Apply(doc, intent);
        Assert.True(result.Layout.Nodes.Length > 0);
    }
}
