# stryker-netx — Memory Index

> Einstiegspunkt zum Project Memory. **Vertiefung in [DEEP_MEMORY.md](DEEP_MEMORY.md)** — 360° Deep Level Memory.
>
> **Lese-Reihenfolge bei Session-Start:** [.sprint/state.md](.sprint/state.md) (aktueller Sprint — die einzige tagesaktuelle Wahrheitsquelle) → dieses Dokument (Index) → [CLAUDE.md](CLAUDE.md) (Direktiven) → bei Bedarf [DEEP_MEMORY.md](DEEP_MEMORY.md) + ADR-Liste.

## Status (Stand: 2026-06-11, Sprint 170)

- **Projekt:** Portierung von Stryker.NET 4.14.1 auf C# 14 / .NET 10 — **seit Sprint 12 production**, seit Sprint 138 öffentlich auf [NuGet.org](https://www.nuget.org/packages/dotnet-stryker-netx) (`dotnet-stryker-netx`)
- **Repo:** [pgm1980/stryker-netx](https://github.com/pgm1980/stryker-netx) (**public**, Apache-2.0 + NOTICE, GitHub Flow)
- **User:** GitHub-Account `pgm1980`, Sprache Deutsch (Anweisungen) + Englisch (Code/Commits)
- **Aktuelle Version:** v3.3.2 (Sprint 170 — CI-Reanimation + Doc-Drift); davor v3.3.1 (Sprint 169, type-aware ConstantReplacement)
- **Umfang:** 170 Sprints, 50 ADRs, ~2.140 Tests grün, 160+ Tags/Releases, 52 Mutatoren, 5 Equivalence-Filter
- **Convention:** 1 Sprint = 1 Feature-PR (squash, `(#NNN)`-Suffix) + Tag auf Merge-Commit + Release via release.yml + Closing-PR (nur `.sprint/state.md`)

## Ära-Übersicht (Details: README „Project status" + `_docs/sprint_*_lessons.md`)

| Sprints | Versionen | Inhalt |
|---------|-----------|--------|
| 0 | — | Brainstorming, 12 Gründungs-ADRs, Design-Spec, License-Stack |
| 1–4 | v1.0.0 | Port: Buildalyzer raus → MSBuildWorkspace, .NET 10, CI |
| 5–17 | v2.0.0–v2.4.0 | 52-Mutator-Katalog, Profiles, Filter-Pipeline, HotSwap-Walk-back |
| 18–24 | v2.5.0–v2.11.0 | Test-Hardening, FsCheck, NetFramework-CI, Dogfood-Nightly |
| 25–138 | v2.12.0–v3.0.24 | Upstream-Test-Suite-Port (~1.200 Dogfood-Tests), erster NuGet-Push |
| 139–169 | v3.0.25–v3.3.1 | Bug-Report-Ära: 3 externe Reporter-Teams, ADR-025…049 |
| 170 | v3.3.2 | CI-Reanimation (NU1902-Bump, Nightly-Schedule-Fix ADR-050), Doc-Refresh |

## Schlüssel-Entscheidungen

Vollständig: [Architecture Spec](_docs/architecture%20spec/architecture_specification.md) (ADR-001…050). Die tragenden:

| ADR | Entscheidung |
|-----|--------------|
| 001–012 | Gründung: 4.14.1-Baseline, net10.0, Namespaces bleiben `Stryker.*`, CLI `dotnet stryker-netx`, Big-Bang-Analyzer, xUnit+FluentAssertions-Vollmigration, 5-Schichten-ArchUnitNET |
| 014/015/017/018 | Operator-Hierarchie (PIT-Modell), SemanticModel-Infrastruktur, Equivalence-Filter als First-Class-Layer, Mutation-Profiles |
| 021 | HotSwap-Engine entfernt (falsches Kostenmodell — es gibt keinen Per-Mutant-Compile) |
| 025 | Mutation-Profile bumpt Mutation-Level automatisch |
| 027/028/032 | Type-Position-Aware Mutation Control + zentrale Syntax-Slot-Validation (Bug-9-Systemik) |
| 039 | `.slnx` Source-Project-Filter 3-Layer-Defense (Aisess) |
| 044/046 | `--test-case-filter` + `--break-after` (Reporter-Wishlists) |
| 047/049 | Type-aware Literal-Emission in BEIDEN Konstanten-Emittern (InlineConstants, ConstantReplacement) |
| 050 | Nightly-Dogfood: schedule fährt local-pack; GH-Expression `null == false`-Koerzierungsfalle; Tool-Manifest pinnt reale NuGet-Version |

## Dokumenten-Index

| Datei | Inhalt |
|-------|--------|
| [.sprint/state.md](.sprint/state.md) | **Tagesaktuelle Wahrheit**: Sprint-State, Backlog, Hook-Steuerung |
| [DEEP_MEMORY.md](DEEP_MEMORY.md) | 360° Deep Memory — Vision, Stack, Architektur, Risiken, Stryker-Background |
| [CLAUDE.md](CLAUDE.md) | Verbindliche Direktiven (Tool-Nutzung, Subagent-Policy, Quality-Gates) |
| [_docs/architecture spec/](_docs/architecture%20spec/architecture_specification.md) | Architecture Spec mit allen 50 ADRs + Änderungshistorie |
| [_docs/design spec/](_docs/design%20spec/software_design_specification.md) | FR-01..09 + NFR-01..09 |
| [_docs/sprint_N_lessons.md](_docs/) | Per-Sprint-Lessons (Sprints 1–62; danach tragen PR-Bodies die Doku) |
| [_bug_reporting/](_bug_reporting/) | Externe Bug-Reports (Calculator, Aisess, filesystem-mcp-server) + Extracts |
| [_config/development_process.md](_config/development_process.md) | Scrum-basierter Entwicklungsprozess |
| [_references/stryker-4.14.1/](_references/) | Original-Source als read-only Portierungs-Baseline |
| [HANDOVER.md](HANDOVER.md) | Historischer Snapshot Sprint 142 (v3.1.2) — nicht tagesaktuell |

## Surprising / Non-Obvious (verifiziert, kumulativ)

- **Buildalyzer wurde in Sprint 1 KOMPLETT ENTFERNT** (ersetzt durch `Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace`) — ältere Sprint-0-Dokumente, die „Buildalyzer 9.0-Update" als Plan nennen, sind insofern historisch.
- **NuGetAudit + TreatWarningsAsErrors = externe Build-Brüche:** Neue Security-Advisories gegen gepinnte (auch transitive) Pakete brechen den Build ohne Code-Änderung (Sprint 159: Nerdbank GHSA-2cwq high; Sprint 170: zwei moderate). Der Nightly-Dogfood ist seit Sprint 170 das Frühwarnsystem dafür.
- **GitHub-Actions-Expression-Koerzierung:** `inputs.x == false` ist bei schedule-Events TRUE (null→0, false→0) — hat den Dogfood-Nightly 42 Runs lang unbemerkt in den falschen Modus geschickt (ADR-050).
- **CI erzwingt `RestoreLockedMode`** — jede Versions-Änderung in `Directory.Packages.props` MUSS die regenerierten `packages.lock.json` mitcommitten, sonst bricht ci.yml.
- **Sprint-Tags zeigen auf den Squash-Merge-Commit auf main**, nie auf den Branch-HEAD (CLAUDE.md Sprint-Tag-Convention; falsch getaggte Tags werden zu unreachable Orphans).
- **`dotnet tool restore` + Manifest:** `.config/dotnet-tools.json` pinnt seit Sprint 170 die neueste publizierte Version (vorher `0.0.0-localdev` → restore schlug überall fehl). `0.0.0-localdev` bleibt die VersionPrefix-Baseline für lokale Builds (Directory.Build.props).
- **`.slnx` statt `.sln`** — neues XML-Solution-Format; ältere dotnet-Tooling-Versionen kennen es nicht.
- **Stryker-Architektur:** Alle Mutationen werden in EINE Assembly kompiliert (`ActiveMutationId`-Switching zur Laufzeit) — es gibt keinen Per-Mutant-Compile (Kern-Einsicht hinter ADR-021).
- **`.claude/settings.json` läuft im `bypassPermissions`-Modus** — alle CLAUDE.md-Direktiven sind Konvention, kein Enforcement.
- **Serena:** Standalone-Server (localhost:9121) mit In-Memory-Projektliste — wenn von anderem Projekt reserviert, dokumentierter Fallback auf Built-In Tools.

## Offene Punkte / Deferred Backlog

- **B.1** (3 Sites): `byte[].AsSpan`-Overload-Kollision in `AsSpanAsMemoryMutator` — honest-deferred Sprint 169
- **B.2** (1 Site): `byte[].AsMemory`-Overload-Mismatch ebd.
- **D** (4 Sites): `PluginManager` CS0165/CS0161 nach Try-Block-Removal — separater Block-Removal-Codegen-Bug
- Warten auf Reporter-Re-Test gegen v3.3.1+ (Pass-Kriterien: < 20 Safe-Mode-Warnings, < 15 % CompileError-Rate; Baseline v3.3.0: 196 / 31,14 %)
- MTP-Runner-Forwarding für `--test-case-filter` (ADR-044, roadmapped)
- SUT-instance-aware Coverage-Capture (ADR-048, v3.4+)
- Optional: release.yml bumpt `.config/dotnet-tools.json` automatisch pro Release (ADR-050 Follow-up-Idee)
