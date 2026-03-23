# Nexu — UI/UX Guidelines

> Design principles and interaction patterns for the Zen-mode mind-map editor.

---

## Design Philosophy

**Zen-mode** = minimal chrome, maximum canvas. The JSON structure *is* the interface.

### Core Principles

1. **The map is the editor.** No separate text pane, no split view. Users see and edit through the mind-map.
2. **Zero-config start.** Open a file → see the map. No project setup, no import wizards.
3. **Calm interface.** Muted palette, no competing UI elements, generous whitespace.
4. **Direct manipulation.** Click a node to select. Double-click to edit. Right-click for actions.
5. **Reversible everything.** Every action can be undone. Destructive actions (delete) require confirmation or are undoable.

---

## Visual Design

### Color Palette

| Element | Role | Guideline |
|---------|------|-----------|
| Canvas background | Neutral base | Soft off-white or dark gray (support light/dark themes later) |
| Object nodes | Container | Subtle border, slightly elevated (shadow or outline) |
| Property nodes | Key-value display | Key in muted bold, value in regular weight |
| Array nodes | Ordered container | Distinct from objects — indexed badges `[0]`, `[1]`, ... |
| Scalar nodes | Leaf values | Color-coded by type: string, number, boolean, null |
| Edges | Parent→child connections | Thin, curved lines. Muted color. No arrowheads. |
| Selected node | Focus indicator | Highlighted border or glow. Single selection only (initially). |

### Typography

- **Monospace** for values — JSON is code, values should look like code
- **Sans-serif** for labels/keys — readable at small sizes during zoom-out
- Minimum readable font size: 10px equivalent at 100% zoom
- At extreme zoom-out, nodes become rectangles without text (LOD)

### Node Design

```
┌──────────────────────────┐
│ "username"               │  ← Property node: key as label
│ ─────────────────────    │
│ "john_doe"               │  ← Value shown below or beside key
└──────────────────────────┘

┌──────────────────────────┐
│ { } config               │  ← Object node: brace icon + key name
│   5 properties           │  ← Child count as subtitle
└──────────────────────────┘

┌──────────────────────────┐
│ [ ] items                │  ← Array node: bracket icon + key name
│   3 items                │  ← Item count as subtitle
└──────────────────────────┘
```

---

## Interaction Patterns

### Navigation

| Action | Input | Effect |
|--------|-------|--------|
| Pan | Middle-mouse drag / two-finger trackpad drag / Space+drag | Move canvas |
| Zoom | Scroll wheel / pinch / Ctrl+Plus/Minus | Scale around cursor |
| Zoom to fit | Ctrl+0 or double-click canvas background | Fit all nodes in viewport |
| Center on node | Double-click node in future search | Animate pan to node |

### Selection

| Action | Input | Effect |
|--------|-------|--------|
| Select node | Left-click node | Highlight node, show context |
| Deselect | Click canvas background / Escape | Clear selection |
| Multi-select | Ctrl+Click (future) | Not in v1 — single selection only |

### Editing

| Action | Input | Trigger |
|--------|-------|---------|
| Edit scalar value | Double-click scalar node | Opens inline popup editor |
| Rename property | Double-click property key | Opens inline popup editor |
| Add child | Right-click node → "Add..." | Opens type picker popup |
| Delete node | Right-click → "Delete" / Delete key | Removes node + re-layout |
| Undo | Ctrl+Z | Reverse last patch |
| Redo | Ctrl+Shift+Z / Ctrl+Y | Re-apply last undone patch |

### Popup Editor

- Appears anchored near the selected node (below or to the right)
- Single-line text input for scalars and property names
- Auto-sized to content width (minimum 120px, maximum 400px)
- Submit: Enter
- Cancel: Escape (reverts to original value)
- Loses focus → auto-submit (commit the edit)
- No separate "Save" button — edits commit on Enter/blur

---

## Layout Rules

### Tree Direction

- **Left-to-right** — root on the left, children expand rightward
- Consistent with reading direction for English/Latin text
- Future: option for top-to-bottom

### Spacing

| Dimension | Value | Notes |
|-----------|-------|-------|
| Horizontal gap (parent→child) | 80px | Between edge of parent and left edge of child |
| Vertical gap (sibling→sibling) | 16px | Between bottom of one sibling and top of next |
| Node padding (internal) | 12px horizontal, 8px vertical | Content inset from border |
| Minimum node width | 80px | Even for short labels |
| Maximum node width | 300px | Truncate with ellipsis |

### Edge Routing

- Bezier curves from right edge of parent to left edge of child
- Control points placed at horizontal midpoint for smooth S-curves
- No overlapping edges (layout guarantees non-overlapping subtrees)

---

## File Handling

| Scenario | Behavior |
|----------|----------|
| Open file | System file dialog, filter: `*.json` |
| Drag-and-drop | Accept `.json` files dropped on canvas |
| Autosave | Debounced (2s after last edit), atomic write |
| Save blocked | If duplicate keys or parse errors → show banner, disable save |
| Unsaved indicator | Title bar shows `*` or dot indicator |
| Empty file | Show empty canvas with "Drop a JSON file here" prompt |
| Parse error | Show error panel with line/column, hide map |

---

## Keyboard Shortcuts (v1)

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open file |
| Ctrl+S | Save (manual) |
| Ctrl+Z | Undo |
| Ctrl+Shift+Z | Redo |
| Delete / Backspace | Delete selected node |
| Escape | Deselect / cancel popup |
| Ctrl+0 | Zoom to fit |
| Ctrl+Plus | Zoom in |
| Ctrl+Minus | Zoom out |
| F2 | Edit selected node (open popup) |

---

## Accessibility (Baseline)

- All interactive elements keyboard-reachable
- High contrast mode support (future)
- Minimum touch target: 32x32px for clickable nodes
- Focus ring visible on selected node

---

## Anti-Patterns (What Nexu is NOT)

- Not a text editor with a preview pane — the map *is* the editor
- Not a form-based JSON editor — no key-value table grids
- Not a tree-view with expand/collapse — all nodes visible, use zoom for overview
- Not a collaborative/real-time editor — single-user, local files
- No tabs, no sidebars, no toolbars (v1) — zen mode means minimal UI
