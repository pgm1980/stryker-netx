---
current_sprint: "148"
sprint_goal: "Bug #4 P1 (--version Flag Konvention): hard rename --version/-V auf Tool-Version (kein Project-Version mehr) + project-version long-only neu. Calculator-Tester Bug-Report 4. ADR-029 Hard-Rename. v3.2.3."
branch: "feature/148-bug4-version-flag-convention"
started_at: "2026-05-06"
housekeeping_done: false
memory_updated: false
github_issues_closed: false
sprint_backlog_written: false
semgrep_passed: true
tests_passed: true
documentation_updated: true
---
# Session State — Sprint 148 in progress (Bug #4 --version Convention)

## Bug-Report 4 Bug #4 — der Auftrag

Calculator-Tester Bug-Report 4: in v3.1.x/v3.2.x druckt `dotnet stryker-netx --version` nicht die Tool-Version (Konvention der .NET-Tool-Plattform) sondern wird als positionsloser Wert für die Dashboard-Project-Version interpretiert. User hat Sprint-141-Workaround (`--tool-version`) explizit zurückgewiesen: `--version` muss konvention-konform sein.

## Sprint 148 — Branch O1 (Hard Rename, 3-Weg ToT)

**Decision:** Hard rename:
- `--version` / `-V` → druckt Tool-Version + exit 0 (short-circuit BEFORE McMaster).
- `--project-version` (long-only, kein short) → übernimmt die historische ProjectVersion-Bindung.
- `--tool-version` / `-T` (Sprint-141-Aliase) → bleiben transitional als Aliase.

**Migration:** `--version <value>` → `--project-version <value>`.

## Sprint-148-Phasen

- **Phase A** ✅ Code-Analyse Z.237 in CommandLineConfigReader.cs
- **Phase D1** ✅ CommandLineConfigReader.cs: `--version`/`-v` → `--project-version` (long-only)
- **Phase D2** ✅ Tool-Version-Handler in StrykerCli.cs: `TryHandleToolVersionFlag` + `IsToolVersionAliasFlag` + `IsBareVersionFlag` + `GetToolVersionString` (refactored)
- **Phase D3** ✅ StrykerCli.RunAsync wiring + MA0051-Refactor (`BuildCommandLineApplication` + `ExecuteWithErrorHandlingAsync`)
- **Phase E1** ✅ 4 neue Sprint-148-Tests + ShouldSetProjectVersion umgestellt auf `--project-version`
- **Phase E2** ✅ Lokal verifiziert: alle 4 Aliases drucken `0.0.0-localdev`, `--help` zeigt `--project-version`
- **Phase F** ✅ Solution-wide build (0 W / 0 E), 817 Unit-Tests grün, Semgrep clean
- **Phase G** ✅ ADR-029 + 0.14.0 history-row geschrieben
- **Phase H** PR + merge + tag v3.2.3

## Sprint-Plan für Bug-Report-4

- **Sprint 147** ✅ closed: Bug #9 P0 (v3.2.2 ADR-028)
- **Sprint 148 (jetzt)**: Bug #4 P1 (`--version` Flag Konvention)
- **Sprint 149**: Bug #6 P1 (`--reporters` plural)
- **Sprint 150**: Bug #8 P1 (Multi-Project UX)
