# Research Summary — Nexu Inline Editing Milestone

---

## Executive Summary

Nexu has a complete editing pipeline (CST → NodeGraph → EditIntent → TextPatch → DocumentEditor → EditHistory) but no UI wiring to activate it. This milestone's sole job is connecting double-click gestures on the Avalonia canvas to that pipeline via a floating TextBox overlay. No new NuGet packages, no framework changes — the work is entirely in `CanvasHostControl`, a new `NodeEditorOverlay` control, and `MainWindow` coordination.

The overlay must be a sibling `Canvas` in a `Panel`, not a child of `CanvasHostControl`. The canvas uses an immediate-mode custom `Render()` override; Avalonia's layout engine is blind to its `DrawingContext.PushTransform`, so any TextBox placed inside it will appear at the wrong location and break on pan/zoom. All screen positioning must use explicit `WorldToScreen` math.

**One pre-existing bug must be fixed before any edit UI work:** `_history.Push()` is never called in `MainWindow` (documented in `CONCERNS.md`). Shipping inline editing without this produces a silent undo regression. Fix it in `ApplyResult` on day one.

---

## Stack Recommendation

Use no new packages. Overlay = plain Avalonia `Canvas` + `TextBox` as sibling to `CanvasHostControl` inside a `Panel`. All required APIs (`PointerPressedEventArgs.ClickCount`, `Canvas.SetLeft/SetTop`, `Dispatcher.UIThread.Post`, `TextBox.LostFocus`) are in Avalonia 11.3.12 already. Zero changes to `Directory.Packages.props`.

---

## Table Stakes Features (this milestone)

| Feature | Notes |
|---------|-------|
| Double-click to open inline editor (key + scalar nodes) | `ClickCount == 2`; objects/arrays not editable |
| Enter to commit / Escape to cancel | `e.Handled = true` on both |
| Blur (click-away) to commit | `LostFocus`; cancel on `Window.Deactivated` |
| Select-all on activation | After `Dispatcher.UIThread.Post(() => Focus())` |
| Empty key blocked at commit | Red border + status message; keep editor open |
| Duplicate key blocked at commit | Apply-and-discard via `DocumentEditor`; keep editor open |
| No-op guard | If text unchanged, treat as cancel |
| Add child property inline | `AddProperty` intent; immediately activate new node's key TextBox |
| Delete node via Del key | `RemoveNode` intent; confirm only if node has children |
| Auto-hide chrome (zen mode) | Fade on idle; state driven by `_isEditing` flag, not focus events |
| Undo/redo of all edits | Fix `_history.Push` in `ApplyResult` first |

**Defer to follow-on:** Tab/Shift+Tab sibling navigation, type coercion, inline type badges, soft-wrap.

---

## Top Architectural Decisions

1. **Overlay as sibling Panel child.** `CanvasHostControl` is a bare `Control` with custom `Render()` — no logical children, render transform is invisible to layout. Overlay `Canvas` + `TextBox` must sit alongside it in a `Panel`. Screen position = `WorldToScreen(worldPoint, _scale, _offsetX, _offsetY)`.

2. **`NodeId` → `CstNode` index in MainWindow.** `CstToNodeGraphMapper` intentionally discards CST positions to keep `Domain` clean. After each parse, `MainWindow` builds `Dictionary<NodeId, CstNode>`. This is the bridge between gesture and patch.

3. **`NodeEditorOverlay` is intentionally thin.** Only knows screen coords, `NodeId` pass-through, and text. Does NOT build `EditIntent`. Emits `EditCommitted(NodeId, string)` and `EditCancelled`. `MainWindow` owns all CST lookup, validation, intent construction, and history.

4. **`_isEditing` flag gates everything.** All undo/redo and canvas-level keyboard shortcuts check `!_isEditing`. Same flag drives chrome visibility (prevents LostFocus flicker) and blocks autosave scheduling during edits.

5. **Duplicate-key validation via apply-and-discard.** Call `DocumentEditor.Apply`, check `HasErrors` for `DiagnosticKind.DuplicateKey`, discard without pushing to `EditHistory`.

6. **Focus must be deferred.** `Focus()` synchronous in the same layout pass as `IsVisible = true` silently no-ops in Avalonia 11. Use `Dispatcher.UIThread.Post(..., DispatcherPriority.Loaded)`.

---

## Critical Pitfalls (phase-mapped)

| # | Pitfall | Phase | Prevention |
|---|---------|-------|------------|
| C1 | TextBox in canvas transform space — wrong coords, breaks on pan | Before overlay work | Sibling `Panel`; `WorldToScreen`; recompute or close on pan |
| C2 | Global `Ctrl+Z` pops history while TextBox open | KeyDown wiring | `_isEditing` gates `OnKeyDown`; mark keys handled in TextBox |
| C3 | Stale CST offsets if revision changes during edit | Commit path | Snapshot revision at open; assert on commit; block undo/redo while editing |
| C4 | `Focus()` silently fails / focus never returns to canvas | Show/hide flow | `Dispatcher.UIThread.Post`; `Focusable = true`; explicit `canvas.Focus()` on close |
| T3 | `_history.Push` never called (existing bug) | Day 1, prerequisite | Fix in `ApplyResult`; test "after rename, `CanUndo == true`" |

---

## Roadmap Implications

**Phase A — Prerequisites (blockers)**
Fix `_history.Push`. Extract `WorldToScreen`/`ScreenToWorld` as pure static methods. Set `Focusable = true` on `CanvasHostControl`.

**Phase B — Overlay foundation (B1 and B2 parallelizable)**
B1: `CanvasHostControl` — `EditRequested` event, `BeginEdit`/`EndEdit`, double-click, pan suppression.
B2: `NodeEditorOverlay` — new control, screen-space TextBox, emits `EditCommitted`/`EditCancelled`.
B3: `MainWindow.axaml` — `Panel` wrap, wire names.

**Phase C — Commit path and validation**
`CstIndex` helper, `OnEditRequested/Committed/Cancelled`, validation (empty key, duplicate key, no-op guard), revision snapshot+assert, `_isEditing` gating.

**Phase D — CRUD gestures**
Add child property with immediate editor activation. Delete node with conditional confirmation.

**Phase E — Auto-hide chrome**
Fade logic driven from `_isEditing` + explicit navigation state, not `GotFocus`/`LostFocus` directly.

---

## Confidence

| Area | Confidence | Basis |
|------|------------|-------|
| Stack | HIGH | All APIs confirmed; zero new deps |
| Features | HIGH | Grounded in PROJECT.md + ecosystem conventions |
| Architecture | HIGH | Direct code inspection of Nexu codebase |
| Pitfalls | MEDIUM-HIGH | Code inspection + `CONCERNS.md`; LostFocus/Deactivated ordering needs runtime verification |

**Open question (low-stakes):** Exact event order of `LostFocus` vs `Window.Deactivated` in Avalonia 11.3 — determines whether blur-commit or blur-cancel is safe on app-switch. Decide at Phase B.
