# Sprint 35 — SingleMicrosoftTestPlatformRunnerTests Port

**Tag:** v2.22.0 | **Branch:** `feature/35-mtp-single-runner-tests-port`

## Outcome
- 51/51 grün (46 [Fact]s + 5 [Theory] cases). Zero skips!
- Largest single MTP test file portiert: 1107 LOC upstream → ~960 LOC port.
- MTP-project total: 114 grün + 6 skipped = 120 tests.
- Solution-wide: 590 tests grün excl E2E.
- Compile-driven port in **2 build-fix-cycles**.

## Why zero skips here vs Sprint 34's 5 skips?
- Sprint 34 tests called `runner.DiscoverTestsAsync(testAssembly.Location)` — i.e., against the test assembly itself, which only works if the test assembly is MTP-native (MSTest in upstream, but stryker-netx is xUnit which is not MTP-compatible).
- Sprint 35 tests use **invalid/nonexistent assembly paths** (`/path/to/nonexistent.dll`, `/test.dll`, etc.) — they exercise the production exception-handling path, which works identically on MSTest and xUnit hosts.
- Lesson: read the upstream tests for `typeof(...).Assembly.Location` calls — those are the MTP-native-host indicators.

## Lessons (NEW — only what Sprints 26-34 didn't cover)
- **C# 14 `field` keyword conflict (CS9273/CS9258)**: local variable named `field` inside a property accessor is now reserved (synthesized backing-field binding). Rename to `coverageField` etc. — common since reflection-based test helpers like `var field = typeof(T).GetField(...)` are an upstream MSTest idiom.
- **CA1822/S2325 on instance-only-by-naming property**: hardcoding values like `MutantFilePath => Path.Combine(..., "stryker-mutant-123.txt")` (where 123 is a hardcoded id) trips CA1822 because the property doesn't access instance data. Fix: store `_id` as field and use it: `=> Path.Combine(..., $"stryker-mutant-{_id}.txt")`.
- **Production `RunTestsInternalAsync` → `RunAssemblyTestsInternalAsync`**: API rename in stryker-netx. 3 call-site updates. Easy mechanical fix once recognized.
- **Production `Dispose(bool)` modifier**: `public override` (upstream) → `protected override` (stryker-netx, Sprint 30 lesson).
- **`[TestCleanup]` → `IDisposable`**: xUnit creates new instance per test, calls Dispose() after. Move cleanup logic from `[TestCleanup]` method to `Dispose()`.
- **`[TestMethod, Timeout(1000)]`**: drop the `Timeout` attribute. xUnit has no per-test timeout (would need TestTimeoutTimer fixture). Tests are fast enough not to need it.
- **`Shouldly: x.ShouldBe(new[] { 1, 2, 3 })` → FluentAssertions: `x.Should().Equal(1, 2, 3)`** for sequence equality on `IReadOnlyList<int>`.

## Mechanical-fix knowledge base (cumulative through Sprint 35)
Now covers ~100% of analyzer-errors AND production-API drifts encountered. Sprint 36+ should be 1-shot ports unless major API changes happen.

## Roadmap
- Sprint 36: TestingPlatformClientTests (640 LOC) — closes MTP track
- Sprint 37+: CLI.UnitTest, RegexMutators.UnitTest, Stryker.Core.UnitTest tranches (5)
