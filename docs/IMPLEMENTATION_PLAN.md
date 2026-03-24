# Nexu — Implementation Plan

> Living document. Updated as work progresses.

---

## Phase 0: Foundation (EPIC 1)

| #   | Task                                                 | Status | Branch | Notes                                              |
|-----|------------------------------------------------------|--------|--------|----------------------------------------------------|
| 0.1 | Create `Nexu.sln` with 9 projects (5 src + 4 test)   | DONE   |        | .NET 10, Avalonia, nullable, TreatWarningsAsErrors |
| 0.2 | `Directory.Build.props` — shared settings, analyzers | DONE   |        | Central package management                         |
| 0.3 | `.editorconfig` — formatting rules                   | DONE   |        |                                                    |
| 0.4 | GitHub Actions CI (build + test + warn-as-error)     | DONE   |        | Matrix: macOS + Windows                            |
| 0.5 | Verify green build on both platforms                 | DONE   |        | Gate for Phase 1                                   |

**Exit criteria:** `dotnet build` and `dotnet test` pass on macOS + Windows with zero warnings.

---

## Phase 1: Core Pipeline — Parse → Model → Layout (EPICs 2–4)

### 1A — JSON CST Parser (EPIC 2)

| #    | Task                                                               | Status | Notes                                     |
|------|--------------------------------------------------------------------|--------|-------------------------------------------|
| 1A.1 | `RawDocument` record (`Text`, `Revision`, `FilePath`)              | DONE   |                                           |
| 1A.2 | CST node types: `CstObject`, `CstProperty`, `CstArray`, `CstValue` | DONE   | Each carries `Start` + `End` char indices |
| 1A.3 | Recursive-descent parser → CST                                     | DONE   | TDD: empty obj, nested, arrays, scalars   |
| 1A.4 | Duplicate key detection → `Diagnostic` list                        | DONE   |                                           |
| 1A.5 | Syntax error reporting (line + column)                             | DONE   |                                           |

### 1B — Domain Node Graph (EPIC 3)

| #    | Task                                                      | Status | Notes                                      |
|------|-----------------------------------------------------------|--------|--------------------------------------------|
| 1B.1 | `NodeId` (strongly-typed wrapper)                         | TODO   |                                            |
| 1B.2 | `NodeType` enum (`Object`, `Property`, `Array`, `Scalar`) | TODO   |                                            |
| 1B.3 | `NodeGraph` — nodes with parent/children relationships    | TODO   |                                            |
| 1B.4 | `CstToNodeGraphMapper`                                    | TODO   | TDD: round-trip CST→Graph→verify structure |

### 1C — Layout Engine (EPIC 4)

| #    | Task                                                  | Status | Notes                                    |
|------|-------------------------------------------------------|--------|------------------------------------------|
| 1C.1 | `PositionedNode` record (NodeId, X, Y, Width, Height) | TODO   |                                          |
| 1C.2 | `Edge` record (From, To, waypoints)                   | TODO   |                                          |
| 1C.3 | Left-to-right tree layout algorithm                   | TODO   | Deterministic — same input = same output |
| 1C.4 | Array-specific layout handling                        | TODO   |                                          |

**Exit criteria:** Given a JSON string, produce a fully laid-out tree of positioned nodes with edges. All pure logic, no UI.

---

## Phase 2: Rendering — Avalonia Canvas (EPIC 5)

| #   | Task                                          | Status | Notes                         |
|-----|-----------------------------------------------|--------|-------------------------------|
| 2.1 | `CanvasHostControl` — custom Avalonia control | DONE   | Renders PositionedNode + Edge |
| 2.2 | Pan (mouse drag / trackpad)                   | DONE   |                               |
| 2.3 | Zoom (scroll wheel / pinch)                   | DONE   |                               |
| 2.4 | Selection state (click node → selected)       | DONE   |                               |
| 2.5 | Wire up: File → Parse → Layout → Render       | DONE   | End-to-end read-only viewer   |

