# Codebase Concerns

**Analysis Date:** 2026-05-02

---

## Missing Features / Incomplete Implementations

**No user-facing editing UI:**
- All five `EditIntent` types are fully implemented in the backend but there is no UI to invoke them. `MainWindow.axaml.cs` has no inline text fields, context menus, or keyboard shortcuts for edits.
- Files: `src/App/MainWindow.axaml.cs`, `src/App/Controls/CanvasHostControl.cs`
- Impact: The app is read-only in practice.

**Undo/redo stack is never populated:**
- `MainWindow.Undo()` and `MainWindow.Redo()` delegate to `_history`, but `_history.Push()` is never called anywhere in `MainWindow.axaml.cs`.
- Files: `src/App/MainWindow.axaml.cs` lines 50–72
- Impact: Ctrl+Z always shows "Nothing to undo."

**`LayoutPoint` is unused in edges:**
- `Edge` carries `ImmutableArray<LayoutPoint>` for waypoints, always populated as `ImmutableArray<LayoutPoint>.Empty`. No curved/routed edge rendering exists.
- Files: `src/Layout/LayoutEngine.cs`, `src/Layout/Edge.cs`, `src/Layout/LayoutPoint.cs`

**`IndentationDetector` is not used by `PatchGenerator`:**
- `PatchGenerator` uses its own inline `DetectLineIndent` helper instead. `AddProperty` and `AddArrayItem` intents require the caller to pre-compute and pass `Indentation` — no enforcement that they use `IndentationDetector`.
- Files: `src/Editing/IndentationDetector.cs`, `src/Editing/PatchGenerator.cs`

---

## Tech Debt / Code Smell

**Hit-testing is O(n) linear scan:**
- `CanvasHostControl.OnPointerPressed` iterates all nodes to find the clicked one.
- Files: `src/App/Controls/CanvasHostControl.cs` lines 144–157

**`RawDocument.Revision` incremented by callers with duplicated logic:**
- Both `MainWindow` and `EditHistory` do `with { Revision = current.Revision + 1 }`.

**Visual constants are hardcoded magic values:**
- All colors, font sizes, paddings, corner radii, and label truncation limits in `CanvasHostControl` are inline constants with no theming support.
- Files: `src/App/Controls/CanvasHostControl.cs` lines 16–27

**`TruncateLabel` truncation limit is 14 characters — very short:**
- Most meaningful property names will be truncated; no tooltip or expansion mechanism exists.
- Files: `src/App/Controls/CanvasHostControl.cs` lines 129–135

**`AutoSaveManager` silently swallows callback exceptions:**
- The `Task.Run` in `Schedule` catches only `OperationCanceledException`. If `_saveCallback` throws, the exception is silently lost.
- Files: `src/Persistence/AutoSaveManager.cs` lines 22–31

**`SaveAsync` is not actually async:**
- `MainWindow.SaveAsync()` is `Task`-returning but contains no `await`. `AtomicFileWriter.Write` is synchronous and runs on the UI thread, freezing it for large files.
- Files: `src/App/MainWindow.axaml.cs` lines 96–109

**`FormattedText` created per node per render frame:**
- `CanvasHostControl.RenderNodes` creates `new FormattedText(...)` for every node on every `Render` call. Not cached.
- Files: `src/App/Controls/CanvasHostControl.cs` lines 115–125

---

## Security Considerations

**Crash log written to temp directory with predictable fixed filename:**
- `Program.cs` writes `Path.GetTempPath() + "nexu-crash.log"` at every startup. On multi-user systems another user could read crash logs (stack traces with file paths). Log is overwritten at each launch.
- Files: `src/App/Program.cs` lines 13–32
- Fix: Write to an app-private directory or use per-session unique filenames. Gate to `#if DEBUG`.

**`AtomicFileWriter` temp file uses a predictable sibling name:**
- Temp file is `filePath + ".tmp"`. On shared filesystems a malicious user could pre-create the `.tmp` as a symlink; `File.Move(..., overwrite: true)` would overwrite the symlink target.
- Files: `src/Persistence/AtomicFileWriter.cs` lines 9–11
- Fix: Use `Path.GetTempFileName()` in the same directory, or use a GUID suffix.

**macOS entitlements grant broad code-signing bypasses:**
- `app.entitlements` includes `allow-unsigned-executable-memory` and `disable-library-validation`. Required for .NET JIT but weakens macOS Hardened Runtime protections. Rationale should be documented.
- Files: `src/App/app.entitlements`

**No size limit on JSON file read:**
- `OpenJsonFileAsync` reads the entire file with `ReadToEndAsync()`. No maximum file size check.
- Files: `src/App/MainWindow.axaml.cs` lines 132–135
- Impact: Multi-hundred-MB JSON files will exhaust memory and freeze/crash the UI thread.

---

## Performance Risks

**Full re-parse + re-layout on every edit:**
- `DocumentEditor.Apply` runs `JsonParser.Parse` → `CstToNodeGraphMapper.Map` → `LayoutEngine.Compute` synchronously on every edit and undo/redo. All O(n) in document size. No incremental update path.
- Files: `src/Editing/DocumentEditor.cs`

**Recursive DFS in layout engine — stack overflow risk on deep JSON:**
- `LayoutEngine.PositionSubtree` is recursive with depth proportional to JSON nesting. 10,000-deep nesting will overflow the call stack.
- Files: `src/Layout/LayoutEngine.cs` lines 25–62
- Fix: Convert to iterative DFS with an explicit stack.

