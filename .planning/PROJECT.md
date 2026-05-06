# Nexu

## What This Is

Nexu is a cross-platform desktop app (Avalonia, .NET 10) that opens a JSON file and renders it as an interactive mind-map. The user clicks any node to edit it inline — renaming keys, changing values, adding children, deleting nodes — and saves. No distracting chrome: controls auto-hide while navigating. Small JSON config files are the primary target. YAML and TOML will follow once the JSON workflow is solid.

## Core Value

Open a JSON file, edit its structure as a mind-map, save — the full loop must work without friction.

## Requirements

### Validated

- ✓ JSON CST parser with start/end indices and diagnostics — Phase 1
- ✓ NodeGraph domain model (NodeId, NodeType, parent/children relationships) — Phase 2
- ✓ Deterministic left-to-right layout engine producing PositionedNode + Edge values — Phase 3
- ✓ Avalonia canvas with pan/zoom, selection, keyboard navigation — Phase 4
- ✓ EditIntent → TextPatch pipeline (PatchGenerator, DocumentEditor) — Phase 4
- ✓ EditHistory with undo/redo stack — Phase 4
- ✓ Atomic file writes + debounced autosave — Phase 4
- ✓ Save blocked on parse errors / duplicate keys — Phase 4

### Active

- [ ] User can double-click a property key node to rename it inline
- [ ] User can double-click a scalar value node to edit its value inline
- [ ] User can add a new child property to any object node
- [ ] User can delete a property or array item node
- [ ] UI chrome auto-hides while navigating; reappears on hover/focus
- [ ] All edits are undoable/redoable via Cmd+Z / Cmd+Shift+Z

### Out of Scope

- YAML file support — deferred to v2; validate JSON workflow first
- TOML file support — deferred to v2; same rationale
- Large files (>1000 nodes) — performance optimization not a v1 concern
- Light theme — single aesthetic is part of the zen intent
- Side panel / property inspector — inline editing is the design, panel adds chrome

## Context

Four phases have been shipped:
- **Parsing.Json** — JSON → CST with character-level start/end indices per node
- **Domain** — CST → NodeGraph mapper; all types are immutable records
- **Layout** — LayoutEngine produces deterministic tree layout; stateless
- **App + Editing + Persistence** — Avalonia canvas, EditIntent system, undo/redo, atomic save

The editing pipeline (PatchGenerator, DocumentEditor, EditHistory) is implemented and tested. The gap is UI wiring: no inline edit control exists on the canvas yet. Edits flow through EditIntent typed cases: RenameProperty, SetScalarValue, AddProperty, AddArrayItem, RemoveNode.

Raw text is always the source of truth — edits produce character-level TextPatch values applied to the raw string, never round-trip through serialization.

## Constraints

- **Stack**: .NET 10, Avalonia 11.3.12 — no framework changes
- **Code quality**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, nullable enabled — zero warnings at CI
- **Architecture**: Raw text source of truth; immutable value pipeline; no DI container
- **Dev model**: TDD-first; one issue → one branch → one PR → squash merge

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Raw text as source of truth | Avoids round-trip serialization bugs; preserves formatting | ✓ Good |
| Immutable record pipeline | Simplifies undo/redo (store full EditResult); no mutation bugs | ✓ Good |
| EditIntent typed cases | Decouples UI gestures from text patch generation; testable | — Pending |
| Inline editing (no side panel) | Fewer UI layers = less chrome = more zen | — Pending |
| YAML/TOML deferred to v2 | Prove the interaction model on JSON before adding parsers | — Pending |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `/gsd-transition`):
1. Requirements invalidated? → Move to Out of Scope with reason
2. Requirements validated? → Move to Validated with phase reference
3. New requirements emerged? → Add to Active
4. Decisions to log? → Add to Key Decisions
5. "What This Is" still accurate? → Update if drifted

**After each milestone** (via `/gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check — still the right priority?
3. Audit Out of Scope — reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-05-03 after initialization*
