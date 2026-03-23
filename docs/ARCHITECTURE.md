# Nexu — Architecture

> Authoritative reference for how the system is structured and why.

---

## High-Level Data Flow

```
┌─────────┐     ┌───────────┐     ┌───────────┐     ┌──────────┐     ┌──────────┐
│ Raw JSON │────►│ CST Parse │────►│ NodeGraph │────►│  Layout  │────►│  Canvas  │
│  string  │     │ (indices) │     │ (domain)  │     │ (x,y,w,h)│     │ (render) │
└─────────┘     └───────────┘     └───────────┘     └──────────┘     └──────────┘
     ▲                                                                     │
     │                                                                     │
     │           ┌───────────┐     ┌───────────┐                           │
     └───────────│ TextPatch │◄────│EditIntent  │◄─────────────────────────┘
                 │ (surgical)│     │ (semantic) │         user interaction
                 └───────────┘     └───────────┘
```

**One-way pipeline, one feedback loop.** Data flows left→right for display, right→left for edits.

---

## Layer Descriptions

### 1. Parsing.Json — CST Parser

**Responsibility:** Parse a raw JSON string into a Concrete Syntax Tree where every node knows its exact character positions in the source text.

**Key types:**
- `RawDocument` — immutable snapshot: `Text`, `Revision`, `FilePath`
- `CstNode` — base for all CST nodes
  - `CstObject { Start, End, Properties[] }`
  - `CstProperty { Start, End, Key, Value }`
  - `CstArray { Start, End, Items[] }`
  - `CstValue { Start, End, RawText, ValueKind }` (string, number, bool, null)
- `ParseResult { Root, Diagnostics[] }` — always returns, even on error
- `Diagnostic { Message, Start, End, Line, Column, Severity }`

**Design decisions:**
- Recursive-descent parser (no parser generator — we need exact index control)
- Preserves whitespace implicitly via Start/End gaps
- Duplicate key detection happens during parse, not as a separate pass
- Parser never throws — errors go into `Diagnostics`

**No dependencies.** This is the leaf layer.

---

### 2. Domain — NodeGraph

**Responsibility:** Provide a clean, editor-friendly graph model decoupled from JSON syntax details.

**Key types:**
- `NodeId` — strongly-typed ID (wraps `int` or `Guid`)
- `NodeType` — enum: `Object`, `Property`, `Array`, `Scalar`
- `Node { Id, Type, Label, ParentId?, ChildIds[], CstStart, CstEnd }`
- `NodeGraph { Nodes, RootId }`

**Design decisions:**
- `Node` keeps `CstStart`/`CstEnd` so we can map edits back to CST positions
- Flat dictionary of nodes (not a recursive tree) — fast lookup by ID
- `Label` = property key name for properties, type indicator for objects/arrays, value text for scalars

**Depends on:** Parsing.Json (for mapping)

---

### 3. Layout — Deterministic Positioning

**Responsibility:** Given a NodeGraph, produce absolute (x, y) positions for every node and edges between parent/child.

**Key types:**
- `PositionedNode { NodeId, X, Y, Width, Height }`
- `Edge { FromId, ToId, Waypoints[] }`
- `LayoutResult { Nodes[], Edges[] }`

**Algorithm:** Left-to-right tree layout (Reingold-Tilford variant or simpler Tidy Tree).

**Design decisions:**
- Deterministic: same input → same output, always
- No animation state — layout is a pure function
- Width/Height estimated from label text length (simple heuristic, not font metrics)
- Array children laid out vertically under parent, same as object children

**Depends on:** Domain

---

### 4. App — Avalonia UI

**Responsibility:** Render the laid-out tree, handle user interaction, orchestrate the pipeline.

**Key components:**
- `CanvasHostControl` — custom Avalonia control, owner-draw via `Render(DrawingContext)`
- `PanZoomBehavior` — translate + scale matrix from mouse/trackpad input
- `SelectionState` — tracks which `NodeId` is currently selected
- `PopupEditor` — inline text editing overlays anchored to nodes
- `MainViewModel` — orchestrator: holds `RawDocument`, triggers parse→layout→render cycle

**Design decisions:**
- MVVM-light: ViewModel orchestrates but rendering is a custom control, not XAML data templates
- No reactive framework (no ReactiveUI) — simple `INotifyPropertyChanged` + commands
- Canvas redraws on: file load, edit commit, pan/zoom change, selection change
- File open via system dialog or drag-and-drop

**Depends on:** Domain, Layout, Parsing.Json, Persistence

---

### 5. Edit Pipeline — EditIntent → TextPatch

**Responsibility:** Convert semantic edit operations into surgical character-level text replacements.

**Key types:**
- `EditIntent` — base type
  - `RenameProperty { NodeId, NewName }`
  - `SetScalarValue { NodeId, NewValue }`
  - `AddProperty { ParentNodeId, Key, Value }`
  - `AddArrayItem { ParentNodeId, Value }`
  - `RemoveNode { NodeId }`
- `TextPatch { Start, Length, OldText, NewText }`

**Design decisions:**
- Patches reference character positions in the *current* raw text
- After applying a patch, the entire pipeline re-runs: parse → graph → layout → render
- Re-parsing validates that the patch produced valid JSON
- Multiple patches from one intent are applied in reverse-offset order (bottom-up) to avoid index shifting

**Lives in:** Parsing.Json (it operates on CST positions and raw text)

---

### 6. Persistence — Save & Load

**Responsibility:** Atomic file I/O with safety guarantees.

**Key types:**
- `AtomicFileWriter` — write to temp file, then `File.Replace()`
- `AutosaveService` — debounced save after edit commit
- `SaveGuard` — blocks save if `ParseResult.Diagnostics` contains errors or duplicate keys

**Design decisions:**
- Never write directly to the target file — always temp + replace
- Autosave is debounced (e.g., 2 seconds after last edit), not immediate
- Save returns a result type, never throws

**Depends on:** Parsing.Json (for diagnostics check)

---

## Cross-Cutting Concerns

### Undo/Redo
- `HistoryStack<TextPatch>` — push on commit, pop on undo
- Undo = apply reverse patch (`OldText` ↔ `NewText`, same position)
- Redo = re-apply forward patch
- History is cleared on file reload

### Error States
- **Parse error:** Show error screen, disable editing, keep last good render
- **Duplicate keys:** Show warning, block save, allow editing
- **Large file (>1000 lines):** Show warning dialog, user can proceed or cancel

---

## Project Dependency Graph

```
Parsing.Json  (no dependencies)
     │
     ▼
  Domain      (depends on Parsing.Json)
     │
     ▼
  Layout      (depends on Domain)
     │
     ▼
   App        (depends on all above + Persistence)
     │
  Persistence (depends on Parsing.Json)
```

**Rule:** Dependencies flow downward only. No circular references. Lower layers never reference upper layers.

---

## Testing Strategy

| Layer | Test Type | What's Tested |
|-------|-----------|---------------|
| Parsing.Json | Unit | Parse various JSON → verify CST structure + indices |
| Domain | Unit | CST → NodeGraph mapping, node relationships |
| Layout | Unit | NodeGraph → PositionedNode positions, determinism |
| Persistence | Integration | Atomic write (temp + replace), save blocking |
| App | Manual + UI tests | Rendering, interaction (future) |
| Edit Pipeline | Unit | EditIntent → TextPatch → re-parse roundtrip |

Every test follows **Arrange → Act → Assert**. No mocks for data transformations — use real objects.