**Exit criteria:** Open a JSON file and see a pannable/zoomable mind-map. Read-only.

---

## Phase 3: Editing — Intents → TextPatch (EPICs 6–7)

| #   | Task                                                 | Status | Notes                                                                 |
|-----|------------------------------------------------------|--------|-----------------------------------------------------------------------|
| 3.1 | `EditIntent` base + 5 subtypes                       | DONE   | RenameProperty, SetScalarValue, AddProperty, AddArrayItem, RemoveNode |
| 3.2 | `TextPatch` record (Start, Length, OldText, NewText) | DONE   |                                                                       |
| 3.3 | Patch generator: RenameProperty                      | DONE   | TDD: verify raw text after patch                                      |
| 3.4 | Patch generator: SetScalarValue                      | DONE   |                                                                       |
| 3.5 | Patch generator: AddProperty                         | DONE   | Handle indentation, trailing commas                                   |
| 3.6 | Patch generator: AddArrayItem                        | DONE   |                                                                       |
| 3.7 | Patch generator: RemoveNode                          | DONE   | Handle comma cleanup                                                  |
| 3.8 | Apply patch → re-parse → re-layout → re-render       | DONE   | Full round-trip                                                       |

**Exit criteria:** Edits produce correct, format-preserving text patches. Reparse validates patch correctness.

---

## Phase 4: Persistence & History (EPICs 8–9)

| #   | Task                                           | Status | Notes                             |
|-----|------------------------------------------------|--------|-----------------------------------|
| 4.1 | Atomic file writer (write temp → File.Replace) | TODO   |                                   |
| 4.2 | Autosave on commit                             | TODO   | Debounced, not on every keystroke |
| 4.3 | Block save on duplicate keys / syntax errors   | TODO   |                                   |
| 4.4 | Patch history stack                            | TODO   |                                   |
| 4.5 | Undo (Ctrl+Z) — reverse patch                  | TODO   |                                   |
| 4.6 | Redo (Ctrl+Shift+Z) — re-apply patch           | TODO   |                                   |

**Exit criteria:** Changes persist atomically. Full undo/redo chain works.

---

## Phase 5: Popup Editors (EPIC 10)

| #   | Task                                          | Status | Notes |
|-----|-----------------------------------------------|--------|-------|
| 5.1 | Popup anchor positioning (near selected node) | TODO   |       |
| 5.2 | Scalar value editor popup                     | TODO   |       |
| 5.3 | Property name editor popup                    | TODO   |       |
| 5.4 | "Add child" popup (choose type → add)         | TODO   |       |

**Exit criteria:** Users can edit values, rename keys, and add children through popups.

---

## Phase 6: Error Handling & Polish (EPIC 11)

| #   | Task                                        | Status | Notes                              |
|-----|---------------------------------------------|--------|------------------------------------|
| 6.1 | Large file warning (>1000 lines)            | TODO   |                                    |
| 6.2 | Syntax error screen                         | TODO   | Show errors instead of broken tree |
| 6.3 | Edge cases: empty file, `null`, bare scalar | TODO   |                                    |

---

## Dependency Graph

```
Phase 0 (Foundation)
    │
    ▼
Phase 1A (CST Parser) ──► Phase 1B (NodeGraph) ──► Phase 1C (Layout)
    │                                                      │
    │                                                      ▼
    │                                               Phase 2 (Rendering)
    │                                                      │
    ▼                                                      ▼
Phase 3 (Edit Intents + TextPatch) ◄────────────── needs both
    │
    ├──► Phase 4 (Persistence + Undo/Redo)
    │
    └──► Phase 5 (Popup Editors)
              │
              ▼
         Phase 6 (Polish)
```

---

## Principles

1. **TDD-first** — every task starts with a failing test
2. **One branch per task** — squash-merge to main
3. **No code without tests** — coverage is a side effect of TDD, not a goal
4. **Pure logic first, UI last** — Phases 1–4 are testable without Avalonia
5. **Keep PRs small** — ≤3 files changed per PR when possible
