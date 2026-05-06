# Roadmap: Nexu — Inline Editing Milestone

**Created:** 2026-05-03
**Granularity:** Coarse
**Coverage:** 16/16 requirements mapped

---

## Phases

- [ ] **Phase 1: Inline Edit Foundation** — Fix history bug, build overlay, wire commit/validation/undo
- [ ] **Phase 2: CRUD Gestures** — Add child property, delete node with confirmation
- [ ] **Phase 3: Zen Chrome** — Auto-hide UI controls driven by editing state flag

---

## Phase Details

### Phase 1: Inline Edit Foundation
**Goal**: User can double-click any key or scalar node, edit it inline, and have the change committed to the document with full undo support
**Depends on**: Nothing (first phase of this milestone)
**Requirements**: EDIT-01, EDIT-02, EDIT-03, EDIT-04, EDIT-05, EDIT-06, VAL-01, VAL-02, HIST-01, HIST-02
**Success Criteria** (what must be TRUE):
  1. Double-clicking a property key node opens a TextBox at the correct screen position with the key text selected; the canvas does not move
  2. Pressing Enter commits the change and the node label updates immediately; pressing Escape restores the original text with no document change
  3. Clicking away (blur) commits the edit; no-op when text is unchanged
  4. Committing an empty key or a duplicate sibling key shows a visible error and keeps the editor open — the document is not modified
  5. After a successful edit, Cmd+Z / Ctrl+Z reverts the document and the canvas updates; Cmd+Shift+Z / Ctrl+Y re-applies it
**Plans**: TBD
**UI hint**: yes

### Phase 2: CRUD Gestures
**Goal**: User can add a new child property to any object node and delete any node from the tree
**Depends on**: Phase 1
**Requirements**: CRUD-01, CRUD-02, CRUD-03, CRUD-04
**Success Criteria** (what must be TRUE):
  1. Activating "add child" on an object node appends a new property and immediately opens its key field in inline edit mode
  2. Pressing Del with a leaf node selected removes it from the document without confirmation
  3. Pressing Del with a node that has children shows a confirmation prompt; the node is only removed if the user confirms
  4. All additions and deletions are undoable via Cmd+Z / Ctrl+Z
**Plans**: TBD

### Phase 3: Zen Chrome
**Goal**: UI controls are invisible during navigation and editing; they reappear predictably without flicker
**Depends on**: Phase 1
**Requirements**: ZEN-01, ZEN-02
**Success Criteria** (what must be TRUE):
  1. After a period of mouse idle, toolbar and chrome controls fade out; moving the mouse or pressing a key causes them to reappear smoothly
  2. Opening or closing the inline editor does not cause chrome to flash — visibility is driven by the `_isEditing` state flag, not raw focus events
**Plans**: TBD
**UI hint**: yes

---

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Inline Edit Foundation | 0/? | Not started | - |
| 2. CRUD Gestures | 0/? | Not started | - |
| 3. Zen Chrome | 0/? | Not started | - |
