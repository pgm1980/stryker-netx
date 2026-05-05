---
current_sprint: "140"
sprint_goal: "Bug #1 Stufe 2 (Code-Side) — Profile×Level Mismatch erkennen und auf User reagieren. ToT-Exploration + Maxential-Decision + Implementation."
branch: "chore/139-closing-and-140-setup"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 140 (active)

## Sprint 139 closed cleanly
- Bug-Report-Batch (4 of 6 bugs from Calculator-tester) released as v3.0.25 on 2026-05-06
- Banner-version correct in production (`Version: 3.0.25`)
- Push-tag-auto-trigger verified
- 4.6 min NuGet-indexing-lag (vs. 14 min for v3.0.24 first-push — second push faster because package skeleton exists)

## Sprint 140 Goal — Bug #1 Stufe 2 (Code-Side)

**Problem:** `--mutation-profile Stronger` (oder `All`) ohne entsprechendes `--mutation-level Advanced/Complete` ist schweigsam wirkungslos. Sprint 139 hat das via Doku adressiert, aber das Tool selbst gibt keine Rückmeldung — Anwender, die nur Profile setzen, glauben es wirkt, aber es tut nichts. Calculator-Tester war exakt dieser Fall.

**Sprint 140 angehen via:**

### Phase 1 — ToT (User-Direktive)
Tree-of-Thoughts-Exploration der Lösungsvarianten. Mindestens 4 Branches:

| Branch | Strategie |
|---|---|
| **A** | Status-quo + Warning-Log bei Mismatch (`profile != Defaults && level <= Standard`) |
| **B** | Auto-Bump: Profile setzt impliziten Default für Level wenn nicht explizit gesetzt |
| **C** | Profile-Default-Level-Bundles (kombinierte Werte: "StrongerStandard", "StrongerAdvanced", "AllComplete" als ein einziger Flag-Wert) |
| **D** | Hybrid: Auto-Bump als Default, opt-out via `--no-auto-mutation-level` oder explizit gesetztes `mutation-level` |

Pro Branch: Pros, Cons, Breaking-Change-Charakter, User-Experience, Implementation-Aufwand.

### Phase 2 — Maxential mit den Top-Ranking ToT-Branches
Mindestens 10 Denkschritte für Architektur-Entscheidung. Branch-Merge-Strategie `full_integration`. Tags: `decision`, `tradeoff`, `risk`. ADR-026 ableiten.

### Phase 3 — ADR-026 schreiben + Implementation

### Phase 4 — Tests + Doku-Update + Tag

**Tag:**
- Wenn Code-Behavior unverändert (nur Warning) → v3.0.26 (Patch)
- Wenn Code-Behavior ändert (Auto-Bump) → v3.1.0 (Minor, Breaking-leicht)
- Wenn neue Profile-Bundles als Optionen → v3.1.0 (additive Features)

## Sprint 140 — Test plan
- [ ] ToT mit mind. 4 Branches durchgespielt (A/B/C/D + ggf. weitere), Top 2 für Maxential
- [ ] Maxential mit mind. 10 Thoughts, mit Branches + Revisions wenn nötig, full_integration merge
- [ ] ADR-026 in `_docs/architecture spec/architecture_specification.md` ergänzt
- [ ] Implementation laut ADR-026
- [ ] Unit-Tests in `tests/Stryker.Core.Tests/` für die neue Behavior
- [ ] Doku-Update in `_config_neuprojekte/Stryker_NetX_Installation.md` (Stripe der "Sprint 140 (geplant)"-Forward-Reference, ersetze durch tatsächliches Verhalten)
- [ ] Build + Test + Semgrep grün
- [ ] PR + Squash-Merge + Tag + push-tag-trigger → NuGet

## Sprint 141+ Roadmap (Bugs verbleibend)
- Bug #4 (`--version` Tool-Convention) — additiv `--tool-version` ODER Breaking zu v3.1
- Hinweis #7 — NuGet-Indexing-Doku in Stryker_NetX_Installation.md
- Hinweis #8 — Multi-Source-Project `--all-source-projects` Feature (eigener Sprint-Größe)
