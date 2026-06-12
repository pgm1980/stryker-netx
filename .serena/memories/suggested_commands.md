# Suggested Commands (Stand v3.3.4)

## Build & Test
```bash
dotnet build                                   # Solution, 0 Warnings/0 Errors Pflicht (TWAE)
dotnet test                                    # Vollsuite (~2.190 Tests; E2E allein ~21 min!)
dotnet test tests/Stryker.Core.Dogfood.Tests --filter "FullyQualifiedName~<Name>"   # gezielt
dotnet test --collect:"XPlat Code Coverage"    # mit Coverage (Sprint-Close-Pflicht)
semgrep scan --config auto <changed-files>     # Security (vor Sprint-Close; docs-only → auf Diff)
```
E2E-Hinweis: 2 bekannte Flaky-Klassen (#273-Kommentar); Erstlauf-Failures parallel zu
lokalen CLI-Läufen auf derselben Maschine sind Ressourcen-Flakes → sauberer Re-Run entscheidet.

## Lokale CLI / Probe-Läufe (Fix-Validierung)
```bash
cd %TEMP%/stryker-probe-174/ProbeLib.Tests
dotnet run -c Release --project C:/claude_code/stryker-netx/src/Stryker.CLI/Stryker.CLI.csproj \
  -- --reporter json --mutate "**/Class1.cs" [--verbosity debug]
# JSON-Auswertung: StrykerOutput/*/reports/mutation-report.json (utf-8-sig!)
# Released-Tool-Vergleich: dotnet tool run dotnet-stryker-netx (Manifest im Probe-Ordner)
```

## Git / GitHub (Sprint-Konventionen!)
```bash
git checkout -b feature/<sprint>-<desc>        # NIE auf main arbeiten (Hook warnt)
git commit -m "type(scope): ... (closes #N)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
gh pr create --title "..." --body "..."        # closes-Keywords schließen Issues beim Squash
gh pr merge <N> --squash --delete-branch       # Squash-Commit-Subject endet auf (#NNN)
# TAG-KONVENTION (KRITISCH): Tag NACH dem Merge auf den NEUEN main-Commit:
git checkout main 2>/dev/null; git pull --ff-only origin main   # (gh merge macht das meist schon)
git tag -a v<X.Y.Z> -m "v<X.Y.Z> — Sprint <N>: <kurz>" && git push origin v<X.Y.Z>
gh release create v<X.Y.Z> --title "..." --notes "..."          # triggert release.yml (NuGet)
# Danach Closing-PR (state.md-Flags). Analyse-/CI-only-Sprints: KEIN Tag.
gh workflow run stryker-on-stryker.yaml        # Nightly-Dogfood manuell dispatchen
gh run view <id> --json jobs --jq '...'        # Modul-Status (11 Module)
```
Worktree-Falle: `gh pr merge --delete-branch` scheitert, wenn main in ANDEREM Worktree
ausgecheckt ist → Workaround `gh api -X PUT repos/.../pulls/<N>/merge -f merge_method=squash`.

## Serena-Betrieb (dieses Setup)
- Standalone-Server localhost:9121 via SerenaMcpProxy; Projekt server-seitig `/workspace/stryker-netx`
  gemappt → IMMER relative Pfade verwenden
- Calls SEQUENZIELL absetzen (parallele Calls → Timeout/unavailable; LSP-Warmup nach
  activate_project abwarten, erster Call kann timeouten → einmal wiederholen)
```
