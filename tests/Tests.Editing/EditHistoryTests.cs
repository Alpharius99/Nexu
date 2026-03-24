using Nexu.Domain;
using Nexu.Editing;
using Nexu.Parsing.Json;

namespace Nexu.Tests.Editing;

public class EditHistoryTests
{
    private static RawDocument Doc(string text) => new(text, 1, null);

    // Renames "name" → "fullName" in {"name":"Alice"}
    private static EditResult RenameNameToFullName(RawDocument doc) =>
        DocumentEditor.Apply(doc, new RenameProperty(NodeId.New(), 1, 7, "name", "fullName"));

    [Fact]
    public void CanUndo_IsFalse_Initially()
    {
        Assert.False(new EditHistory().CanUndo);
    }

    [Fact]
    public void CanRedo_IsFalse_Initially()
    {
        Assert.False(new EditHistory().CanRedo);
    }

    [Fact]
    public void Undo_OnEmpty_ReturnsNull()
    {
        Assert.Null(new EditHistory().Undo(Doc("{}")));
    }

    [Fact]
    public void Redo_OnEmpty_ReturnsNull()
    {
        Assert.Null(new EditHistory().Redo(Doc("{}")));
    }

    [Fact]
    public void Push_SetsCanUndo()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var history = new EditHistory();
        history.Push(RenameNameToFullName(doc));
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Push_DoesNotSetCanRedo()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var history = new EditHistory();
        history.Push(RenameNameToFullName(doc));
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Undo_RestoresOriginalText()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var result = RenameNameToFullName(doc);
        var history = new EditHistory();
        history.Push(result);

        var undoResult = history.Undo(result.Document);

        Assert.NotNull(undoResult);
        Assert.Equal(doc.Text, undoResult.Document.Text);
    }

    [Fact]
    public void Undo_SetsCanRedo()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var result = RenameNameToFullName(doc);
        var history = new EditHistory();
        history.Push(result);
        history.Undo(result.Document);

        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Undo_ClearsCanUndo_WhenSingleItem()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var result = RenameNameToFullName(doc);
        var history = new EditHistory();
        history.Push(result);
        history.Undo(result.Document);

        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Undo_ThenRedo_ReturnsToEditedText()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var result = RenameNameToFullName(doc);
        var history = new EditHistory();
        history.Push(result);

        var undoResult = history.Undo(result.Document)!;
        var redoResult = history.Redo(undoResult.Document);

        Assert.NotNull(redoResult);
        Assert.Equal(result.Document.Text, redoResult.Document.Text);
    }

    [Fact]
    public void PushAfterUndo_ClearsRedoStack()
    {
        var doc = Doc("{\"name\":\"Alice\"}");
        var result = RenameNameToFullName(doc);
        var history = new EditHistory();
        history.Push(result);
        var undoResult = history.Undo(result.Document)!;

        // Push a new edit — redo stack must be cleared
        var newResult = DocumentEditor.Apply(undoResult.Document,
            new RenameProperty(NodeId.New(), 1, 7, "name", "alias"));
        history.Push(newResult);

        Assert.False(history.CanRedo);
    }
}
