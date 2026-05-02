# Architecture

**Analysis Date:** 2026-05-02

## System Overview

```
┌──────────────────────────────────────────────────────────────┐
│                        App (Avalonia UI)                     │
│   MainWindow.axaml.cs        CanvasHostControl.cs            │
│   pan/zoom, selection, keyboard, file-open, save, undo/redo  │
└──────┬──────────────┬──────────────────────────┬─────────────┘
       │              │                          │
       ▼              ▼                          ▼
┌────────────┐  ┌───────────────┐   ┌────────────────────────┐
│  Editing   │  │    Layout     │   │      Persistence        │
│ EditIntent │  │ LayoutEngine  │   │ AtomicFileWriter        │
│ PatchGen.  │  │ PositionedNode│   │ AutoSaveManager         │
│ EditHistory│  │ Edge          │   └────────────────────────┘
└──────┬─────┘  └───────┬───────┘
       │                │
       ▼                ▼
┌──────────────────────────────────────────────────────────────┐
│                         Domain                               │
│   NodeGraph   NodeId   Node   NodeType                       │
│   CstToNodeGraphMapper                                        │
└──────────────────────┬───────────────────────────────────────┘
                       │
                       ▼
┌──────────────────────────────────────────────────────────────┐
│                      Parsing.Json                            │
│   RawDocument   JsonParser   CstNode hierarchy               │
│   ParseResult   Diagnostic                                   │
└──────────────────────────────────────────────────────────────┘
```

## Component Responsibilities

| Component | Responsibility | Key Files |
|-----------|----------------|-----------|
| Parsing.Json | Produce CST from raw text; emit Diagnostics | `src/Parsing.Json/JsonParser.cs`, `CstNode.cs` |
| Domain | Map CST → NodeGraph; structural node model | `src/Domain/CstToNodeGraphMapper.cs`, `NodeGraph.cs` |
| Layout | Deterministic left-to-right tree layout | `src/Layout/LayoutEngine.cs` |
| Editing | EditIntent → TextPatch; undo/redo history | `src/Editing/PatchGenerator.cs`, `EditHistory.cs`, `DocumentEditor.cs` |
| Persistence | Atomic file writes; debounced autosave | `src/Persistence/AtomicFileWriter.cs`, `AutoSaveManager.cs` |
| App | Avalonia host; canvas render; user input | `src/App/MainWindow.axaml.cs`, `src/App/Controls/CanvasHostControl.cs` |

## Pattern Overview

**Overall:** Layered pipeline — each layer consumes the output of the layer below and produces an immutable value. All core types are `record` (immutable). No DI container — layers referenced as static methods or `new`.

**Key Characteristics:**
- Raw text is the single source of truth; no round-trip serialization
- All CST, Domain, and Layout values are immutable (`ImmutableArray`, `record`)
- Each edit produces a complete new `EditResult` (doc + parse + graph + layout + patch) in one call
- `EditHistory` stores `EditResult` values; undo inverts the stored `TextPatch`
- Save is blocked when `ParseResult.HasErrors` is true

## Data Flow

### File Open Path

1. User clicks Open → `MainWindow.OpenJsonFileAsync()` (`src/App/MainWindow.axaml.cs:111`)
2. File text read from disk into `string text`
3. `RawDocument(text, Revision:1, FilePath)` constructed
4. `JsonParser.Parse(doc)` → `ParseResult(CstNode root, Diagnostics)`
5. If `ParseResult.HasErrors` → display errors in status bar, abort
6. `CstToNodeGraphMapper.Map(parseResult.Root)` → `NodeGraph`
7. `LayoutEngine.Compute(graph)` → `LayoutResult`
8. `Canvas.SetLayout(layout)` → `CanvasHostControl` builds node map, calls `InvalidateVisual()`
9. `EditHistory` reset to empty; `AutoSaveManager` (re)created with 2-second debounce

### Edit Path

1. App constructs an `EditIntent` subtype with positions drawn from the current CST
2. `DocumentEditor.Apply(currentDoc, intent)`:
   a. `PatchGenerator.Generate(intent, rawText)` → `TextPatch(Start, Length, OldText, NewText)`
   b. `patch.ApplyTo(document.Text)` — validates OldText match, returns new `string`
   c. `document with { Text = newText, Revision = Revision + 1 }` → new `RawDocument`
   d. Re-parse → Re-map → Re-layout
   e. Returns `EditResult(doc, parseResult, graph, layout, patch)`
3. `EditHistory.Push(result)` — stores on undo stack, clears redo stack
4. `MainWindow.ApplyResult(result)` — updates `_currentDoc`, calls `Canvas.SetLayout`, schedules autosave

### Undo Path

1. `EditHistory.Undo(currentDoc)`:
   a. Pops top `EditResult` from `_undoStack`
   b. `toUndo.Patch.Invert()` — swaps `OldText`/`NewText`, adjusts `Length`
   c. Applies inverted patch; full re-parse + re-map + re-layout
   d. Pushes original result onto `_redoStack`

