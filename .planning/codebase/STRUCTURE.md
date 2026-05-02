# Codebase Structure

**Analysis Date:** 2026-05-02

## Directory Layout

```
Nexu/
├── Nexu.sln
├── CLAUDE.md
├── src/
│   ├── App/                        # Avalonia entry point and UI
│   │   ├── Program.cs
│   │   ├── App.axaml / App.axaml.cs
│   │   ├── MainWindow.axaml / MainWindow.axaml.cs
│   │   ├── Controls/
│   │   │   └── CanvasHostControl.cs
│   │   ├── Nexu.App.csproj
│   │   └── app.entitlements        # macOS entitlements (untracked)
│   ├── Domain/                     # Structural node model
│   │   ├── NodeId.cs
│   │   ├── NodeType.cs
│   │   ├── Node.cs
│   │   ├── NodeGraph.cs
│   │   ├── CstToNodeGraphMapper.cs
│   │   └── Nexu.Domain.csproj
│   ├── Parsing.Json/               # JSON CST parser
│   │   ├── RawDocument.cs
│   │   ├── JsonParser.cs
│   │   ├── CstNode.cs
│   │   ├── ParseResult.cs
│   │   ├── Diagnostic.cs
│   │   └── Nexu.Parsing.Json.csproj
│   ├── Editing/                    # EditIntent → TextPatch; undo/redo
│   │   ├── EditIntent.cs
│   │   ├── PatchGenerator.cs
│   │   ├── TextPatch.cs
│   │   ├── DocumentEditor.cs
│   │   ├── EditHistory.cs
│   │   ├── IndentationDetector.cs
│   │   └── Nexu.Editing.csproj
│   ├── Layout/                     # Deterministic tree layout
│   │   ├── LayoutEngine.cs
│   │   ├── LayoutResult.cs
│   │   ├── PositionedNode.cs
│   │   ├── Edge.cs
│   │   ├── LayoutPoint.cs
│   │   └── Nexu.Layout.csproj
│   └── Persistence/                # Atomic file I/O; autosave
│       ├── AtomicFileWriter.cs
│       ├── AutoSaveManager.cs
│       └── Nexu.Persistence.csproj
└── tests/
    ├── Tests.Domain/
    │   └── CstToNodeGraphMapperTests.cs
    ├── Tests.Parsing.Json/
    │   └── JsonParserTests.cs
    ├── Tests.Editing/
    │   ├── AddArrayItemTests.cs
    │   ├── AddPropertyTests.cs
    │   ├── DocumentEditorTests.cs
    │   ├── EditHistoryTests.cs
    │   ├── EditIntentEqualityTests.cs
    │   ├── RemoveNodeTests.cs
    │   ├── RenamePropertyTests.cs
    │   ├── SetScalarValueTests.cs
    │   └── TextPatchTests.cs
    ├── Tests.Layout/
    │   └── LayoutEngineTests.cs
    └── Tests.Persistence/
        ├── AtomicFileWriterTests.cs
        └── AutoSaveManagerTests.cs
```

## Directory Purposes

**`src/Parsing.Json/`**
- Purpose: Parse a `RawDocument` into a CST. Detect `DuplicateKey` and `SyntaxError` diagnostics.
- Contains: `JsonParser` (one static `Parse` method), full `CstNode` hierarchy, `RawDocument`, `ParseResult`, `Diagnostic`
- Key files: `JsonParser.cs` (entry point), `CstNode.cs` (all node types), `RawDocument.cs` (source-of-truth carrier)

**`src/Domain/`**
- Purpose: Structural graph representation of the JSON document. CST → NodeGraph mapping.
- Contains: `NodeId`, `NodeType` enum, `Node` record, `NodeGraph` record, `CstToNodeGraphMapper` (static)
- Key files: `CstToNodeGraphMapper.cs` (mapping logic), `NodeGraph.cs` + `Node.cs` (model)

**`src/Layout/`**
- Purpose: Deterministic left-to-right tree layout. Takes a `NodeGraph`, returns pixel coordinates for nodes and edges.
- Contains: `LayoutEngine` (static `Compute`), `LayoutResult`, `PositionedNode`, `Edge`, `LayoutPoint`
- Key files: `LayoutEngine.cs` (all layout logic — recursive subtree positioning)

**`src/Editing/`**
- Purpose: Translate user edit intent into surgical character-level patches. Maintain undo/redo history.
- Contains: `EditIntent` subtypes, `PatchGenerator` (static), `TextPatch` (apply + invert), `DocumentEditor` (static full-pipeline entry), `EditHistory` (two stacks), `IndentationDetector` (static helper)
- Key files: `EditIntent.cs`, `PatchGenerator.cs`, `DocumentEditor.cs`, `EditHistory.cs`

**`src/Persistence/`**
- Purpose: Write-through file persistence. Atomic writes. Debounced autosave.
- Contains: `AtomicFileWriter` (static — write-to-tmp then move), `AutoSaveManager` (debounced timer)
- Key files: `AtomicFileWriter.cs`, `AutoSaveManager.cs`

**`src/App/`**
- Purpose: Avalonia host. Main window orchestrates all layers. Canvas control handles rendering, pan/zoom, selection.
- Contains: `Program` (entry, crash logger), `App` (AppBuilder), `MainWindow` (all app logic + layer wiring), `CanvasHostControl` (immediate-mode renderer + pointer input)
- Key files: `Program.cs`, `MainWindow.axaml.cs`, `Controls/CanvasHostControl.cs`

## Key Source Files Per Project

### `src/Parsing.Json/`

