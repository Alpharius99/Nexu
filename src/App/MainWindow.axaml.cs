using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

using Nexu.Domain;
using Nexu.Editing;
using Nexu.Layout;
using Nexu.Parsing.Json;
using Nexu.Persistence;

namespace Nexu.App;

public partial class MainWindow : Window
{
    private EditHistory? _history;
    private RawDocument? _currentDoc;
    private ParseResult? _currentParseResult;
    private AutoSaveManager? _autoSave;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        await OpenJsonFileAsync();
    }

    private async void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        await SaveAsync();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Z && e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                Redo();
            else
                Undo();
            e.Handled = true;
        }
    }

    private void Undo()
    {
        if (_history is null || _currentDoc is null) return;
        var result = _history.Undo(_currentDoc);
        if (result is null)
        {
            StatusBar.Text = "Nothing to undo.";
            return;
        }
        ApplyResult(result);
    }

    private void Redo()
    {
        if (_history is null || _currentDoc is null) return;
        var result = _history.Redo(_currentDoc);
        if (result is null)
        {
            StatusBar.Text = "Nothing to redo.";
            return;
        }
        ApplyResult(result);
    }

    private void ApplyResult(EditResult result)
    {
        _currentDoc = result.Document;
        _currentParseResult = result.ParseResult;

        if (result.ParseResult.HasErrors)
        {
            var errors = string.Join("; ", result.ParseResult.Diagnostics.Select(
                d => $"{d.Kind} at {d.Line}:{d.Column} — {d.Message}"));
            StatusBar.Text = $"Parse errors: {errors}";
            Canvas.SetLayout(null);
            return;
        }

        Canvas.SetLayout(result.Layout);
        var fileName = _currentDoc.FilePath is not null
            ? Path.GetFileName(_currentDoc.FilePath)
            : "Untitled";
        StatusBar.Text = $"{fileName} — {result.Layout.Nodes.Length} nodes, {result.Layout.Edges.Length} edges";
        _autoSave?.Schedule();
    }

    private Task SaveAsync()
    {
        if (_currentDoc?.FilePath is null) return Task.CompletedTask;

        if (_currentParseResult?.HasErrors == true)
        {
            StatusBar.Text = "Save blocked: document has errors.";
            return Task.CompletedTask;
        }

        AtomicFileWriter.Write(_currentDoc.FilePath, _currentDoc.Text);
        StatusBar.Text = $"{Path.GetFileName(_currentDoc.FilePath)} saved.";
        return Task.CompletedTask;
    }

    private async Task OpenJsonFileAsync()
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open JSON File",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("JSON Files") { Patterns = ["*.json"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        if (files.Count == 0) return;

        var file = files[0];
        string text;

        await using (var stream = await file.OpenReadAsync())
        using (var reader = new StreamReader(stream))
        {
            text = await reader.ReadToEndAsync();
        }

        var filePath = file.Path.LocalPath;
        var doc = new RawDocument(text, Revision: 1, FilePath: filePath);
        var parseResult = JsonParser.Parse(doc);

        if (parseResult.HasErrors)
        {
            var errors = string.Join("; ", parseResult.Diagnostics.Select(
                d => $"{d.Kind} at {d.Line}:{d.Column} — {d.Message}"));
            StatusBar.Text = $"Parse errors: {errors}";
            Canvas.SetLayout(null);
            return;
        }

        _currentDoc = doc;
        _currentParseResult = parseResult;
        _history = new EditHistory();
        _autoSave?.Dispose();
        _autoSave = new AutoSaveManager(TimeSpan.FromSeconds(2), () =>
        {
            Dispatcher.UIThread.Post(() => _ = SaveAsync());
            return Task.CompletedTask;
        });

        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        var layout = LayoutEngine.Compute(graph);
        Canvas.SetLayout(layout);

        var fileName = Path.GetFileName(filePath);
        StatusBar.Text = $"{fileName} — {layout.Nodes.Length} nodes, {layout.Edges.Length} edges";
    }
}
