# Features Research — Nexu Inline Editing Milestone

**Analysis:** Ecosystem features for a JSON mind-map editor adding inline editing to an existing Avalonia app.
**Confidence:** HIGH

---

## Table Stakes

| Feature | Why Expected | Complexity |
|---------|--------------|------------|
| Double-click to enter edit mode | Universal convention in XMind, MindNode, Miro, draw.io | Low |
| Enter to commit | Universal across every inline editor | Low |
| Escape to cancel | Universal discard without side effects | Low |
| Blur (click-away) to commit | Users expect form-field semantics | Low |
| Select-all on activate | Standard in mind-map tools; lets user immediately retype | Low |
| Visual distinction key vs value editing | JSON semantics differ; users need the cue | Low |
| Delete node via keyboard (Del on selection) | Expected in every tree/graph editor | Low |
| Undo/redo of inline edits | Anyone who has mistyped expects this | Low (already implemented) |
| Validation: empty key blocked | JSON requires non-empty keys | Medium |
| Validation: duplicate key blocked | JSON semantics; save-block exists at persistence layer already | Medium |
| Add child property gesture | Every tree editor has this (Notion, Obsidian, XMind) | Medium |

## Differentiators

| Feature | Value Proposition | Complexity |
|---------|-------------------|------------|
| Auto-hide chrome (zen mode) | Controls fade idle, reappear on hover — core brand | Medium |
| Type inference on commit | `"42"` → `42`, `"true"` → `true`, `"null"` → `null` | Medium |
| Tab to commit + move to next sibling | Spreadsheet-style; speeds config editing | Medium |
| Inline type indicator badge (hover) | Helps users understand JSON types without raw text | Low |
| Keyboard-only CRUD | Full workflow without mouse; arrows + Enter/F2 + Del | Medium |

## Anti-Features

| Anti-Feature | Reason |
|--------------|--------|
| Side panel / property inspector | PROJECT.md explicit out of scope; breaks zen |
| Modal dialogs for editing | Disruptive; TextBox overlay is correct |
| Persistent toolbar (Add/Delete buttons) | Clutter; use hover-reveal per node |
| Multi-select editing | Complex conflict resolution; not needed for config files |
| Find/replace | Scope creep in v1 |
| Large file support (>1000 nodes) | PROJECT.md explicit out of scope |
| Light theme | Single aesthetic is zen intent |

---

## UX Contract for Inline Editing

### Activation
- Double-click on key node → RenameProperty mode
- Double-click on scalar value node → SetScalarValue mode
- F2 / Enter on focused (selected) node → same as double-click
- Single-click = select only, never activates edit

### While Editing
- TextBox overlays node label at node bounding box
- Text pre-filled, all text selected on activation
- Canvas pan/zoom suppressed while TextBox has focus
- Global keyboard shortcuts (undo, delete) suppressed while editing

### Commit
- Enter → fire EditIntent, dismiss TextBox, node re-renders
- Blur → same as Enter
- No-op guard: if text unchanged from original, treat as cancel

### Cancel
- Escape → dismiss, no EditIntent, node unchanged

### Validation
- Empty key → block commit, red border, "Key cannot be empty"
- Duplicate key → block commit, red border, "Key already exists"
- Scalar type coercion: attempt number → bool → null before defaulting to string; yellow warning (not hard block) if coercion fails

---

## Feature Dependencies

```
Double-click activation
  → TextBox overlay at canvas-local node coordinates
  → Commit on Enter/blur
      → EditIntent dispatch (RenameProperty | SetScalarValue)  ← already implemented
          → TextPatch → DocumentEditor → EditHistory            ← already implemented

Add child property
  → Same commit flow but EditIntent.AddProperty
  → Immediately activate new node's key TextBox after creation

Delete node
  → EditIntent.RemoveNode (already implemented)
  → Confirm only if node has children (object/array with content)

Auto-hide chrome
  → Mouse idle timer + hover detection
  → Opacity animation on overlay controls
  → Must remain visible during active edit
```

---

## MVP Priorities (this milestone)

**Must ship:**
1. Double-click → TextBox overlay (key and scalar value nodes)
2. Enter commit / Escape cancel / blur commit
3. Validation: empty key, duplicate key, no-op guard
4. Add child property inline (with immediate activation of new node)
5. Delete node via Del key
6. Auto-hide chrome (fade idle controls)

**Defer to follow-on:**
- Tab/Shift+Tab sibling navigation
- Type coercion (ship "always string", upgrade later)
- Soft-wrap for long values (layout engine change, separate issue)
- Inline type indicator badge (cosmetic)
