using Nexu.Domain;

namespace Nexu.Layout;

public sealed record PositionedNode(
    NodeId NodeId,
    double X,
    double Y,
    double Width,
    double Height,
    string? Label);
