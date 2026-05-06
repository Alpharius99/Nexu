# Pitfalls Research — Nexu Inline Editing Milestone

**Analysis:** Grounded in direct code inspection of the existing Nexu codebase.
**Confidence:** MEDIUM-HIGH

---

## Summary

The most dangerous pitfall is **coordinate space confusion**: `DrawingContext.PushTransform` is render-only; a TextBox child of `CanvasHostControl` ignores it entirely. The overlay must be a sibling control in a Panel, positioned using `WorldToScreen` math.

**Undo-during-edit** is an immediate correctness hazard: `MainWindow.OnKeyDown` fires `_history.Undo` globally; a `Ctrl+Z` inside the TextBox will pop committed history. Gate undo on an `_isEditing` flag.

**`_history.Push` is never called** (documented in `CONCERNS.md`). Any editing work that does not fix this first will pass edit tests but silently leave undo broken.

---

## Critical Pitfalls

### C1: TextBox Positioned in Canvas Transform Space, Not Screen Space
- **What goes wrong:** `DrawingContext.PushTransform` is rendering-only. Child controls added to `CanvasHostControl` are laid out in local (screen) space — the transform is invisible to Avalonia's layout pass. A TextBox positioned with world coordinates appears at the wrong location and does not track pan/zoom.
- **Prevention:** Keep `CanvasHostControl` as a pure render control (no child controls). Place the overlay TextBox as a sibling in a containing `Panel` or `Grid`. Compute screen position via `WorldToScreen(worldPoint, _scale, _offsetX, _offsetY)`. On pan/zoom while editing: either recompute TextBox position or close the editor.
- **Warning sign:** TextBox visually offset from the node on first render; position jumps on scroll.
- **Phase:** Address before any other inline editing work.

### C2: Undo Fires Globally While Inline Editor Is Open
- **What goes wrong:** `MainWindow.OnKeyDown` handles `Ctrl+Z` unconditionally. When a TextBox is open, `Ctrl+Z` intended for the TextBox's internal undo pops a committed edit from `EditHistory`, reloads the document, and destroys the in-progress edit.
- **Prevention:** Add `bool _isEditing` flag. Gate `OnKeyDown` undo/redo on `!_isEditing`. Mark `Ctrl+Z`/`Ctrl+Y` as `e.Handled = true` in the TextBox `KeyDown` handler to prevent bubbling. Escape = cancel edit + clear flag. Enter = commit + clear flag.
- **Warning sign:** `Ctrl+Z` inside TextBox reloads the document instead of undoing a character.
- **Phase:** Design before wiring any `KeyDown` handler to edit flow.

### C3: Stale Patch Offsets After Revision Change During Edit
- **What goes wrong:** The editor captures `KeyStart`/`KeyEnd` (or `ValueStart`/`ValueEnd`) from the CST at open time. If the document revision changes (undo, redo, or concurrent edit) while the editor is open, those offsets are stale. `TextPatch.ApplyTo` throws (or corrupts) if the document revision changed since the editor opened. `CONCERNS.md` explicitly flags this.
- **Prevention:** Snapshot `_currentDoc.Revision` when the editor opens. On commit, assert snapshot == current revision; abort with status message if they differ. Block undo/redo while `_isEditing`. Autosave (file write only) is safe during editing; undo/redo is not.
- **Warning sign:** After editing, a character appears at an unexpected position in the file.
- **Phase:** Address when wiring the commit path.

### C4: Focus Never Reaches TextBox, or Never Returns to Canvas
- **What goes wrong:** Two failure modes. (1) `Focus()` called synchronously in the same layout pass as showing the TextBox — Avalonia's `FocusManager` requires the control to be attached and have completed layout; the call silently fails. (2) On editor close, focus is not explicitly returned to `CanvasHostControl`, which is a custom `Control` that is not focusable by default — keyboard navigation stops working.
- **Prevention:** Defer `Focus()` via `Dispatcher.UIThread.Post(() => textBox.Focus(), DispatcherPriority.Loaded)`. Set `Focusable = true` on `CanvasHostControl`. On editor close, explicitly call `canvasHostControl.Focus()`.
- **Warning sign:** TextBox is visible but keystrokes go nowhere. After close, arrow keys do not navigate the canvas.
- **Phase:** Address when building show/hide flow.

---

## Moderate Pitfalls

### M1: Pan Gesture Starts on Double-Click, Misplacing the Editor
- **What goes wrong:** `OnPointerPressed` always starts pan and captures the pointer. A double-click sends two `PointerPressed` events. The second press opens the editor and also starts a pan; any mouse movement between clicks shifts `_offsetX`/`_offsetY`, causing the TextBox to be positioned against a stale offset.
- **Prevention:** Suppress pan start when `e.ClickCount == 2` or when `_isEditing`. Use `e.Handled = true` on double-click to prevent pointer capture for panning.
- **Phase:** Implement alongside double-click detection.

