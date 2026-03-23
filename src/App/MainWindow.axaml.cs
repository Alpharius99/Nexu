using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Nexu.Domain;
using Nexu.Layout;
using Nexu.Parsing.Json;

namespace Nexu.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        await OpenJsonFileAsync();
    }

    private async Task OpenJsonFileAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
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

        var graph = CstToNodeGraphMapper.Map(parseResult.Root);
        var layout = LayoutEngine.Compute(graph);
        Canvas.SetLayout(layout);

        var fileName = Path.GetFileName(filePath);
        StatusBar.Text = $"{fileName} — {layout.Nodes.Length} nodes, {layout.Edges.Length} edges";
    }
}
