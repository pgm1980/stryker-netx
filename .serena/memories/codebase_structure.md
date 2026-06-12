# Codebase Structure (Stand v3.3.4 / Sprint 179)

## Top-Level
```
stryker-netx/
├── .claude/  .serena/  .sprint/state.md      # Tooling + Sprint-Status (state.md FÜHREND)
├── _config/development_process.md
├── _docs/
│   ├── architecture spec/architecture_specification.md   # ADR-001…053 + Änderungshistorie
│   ├── design spec/  analysis/                            # FRs/NFRs; 360°-Register 173–178
├── _references/stryker-net/                  # READ-ONLY Upstream-4.14.1-Baseline
├── src/          (11 Projekte, s. Layering)
├── tests/        (10 Testprojekte, s. unten)
├── benchmarks/Stryker.Benchmarks/
├── integrationtest/TargetProjects/           # NetCore/MicrosoftTestPlatform/NetFramework-Fixtures
├── samples/                                  # Sample.Library + Sample.Tests (E2E-Profil-Fixture)
├── .github/workflows/                        # ci, integration-test, release, stryker-on-stryker (Nightly-Dogfood)
├── stryker-netx.slnx  global.json  Directory.Build.props  Directory.Packages.props
└── CLAUDE.md  MEMORY.md  DEEP_MEMORY.md  README.md
```

## src/-Layering (ADR-012)
```
L4  Stryker.CLI                     Program (MSBuildLocator.RegisterDefaults!), StrykerCli,
                                    CommandLineConfig/, FileConfigReader, LoggingInitializer
L3  Stryker.Core                    Das Herz — Unterordner:
      Mutants/                      CsharpMutantOrchestrator, MutationStore, MutantPlacer,
        CsharpNodeOrchestrators/    26 Orchestratoren (NonMutableSyntaxFences seit 179!)
        Filters/                    Equivalence-Pipeline (5 Filter, ADR-017)
      Mutators/                     55 Mutatoren (Profil-Attribute, ADR-018)
      Instrumentation/              If/Conditional/EndingReturn/DefaultInit/Static-Engines
      InjectedHelpers/              MutantControl + MutantContext (laufen im USER-Testprozess, C#2-Limit)
      Compiling/                    CsharpCompilingProcess (MaxAttempt 50), CSharpRollbackProcess
      MutationTest/  CoverageAnalysis/  MutantFilters/ (13)  ProjectComponents/
      Initialisation/               InputFileResolver (ADR-039/-052), Builder, Prozesse
      Reporters/                    Json/Html(+RealTime-SSE)/Dashboard/ClearText/Markdown/Progress
      Baseline/  DiffProviders/  Clients/  Helpers/ (RoslynHelper, SyntaxSlotValidator ADR-028)
      Infrastructure/ServiceCollectionExtensions
L2  Stryker.TestRunner.VsTest       Pool/Runner/Context/Handler (ADR-Hotspots #296/#297)
    Stryker.TestRunner.MicrosoftTestPlatform   MTP-Server-Protokoll (39 Dateien, RPC/Models)
L1  Stryker.Configuration           StrykerOptions/Inputs (46 Input-Klassen), FilePattern
    Stryker.RegexMutators  Stryker.Solutions  Stryker.TestRunner (TestIdentifierList!)
L0  Stryker.Abstractions (~70)  Stryker.Utilities (MSBuild-Schicht: MSBuildWorkspaceProvider,
    RoslynProjectAnalysis, IProjectAnalysis*Extensions = C#14-extension-members)
    Stryker.DataCollector (netstandard2.0, VSTest-In-Proc-Collector)
```

## tests/
Stryker.Core.Dogfood.Tests (portierte Upstream-Suite ~1200 Tests, Struktur-Assertions
statt Literal-IDs seit Sprint 119) · Stryker.Core.Tests · Stryker.CLI.Tests ·
Stryker.TestRunner.VsTest.Tests · Stryker.TestRunner.MicrosoftTestPlatform.Tests ·
Stryker.RegexMutators.Tests · Stryker.Solutions.Tests · Stryker.Architecture.Tests
(ArchUnitNET) · Stryker.E2E.Tests (18 Tests, ~21 min, 2 bekannte Flaky-Klassen) ·
Stryker.TestHelpers (TestBase seeded ApplicationLogging; ProjectAnalysisMockBuilder).
MockFileSystem (TestableIO) nur in Dogfood-/VsTest-/CLI-Tests referenziert, NICHT in Core.Tests.