### M2: Validation on Every Keystroke Creates Error Anxiety
- **What goes wrong:** Duplicate-key validation connected to `TextChanged` fires for every intermediate state. Intermediate states may match sibling keys, showing false errors.
- **Prevention:** Validate on `Enter` (commit) and `LostFocus`. Debounce real-time feedback (200–300ms idle). Never flag the original key as a duplicate of itself (`newKey != oldKey && siblings.Contains(newKey)`). For scalar values: only validate valid-JSON-ness on commit, not during typing.
- **Phase:** Validation layer design.

### M3: Autosave Fires Between Patch Application and Parse Result Update
- **What goes wrong:** `AutoSaveManager` fires on a timer. If autosave fires between `DocumentEditor.Apply` returning a new `EditResult` and `ApplyResult` updating `_currentParseResult`, the save-block check reads a stale `_currentParseResult`. A document with invalid JSON passes the error check and gets written to disk.
- **Prevention:** Block `AutoSaveManager.Schedule()` while `_isEditing`. In `ApplyResult`, update `_currentDoc` and `_currentParseResult` atomically before returning. Add assertion in `SaveAsync`: `_currentParseResult.Document.Revision == _currentDoc.Revision`.
- **Phase:** Autosave interaction design.

### M4: Auto-Hide Chrome Flickers When Focus Moves to Overlay TextBox
- **What goes wrong:** Chrome visibility driven by canvas focus events. Opening the inline editor moves focus from canvas to TextBox → canvas fires `LostFocus` → chrome becomes visible → chrome sees TextBox has focus (not canvas) → chrome hides → repeat flicker.
- **Prevention:** Drive chrome visibility from `_isEditing` flag and an explicit "navigation mode" state, not directly from `GotFocus`/`LostFocus` events. "Editing mode" is a sub-state of "canvas mode" — both keep chrome hidden.
- **Phase:** Auto-hide feature design. Must coordinate with inline editor state machine.

### M5: `FormattedText` Not Cached — CPU Spike During Pan While Editing
- **What goes wrong:** `CONCERNS.md` documents `FormattedText` is created per-node per-render frame. During inline editing, pan events call `InvalidateVisual()` continuously. With 50+ nodes, noticeable CPU pressure.
- **Prevention:** Introduce a `FormattedText` cache keyed by `(label, fontSize, brush)` before heavy inline editing work. Invalidate on `SetLayout`.
- **Phase:** Hardening step; not blocking for correctness.

---

## Minor Pitfalls

- **m1:** Set `AcceptsReturn="False"` and `AcceptsTab="False"` explicitly. Handle `Key.Return` in `KeyDown` with `e.Handled = true` before base to prevent newline insertion.
- **m2:** After `Focus()` in the `Dispatcher.UIThread.Post` callback, call `textBox.SelectAll()`. User types to replace, not prepend.
- **m3:** `TruncateLabel` is display-only — `PositionedNode.Label` is always the full value. Populate the inline TextBox from `PositionedNode.Label` directly, never from the rendered string.

---

## TDD-Specific Pitfalls

### T1: No `TestApplication` — Control Tests Crash Immediately
- Any test that instantiates an Avalonia control without a running application throws on the first property access. Use `Avalonia.Headless.XUnit` with `[AvaloniaFact]`/`[AvaloniaTheory]`. Pure logic tests (intent construction, patch generation, history) need no Avalonia setup. Design the commit path so it accepts `string newValue` and is testable without a TextBox.
- **Phase:** Before writing any UI layer tests.

### T2: Coordinate Math Untestable in Private Event Handlers
- `_scale`, `_offsetX`, `_offsetY` are private; `ScreenToWorld` is private. Extract pure static methods `ScreenToWorld(Point, double, double, double)` and `WorldToScreen(Point, double, double, double)` — fully unit-testable with no Avalonia infrastructure.
- **Phase:** Refactor during inline editor implementation.

### T3: `_history.Push` Is Never Called (Existing Bug — Must Fix First)
- `CONCERNS.md` confirms: `_history.Push()` is never called in `MainWindow`. Wire `_history.Push(result)` inside `ApplyResult` (called on every edit). Write test: "after inline rename, `_history.CanUndo == true`."
- **Phase:** Day 1 of inline editing implementation — prerequisite.

---

## Phase Warning Table

| Phase Topic | Pitfall | Mitigation |
|---|---|---|
| TextBox overlay placement | C1: coordinate space | Sibling Panel + `WorldToScreen` |
| Double-click to open | M1: pan on double-click | Guard pan on `_isEditing` and click count |
| Enter/Escape handling | C2: global undo fires | `_isEditing` flag gates all history operations |
| Patch commit | C3: stale offsets | Snapshot revision at open; assert on commit |
| Validation feedback | M2: keystroke errors | Validate on commit + debounce |
| Autosave interaction | M3: stale parse result | Block schedule during edit; atomic state update |
| Auto-hide chrome | M4: focus flicker | Chrome state from explicit flag, not focus events |
| Focus management | C4: focus lost/not granted | `Dispatcher.Post` for focus; `Focusable=true` on canvas |
| History wiring | T3: Push never called | Fix in `ApplyResult` before any edit UI work |
| UI layer tests | T1: no test app | `Avalonia.Headless.XUnit` + `[AvaloniaFact]` |
| Coordinate tests | T2: private math | Extract to pure static methods |
