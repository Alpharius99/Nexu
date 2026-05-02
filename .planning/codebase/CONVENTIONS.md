# Coding Conventions

**Analysis Date:** 2026-05-02

## Naming Patterns

**Files:**
- PascalCase for all `.cs` files matching the primary type they contain: `NodeGraph.cs`, `PatchGenerator.cs`, `AtomicFileWriter.cs`
- Test files named `<SubjectType>Tests.cs`: `TextPatchTests.cs`, `DocumentEditorTests.cs`
- AXAML files match their code-behind: `MainWindow.axaml` / `MainWindow.axaml.cs`

**Types (classes, records, enums, interfaces):**
- PascalCase: `NodeGraph`, `EditIntent`, `CstToNodeGraphMapper`, `CstValueKind`
- Abstract base records use a bare noun: `abstract record EditIntent`, `abstract record CstNode`
- Sealed concrete records use descriptive compound nouns: `RenameProperty`, `SetScalarValue`, `AddArrayItem`

**Methods:**
- PascalCase: `ApplyTo`, `Generate`, `Compute`, `Parse`, `Map`
- Static factory helpers: `New()` on value types (`NodeId.New()`), `Parse(…)` / `Map(…)` on mappers
- Private helpers: PascalCase with a verb prefix — `FindCommaAfter`, `ScanForwardPastWhitespace`, `DetectLineIndent`

**Parameters and locals:**
- camelCase: `rawText`, `parseResult`, `innerStart`, `yOffset`
- Discard unused parameters explicitly: `_ = rawText;`

**Constants:**
- `public const double NodeWidth = 120.0;` — PascalCase on `static` classes

**Private fields:**
- Prefixed with `_` + camelCase: `_text`, `_pos`, `_diagnostics`

## Type Design

**Prefer records over classes for data:**
- All domain value objects are `readonly record struct` (`NodeId`) or `sealed record` (`Node`, `NodeGraph`, `TextPatch`, `CstNode` subtypes, `EditIntent` subtypes, `EditResult`).
- Mutable services that own state use `sealed class`: `AutoSaveManager`, `EditHistory`.
- Pure stateless logic lives in `public static class`: `PatchGenerator`, `DocumentEditor`, `JsonParser`, `LayoutEngine`, `CstToNodeGraphMapper`, `AtomicFileWriter`.

**Immutable collections:**
- Use `ImmutableArray<T>` and `ImmutableDictionary<K,V>` from `System.Collections.Immutable` for all collections on records.

**Nullable:**
- Nullable reference types enabled globally via `Directory.Build.props`.
- Use `NodeId?` (nullable value types) for optional identifiers: `Node.ParentId`.
- `string?` for optional labels: `Node.Label`.

## Code Style

**Formatting (`.editorconfig`):**
- Indent: 4 spaces for `.cs`; 2 spaces for `.csproj`, XML, JSON, YAML
- Line endings: LF (`\n`)
- Charset: UTF-8
- Trailing whitespace trimmed; final newline required

**Warnings:**
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` in `Directory.Build.props` — zero warnings tolerated.

**Language version:** `latest` (C# 13 on .NET 10).

**`ImplicitUsings`:** enabled — no explicit `using System;` in source.

## Import Organization

**Order (observed):**
1. `System.*` / BCL namespaces
2. `System.Collections.Immutable`
3. Blank line
4. `Nexu.*` project namespaces

**No path aliases** — standard `using` directives only.

## Error Handling

**Validation in `ApplyTo`:** Guard clauses throw `ArgumentOutOfRangeException` for range violations and `InvalidOperationException` for `OldText` mismatches. Exceptions carry descriptive messages with exact positions/values.

**No custom exception types** — use BCL exceptions (`ArgumentOutOfRangeException`, `InvalidOperationException`, `ArgumentException`).

**Parser errors:** Represented as `CstError` nodes in the CST and `Diagnostic` records in `ParseResult` — never thrown as exceptions. Consumers check `ParseResult.HasErrors`.

**Switch exhaustiveness:** `_ => throw new ArgumentOutOfRangeException(nameof(intent), ...)` at the end of every pattern-matching switch on discriminated unions (`PatchGenerator.Generate`).

## Logging

No logging framework present. The application does not emit logs.

## Comments

**When to comment:**
- Inline comments explain non-obvious position arithmetic: `// "name" = 1..7 (includes quotes)`
- Comment sentinel values: `// PrevSiblingEnd / NextSiblingStart = -1 when absent`
- Section separators inside long static classes use `// ── Name ───` banners

**No XML doc comments** (`///`) on public API — types and parameters are self-documenting via record syntax.

## Function Design

**Static utility methods:**
- Keep to a single responsibility; delegate sub-cases to private helpers.
- Private helpers are named by what they scan/find: `FindCommaAfter`, `ScanBackToNewlineOrDelimiter`.

**Expression bodies:**
- Used for trivial one-liners: `public Node Root => Nodes[RootId];`
- `static` factory short: `public static NodeId New() => new(Guid.NewGuid());`

**With-expressions for mutation:**
- Records are mutated via `with`: `document with { Text = newText, Revision = document.Revision + 1 }`

## Module Design

**Namespace = project name:** `Nexu.Domain`, `Nexu.Editing`, `Nexu.Parsing.Json`, `Nexu.Layout`, `Nexu.Persistence`.

**One type per file** (observed throughout).

**No barrel files** — types are imported directly by namespace.

**`global using`:** Only `Xunit` is added as a global implicit using in test projects via `<Using Include="Xunit" />` in `.csproj`.

---

*Convention analysis: 2026-05-02*
