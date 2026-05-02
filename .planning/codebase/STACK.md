# Technology Stack

**Analysis Date:** 2026-05-02

## Languages

**Primary:**
- C# (LangVersion: latest) — all source and test projects

## Runtime

**Environment:**
- .NET 10 (`net10.0`) — all projects target this TFM

**Package Manager:**
- NuGet with Central Package Version Management (`Directory.Packages.props`)
- Lockfile: not present (no `packages.lock.json` detected)

## Frameworks

**Core:**
- Avalonia 11.3.12 — cross-platform desktop UI framework (XAML-based, WPF-style)
- Avalonia.Desktop 11.3.12 — native desktop lifetime/windowing
- Avalonia.Themes.Fluent 11.3.12 — Fluent Design theme
- Avalonia.Fonts.Inter 11.3.12 — Inter font bundling (`.WithInterFont()`)
- Avalonia.Diagnostics 11.3.12 — dev-only diagnostics overlay (excluded from Release builds)

**Testing:**
- xunit 2.9.3 — test framework
- xunit.runner.visualstudio 3.1.5 — IDE/CI runner integration
- Microsoft.NET.Test.Sdk 18.3.0 — test host
- coverlet.collector 8.0.1 — code coverage collection

**Build/Dev:**
- MSBuild / `dotnet` CLI — build and test runner
- Central Package Management (`ManagePackageVersionsCentrally=true`) — all versions in `Directory.Packages.props`

## Key Dependencies

**Critical:**
- `Avalonia` 11.3.12 — entire UI layer; all rendering, input, layout, pan/zoom
- `xunit` 2.9.3 — all test projects depend on this

**Infrastructure:**
- No ORM, no database, no HTTP client — fully offline desktop app
- All persistence is raw file I/O via BCL (`System.IO`)

## Configuration

**Build-wide settings (`Directory.Build.props`):**
- `<Nullable>enable</Nullable>` — nullable reference types enforced everywhere
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` — zero-warning policy
- `<LangVersion>latest</LangVersion>`

**App-specific (`src/App/Nexu.App.csproj`):**
- `<OutputType>WinExe</OutputType>` — native desktop executable
- `<AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>` — compile-time bindings
- `<ApplicationManifest>app.manifest</ApplicationManifest>`
- Post-build `codesign` step on non-Windows for macOS 15+ JIT entitlements (`app.entitlements`)

**Environment:**
- No `.env` or secrets files — desktop app, no server-side config

## Platform Requirements

**Development:**
- .NET 10 SDK
- JetBrains Rider (primary IDE)

**Production:**
- Windows, macOS (Apple Silicon: requires JIT entitlements re-sign), Linux — all via Avalonia's `UsePlatformDetect()`
- macOS 15+: `app.entitlements` grants `com.apple.security.cs.allow-jit`, `allow-unsigned-executable-memory`, `disable-library-validation`

## Project Structure

| Project | Path | Output |
|---------|------|--------|
| `Nexu.App` | `src/App/Nexu.App.csproj` | WinExe (entry point) |
| `Nexu.Domain` | `src/Domain/Nexu.Domain.csproj` | Library |
| `Nexu.Parsing.Json` | `src/Parsing.Json/Nexu.Parsing.Json.csproj` | Library |
| `Nexu.Persistence` | `src/Persistence/Nexu.Persistence.csproj` | Library |
| `Nexu.Layout` | `src/Layout/Nexu.Layout.csproj` | Library |
| `Nexu.Editing` | `src/Editing/Nexu.Editing.csproj` | Library |
| `Nexu.Tests.Domain` | `tests/Tests.Domain/Nexu.Tests.Domain.csproj` | Test |
| `Nexu.Tests.Parsing.Json` | `tests/Tests.Parsing.Json/Nexu.Tests.Parsing.Json.csproj` | Test |
| `Nexu.Tests.Persistence` | `tests/Tests.Persistence/Nexu.Tests.Persistence.csproj` | Test |
| `Nexu.Tests.Layout` | `tests/Tests.Layout/Nexu.Tests.Layout.csproj` | Test |
| `Nexu.Tests.Editing` | `tests/Tests.Editing/Nexu.Tests.Editing.csproj` | Test |

---

*Stack analysis: 2026-05-02*
