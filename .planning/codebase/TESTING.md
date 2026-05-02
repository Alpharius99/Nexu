# Testing Patterns

**Analysis Date:** 2026-05-02

## Test Framework

**Runner:** xunit
- Config: no `xunit.runner.json` detected; defaults apply.

**Assertion library:** `Xunit.Assert` (built-in)

**Coverage collector:** `coverlet.collector` (referenced in all test `.csproj` files)

**Run commands:**
```bash
dotnet test                                           # run all tests
dotnet test --filter "FullyQualifiedName~MyTest"      # single test
dotnet test --collect:"XPlat Code Coverage"           # coverage
```

## Test File Organization

**Location:** Separate `tests/` directory tree mirroring `src/`:
```
tests/
├── Tests.Domain/               # Tests for src/Domain
├── Tests.Editing/              # Tests for src/Editing
├── Tests.Layout/               # Tests for src/Layout
├── Tests.Parsing.Json/         # Tests for src/Parsing.Json
└── Tests.Persistence/          # Tests for src/Persistence
```

**Naming:** `<SubjectType>Tests.cs` — one test class per subject type.

**Namespace:** `Nexu.Tests.<ProjectSuffix>` (e.g., `Nexu.Tests.Editing`, `Nexu.Tests.Domain`).

**Test class visibility:** `public sealed class` for single-concern classes.

## Test Structure

**Static parse helper pattern:**
Every test class that exercises parsing/editing defines a private static helper that reduces boilerplate:

```csharp
// Tests.Domain/CstToNodeGraphMapperTests.cs
private static NodeGraph Map(string json)
{
    var result = JsonParser.Parse(new RawDocument(json, 0, null));
    return CstToNodeGraphMapper.Map(result.Root);
}
```

```csharp
// Tests.Editing/RenamePropertyTests.cs
private static string Apply(string json, EditIntent intent)
{
    var patch = PatchGenerator.Generate(intent, json);
    return patch.ApplyTo(json);
}
```

**Static shared fixture values** for required but irrelevant IDs:
```csharp
private static readonly NodeId AnyId = NodeId.New();
```

**Test method names:** `<Method>_<Scenario>_<ExpectedOutcome>` or `<Method>_<Input>`:
- `Map_EmptyObject_ReturnsSingleObjectNode`
- `ApplyTo_ThrowsWhenOldTextMismatch`
- `Undo_RestoresOriginalText`
- `Write_CreatesFileWithCorrectContent`

**No `[Theory]` / `[InlineData]`** observed — each scenario is a distinct `[Fact]`.

**No setup/teardown** — setup is inline per test or via private static helpers.

## Mocking

**No mocking framework** (no Moq, NSubstitute, etc.) — dependencies are real implementations.

**Async callback injection** used instead of mocks for `AutoSaveManager`:
```csharp
using var manager = new AutoSaveManager(TimeSpan.FromMilliseconds(50), () =>
{
    fired = true;
    return Task.CompletedTask;
});
```

**File system tests** use real temp files:
```csharp
private static string TempPath() =>
    Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
```
Always cleaned up in `finally` blocks.

## Fixtures and Factories

**Inline construction** — no factory classes or shared fixtures.

**Document factory helper** (local to test class):
```csharp
private static RawDocument Doc(string text) => new(text, 1, null);
```

**Test data:** Minimal inline JSON strings — no external fixture files.

## Coverage

**Requirements:** None enforced (no coverage thresholds in `.csproj` or CI config).

**Collector present:** `coverlet.collector` in all test projects — opt-in coverage collection available.

## Test Types

**Unit tests (all tests are unit):**
- Pure functions (`PatchGenerator`, `TextPatch.ApplyTo`, `LayoutEngine`) tested with inline inputs.
- Stateful components (`EditHistory`, `AutoSaveManager`) tested by directly exercising state transitions.

**Integration-style tests** (within unit test projects):
- `DocumentEditorTests` exercises full pipeline: `PatchGenerator → TextPatch.ApplyTo → JsonParser → CstToNodeGraphMapper → LayoutEngine`.
- `RenamePropertyTests` / `AddPropertyTests` / `RemoveNodeTests` test `PatchGenerator` + `TextPatch.ApplyTo` end-to-end.

**E2E tests:** Not present.

## Common Patterns

**Assertion style — xUnit built-ins only:**
```csharp
Assert.Equal(expected, actual);
Assert.True(condition);
Assert.Single(collection);
Assert.Empty(collection);
Assert.NotNull(value);
Assert.IsType<CstObject>(result.Root);
Assert.Contains(collection, predicate);
Assert.Throws<InvalidOperationException>(() => patch.ApplyTo(json));
```

**Async testing:** Uses real `Task.Delay` with generous multipliers — no fake timers.

**Round-trip pattern** — verify edit produces parseable output:
```csharp
var parseResult = JsonParser.Parse(new RawDocument(result, 1, null));
Assert.False(parseResult.HasErrors);
```

**Invertibility pattern** — verify undo produces original text:
```csharp
var restored = result.Patch.Invert().ApplyTo(result.Document.Text);
Assert.Equal(doc.Text, restored);
```

---

*Testing analysis: 2026-05-02*
