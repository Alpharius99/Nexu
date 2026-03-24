using Nexu.Domain;

namespace Nexu.Editing;

public abstract record EditIntent;

// KeyStart..KeyEnd spans the raw key INCLUDING surrounding quotes (mirrors CstProperty.KeyStart/KeyEnd)
public sealed record RenameProperty(
    NodeId TargetId,
    int KeyStart,
    int KeyEnd,
    string OldName,
    string NewName) : EditIntent;

// ValueStart..ValueEnd spans the full raw value span (quotes included for strings)
// NewRawText is the complete replacement (caller provides quotes for strings)
public sealed record SetScalarValue(
    NodeId TargetId,
    int ValueStart,
    int ValueEnd,
    string OldRawText,
    string NewRawText) : EditIntent;

// ParentObjectEnd = position of '}'. LastPropertyEnd = -1 if empty.
public sealed record AddProperty(
    NodeId ParentObjectId,
    int ParentObjectStart,
    int ParentObjectEnd,
    int LastPropertyEnd,
    string Indentation,
    string NewKey,
    string NewValueRaw) : EditIntent;

// ParentArrayEnd = position of ']'. LastElementEnd = -1 if empty.
public sealed record AddArrayItem(
    NodeId ParentArrayId,
    int ParentArrayStart,
    int ParentArrayEnd,
    int LastElementEnd,
    string Indentation,
    string NewValueRaw) : EditIntent;

// PrevSiblingEnd / NextSiblingStart = -1 when absent. SiblingCount drives comma-cleanup case selection.
public sealed record RemoveNode(
    NodeId TargetId,
    int NodeStart,
    int NodeEnd,
    int PrevSiblingEnd,
    int NextSiblingStart,
    int SiblingCount) : EditIntent;
