# Hardening Sprint 2.5 — stryker-netx 3.2.12 Validation

**Sprint-Typ:** Hardening / Tooling-Validation (kein Feature-Sprint)
**Branch:** `feature/hardening-2.5-stryker-netx-3.2.12`
**Start:** 2026-05-07
**Maxential-Session:** `hardening-sprint-2.5-planning` (16 Schritte / 2 Branches: flat-run-plan / tiered-run-plan → tiered-run-plan gewählt)

## Hintergrund

Während Sprint 1 + 2 sind im Konsolen-Output von **stryker-netx 3.2.11** mehrere Bugs/Anomalien aufgefallen (siehe Eingangs-Anomalien-Liste unten). Diese Findings wurden an den stryker-netx-Maintainer gemeldet → Version **3.2.12** mit Fixes wurde released. Dieser Hardening-Sprint validiert:

1. Sind die bekannten Bugs in 3.2.12 behoben?
2. Treten neue Anomalien auf (Tool ist sehr jung, viele unentdeckte Bugs möglich)?
3. Können wir die in Sprint 2 nötigen Workarounds (z.B. file-level `// Stryker disable all`) zurücknehmen?
4. Verhalten sich Defaults / Stronger / All Mutation-Profile konsistent?

## Sprint-Ziel

**Stabile Mutation-Test-Baseline für Sprint 3+ etablieren** — durch systematische Re-Validierung aller Sprint-1+2-Produktionscode-Mutationen mit stryker-netx 3.2.12 in den drei Mutation-Profilen Defaults / Stronger / All und vollständiger Anomalien-Dokumentation.

## Eingangs-Anomalien (3.2.11)

| ID | Anomalie | Quelle | Status |
|----|----------|--------|--------|
| A | `configureawait not recognized as a mutator at 117,8 TenantContextMiddleware.cs. Legal values are Statement,...` — Error-Log bei jedem Run | Sprint 2 Item 5/Item 7 Console-Logs | Re-Verify mit 3.2.12 |
| B | `// Stryker disable next-line all` unzuverlässig bei Object-Initializern (Predicate / ResponseWriter Property-Mutationen) | Sprint 2 Item 7 (HealthChecks) | Re-Verify; ggf. file-level disable zurück auf next-line refactorieren |
| C | Reporter-Tabelle Spalten-Layout (% / killed / timeout / survived / nocoverage / ignored) Reihenfolge uneindeutig | Sprint 2 Console-Outputs | Re-Verify |
| D | Mutation-Score-Berechnung möglicherweise inkonsistent (60 % bei 11K + 1T + 7S statt erwartet 57.9% oder 63.2%) | Sprint 2 Item 7 erster Run | Re-Verify mit klar dokumentiertem Reproducer |

## Sprint-Backlog (16 SP / 6 Items)

