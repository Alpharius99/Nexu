# Architecture Research — Nexu Inline Editing Milestone

**Analysis:** Component boundaries and integration for inline editing in existing Avalonia app.
**Confidence:** HIGH

---

## Key Findings

- `CanvasHostControl` is pure immediate-mode (`Control` + custom `Render`). It holds no Avalonia child controls. A real `TextBox` cannot be "drawn" — it needs a retained-mode visual parent **alongside** the canvas, not inside it.
- The correct Avalonia pattern is a `Panel` stacking `CanvasHostControl` and a new `NodeEditorOverlay` as siblings. The overlay hosts a single `TextBox`, positioned in **screen space** (not world/canvas space) using the pan/zoom transform: `screenX = node.X * scale + offsetX`.
- `EditIntent` constructors need character-index spans (`KeyStart`, `KeyEnd`, etc.). These are on `CstNode`, not `NodeId`. Bridge: build a `Dictionary<NodeId, CstNode>` in `MainWindow` after each parse — keeps domain model clean, consistent with existing static/immutable style.
- The duplicate-key validation path requires calling `DocumentEditor.Apply` and checking `result.ParseResult.HasErrors` for `DiagnosticKind.DuplicateKey`, then discarding the result without pushing to `EditHistory`. This is the only intentional "apply and discard" case.

---

## Component Boundaries

| Component | Inputs | Outputs | Knows About |
|-----------|--------|---------|-------------|
| `CanvasHostControl` (extend) | `PointerPressed` (double-click) | `EditRequested(NodeId, EditKind, Point)` event | `LayoutResult`, screen coords |
| `NodeEditorOverlay` (new) | `Show(PositionedNode, text, scale, offsetX, offsetY)` | `EditCommitted(NodeId, string)`, `EditCancelled` events | Screen coords, `NodeId` pass-through, text only |
| `MainWindow` (extend) | `EditRequested`, `EditCommitted`, `EditCancelled` | `EditIntent` → `DocumentEditor.Apply` | All state: CST index, graph, doc, history |
| `DocumentEditor` (unchanged) | `RawDocument`, `EditIntent` | `EditResult` | Editing layer only |

---

## Data Flow

```
double-click node
  → CanvasHostControl raises EditRequested(NodeId, EditKind, screenPoint)
    → MainWindow.OnEditRequested:
        look up PositionedNode from _layout
        look up label from _cstIndex[nodeId]
        stash _editingNodeId + _editingKind
        Canvas.BeginEdit()          ← suppress pan/selection
        Overlay.Show(posNode, text, scale, offsetX, offsetY)
          [user types]
          Escape → OnEditCancelled → Canvas.EndEdit(), Overlay.Hide()
          Enter/blur → OnEditCommitted(nodeId, newText):
            pre-validate empty key (status bar, stay open)
            _cstIndex[nodeId] → CstProperty/CstValue → extract spans
            build RenameProperty or SetScalarValue
            DocumentEditor.Apply(_currentDoc, intent) → EditResult
            if HasErrors (DuplicateKey): warn, restore text, stay open
            else: EditHistory.Push, ApplyResult, Canvas.EndEdit(), Overlay.Hide()
```

---

## Build Order

1. **`CanvasHostControl`** — add `EditRequested` event + double-click detection + `BeginEdit`/`EndEdit` gate
2. **`NodeEditorOverlay`** — new control; pure screen-space TextBox; emits `EditCommitted`/`EditCancelled`
3. **`MainWindow.axaml`** — wrap canvas+overlay in `Panel`; wire `x:Name="Overlay"`
4. **`MainWindow.axaml.cs`** — CstIndex helper, `OnEditRequested`, `OnEditCommitted`, `OnEditCancelled`, validation path
5. **Validation feedback** — duplicate-key apply-and-discard path, empty-key pre-check

Steps 1 and 2 can be built in parallel (no mutual dependency). Steps 3–5 require 1 and 2 complete.

---

## Critical Anti-Patterns

- Never position `TextBox` in world/canvas coordinates — it ignores the canvas transform. Always screen-space.
- Never cache `NodeId` across a `DocumentEditor.Apply` call — new GUIDs per parse.
- Never let `NodeEditorOverlay` build `EditIntent` — it should only emit `(NodeId, string)`.
- `LostFocus` alone is not a reliable commit trigger — window blur (`cmd-tab`) fires it; handle `Window.Deactivated` to cancel instead of commit in that case.
- `AddProperty`/`AddArrayItem`/`RemoveNode` UI follows the same event pattern but needs additional gestures — separate concern, no blocking dependency.

---

## Open Questions

- `LostFocus` vs `Window.Deactivated` exact event order in Avalonia 11.3 — verify during implementation.
- Whether to expose `BeginEdit`/`EndEdit` as a bool field or methods on `CanvasHostControl` — either works.
- Scale/offset are private fields today. Either expose as readonly properties, or pass as parameters on the `EditRequested` event.
