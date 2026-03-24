using Nexu.Domain;
using Nexu.Layout;
using Nexu.Parsing.Json;

namespace Nexu.Editing;

public sealed class EditHistory
{
    private readonly Stack<EditResult> _undoStack = new();
    private readonly Stack<EditResult> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Push(EditResult result)
    {
        _undoStack.Push(result);
        _redoStack.Clear();
    }

    public EditResult? Undo(RawDocument current)
    {
        if (!CanUndo) return null;

        var toUndo = _undoStack.Pop();
        var invertedPatch = toUndo.Patch.Invert();
        var restoredText = invertedPatch.ApplyTo(current.Text);
        var restoredDoc = current with { Text = restoredText, Revision = current.Revision + 1 };
        var parseResult = JsonParser.Parse(restoredDoc);
        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        var layout = LayoutEngine.Compute(graph);

        _redoStack.Push(toUndo);
        return new EditResult(restoredDoc, parseResult, graph, layout, invertedPatch);
    }

    public EditResult? Redo(RawDocument current)
    {
        if (!CanRedo) return null;

        var toRedo = _redoStack.Pop();
        var newText = toRedo.Patch.ApplyTo(current.Text);
        var newDoc = current with { Text = newText, Revision = current.Revision + 1 };
        var parseResult = JsonParser.Parse(newDoc);
        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        var layout = LayoutEngine.Compute(graph);

        _undoStack.Push(toRedo);
        return new EditResult(newDoc, parseResult, graph, layout, toRedo.Patch);
    }
}
