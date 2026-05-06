# Requirements: Nexu

**Defined:** 2026-05-03
**Core Value:** Open a JSON file, edit its structure as a mind-map, save — the full loop must work without friction.

## v1 Requirements

### Editing — Inline Edit Control

- [ ] **EDIT-01**: User can double-click a property key node to rename it inline
- [ ] **EDIT-02**: User can double-click a scalar value node to edit its value inline
- [ ] **EDIT-03**: Inline editor activates with full text selected (immediate replace)
- [ ] **EDIT-04**: Enter commits the edit; Escape cancels with no change
- [ ] **EDIT-05**: Clicking away (blur) commits the edit
- [ ] **EDIT-06**: No-op guard — if text unchanged, treated as cancel

### Editing — CRUD Gestures

- [ ] **CRUD-01**: User can add a new child property to any object node
- [ ] **CRUD-02**: After adding a child, its key field immediately opens for inline editing
- [ ] **CRUD-03**: User can delete a node via the Del key when a node is selected
- [ ] **CRUD-04**: Delete of a node with children requires confirmation

### Editing — Validation

- [ ] **VAL-01**: Empty key is blocked at commit with visual error feedback
- [ ] **VAL-02**: Duplicate sibling key is blocked at commit with visual error feedback

### Editing — History

- [ ] **HIST-01**: All inline edits are undoable via Cmd+Z / Ctrl+Z
- [ ] **HIST-02**: Undone edits are redoable via Cmd+Shift+Z / Ctrl+Y

### UI — Zen Chrome

- [ ] **ZEN-01**: UI controls auto-hide on mouse idle; reappear on hover or focus
- [ ] **ZEN-02**: Chrome state driven by editing state flag, not focus events (no flicker)

## v2 Requirements

### Format Support

- YAML file format support
- TOML file format support

### Editing Enhancements

- Tab/Shift+Tab sibling navigation while editing (spreadsheet-style)
- Type coercion on commit (string `"42"` → number `42`, `"true"` → `true`, `"null"` → `null`)
- Inline type indicator badge on hover

## Out of Scope

| Feature | Reason |
|---------|--------|
| Side panel / property inspector | Breaks zen aesthetic; PROJECT.md explicit exclusion |
| Large file support (>1000 nodes) | Not the target; performance optimization is separate concern |
| Light theme | Single aesthetic is intentional; zen design decision |
| Multi-select editing | Complex conflict resolution; not needed for config files |
| Find/replace | Scope creep in v1 |
| Modal dialogs for editing | Disruptive; TextBox overlay is the correct pattern |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| EDIT-01 | Phase 1 | Pending |
| EDIT-02 | Phase 1 | Pending |
| EDIT-03 | Phase 1 | Pending |
| EDIT-04 | Phase 1 | Pending |
| EDIT-05 | Phase 1 | Pending |
| EDIT-06 | Phase 1 | Pending |
| VAL-01 | Phase 1 | Pending |
| VAL-02 | Phase 1 | Pending |
| HIST-01 | Phase 1 | Pending |
| HIST-02 | Phase 1 | Pending |
| CRUD-01 | Phase 2 | Pending |
| CRUD-02 | Phase 2 | Pending |
| CRUD-03 | Phase 2 | Pending |
| CRUD-04 | Phase 2 | Pending |
| ZEN-01 | Phase 3 | Pending |
| ZEN-02 | Phase 3 | Pending |