**`CstToNodeGraphMapper` is also recursive:**
- Same stack-overflow risk as layout engine for pathologically deep JSON.
- Files: `src/Domain/CstToNodeGraphMapper.cs`

**`FormattedText` not cached (see Tech Debt above):**
- Significant CPU pressure during pan/zoom on large graphs.

---

## Missing Error Handling / Edge Cases

**`JsonParser` does not handle `\uXXXX` surrogate pairs:**
- `ParseStringRaw` processes `\uXXXX` as a single `char`. Surrogate pair sequences (`\uD800\uDC00`) are stored as two separate chars rather than a single code point. Keys or values with emoji/supplementary-plane characters may cause patch index mismatches.
- Files: `src/Parsing.Json/JsonParser.cs` lines 201–207

**`TextPatch.ApplyTo` throws on stale offsets:**
- If two `EditIntent`s are generated from the same parse result and applied sequentially, the second patch's offsets are stale after the first shifts the text. No multi-patch batching is supported.
- Files: `src/Editing/TextPatch.cs` lines 11–14

**`OpenJsonFileAsync` does not catch `IOException` / `UnauthorizedAccessException`:**
- Exceptions propagate through `async void OnOpenClick` and are swallowed by the Avalonia event loop. File-read failures are silent.
- Files: `src/App/MainWindow.axaml.cs` lines 132–136

**`SaveAsync` does not catch `AtomicFileWriter.Write` exceptions:**
- Save failures are silent (no user notification).
- Files: `src/App/MainWindow.axaml.cs` line 106

**`AtomicFileWriter` does not clean up `.tmp` on failure:**
- If `File.WriteAllText(tempPath, ...)` succeeds but `File.Move` fails, the `.tmp` file is left on disk.
- Files: `src/Persistence/AtomicFileWriter.cs`

**`AddArrayItem` zero-length gap edge case:**
- For a tight single-line non-empty array (`[1]` → add item), if `LastElementEnd == ParentArrayEnd - 1`, a zero-length gap replacement may produce malformed JSON.
- Files: `src/Editing/PatchGenerator.cs` lines 89–99
- Priority: High — no test covers this case.

---

## Test Coverage Gaps

**No tests for `CanvasHostControl` or any App layer:**
- Pan/zoom math, hit-testing, label truncation, render output, `SetLayout` state transitions are all untested.
- Risk: Visual regressions and pointer interaction bugs go undetected.

**No tests for `IndentationDetector`:**
- Tab-indented files, CRLF files, mixed indentation, container at position 0 are untested.
- Priority: Medium — wrong indentation corrupts formatting silently.

**No tests for `AutoSaveManager` exception path or post-`Dispose` behavior.**

**`JsonParserTests` does not cover:**
- Surrogate pair `\uXXXX` sequences
- Numbers with exponents (`1e10`, `1.5E-3`)
- Trailing content after root value
- Deeply nested structures

**`LayoutEngineTests` does not cover:**
- Empty graph / graph with only a root scalar
- Very wide arrays (100+ elements)
- Subtree Y-overlap scenarios

**No integration test for full pipeline (file open → parse → layout → edit → save):**
- The `MainWindow` orchestration logic, autosave interaction with edits, and undo-across-save are entirely untested.
- Priority: High — this is the critical path.

**`AddArrayItem` zero-length gap edge case (no test):**
- See Missing Error Handling above.
- Priority: High.

---

## Avalonia / Cross-Platform Issues

**`SignWithEntitlements` build target runs on Linux:**
- Condition `'$(OS)' != 'Windows_NT'` triggers on Linux CI where `codesign` is not available.
- Files: `src/App/Nexu.App.csproj` lines 32–38
- Fix: Change condition to `$([MSBuild]::IsOsPlatform('OSX'))`.

**`app.manifest` referenced but may not be present:**
- `Nexu.App.csproj` references `<ApplicationManifest>app.manifest</ApplicationManifest>`. If the file is missing, the Windows build will fail.
- Files: `src/App/Nexu.App.csproj` line 6
- Priority: High for Windows builds.

**No `<Nullable>enable</Nullable>` or `<TreatWarningsAsErrors>` in `Nexu.App.csproj`:**
- Inconsistent with the rest of the solution. Nullable and warnings-as-errors may not apply to the App project.
- Files: `src/App/Nexu.App.csproj`

**`CanvasHostControl` full re-render on every pointer-move during pan:**
- Every pointer-move calls `InvalidateVisual()`, triggering a full render pass. Avalonia has no dirty-region invalidation API.
- Files: `src/App/Controls/CanvasHostControl.cs` lines 178, 204

---

## Recent Uncommitted Changes Analysis

**`src/App/Program.cs` (modified):**
- Added a crash log written unconditionally to `Path.GetTempPath()/nexu-crash.log` at every startup. Should be gated to `#if DEBUG` or written to an app-private directory before release. See Security section above.

**`src/App/Nexu.App.csproj` (modified):**
- Added `SignWithEntitlements` MSBuild target to re-sign the apphost with `app.entitlements` after every build on non-Windows. Required for .NET JIT on macOS 15+ / Apple Silicon. The condition is too broad (see Cross-Platform section).

**`src/App/app.entitlements` (new untracked file):**
- Grants `allow-jit`, `allow-unsigned-executable-memory`, and `disable-library-validation`. Standard requirements for .NET on macOS Hardened Runtime. **Must be committed** alongside the `.csproj` change — a fresh clone without this file will fail the build target.

---

*Concerns audit: 2026-05-02*
