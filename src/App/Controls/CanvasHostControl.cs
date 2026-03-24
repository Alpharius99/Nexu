using System.Globalization;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

using Nexu.Domain;
using Nexu.Layout;

namespace Nexu.App.Controls;

public sealed class CanvasHostControl : Control
{
    // Visual constants
    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(241, 245, 249));
    private static readonly IBrush NodeFill = new SolidColorBrush(Color.FromRgb(255, 255, 255));
    private static readonly IBrush NodeFillSelected = new SolidColorBrush(Color.FromRgb(219, 234, 254));
    private static readonly IPen NodePen = new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)), 1.5);
    private static readonly IPen NodePenSelected = new Pen(new SolidColorBrush(Color.FromRgb(59, 130, 246)), 2.5);
    private static readonly IPen EdgePen = new Pen(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));
    private static readonly IBrush EmptyLabelBrush = new SolidColorBrush(Color.FromRgb(148, 163, 184));
    private const double FontSize = 12.0;
    private const double NodeCornerRadius = 4.0;
    private const double LabelPadding = 6.0;

    // State
    private LayoutResult? _layout;
    private Dictionary<NodeId, PositionedNode> _nodeMap = new();
    private NodeId? _selectedId;

    // Pan/zoom state
    private double _scale = 1.0;
    private double _offsetX = 20.0;
    private double _offsetY = 20.0;

    // Pan drag state
    private bool _isPanning;
    private Point _panAnchorScreen;
    private double _panStartOffsetX;
    private double _panStartOffsetY;

    public NodeId? SelectedNodeId => _selectedId;

    public void SetLayout(LayoutResult? layout)
    {
        _layout = layout;
        _nodeMap = new Dictionary<NodeId, PositionedNode>();
        _selectedId = null;
        _scale = 1.0;
        _offsetX = 20.0;
        _offsetY = 20.0;

        if (layout != null)
        {
            foreach (var node in layout.Nodes)
                _nodeMap[node.NodeId] = node;
        }

        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

        if (_layout == null || _layout.Nodes.IsEmpty)
            return;

        var transform = Matrix.CreateScale(_scale, _scale)
                        * Matrix.CreateTranslation(_offsetX, _offsetY);

        using (context.PushTransform(transform))
        {
            RenderEdges(context);
            RenderNodes(context);
        }
    }

    private void RenderEdges(DrawingContext context)
    {
        if (_layout == null) return;

        foreach (var edge in _layout.Edges)
        {
            if (!_nodeMap.TryGetValue(edge.From, out var from) ||
                !_nodeMap.TryGetValue(edge.To, out var to))
                continue;

            var p1 = new Point(from.X + from.Width, from.Y + from.Height / 2.0);
            var p2 = new Point(to.X, to.Y + to.Height / 2.0);
            context.DrawLine(EdgePen, p1, p2);
        }
    }

    private void RenderNodes(DrawingContext context)
    {
        if (_layout == null) return;

        foreach (var node in _layout.Nodes)
        {
            var rect = new Rect(node.X, node.Y, node.Width, node.Height);
            var isSelected = node.NodeId == _selectedId;

            context.DrawRectangle(
                isSelected ? NodeFillSelected : NodeFill,
                isSelected ? NodePenSelected : NodePen,
                rect,
                NodeCornerRadius, NodeCornerRadius);

            var label = TruncateLabel(node.Label);
            var labelBrush = node.Label is null ? EmptyLabelBrush : LabelBrush;

            var ft = new FormattedText(
                label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                FontSize,
                labelBrush);

            var textX = node.X + LabelPadding;
            var textY = node.Y + (node.Height - ft.Height) / 2.0;
            context.DrawText(ft, new Point(textX, textY));
        }
    }

    private static string TruncateLabel(string? label)
    {
        if (label is null) return "(null)";
        if (label.Length == 0) return "(empty)";
        const int maxChars = 14;
        return label.Length > maxChars ? label[..(maxChars - 1)] + "…" : label;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var screenPos = e.GetPosition(this);
        var worldPos = ScreenToWorld(screenPos);

        // Hit test for selection
        _selectedId = null;
        if (_layout != null)
        {
            foreach (var node in _layout.Nodes)
            {
                var rect = new Rect(node.X, node.Y, node.Width, node.Height);
                if (rect.Contains(worldPos))
                {
                    _selectedId = node.NodeId;
                    break;
                }
            }
        }

        // Begin pan
        _isPanning = true;
        _panAnchorScreen = screenPos;
        _panStartOffsetX = _offsetX;
        _panStartOffsetY = _offsetY;
        e.Pointer.Capture(this);

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (!_isPanning) return;

        var screenPos = e.GetPosition(this);
        var deltaX = screenPos.X - _panAnchorScreen.X;
        var deltaY = screenPos.Y - _panAnchorScreen.Y;
        _offsetX = _panStartOffsetX + deltaX;
        _offsetY = _panStartOffsetY + deltaY;

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isPanning = false;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);

        var screenPos = e.GetPosition(this);
        var worldPos = ScreenToWorld(screenPos);

        double factor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
        _scale = Math.Clamp(_scale * factor, 0.05, 10.0);

        // Recompute offset so worldPos stays under the mouse
        _offsetX = screenPos.X - worldPos.X * _scale;
        _offsetY = screenPos.Y - worldPos.Y * _scale;

        InvalidateVisual();
    }

    private Point ScreenToWorld(Point screen) =>
        new((screen.X - _offsetX) / _scale, (screen.Y - _offsetY) / _scale);
}
