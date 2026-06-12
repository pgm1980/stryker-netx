# Tech Stack (Stand v3.3.4)

## Runtime / Language / Build
- **.NET 10** (`net10.0`; Stryker.DataCollector = `netstandard2.0` wegen VSTest), **C# 14**
  (`latest`; extension members produktiv in IProjectAnalysis*Extensions!)
- `.slnx`-Solution; `global.json` 10.0.100 rollForward latestFeature
- Central Package Management (`Directory.Packages.props`) + **RestoreLockedMode in CI**
  → bei Paket-Bumps IMMER alle `packages.lock.json` regenerieren (10+ Dateien)
- `TreatWarningsAsErrors` + Roslynator 4.15 + SonarAnalyzer 10.20 + Meziantou 3.0.22
  + DotNet.ReproducibleBuilds (Version im Release = Git-Tag, Repo-VersionPrefix bleibt 0.0.0)

## Projektanalyse (WICHTIG — Architekturwechsel ggü. Frühphase)
- **KEIN Buildalyzer mehr!** Phase 9b: `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`
  + `Microsoft.Build.Locator.RegisterDefaults()` (CLI-Start) + paralleles
  `Microsoft.Build.Evaluation.Project` für Raw-Property-Zugriff (RoslynProjectAnalysis).
- Bekannte offene Folge: Workspace bekommt globalProperties NICHT (Issue #290 —
  --configuration/--target-framework wirken nur auf die Evaluation-Seite).
- MSBuildWorkspace braucht `obj/project.assets.json` auch der REFERENZIERTEN Projekte
  (CI: Restore-Fixture-Pflicht, ADR-051).

## Test-Stack (CLAUDE.md-Pflicht)
xUnit 2.9.3 · xunit.runner.visualstudio 3.1.4 · **FluentAssertions 8.8.0 (Pflicht)** ·
Moq 4.20.72 · Microsoft.NET.Test.Sdk 17.14.1 · coverlet.collector 8.0.0
(`--collect:"XPlat Code Coverage"`) · ArchUnitNET.xUnit 0.11.0 · FsCheck.Xunit 3.1.0 ·
BenchmarkDotNet 0.14.0 (nur Release) · TestableIO MockFileSystem (nicht in Core.Tests!)

## Stryker-spezifische Dependencies
Microsoft.CodeAnalysis.* (Roslyn) · Microsoft.TestPlatform (VSTest TranslationLayer) ·
Microsoft.Testing.Platform (eigener MTP-Runner, `--test-runner mtp`) ·
Microsoft.VisualStudio.SolutionPersistence (.slnx) · McMaster CommandLineUtils (ADR-007) ·
Spectre.Console · Serilog · YamlDotNet · Mono.Cecil · LibGit2Sharp · AWSSDK.S3 +
Azure.Storage.Files.Shares (Baseline-Provider) · ShellProgressBar · Grynwald.MarkdownGenerator ·
**Nerdbank.MessagePack ≥1.2.4** (Security-Pin, ADR-050) · System.Resources.NetStandard

## Tooling (CLAUDE.md-Pflicht)
Serena (dieser Server — Symbol-first!) · Context7 (vor neuen APIs) · Maxential CoT
(Architektur ≥10 Thoughts) · NextGen ToT · `gh` CLI · Semgrep (vor Sprint-Close)