### Redo Path

1. `EditHistory.Redo(currentDoc)`:
   a. Pops top `EditResult` from `_redoStack`
   b. Re-applies original patch; full re-parse + re-map + re-layout
   c. Pushes back onto `_undoStack`

## Key Abstractions

**RawDocument** (`src/Parsing.Json/RawDocument.cs`)
- `record RawDocument(string Text, int Revision, string? FilePath)`
- The canonical state carried through the entire pipeline. `Revision` is a monotonic counter.

**CstNode hierarchy** (`src/Parsing.Json/CstNode.cs`)
- `abstract record CstNode(int Start, int End)` — base; all nodes carry character-index span
- `CstObject`, `CstProperty`, `CstArray`, `CstValue` (scalar), `CstError` (parse failure)
- `CstProperty` carries `KeyStart`/`KeyEnd` spanning the raw key including quotes

**NodeId** (`src/Domain/NodeId.cs`)
- `readonly record struct NodeId(Guid Value)` — stable within one `NodeGraph`; new GUIDs on every parse

**NodeGraph** (`src/Domain/NodeGraph.cs`)
- `record NodeGraph(NodeId RootId, ImmutableDictionary<NodeId, Node> Nodes)` — structural model without position
- `Node(NodeId, NodeType, string? Label, NodeId? ParentId, ImmutableArray<NodeId> ChildIds)`

**EditIntent subtypes** (`src/Editing/EditIntent.cs`)
- `abstract record EditIntent` — discriminated union for all mutation kinds
- `RenameProperty`, `SetScalarValue`, `AddProperty`, `AddArrayItem`, `RemoveNode`
- All carry character-position fields drawn from the current CST

**TextPatch** (`src/Editing/TextPatch.cs`)
- `record TextPatch(int Start, int Length, string OldText, string NewText)`
- `ApplyTo(string)` — validates `OldText` match before applying; throws on mismatch
- `Invert()` — returns `new TextPatch(Start, NewText.Length, NewText, OldText)` for undo

**EditResult** (`src/Editing/DocumentEditor.cs`)
- `record EditResult(RawDocument Document, ParseResult ParseResult, NodeGraph Graph, LayoutResult Layout, TextPatch Patch)`
- The single value returned by every apply/undo/redo operation; complete post-edit state

**PositionedNode** (`src/Layout/PositionedNode.cs`)
- `record PositionedNode(NodeId, double X, double Y, double Width, double Height, string? Label)`
- Fixed dimensions: `Width=120`, `Height=40`, `HorizontalGap=60`, `VerticalGap=20`

## Persistence

**AtomicFileWriter** (`src/Persistence/AtomicFileWriter.cs`):
- Writes to `filePath + ".tmp"`, then `File.Move(..., overwrite: true)` — no corrupt partial writes

**AutoSaveManager** (`src/Persistence/AutoSaveManager.cs`):
- Debounced: each `Schedule()` cancels any in-flight timer and starts a new `Task.Delay` (2 seconds)
- On expiry, calls injected `Func<Task> saveCallback` marshalled via `Dispatcher.UIThread.Post`

## Save-Blocking on Parse Errors

Save is blocked in `SaveAsync` (`src/App/MainWindow.axaml.cs:98-109`):
- `if (_currentParseResult?.HasErrors == true)` → sets status bar "Save blocked: document has errors." and returns.
- `ParseResult.HasErrors` is true whenever `Diagnostics` is non-empty (covers `DuplicateKey` and `SyntaxError`).

## Architectural Constraints

- **Threading:** Avalonia UI thread owns all state. `AutoSaveManager` timer fires on thread-pool and marshals back via `Dispatcher.UIThread.Post`.
- **Global state:** None — no module-level singletons. All state is in `MainWindow` fields.
- **NodeId stability:** `NodeId` values are GUIDs minted during `CstToNodeGraphMapper.Map`. They do NOT persist across re-parses. `EditIntent` stores positions (character indices), not `NodeId`s.
- **Circular imports:** None — strict acyclic dependency: `App → Editing → Domain → Parsing.Json`; `App → Layout → Domain`; `App → Persistence`.

## Anti-Patterns

**Resolving NodeId across re-parses:** `NodeId` values from one `NodeGraph` do not exist in the next. Always read `NodeId` from the current `EditResult.Graph`, or navigate by CST position.

**Deserializing and re-serializing JSON for edits:** Round-trip serialization destroys formatting. Use `EditIntent` → `DocumentEditor.Apply` → surgical `TextPatch` on raw string.

**Writing files without AtomicFileWriter:** `File.WriteAllText` directly leaves a corrupt file on crash. Always use `AtomicFileWriter.Write`.

**Saving when HasErrors is true:** Overwrites a valid file with syntactically broken content. Always check `ParseResult.HasErrors` before writing.

---

*Architecture analysis: 2026-05-02*