| File | Purpose |
|------|---------|
| `RawDocument.cs` | `record RawDocument(string Text, int Revision, string? FilePath)` |
| `JsonParser.cs` | Static `Parse(RawDocument)` → `ParseResult`; inner `ParserImpl` class |
| `CstNode.cs` | Full CST hierarchy: `CstObject`, `CstProperty`, `CstArray`, `CstValue`, `CstError` |
| `ParseResult.cs` | `record ParseResult(CstNode Root, ImmutableArray<Diagnostic> Diagnostics)` + `HasErrors` |
| `Diagnostic.cs` | `enum DiagnosticKind { DuplicateKey, SyntaxError }` + `Diagnostic` record |

### `src/Domain/`

| File | Purpose |
|------|---------|
| `NodeId.cs` | `readonly record struct NodeId(Guid Value)` |
| `NodeType.cs` | `enum NodeType { Object, Property, Array, Scalar }` |
| `Node.cs` | `record Node(NodeId, NodeType, string? Label, NodeId? ParentId, ImmutableArray<NodeId> ChildIds)` |
| `NodeGraph.cs` | `record NodeGraph(NodeId RootId, ImmutableDictionary<NodeId, Node> Nodes)` |
| `CstToNodeGraphMapper.cs` | Static `Map(CstNode)` → `NodeGraph`; recursive DFS via `MapNode` |

### `src/Layout/`

| File | Purpose |
|------|---------|
| `LayoutEngine.cs` | Static `Compute(NodeGraph)` → `LayoutResult`; recursive `PositionSubtree` |
| `LayoutResult.cs` | `record LayoutResult(ImmutableArray<PositionedNode>, ImmutableArray<Edge>)` |
| `PositionedNode.cs` | `record PositionedNode(NodeId, X, Y, Width, Height, Label?)` |
| `Edge.cs` | `record Edge(NodeId From, NodeId To, ImmutableArray<LayoutPoint> Waypoints)` |
| `LayoutPoint.cs` | `readonly record struct LayoutPoint(double X, double Y)` |

### `src/Editing/`

| File | Purpose |
|------|---------|
| `EditIntent.cs` | Abstract base + 5 sealed subtypes: `RenameProperty`, `SetScalarValue`, `AddProperty`, `AddArrayItem`, `RemoveNode` |
| `TextPatch.cs` | `record TextPatch(Start, Length, OldText, NewText)` + `ApplyTo` + `Invert` |
| `PatchGenerator.cs` | Static `Generate(EditIntent, string)` → `TextPatch`; one private method per intent type |
| `DocumentEditor.cs` | `EditResult` record; static `Apply(RawDocument, EditIntent)` → `EditResult` |
| `EditHistory.cs` | `_undoStack` / `_redoStack`; `Push`, `Undo(RawDocument)`, `Redo(RawDocument)` |
| `IndentationDetector.cs` | Static `Detect(rawText, containerStart, firstChildStart)` → indentation string |

### `src/Persistence/`

| File | Purpose |
|------|---------|
| `AtomicFileWriter.cs` | Static `Write(filePath, content)` — write to `.tmp`, then `File.Move` |
| `AutoSaveManager.cs` | `Schedule()` debounce; `IDisposable` cancel-on-dispose |

### `src/App/`

| File | Purpose |
|------|---------|
| `Program.cs` | `[STAThread] Main`; crash logger to temp dir; `BuildAvaloniaApp()` |
| `App.axaml.cs` | Avalonia `Application` subclass |
| `MainWindow.axaml.cs` | All app state (`_currentDoc`, `_history`, `_autoSave`); `OpenJsonFileAsync`, `SaveAsync`, `Undo`, `Redo`, `ApplyResult` |
| `Controls/CanvasHostControl.cs` | Custom `Control`; immediate-mode `Render`; pan/zoom via pointer + wheel; node hit-test for selection |

## Test Project Mapping

| Test Project | Covers | Test Files |
|-------------|--------|------------|
| `tests/Tests.Parsing.Json/` | `src/Parsing.Json/` | `JsonParserTests.cs` |
| `tests/Tests.Domain/` | `src/Domain/` | `CstToNodeGraphMapperTests.cs` |
| `tests/Tests.Layout/` | `src/Layout/` | `LayoutEngineTests.cs` |
| `tests/Tests.Editing/` | `src/Editing/` | `TextPatchTests.cs`, `RenamePropertyTests.cs`, `SetScalarValueTests.cs`, `AddPropertyTests.cs`, `AddArrayItemTests.cs`, `RemoveNodeTests.cs`, `DocumentEditorTests.cs`, `EditHistoryTests.cs`, `EditIntentEqualityTests.cs` |
| `tests/Tests.Persistence/` | `src/Persistence/` | `AtomicFileWriterTests.cs`, `AutoSaveManagerTests.cs` |

No test project covers `src/App/` — UI and integration paths are entirely untested at the automated level.

## Where to Add New Code

**New EditIntent type:**
- Add sealed record to `src/Editing/EditIntent.cs`
- Add `Generate*` method in `src/Editing/PatchGenerator.cs` and a case to the `Generate` switch
- Add tests in `tests/Tests.Editing/` following the pattern of `RenamePropertyTests.cs`

**New CST node type:**
- Add to `src/Parsing.Json/CstNode.cs`
- Add a case in `src/Parsing.Json/JsonParser.cs` `ParseValue`
- Add a case in `src/Domain/CstToNodeGraphMapper.cs` `MapNode`

**New layout variant:**
- Implement in `src/Layout/LayoutEngine.cs`; update `LayoutResult` if new output values needed

**New persistence strategy:**
- Add to `src/Persistence/`; keep `AtomicFileWriter` as the low-level primitive

**New controls:**
- Add to `src/App/Controls/`

---

*Structure analysis: 2026-05-02*
