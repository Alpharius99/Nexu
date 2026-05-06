# Stack Research — Nexu Inline Editing Milestone

**Analysis:** Avalonia 11.x APIs for inline canvas editing. No new packages needed.
**Confidence:** HIGH

---

## Key Findings

- **Overlay pattern:** Wrap `CanvasHostControl` and an `EditOverlay` `Canvas` in a `Panel` in `MainWindow.axaml`. The `TextBox` lives in the overlay layer, hidden by default, positioned via `Canvas.Left`/`Canvas.Top` on double-click. No new packages needed.
- **Double-click:** `PointerPressedEventArgs.ClickCount == 2` is the Avalonia 11 idiomatic API. Already used in `OnPointerPressed`. No timer needed.
- **Focus order is load-bearing:** `IsVisible = true` must precede `Focus()`. Invisible controls silently no-op in Avalonia 11's focus system. Call order: `IsVisible = true` → set `Text` → `Focus()` → `SelectAll()`.
- **Critical architecture gap:** `NodeGraph`/`PositionedNode` do NOT carry CST character positions — `CstToNodeGraphMapper` discards all `Start`/`End` values. At double-click time the canvas must fire a `NodeEditRequested` event; `MainWindow` (which holds `_currentParseResult`) walks the CST to find `CstProperty.KeyStart/KeyEnd` or `CstValue.Start/End` and constructs the intent.
- **Popup is wrong:** Avalonia `Popup` creates a separate OS-level window layer with independent Z-ordering and complex focus ownership. Do not use it for inline editing. Overlay `Canvas` is 30–50 lines and precise.

---

## Recommended Stack (no changes to existing)

| Capability | API | Confidence |
|------------|-----|------------|
| Overlay positioning | `Canvas.SetLeft`/`SetTop` attached properties | HIGH |
| Text input | `TextBox` (`AcceptsReturn=False`, `TextWrapping=NoWrap`) | HIGH |
| Double-click detection | `PointerPressedEventArgs.ClickCount == 2` | HIGH |
| Focus management | `control.IsVisible = true` → `Focus()` → `SelectAll()` | HIGH |
| Event routing | `e.Handled = true` on Enter/Escape in `TextBox.KeyDown` | HIGH |
| Commit on blur | `TextBox.LostFocus` → `CommitEdit` | HIGH |

---

## AXAML Change (MainWindow.axaml)

```xml
<!-- Replace bare CanvasHostControl with: -->
<Panel>
    <controls:CanvasHostControl x:Name="Canvas" ClipToBounds="True" />
    <Canvas x:Name="EditOverlay" IsHitTestVisible="False">
        <TextBox x:Name="InlineEditor"
                 IsVisible="False"
                 MinWidth="80" MaxWidth="300"
                 Padding="4,2" FontSize="12"
                 AcceptsReturn="False" TextWrapping="NoWrap" />
    </Canvas>
</Panel>
```

---

## Event Flow

1. `CanvasHostControl.OnPointerPressed` detects `ClickCount == 2`, fires `NodeEditRequested(NodeId, screenPoint)`.
2. `MainWindow` receives event, walks `_currentParseResult.Root` to find the `CstNode` for that `NodeId`.
3. `MainWindow` positions `InlineEditor` via `Canvas.SetLeft/SetTop`, sets `Text`, sets `IsVisible = true`, calls `Focus()`, `SelectAll()`.
4. On `Enter` or `LostFocus` → `MainWindow` constructs `RenameProperty` or `SetScalarValue` from the CST positions, calls `DocumentEditor.Apply`, calls `_history.Push`, calls `ApplyResult`.
5. On `Escape` → hide editor, no edit applied.
6. After hide: `Canvas.Focus()` to restore keyboard navigation.

---

## NodeType Dispatch

| `NodeType` | Pre-populate TextBox with | On commit |
|------------|--------------------------|-----------|
| `Property` | `node.Label` (key, no quotes) | Wrap: `"\"" + JsonEscapeString(text) + "\""` → `RenameProperty` |
| `Scalar` string | `node.Label` stripped of outer `"` | Rewrap + escape → `SetScalarValue` |
| `Scalar` non-string | `node.Label` as-is | Validate is valid JSON primitive → `SetScalarValue` |
| `Object`, `Array` | Not editable inline | Ignore double-click |

---

## What NOT To Do

- **Do NOT use `Popup`** — OS-level window layer, wrong Z-ordering, complex focus ownership.
- **Do NOT embed `TextBox` as a child of `CanvasHostControl`** — it's a bare `Control` with custom `Render()`; no logical children support without changing the base class.
- **Do NOT call `Focus()` before `IsVisible = true`** — silently no-ops in Avalonia 11.
- **Do NOT use third-party graph/node editors** — none are stable on Avalonia 11 as of 2026.
- **Do NOT store CST positions in `Node`/`NodeGraph`** — violates the layering contract; `Domain` must not depend on `Parsing.Json` types.

---

## Roadmap Implications

- Phase modifies: `CanvasHostControl.cs` (double-click event), `MainWindow.axaml` (overlay Panel), `MainWindow.axaml.cs` (CST lookup, editor show/hide, intent construction).
- Adds 1 new file: `NodeEditorOverlay` control or thin `EditSession` record.
- The CST-position lookup (walking `ParseResult.Root` to match `NodeId` → `CstNode`) deserves its own tested helper in `Domain` or `Editing`.
- Zero new NuGet packages. Zero changes to `Directory.Packages.props`.
