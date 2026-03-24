using Nexu.Domain;
using Nexu.Layout;
using Nexu.Parsing.Json;

namespace Nexu.Editing;

public sealed record EditResult(
    RawDocument Document,
    ParseResult ParseResult,
    NodeGraph Graph,
    LayoutResult Layout,
    TextPatch Patch);

public static class DocumentEditor
{
    public static EditResult Apply(RawDocument document, EditIntent intent)
    {
        var patch = PatchGenerator.Generate(intent, document.Text);
        var newText = patch.ApplyTo(document.Text);
        var newDoc = document with { Text = newText, Revision = document.Revision + 1 };
        var parseResult = JsonParser.Parse(newDoc);
        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        var layout = LayoutEngine.Compute(graph);
        return new EditResult(newDoc, parseResult, graph, layout, patch);
    }
}