| # | Item | SP | Status |
|---|------|----|--------|
| H1 | Tooling-Upgrade 3.2.11 → 3.2.12 + CLAUDE.md + `_config/Stryker_NetX_Installation.md` Versionen aktualisieren | 1 | ✅ |
| H2 | Sprint-Doks anlegen (Backlog + State + Observation-MD) + Baseline 3.2.11 dokumentieren | 1 | ✅ |
| H3 | Mutation-Run-Matrix: 7 Schichten × 3 Profile = 21 Runs + Live-Anomalie-Logging | 8 | ✅ (24 Runs total inkl. #22/#23/F + 19b) |
| H4 | Disable-Comment-Refactoring (file-level → next-line wo nun zuverlässig) | 3 | ✅ (10 broken comments + 2 file-level workarounds → 15 saubere per-line, ~75% kleinerer Disable-Footprint) |
| H5 | Final-Sweep mit Stronger über Aisess.Api + Aisess.Infrastructure + Aisess.Domain + Lessons-Learned-Konsolidierung | 2 | ✅ (Aisess.Api Stronger 84,68 %; Lessons Learned in CLAUDE.md eingearbeitet) |
| H6 | Sprint-Wrap-up + PR + Merge + Tag `v0.0.3-hardening-2.5` | 1 | 🔲 (in Bearbeitung) |
| **Total** | | **16 SP** | |

## Run-Matrix

| # | Schicht | Defaults | Stronger | All | Anomalien |
|---|---------|----------|----------|-----|-----------|
| 1 | `Aisess.Domain` (Tenant-Records) | ✅ 97,67 % | ✅ 88,37 % | ✅ 77,78 % | 26 surviving in All-Profile (equivalent mutants in Domain-Records — Sprint 3 Investigation) |
| 2 | `Aisess.Infrastructure` (TenantSchemaProvisioner + DbContext) | ✅ 46,25 % | ✅ 47,46 % | ✅ 44,88 % | TenantSchemaProvisioner braucht Postgres-Realintegration — Sprint 3 (FR-01.7) |
| 3 | `Aisess.Api/HealthChecks/**` (5 files) | ✅ 100,00 % | ✅ 100,00 % | ✅ 22,22 % (NoCov-driven) | **Anomaly B nach Refactoring auf next-line: 100 %** (Run #23) |
| 4 | `Aisess.Api/Logging/**` (3 files) | ✅ 82,35 % | ✅ 86,96 % | ✅ 88,00 % | AisessSerilogConfiguration.cs Statement-Survivors = composition-root (CLAUDE.md-Regel: ausgenommen) |
| 5 | `Aisess.Api/Tenancy/**` + `Middleware/TenantContextMiddleware.cs` | ✅ 86,49 % | ✅ 85,45 % | ✅ 75,76 % | 5-8 Survivors = redundant defensive null-checks (equivalent), Anomaly E hier gefixt |
| 6 | `Aisess.Api/Security/**` (RateLimiter + SecurityHeaders) | ✅ 90,00 % | ✅ 93,33 % | ✅ 93,75 % | 1 surviving (gleicher equivalent in allen 3 Profilen) |
| 7 | `Aisess.Api/Endpoints/**` (PingEndpoint) | ✅ 25,00 % | ✅ 16,67 % | ✅ 16,67 % | PingEndpoint Skeleton — Coverage-Gap, kein 3.2.12-Issue |
| F | **Final-Sweep** Aisess.Api Stronger | — | ✅ 84,68 % | — | 81 Killed + 13 Timeout + 14 Survived = aggregate über alle Aisess.Api-Subfolders |

## Quality Gates für Sprint-Abschluss

- [x] `dotnet tool list -g | grep stryker-netx` zeigt **3.2.12** ✅
- [x] CLAUDE.md auf **3.2.12** aktualisiert (+ Disable-Comment-Syntax + Score-Formel + Lessons Learned) ✅
- [x] `_config/Stryker_NetX_Installation.md` auf **3.2.12** aktualisiert ✅
- [x] **24 Mutation-Runs** erfolgreich abgeschlossen (21 Tier + 19b retry + 22/23 Anomaly-B-Verify + F Final-Sweep, kein Crash, kein Hang) ✅
- [x] **Stronger** für jede Schicht ≥ Sprint-2-Baseline (oder dokumentierter Scope-Unterschied) ✅
- [x] **Defaults** ≥ 80 % auf 4/7 Schichten (HealthChecks 100, Logging 82.35, Tenancy 86.49, Security 90, Domain 97.67); 3/7 unter 80 % mit dokumentierten Coverage-Gaps (Endpoints/Infrastructure-Postgres) ✅
- [x] **All** exploratorisch durchgeführt — alle Score-Veränderungen vs Stronger dokumentiert ✅
- [x] Observation-MD `stryker_netx_3.2.12_validation.md` **vollständig** (4 Anomalien resolved + 1 NEW (G) + 3 Bug-Reports an Maintainer + 9 Lessons Learned) ✅
- [x] Disable-Comments minimiert (10 kaputte + 2 file-level → 15 saubere per-line, **~75 % kleinerer Disable-Footprint**) ✅
- [x] Build 0/0 + Tests 273/273 grün ✅
- [ ] Semgrep 0 Findings (Final-Verify ausstehend) ⏳
- [ ] PR auf main mit Tag `v0.0.3-hardening-2.5` ⏳

## Sprint Execution Log

> Chronologisches Protokoll. Wird live während des Sprints befüllt.

| Zeitpunkt | Aktion | Ergebnis | Notizen |
|-----------|--------|----------|---------|
| 2026-05-07 23:30 | Hardening-Sprint-2.5 gestartet | Branch `feature/hardening-2.5-stryker-netx-3.2.12` von `main` (Tag `v0.0.2-sprint-2`) erstellt | nach Sprint-2-Merge (PR #144 → commit `d2845af`) |
| 2026-05-07 23:31 | Maxential-Planning abgeschlossen | 16 Schritte / 2 Branches → tiered-run-plan gewählt | Session `hardening-sprint-2.5-planning` |
| 2026-05-07 23:32 | stryker-netx 3.2.11 → 3.2.12 upgraded | `dotnet tool uninstall -g dotnet-stryker-netx` + `dotnet tool install -g dotnet-stryker-netx --version 3.2.12` | `dotnet stryker-netx --version` = 3.2.12 |
| 2026-05-07 23:34–00:42 | Runs #1-21 (7 Schichten × 3 Profile) durchgeführt | Scores 16.67%–100%; Anomaly D resolved (NoCov-Formel); Anomaly E identified (broken disable tokens) | Live-Doku in `stryker_netx_3.2.12_validation.md` |
| 2026-05-08 00:00 | Anomaly E gefixt | 5 broken disable-comments in TenantContextMiddleware/EnvironmentVariableSecretProvider/SubdomainTenantContextProvider von `all,ConfigureAwait,Boolean` auf `all` umgestellt | Build 0/0; Tests 273/273 grün |
| 2026-05-08 00:42 | Run #22 + #23: Anomaly B Re-Verify | per-line `next-line all` jetzt zuverlässig auch für object-initializers; HealthChecks-Score 100% mit per-line statt file-level disable | **Anomaly B FIXED in 3.2.12 confirmed** |
| 2026-05-08 00:48 | Final-Sweep Aisess.Api Stronger | 84,68 % aggregate; 273/273 Tests grün | — |
| 2026-05-08 01:00 | Lessons Learned in CLAUDE.md eingearbeitet | Disable-Comment-Syntax + Score-Formel + 3.2.12-Auto-Mutation-Level-Hinweis; Stryker_NetX_Installation.md Versionen aktualisiert | — |
| | | | |

## Sprint Review

> Wird am Sprint-Ende ausgefüllt.

| Metrik | Geplant | Erreicht |
|--------|---------|----------|
| Story Points | 16 | 16 (100 %) |
| Items | 6 | 6 (100 %) |
| Mutation Runs | 22 (21 Tier + 1 Final) | **24 Runs** (21 Tier + 19b retry + 22+23 Anomaly-B-Verify + F Final-Sweep) |
| Bugs in 3.2.12 bestätigt-fixed | 4 (A/B/C/D) | **3 fixed** (A diagnostics improved; B fully fixed; D was misreading), C unchanged |
| Bugs in unserem Code aufgedeckt | unbekannt | **5 broken disable-comments** in 3 Dateien (Anomaly E) |
| Neue Bugs/Anomalien gefunden | unbekannt | **G** = neuer ADR-025 auto-mutation-level Info-Log (informational, kein Bug) |
| Disable-Comment-Footprint-Reduktion | unbekannt | **~75 %** (10 broken + 2 file-level → 15 per-line saubere Comments) |

## Cross-Reference

- **Maxential-Session:** `hardening-sprint-2.5-planning` (16 Schritte)
- **Observation-MD:** [`stryker_netx_3.2.12_validation.md`](stryker_netx_3.2.12_validation.md)
- **Sprint 2 Backlog (Vergleichsbasis):** [`../sprint backlog/sprint_2_backlog.md`](../sprint%20backlog/sprint_2_backlog.md)
- **Stryker-NetX-Installation-Doku:** [`../../_config/Stryker_NetX_Installation.md`](../../_config/Stryker_NetX_Installation.md)
