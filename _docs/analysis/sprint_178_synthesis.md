# 360°-Analyse — Sprint 178: Synthese & priorisierter Fix-Backlog (Programm-Abschluss)

> **Programm:** Sprints 173–178 (Issue #276), Findings-only. Dieses Dokument konsolidiert
> die fünf Register (`sprint_173_findings.md` … `sprint_177_findings.md`) zu Befund-Clustern,
> einem priorisierten Fix-Backlog und einem empfohlenen Fix-Fahrplan. Es ist die
> Entscheidungsgrundlage für die Fix-Sprints ab 179.

## 1. Programm-Bilanz

| Sprint | Scope | Dateien | Findings | Proben | Issues |
|--------|-------|---------|----------|--------|--------|
| 173 (A) | Mutatoren (Core 55 + Regex 20) | 75 | 41 (F) | 5 | #277 P1, #278, #279, #280 |
| 174 (B) | Mutations-Pipeline (Orchestratoren, Placer, Filter, Compiling, MutationTest, InjectedHelpers) | 96 | 39 (G) | 4 (4/4 ✓) | #282 P1, #283–#288 |
| 175 (C) | Initialisation, Utilities, Solutions, Configuration | 98 | 28 (H) | 3 (2 ✓, 1 ✗) | #290–#292 |
| 176 (D) | Test-Runner-Kette (VsTest, MTP, DataCollector) + #274 | 62 | 16 (I, 2 entkräftet) | Batch-0-Beweis | #294–#297 |
| 177 (E) | Reporters, Baseline, CLI, Abstractions, Helpers | ~95 voll (+62 Trivia klassifiziert) | 15 (J) | — | #299, #300 |
| **Σ** | | **~430** | **139** | **12** | **22 offen (2×P1-Klasse, 20×P2)** |

**Methode:** Volltext-Lektüre je Datei, Verdacht→Probe→Issue (Scratch-Projekt gegen Release-CLI
3.3.3, Default-Profil, JSON-Report + Debug-Logs als Evidenz), Cross-Layer-Verifikation über
Sprint-Grenzen. Register batch-weise committet (kompaktierungssicher).

**Trefferquote der Proben:** 8 Verdachte bestätigt (G-10, G-14, G-21, G-25→P1, H-17, H-18 +
2×173er), 2 entkräftet (H-21 Ressourcen-Streams, I-12 Timeout-Inflation), dazu 4 Verdachte
durch Cross-Layer-Code-Verifikation entschärft (I-13 Executor-Guard, J-07 Broadcast-Lock,
H-06 GetFullPath-Validierung, H-14 Interlocked-IdProvider). **Kein einziger Probe-Lauf
widersprach einer Code-Herleitung — aber 6 Herleitungen wurden durch Gegenlese von
Nachbarschichten korrigiert.** Die Lehre: Schicht-übergreifend lesen lohnt.

## 2. Befund-Cluster

### Cluster 1 — CE-Noise im Default-Profil (größtes Einzelthema)

Tote Mutanten (CompileError) verzerren Statistik, kosten Compile-Runden und verstecken
echte Befunde. Probe-Mikrokosmos: **10 Mutanten auf harmloser Datei → 7 CE (70 %)**.

| Issue/Finding | Mechanik | Beweis |
|---|---|---|
| G-01 (in #279) | EndingReturnEngine wird im Mutations-Pfad NIE aufgerufen → Block-Removal-CS0161-Klasse | Fix-Ort BaseFunctionOrchestrator:140, Probe 2/2 |
| #283 | case-Labels = ungeschützter 5. Konstanten-Kontext (StringMutator/Defaults) | Probe 2/2 CE, Kontrollgruppe Killed |
| #284 | Designation-Patterns entgehen Block-Lift → CS0128 (RelationalPattern/Defaults) | Probe 2/2 CE |
| #285 | TrackValue-Wrap bricht `= new();`/Konstanten-Statics → Erst-Compile-CE, stiller Rollback-Heal; MustInjectCoverageLogic ist default-TRUE (H-28) | Probe: „mutant −1 … StaticInitializerMarkerEngine" im Log |
| #278 | is→is-not auf Declaration-Patterns → CS0165 (Defaults!) | Probe 1/1 CE |
| #279 (Epic) | Typ-/Flow-blinde Mutatoren (UOI, ROR-Matrix, TypeDrivenReturn-async, ConstructorNull, AsSpanAsMemory…) | 56 %-CE-Probe; G-03: Deklarations-Level ohne Hosting („orchestrator bug"-Selbstdiagnose) |
| G-17 | Kein generischer No-op-Filter (`AreEquivalent(Original, Replacement)`) → False Survivors | Mechanik komplett |

**Cluster-Fix-Logik:** Erst die zentralen Einzeiler (G-01-EndingReturn, #283-DoNotMutate,
#284-designation-aware Store-Level), dann #285-Marker-Skip, dann die #279-Checkliste
(Emissions-Disziplin je Mutator; Blaupause: NullCoalescing/ArgumentPropagation/MemberVariable).
Erfolgsmessung: Probe-Projekt-CE-Rate vor/nach (Erwartung 70 % → <20 %).

### Cluster 2 — Crash/Hang-Robustheit (#277-Klasse)

Ein einzelner Input-Edge reißt den GESAMTEN Lauf — schlechteste Failure-Mode für CI.

| Issue | Mechanik |
|---|---|
| **#282 P1** | C# 14 `(out v) =>` → unhandled NRE in DefaultInitializationEngine:57, Exit 127 (Probe); läuft im No-Mutations-Pfad → JEDE Datei mit so einem Lambda killt den Lauf |
| #277 P1 | RegexMutator-InvalidCastException auf interpolierten Patterns + DIE Systemfrage: kein try/catch um `mutator.Mutate` (CsharpMutantOrchestrator:223) — In-Repo-Vorbild existiert (CommentParser-Timeout-Catch, G-12) |
| #292 | VsTest-Metadaten (LineNumber=0, fehlende MethodDecl) crashen Enrichment ungefangen |
| #288 | Unbegrenzte `while (emitResult == null)`-NRE-Retry-Schleife → Hang |
| #297 (Epic) | 3 Hang-Stellen: VsTest-Pool-Warm-up (unobserved Exceptions → Ewig-Wait), Discovery-Wait ohne Timeout, MTP-Disconnect komplettiert Listener nicht — MTP-Pool zeigt intern das Vorbild-Muster |
| Register-Sammel | Ungeguardete Parses: `bool.Parse`/`int.Parse` auf MSBuild-Props (H-19), `Enum.Parse` auf Fremd-Baseline (G-37b), GetSolution-InvalidOperation außerhalb des Catch-Sets |

**Cluster-Fix-Logik:** Der systemische Fix ist EINE Robustheitsschicht um `mutator.Mutate`
(Datei-/Mutator-skip + WARN statt Lauf-Abbruch) — sie hätte #277 UND #282 auf Mutanten-Ebene
begrenzt. Dazu die Einzelguards (3-Zeilen-Fixes) und Schleifen-Bounds.

### Cluster 3 — Races/Nebenläufigkeit

| Issue | Mechanik |
|---|---|
| #296 | Multi-Projekt-Init: Task.WhenAll (H-13) × plain `VsTests`/`TestsPerSource` + Instanz-Ersetzung + Cross-`ClearInitialResult` — einfachster Fix: Initial-Tests sequenzialisieren |
| #300 | Realtime-HTML: `_writers`-List/`_delayedEventQueue` zwischen Listener-Task und Mutant-Threads; CloseSse-AggregateException bricht Broadcast-Kette → Json-/Baseline-Report-Verlust |
| I-11 (Register) | MTP Multi-Mutant-Gruppe ⇒ aktive Mutation −1 ⇒ alles überlebt — heute durch Singleton-Gruppen-Invariante tot, NIRGENDS kodifiziert → Guard/Exception einbauen |
| Dokumentations-Pflicht | Verifizierte Netze als Invarianten kommentieren: BroadcastReporter-Lock (einziger sicherer OnMutantTested-Eintritt), Interlocked-IdProvider, Executor-Pending-Guard |

### Cluster 4 — Config-Reichweite & Builds

| Issue | Mechanik |
|---|---|
| #290 | `--configuration`/`--target-framework` erreichen die MSBuildWorkspace nie (DI Default-Ctor) — Probe: `-c Release` injiziert in `bin\Debug\` → Flag faktisch wirkungslos; Ctor-Overload existiert bereits |
| #291 | Multi-TFM ohne `--target-framework` → InputException auf der `TargetFrameworks`-LISTE (Probe Exit 1) — Standard-Projektform crasht |
| #274 | **Shovel-ready Config-Einzeiler** (vendored `test-runner: mtp`-Key raus) → Nightly 11/11; Sekundär: stderr-Ringpuffer + Log-Level-Hebung (I-14) |
| H-25 (Register) | Threshold-Quervalidierung ist bei Einzeloptionen ein No-op (lifted-null) — `--threshold-break 70` passiert gegen Default-Low 60 |
| #299-Anteil | `master`-Default für since-target ist für main-Repos stale (Fehlpfad mit Jargon-Meldung) |

### Cluster 5 — Score-/Report-Integrität

| Issue | Mechanik |
|---|---|
| #286 | ADR-032-Drops erzeugen Geister-Mutanten → False Survivors/NoCoverage statt CE (3-Schichten-Kette verifiziert) |
| #295 | `&= ~` statt `\|=` → MsTest nie erkannt → kein DisableParallelization → stille Coverage-Fehler bei [Parallelize] (trifft TUnit via Fallback) |
| #294 | `Contains` invertiert (`is false`) — ruhend, ungetestet; jeder künftige Aufrufer erbt falsche Semantik |
| #299 | Since-Substring-Match → stiller falscher Diff-Base → falsche Mutantenselektion |
| #287 | RegisterCoverage lock+O(n) je Ausführung im User-Prozess (quadratisch im Coverage-Lauf) |
| #280 | Konstanten-Mutatoren als Mutator.Linq → ignore-Konfusion; dazu G-34 EndsWith-Over-Ignore (`Count`⊃`LongCount`) |
| #273 | Flaky test-seitig (H-01: Calculator deterministisch) — Test-Toleranz/TimeProvider |
| Register | G-19 (Equivalence-Drops report-unsichtbar), J-01 („Pending" schema-fremd; CoveredBy bei EveryTest leer; TestsCompleted/Duration nie befüllt), J-08 (Baseline parst Datei pro Mutant) |

## 3. Quick-Wins (Einzeiler / <10 Zeilen, sofortiger Nutzen)

| # | Issue | Fix | Aufwand |
|---|-------|-----|---------|
| 1 | #274 | Config-Key `"test-runner": "mtp"` entfernen → Nightly 11/11 | 1 Zeile Config |
| 2 | #295 | `&= ~` → `\|=` + Testfall | 1 Zeichen + Test |
| 3 | #294 | `is false` → `is true` + Mengen-API-Tests | 1 Wort + Tests |
| 4 | **#282 P1** | `Type == null` → typloses `default`-Literal | ~3 Zeilen |
| 5 | G-01 (#279) | `AddEndingReturn` in den Mutations-Pfad (BaseFunctionOrchestrator:140) | ~1 Zeile |
| 6 | #292 | LineNumber-Guard + FirstOrDefault + DBG-Skip | ~3 Zeilen |
| 7 | #283 | `DoNotMutateOrchestrator<CaseSwitchLabelSyntax>` registrieren | ~2 Zeilen |
| 8 | P3-Duo | H-05 (Extension-Check `Path.GetExtension(path)`), J-11 (fullPath statt bareName) | je 1 Zeile |

## 4. Empfohlener Fix-Fahrplan

> Jeder Fix-Sprint: TDD (Red-Test aus dem Register-Befund zuerst), Probe-Validierung wo
> ein 173–175er-Probe-Setup existiert, Semgrep, kein Fix ohne Issue-Referenz.

| Sprint | Thema | Inhalt | Erfolgsmaß |
|--------|-------|--------|-----------|
| **179** | **Quick-Wins + P1** | #282, #295, #294, #274-Config, #292, #283, G-01-Einzeiler, H-05/J-11, #273-Testfix | Nightly 11/11; P-4-Probe läuft grün; Probe-CE-Rate sinkt bereits |
| **180** | **CE-Noise I** | #284 (designation-aware Store-Level + ContainsDeclarations-Erweiterung), #278 (Designation-Skip), #285 (Marker-Shape-Skip + Property-Gate) | Probe-Mikrokosmos 70 % → <30 %; keine „mutant −1"-Rollback-Runde mehr im Probe-Log |
| **181** | **Score-Integrität** | #286 (Drop-Pfad markiert Mutanten), G-17 (AreEquivalent-Filter #0), G-19 (Ignored-Status statt continue), J-01 (Pending-Mapping im Report) | Geister-Probe (Slot-Reject-Trigger) zeigt CE statt Survived |
| **182** | **Robustheit** | #277-Schicht um mutator.Mutate, #288 (Loop-Bound), #297-Epic (3 Hang-Stellen), Parse-Guard-Sammel (H-19/G-37b/J-Reste) | Crash-Proben (interpolierter Regex, out-Lambda-Restfälle) enden mit WARN statt Exit ≠ 0 |
| **183** | **Config & Nebenläufigkeit** | #290 (Workspace-Props via Factory), #291 (TFM-Listen-Fallback), #296 (Init sequenzialisieren), #299 (exaktes Branch-Matching + main-Fallback), #300 (SSE-Sync) | P-5-Probe: `-c Release` injiziert in `bin\Release\`; P-6-Probe läuft durch |
| **184** | **Backlog-Rest** | #279-Checkliste (große Mutator-Arbeit, inkrementell), #280, #287 (HashSet + BenchmarkDotNet), H-25, I-11-Guard, Invarianten-Kommentare, P3-Sammelliste nach Bedarf | 56 %-CE-Probe je Mutator-Batch rückläufig; Benchmark-Delta dokumentiert |

**Reihenfolge-Begründung:** 179 maximiert Nutzen/Risiko-Verhältnis (alles Einzeiler, P1 weg,
CI grün). 180/181 adressieren das größte Wertversprechen (ehrliche Scores) vor der
Robustheit, weil deren Proben-Infrastruktur schon steht. 182 vor 183, weil die
Robustheitsschicht die Config-Fixes absichert. #279-Vollausbau zuletzt — größter Aufwand,
inkrementell schneidbar.

## 5. Positiv-Bilanz (entkräftet & verifiziert)

**Entkräftete Verdachte:** H-21 (Ressourcen-Streams überleben Re-Emits — Designed-Survivor-Probe
über 3 Emit-Runden), H-04 (TextSpanHelper sound), H-06 (TestProjects-Validierung normalisiert),
I-12 (First-only-Timing), I-13 (Executor-Guard fängt MTP-Fehlerpfad), J-07-Kern
(Broadcast-Lock serialisiert Dashboard-Batch), H-14 (Interlocked-IdProvider), G-32-Frage
(CaptureCoverage beidseitig materialisiert/pur).

**Vorbildlicher Code (als Muster referenzierbar):** HeartbeatLogger (CAS+One-Shot-Re-Arm),
StrykerVsTestHostLauncher (unconditional BeginOutputReadLine), CommentParser-Timeout-Catch
(das #277-Vorbild), MTP-Pool-Initialisierung (das #297-Vorbild), CoverageCollector
(ThrowingListener-Swap, Late-Binding), ContainsNodeThatVerifies (scope-korrekte
Lambda-Ausschlüsse), FileConfigReader-Key-Guards.

## 6. Offene Restposten

- **P3-Sammelliste:** kuratierte Checkliste als eigenes Issue (siehe #-Verweis im
  Programm-Issue) — Einstreuware für Fix-Sprints.
- **Trivia-Restposten Sprint 177:** ≈62 vertragsfreie Ein-Zeilen-Dateien klassifiziert,
  nicht zeilenweise gelesen — Stichprobe bei Bedarf.
- **Dogfood als Regressionsnetz:** Nach 179 (#274-Fix) liefert Nightly 11/11 das
  Sicherheitsnetz für alle weiteren Fix-Sprints.
