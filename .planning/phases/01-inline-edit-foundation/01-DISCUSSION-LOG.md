# Phase 1: Inline Edit Foundation - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-05-04
**Phase:** 1-Inline Edit Foundation
**Areas discussed:** Validation Feedback UX, Blur Commit Semantics, Test Boundary for UI Logic, Overlay Coordinate Translation

---

## Validation Feedback UX

**Q1: What should the visual error feedback look like (VAL-01/02)?**

| Option | Description | Selected |
|--------|-------------|----------|
| Red border only | TextBox gets red border; editor stays open. Minimal — zen. | ✓ |
| Red border + status bar message | Red border AND message in existing status bar. More informative. | |
| Status bar only | No border change; error in status bar only. Easy to miss. | |

**User's choice:** Red border only

---

**Q2: How does the error state clear?**

| Option | Description | Selected |
|--------|-------------|----------|
| Clears on next keystroke | As soon as user types any character, red border disappears. | ✓ |
| Clears on valid input only | Border stays red until value is actually valid. | |
| Clears on focus/blur | Persists until user leaves and re-enters the field. | |

**User's choice:** Clears on next keystroke

---

**Q3: Should the duplicate key check (VAL-02) be case-sensitive?**

| Option | Description | Selected |
|--------|-------------|----------|
| Case-sensitive | JSON spec; matches existing JsonParser behavior. | ✓ |
| Case-insensitive | Safer for config files but deviates from spec. | |

**User's choice:** Case-sensitive

---

## Blur Commit Semantics

**Q1: Double-clicking another node while the editor is open?**

| Option | Description | Selected |
|--------|-------------|----------|
| Commit + select new node | Commit current edit, then select the new node (spreadsheet-style). | ✓ |
| Commit only, no selection | Blur commits but the double-click on new node is consumed. | |
| Escape first, then select | Blur cancels the edit (like Escape) then selects new node. | |

**User's choice:** Commit + select new node

---

**Q2: Window loses focus (Cmd+Tab) while editor is open?**

| Option | Description | Selected |
|--------|-------------|----------|
| Commit | Treat window deactivation as blur — user work preserved. | ✓ |
| Cancel (Escape) | Discard the edit on window deactivation. | |
| Leave editor open | Do nothing; editor stays open when focus returns. | |

**User's choice:** Commit

---

**Q3: No-op guard — text changed then changed back?**

| Option | Description | Selected |
|--------|-------------|----------|
| String equality = no-op | Compare final to original; if identical, cancel — clean undo stack. | ✓ |
| Any commit action = real commit | Always commit if Enter/blur, even if unchanged. | |

**User's choice:** String equality = no-op

---

## Test Boundary for UI Logic

**Q1: Where should inline editor state logic live?**

| Option | Description | Selected |
|--------|-------------|----------|
| Extract to InlineEditorState class | Pure C# state machine, no Avalonia. MainWindow wires it to TextBox. | ✓ |
| Logic inline in MainWindow | All logic in code-behind. Simpler but untestable. | |
| Tests.App with Avalonia headless runner | Full UI test capability but heavy setup. | |

**User's choice:** Extract to InlineEditorState class

---

**Q2: What should InlineEditorState know about?**

| Option | Description | Selected |
|--------|-------------|----------|
| Strings + sibling key list only | Takes text strings and sibling keys; returns IsError, IsNoOp, ValidationResult. No Avalonia types. | ✓ |
| Knows about EditIntent types | Also constructs the EditIntent to keep intent construction out of MainWindow. | |
| Minimal: just validation | Only the validation check; everything else stays in MainWindow. | |

**User's choice:** Strings + sibling key list only

---

**Q3: New tests/Tests.App/ project or existing?**

| Option | Description | Selected |
|--------|-------------|----------|
| New tests/Tests.App/ project | Mirrors conventions of Tests.Editing, Tests.Domain, etc. | ✓ |
| Inline in tests/Tests.Editing/ | Co-located with editing tests; blurs project boundaries. | |

**User's choice:** New tests/Tests.App/ project

---

## Overlay Coordinate Translation

**Q1: How should MainWindow get the screen rect for a node?**

| Option | Description | Selected |
|--------|-------------|----------|
| CanvasHostControl.GetNodeScreenRect(NodeId) | Method applies current pan/zoom matrix; returns Rect in window coords. | ✓ |
| CanvasHostControl raises NodeActivated event with screen rect | Decoupled event; more plumbing. | |
| MainWindow reads pan/zoom properties and computes itself | Exposes PanOffset + ZoomScale; MainWindow does the math. | |

**User's choice:** CanvasHostControl exposes GetNodeScreenRect(NodeId)

---

**Q2: Which Avalonia panel type should host the overlay TextBox?**

| Option | Description | Selected |
|--------|-------------|----------|
| Canvas panel with Canvas.Left/Top | Absolute positioning; no layout interference. TextBox declared in AXAML with IsVisible="False". | ✓ |
| Grid with Margin-based positioning | Margin = Left, Top, 0, 0 from screen rect. Less idiomatic. | |
| OverlayLayer (Avalonia popup layer) | Built-in Z-order handling but more complex setup. | |

**User's choice:** Canvas panel with Canvas.Left/Top

---

**Q3: When user pans or zooms while editor is open?**

| Option | Description | Selected |
|--------|-------------|----------|
| Close editor on pan/zoom | Commit (if valid, non-no-op) and close overlay. No live tracking needed. | ✓ |
| Track the node (live update) | CanvasHostControl raises ViewChanged; MainWindow repositions TextBox. Complex. | |
| Block pan/zoom while editing | Suppress pointer scroll/drag events while `_isEditing` is true. | |

**User's choice:** Close editor on pan/zoom (commit + close)

---

## Claude's Discretion

None — all areas had explicit user decisions.

## Deferred Ideas

None — discussion stayed within phase scope.
