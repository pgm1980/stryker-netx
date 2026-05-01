# Sprint 30 — MTP UnitTest Project Setup + Smoke: Lessons Learned

**Sprint:** 30 (2026-05-01)
**Branch:** `feature/30-mtp-unittest-port`
**Base:** v2.16.0 (Sprint 29 closed)
**Final Tag:** `v2.17.0`
**Type:** Test-only release. MTP project foundation + smoke (analog Sprint 25 für VsTest-Track).

## Outcome

| Sub-Task | Result |
|---|---|
| `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/` project + slnx | ✅ |
| `RpcJsonSerializerOptionsTests` smoke port (2 tests) | ✅ 2/2 grün |
| `TestableRunner` helper port (Sprint-31+ foundation) | ✅ |
| Solution-wide tests | ✅ 497 grün excl E2E (+2) |
| Semgrep clean (2 files) | ✅ |
| Tag | `v2.17.0` |

## What landed

### `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests.csproj`
Standard test-project shape: xUnit + FluentAssertions + Moq + coverlet + ProjectReference auf Stryker.Abstractions, Stryker.TestRunner.MicrosoftTestPlatform, Stryker.Utilities, Stryker.TestHelpers (Sprint-24+25 Foundation).

### `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/RpcJsonSerializerOptionsTests.cs`
2 [Fact]s — smoke validation der Pipeline. Trivial port (Shouldly→FluentAssertions, [TestMethod]→[Fact]).

### `tests/Stryker.TestRunner.MicrosoftTestPlatform.Tests/TestableRunner.cs`
Helper subclass of `SingleMicrosoftTestPlatformRunner` für künftige Sprint-31+-Tests. Architecture-adaptions:
- `Dispose(bool)` modifier `public` → `protected` (production code Sprint 1 cleanup tightened access)
- `new object()` → `new Lock()` ctor argument (Sprint 2 .NET 10 Lock-type adoption)

## Process lessons

### 1. **Production-API drift surface zwischen upstream und stryker-netx zeigt sich schon im 25-LOC-Helper**
TestableRunner ctor signature stimmt nicht 1:1 zu upstream:
- Upstream `Dispose(bool)` ist `public override` — bei uns `protected override` (Sprint 1 Phase tightening)
- Upstream `new object()` als 5. ctor-arg — bei uns `Lock` type (Sprint 2 modernisierung)

Beides sind Sprint-25-prediction-confirmations: API-drift überall in upstream-tests.

### 2. **`new Lock()` statt `new object()` für SyncRoot-fields ist Sprint-2 baseline**
.NET 10's `System.Threading.Lock` wurde in Sprint 2 als idiomatic SyncRoot-Type eingeführt. Tests die Production-Konstruktoren spiegeln müssen das übernehmen. CS1503-Konvertierung ist die diagnostic.

### 3. **Foundation-Sprint-Pattern bestätigt**
| Sprint | Track-Start | Files | Strategy |
|---|---|---|---|
| 25 | VsTest | 1 smoke | Project setup + smoke + helpers |
| 30 | MTP | 1 smoke + 1 helper | Same pattern |

## v2.17.0 progress map
```
[done]    Sprint 30 → MTP project setup + smoke + TestableRunner → v2.17.0
[next]    Sprint 31 → MTP DefaultRunnerFactoryTests (102 LOC) + ResponseListenerTests (126) → v2.18.0
[planned] Sprint 32 → MTP MicrosoftTestPlatformRunnerPoolTests (319) → v2.19.0
[planned] Sprint 33 → MTP AssemblyTestServerTests (483) → v2.20.0
[planned] Sprint 34 → MTP SingleMicrosoftTestPlatformRunnerCoverageTests (488) → v2.21.0
[planned] Sprint 35 → MTP SingleMicrosoftTestPlatformRunnerTests (1107) → v2.22.0
[planned] Sprint 36 → MTP TestingPlatformClientTests (640) → v2.23.0
[planned] Sprint 37+ → CLI / RegexMutators / Stryker.Core.UnitTest tranches
```
