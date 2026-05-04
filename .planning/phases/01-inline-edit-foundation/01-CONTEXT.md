# Phase 1: Inline Edit Foundation - Context

**Gathered:** 2026-05-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Wire an inline TextBox overlay onto the Avalonia canvas so the user can double-click any property key or scalar value node, edit it in place, and have the change committed to the raw document with full undo support. All the underlying editing machinery (EditIntent, PatchGenerator, DocumentEditor, EditHistory) already exists — this phase is UI wiring and validation state machine only.

</domain>

<decisions>
## Implementation Decisions

### Validation Feedback (VAL-01, VAL-02)
- **D-01:** Red border only — when a commit is blocked (empty key or duplicate sibling key), the TextBox gets a red border and stays open. No status bar message. Keeps the zen aesthetic.
- **D-02:** Error state clears on next keystroke — as soon as the user types any character, the red border disappears.
- **D-03:** Duplicate key check is case-sensitive — matches JSON spec and the existing `JsonParser` duplicate-key detection.

### Blur Commit Semantics (EDIT-05, EDIT-06)
- **D-04:** Double-clicking another node while the editor is open: commit current edit, then select the new node (spreadsheet-style). If the edit was a no-op, just select.
- **D-05:** Window losing focus (Cmd+Tab to another app) while the editor is open: commit. Treat window deactivation as a blur event — user work is preserved.
- **D-06:** No-op guard is string equality — compare final text to original text on commit. If identical, treat as cancel: no TextPatch, no history entry. Keeps the undo stack clean.

### Test Boundary for UI Logic
- **D-07:** Extract a new `InlineEditorState` class to `src/App/InlineEditorState.cs` — a pure C# state machine with no Avalonia dependencies. Holds `OriginalText`, `CurrentText`, `SiblingKeys` (for duplicate check), `IsError`, `IsNoOp`, `Validate()`. MainWindow holds an instance and wires it to the TextBox.
- **D-08:** Create a new `tests/Tests.App/` test project (mirrors `Tests.Editing`, `Tests.Domain`, etc.) with `InlineEditorStateTests.cs` covering validation logic, no-op detection, and error-clear-on-keystroke.

### Overlay Coordinate Translation
- **D-09:** `CanvasHostControl` exposes `GetNodeScreenRect(NodeId) → Rect` — applies the current pan/zoom matrix to the node's `PositionedNode` bounds and returns a `Rect` in window coordinates. MainWindow calls this when opening the overlay.
- **D-10:** The overlay TextBox is hosted in an Avalonia `Canvas` panel (wrapping both `CanvasHostControl` and the TextBox sibling). Positioned via `Canvas.Left` / `Canvas.Top` set from `GetNodeScreenRect`.
- **D-11:** Pan or zoom while the editor is open commits the current edit (if valid, non-no-op) and closes the overlay. No live tracking — overlay does not follow the node as the view moves.

### Implementation Constraints (from STATE.md — already decided)
- **C-01:** Fix the `_history.Push()` bug in `MainWindow.ApplyResult` before any overlay work — it currently blocks undo for all edits.
- **C-02:** TextBox overlay is a sibling `Canvas` child, NOT inside `CanvasHostControl` — the layout engine is blind to the render transform.
- **C-03:** `_isEditing` flag gates: undo/redo keyboard shortcuts, autosave scheduling, and (in Phase 3) chrome visibility.
- **C-04:** `Focus()` on the TextBox must be deferred via `Dispatcher.UIThread.Post(..., DispatcherPriority.Loaded)` — synchronous call silently no-ops.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Project context and requirements
- `.planning/PROJECT.md` — Nexu project goals, constraints, key decisions, and evolution
- `.planning/REQUIREMENTS.md` — All v1 requirements with IDs (EDIT-01–06, VAL-01–02, HIST-01–02 are Phase 1)
- `.planning/ROADMAP.md` — Phase 1 goal, success criteria, and dependency graph

### Known constraints and prior decisions
- `.planning/STATE.md` — Key implementation constraints for Phase 1 (history bug, overlay placement, `_isEditing` flag, Focus() deferral)

### Codebase maps
- `.planning/codebase/ARCHITECTURE.md` — Edit path, undo/redo path, data flow
- `.planning/codebase/STRUCTURE.md` — Where new files go, test project conventions
- `.planning/codebase/CONVENTIONS.md` — Naming, type design, code style rules

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Editing/EditIntent.cs` — `RenameProperty` and `SetScalarValue` subtypes are the two intents this phase will fire; already implemented and tested
- `src/Editing/DocumentEditor.cs` — Static `Apply(RawDocument, EditIntent)` → `EditResult`; the single call that commits an edit
- `src/Editing/EditHistory.cs` — `Push`, `Undo`, `Redo`; already tested; just needs the `_history.Push()` bug fixed in `MainWindow.ApplyResult`
- `src/App/Controls/CanvasHostControl.cs` — Immediate-mode renderer with existing node hit-test and selection; needs double-click event wiring and `GetNodeScreenRect` method added
- `src/App/MainWindow.axaml.cs` — Already owns `_currentDoc`, `_history`, `_autoSave`; `ApplyResult` exists and is the integration hub for every edit

### Established Patterns
- Immutable value pipeline: every edit produces a complete new `EditResult`; no mutation — store and display the new state
- `NodeId` is NOT stable across re-parses; always navigate by CST character position, not by `NodeId` across parse cycles
- `TreatWarningsAsErrors = true` — zero warnings tolerated; nullable annotations required throughout

### Integration Points
- `MainWindow.ApplyResult(EditResult)` — new inline editor calls this on commit, same as all other edits
- `CanvasHostControl.SetLayout(LayoutResult)` — called inside `ApplyResult`; canvas re-renders automatically
- `AutoSaveManager.Schedule()` — called inside `ApplyResult`; gated by `_isEditing` flag (C-03)

</code_context>

<specifics>
## Specific Ideas

- The user wants the overlay TextBox declared in `MainWindow.axaml` (not created dynamically in code-behind) with `IsVisible="False"` initially, shown by setting `IsVisible="True"` and positioning via `Canvas.Left`/`Canvas.Top`.
- `InlineEditorState` shape: `OriginalText`, `CurrentText`, `IEnumerable<string> SiblingKeys`, `bool IsError`, computed `bool IsNoOp => CurrentText == OriginalText`, method `ValidationResult Validate()`.
- `tests/Tests.App/` is a new xUnit test project following the same conventions as `tests/Tests.Editing/`.

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 1-Inline Edit Foundation*
*Context gathered: 2026-05-04*
