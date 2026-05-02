# External Integrations

**Analysis Date:** 2026-05-02

## APIs & External Services

None. Nexu is a fully offline desktop application. No HTTP clients, no cloud APIs, no telemetry SDKs are referenced in any project.

## Data Storage

**Databases:** None.

**File Storage:**
- Local filesystem only — reads and writes `.json` files chosen by the user
- Persistence project (`src/Persistence/`) handles all file I/O
- Write pattern: atomic (write to temp file, then replace original) to prevent data loss on crash
- Autosave is implemented in the Persistence layer
- Save is blocked when the CST detects duplicate keys or unresolved syntax errors

**Caching:** None.

## Authentication & Identity

None — desktop app, no user accounts.

## Monitoring & Observability

**Error Tracking:** None (no Sentry, Raygun, etc.)

**Crash Logging:**
- Minimal crash log written to OS temp directory at startup: `Path.Combine(Path.GetTempPath(), "nexu-crash.log")`
- Logs `AppDomain.CurrentDomain.UnhandledException` and caught exceptions in `Main`
- Implementation: `src/App/Program.cs`

**Structured Logging:** None — Avalonia's built-in `LogToTrace()` is the only logging sink (trace output, dev only).

## CI/CD & Deployment

**Hosting:** Desktop app — distributed as a native executable, no server deployment.

**CI Pipeline:** Not detected (no `.github/`, `.gitlab-ci.yml`, `azure-pipelines.yml`, etc. found).

## Inter-Project Dependencies (within solution)

```
Nexu.App
  ├── Nexu.Parsing.Json
  ├── Nexu.Domain
  │     └── Nexu.Parsing.Json
  ├── Nexu.Layout
  │     └── Nexu.Domain
  │           └── Nexu.Parsing.Json
  ├── Nexu.Editing
  │     ├── Nexu.Parsing.Json
  │     ├── Nexu.Domain
  │     └── Nexu.Layout
  └── Nexu.Persistence

Nexu.Tests.Domain       → Nexu.Domain
Nexu.Tests.Parsing.Json → Nexu.Parsing.Json
Nexu.Tests.Persistence  → Nexu.Persistence
Nexu.Tests.Layout       → Nexu.Layout
Nexu.Tests.Editing      → Nexu.Editing
```

**Dependency rules:**
- `Nexu.Parsing.Json` — leaf; no internal project dependencies
- `Nexu.Persistence` — leaf; no internal project dependencies
- `Nexu.Domain` — depends only on `Nexu.Parsing.Json`
- `Nexu.Layout` — depends only on `Nexu.Domain`
- `Nexu.Editing` — depends on `Nexu.Parsing.Json`, `Nexu.Domain`, `Nexu.Layout`
- `Nexu.App` — depends on all five src libraries; is the composition root

## File I/O Patterns

**Input:**
- User selects a JSON file via OS file dialog (Avalonia)
- File is read as raw text — not deserialized to objects
- Raw string is passed to `Nexu.Parsing.Json` for CST construction

**Output:**
- Edits produce character-level `TextPatch` records `{ Start, Length, OldText, NewText }`
- Patches are applied to the raw string in memory
- `Nexu.Persistence` writes the patched string atomically to disk
- Undo/redo history is maintained as a patch stack (in-memory only; not persisted across sessions)

## Platform-Specific Integration

**macOS (15+, Apple Silicon):**
- Post-build codesign step in `src/App/Nexu.App.csproj` re-signs the native apphost with `app.entitlements`
- Entitlements file: `src/App/app.entitlements`
- Required for .NET JIT to function under macOS Hardened Runtime

## Extension Points

**Avalonia Diagnostics:**
- `Avalonia.Diagnostics` package included for Debug builds only
- Provides runtime XAML inspector and visual tree explorer (dev tool, not a user-facing extension point)

**No plugin system** — no MEF, no IPlugin interfaces, no dynamic loading detected.

---

*Integration audit: 2026-05-02*
