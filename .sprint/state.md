---
current_sprint: "141"
sprint_goal: "Bug-Report Restitems aufarbeiten: Bug #4 (--version Tool-Convention) + Hinweis #7 (NuGet-Indexing-Doku) + Hinweis #8 (Multi-Source-Project)"
branch: "chore/140-closing-and-141-setup"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: false
tests_passed: false
documentation_updated: false
---
# Session State — Sprint 141 (active)

## Sprint 140 closed cleanly
- Decision-trail: ToT (5 branches) + Maxential (14 thoughts, 2 full-integration-merged branches) → B-AutoBump (= D-lite YAGNI-pruned)
- ADR-025 written
- Implementation: StrykerInputs.ResolveMutationLevel + [LoggerMessage] (9 LOC core)
- 9 new AutoBump unit-tests, all green
- Pre-existing OnAlreadyNewestVersion CLI-test fixed (Sprint-139 0.0.0-localdev baseline shift)
- v3.1.0 published to NuGet.org, banner production-verified
- Total dogfood: 1184 grün / 9 skip

## Sprint 141 Goal — Last 3 bug-report items

### Bug #4 (NIEDRIG) — `--version` Flag liefert Project-Version statt Tool-Version
**Decision (vorab):** additiv `--tool-version` einführen statt Breaking-Change auf `--version`. Bewahrt 1:1-CLI-Schema-Compat mit upstream Stryker.NET (README-Versprechen). Mehr UX-Polish als breaking-change.

**Plan:**
- Neuer `--tool-version` (oder `-T`) Flag in `CommandLineConfigReader.cs`
- Returnt eine Zeile (z.B. `3.1.1`) und exit 0
- Doku in Stryker_NetX_Installation.md: Sektion "Tool-Version abfragen" hinzufügen

### Hinweis #7 (INFO) — NuGet-Indexing-Latenz-Doku
**Plan:**
- Neue Sektion in Stryker_NetX_Installation.md: "Häufige Stolpersteine" → "NuGet-Indexing-Latenz" (5–30 Min normal nach `dotnet nuget push`)
- Empfehlung: bei CI-Pipelines Tool-Version pinnen via `dotnet-tools.json` statt latest

### Hinweis #8 (NIEDRIG) — Multi-Source-Project-UX
**Plan:**
- Decision: `--project '**/*.csproj'`-Pattern unterstützen ODER multi-`--project`-Argumente erlauben?
- Eigentlich: stryker-netx liest aus dem TestProject's `<ProjectReference>`, daher braucht es eine Strategie für mehrere Refs
- **Größere Feature-Arbeit** — könnte ein eigener Sprint sein, falls Komplexität auftaucht
- Wenn zeitlich machbar in Sprint 141: implementieren. Sonst: ADR + defer.

### Tag-Strategy für Sprint 141
- Wenn nur Bug #4 + Hinweis #7 (Doku + 1 additive Flag): **v3.1.1** (Patch)
- Wenn auch Hinweis #8 implementiert: **v3.2.0** (Minor — neues Multi-Project-Feature)

## Test plan
- [ ] Bug #4: `--tool-version` Flag implementiert + 2 Unit-Tests
- [ ] Hinweis #7: Doku-Sektion ergänzt
- [ ] Hinweis #8: implementiert ODER als ADR geplant für künftigen Sprint
- [ ] Build + Test + Semgrep grün
- [ ] PR + squash-merge
- [ ] Tag v3.1.1 (oder v3.2.0)
- [ ] NuGet.org verify
