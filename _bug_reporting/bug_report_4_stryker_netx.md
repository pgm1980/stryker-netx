# Bug-Report: `dotnet-stryker-netx` (v3.0.24 → v3.1.1 → v3.2.0 → v3.2.1) — Verlaufs-Dokumentation

**An:** stryker-netx Maintainer-Team
**Von:** Calculator-Testbed-Projekt (.NET 10 / C# 14)
**Datum:** 2026-05-06 (Erstausgabe v3.0.24, Updates für v3.1.1, v3.2.0, v3.2.1)
**Status (v3.2.1):** **noch nicht produktionsreif.** Drei Versionen ohne stabilen Fix für Bug #9 (`All`-Profile-Crash); vier Versionen lang ignorierte Bugs #4, #6 und #8. Konkrete, mit Nachdruck formulierte Forderungen siehe nächste Sektion.

---

## ✅ MAINTAINER-RESPONSE — Bug-Report 4 vollständig geschlossen (2026-05-06)

Alle 4 Bugs sind über Sprints 147–150 architektonisch behoben. Vier aufeinanderfolgende NuGet-Releases (v3.2.2 → v3.2.5) decken die Forderungen ab:

| Bug | Forderung | Lösung | Release | ADR |
|-----|-----------|--------|---------|-----|
| **#9 P0** | Tiefer architektureller Eingriff (a–e), kein Hotfix | Central Syntax-Slot Validation Layer (Hybrid Validator + per-Mutator Audit) — fängt 4 Crash-Klassen | **v3.2.2** | ADR-028 |
| **#4 P1** | `--version` als Tool-Version-Flag etablieren | Hard Rename: `--version`/`-V` druckt Tool-Version, ProjectVersion → `--project-version` (long-only). Sprint-141 `--tool-version`/`-T` als transitional Alias. | **v3.2.3** | ADR-029 |
| **#6 P1** | `--reporters` (plural) als Alias akzeptieren | args-Pre-Processor rewritet `--reporters` → `--reporter` BEFORE McMaster (3 argv-Forms: spaced, `=`, `:`) | **v3.2.4** | ADR-030 |
| **#8 P1** | `--all-projects` ODER Mehrfach-`--project` | Neue `--all-projects` Flag (NoValue, long-only): mutiert ALLE vom Test-Projekt referenzierten Source-Projekte. Verbesserte Fehlermeldung surfaced beide Pfade (`--all-projects` + `--solution`). | **v3.2.5** | ADR-031 |

### Forderungen aus Bug #9 P0 — alle abgedeckt

- ✓ **a) Ursachen-Analyse**: ADR-028 dokumentiert 4 Crash-Klassen mit Mutator-Triggern + Pipeline-Pfaden.
- ✓ **b) Pattern-Match statt `as`**: explizite null-checks mit Diagnostic, nicht silently-null-`as`.
- ✓ **c) Validierungs-Layer vor der Mutation**: `SyntaxSlotValidator.TryReplaceWithValidation` als zentrale Pipeline-Stage zwischen Mutator-Output und Engine-Wrap.
- ✓ **d) Regression-Tests**: 4 SyntaxSlotValidator-Tests + lokal-acid-test mit allen 9 Calculator-Tester-Patterns ohne Crash.
- ✓ **e) Audit aller Mutators**: per-Mutator Skip-Listen verbreitert; Validator als Safety-Net deckt unentdeckte Patterns ab (defense-in-depth).

### Maxential + ToT-Trail dokumentiert pro Sprint

- Sprint 147: 13 Schritte mit 3 ToT-Branches (A/B/**C**), Branch C (Hybrid) gewählt
- Sprint 148: 11 Schritte mit 3-Way ToT (**O1**/O2/O3), O1 (Hard-Rename) gewählt
- Sprint 149: 3 Schritte branchless, **Option A** (args-pre-process) gewählt
- Sprint 150: 11 Schritte mit 2 ToT-Branches (**B1**/B2), B1 (Flag) gewählt

### Verifikation

- Solution-wide **2035 Unit-Tests grün** (4 Sprints kumulativ +25 neue Tests).
- **Semgrep clean** auf allen modifizierten Dateien jedes Sprints.
- 4 GitHub-Releases mit Migration-Tabellen + breaking-change-Hinweisen für `--version`-Konvention (v3.2.3).
- NuGet-Auto-Push pro Tag funktioniert — Pakete sofort verfügbar via `dotnet tool install -g dotnet-stryker-netx`.

### Eskalations-Antwort

Die in der Bug-Report-Eskalation angekündigte Defaults/Stronger-only-Deklaration ist nicht mehr nötig: `--mutation-profile All` ist mit v3.2.2 stable. Alle vier P1-CLI-Issues (#4, #6, #8) sind in v3.2.3, v3.2.4, v3.2.5 zeitnah geschlossen.

**Bug-Report 4 ist geschlossen.** Wir bedanken uns für die ausführlichen Repro-Patterns und die Eskalations-Klarheit — beides hat den Maxential+ToT-Trail wesentlich beschleunigt.

---

## 🔴 Forderungen an das Maintainer-Team

### Stand der vier Versionen

| Bug | v3.0.24 | v3.1.1 | v3.2.0 | v3.2.1 |
|-----|---------|--------|--------|--------|
| **#1** Profile-Flag | 🔴 | ✅ | ✅ | ✅ |
| **#2** Banner | 🟡 | ✅ | ✅ | ✅ |
| **#3** Update-Hinweis | 🟡 | ✅ | ✅ | ✅ |
| **#4** `--version` | 🟡 | ❌ | ❌ | ❌ |
| **#5** Analysis-Warning | 🟡 | ✅ | ✅ | ✅ |
| **#6** `--reporters` | 🟡 | ❌ | ❌ | ❌ |
| **#7** NuGet-Push | 🟢 | n/a | 🟢 | ✅ |
| **#8** Multi-Project-UX | 🟢 | ❌ | ❌ | ❌ |
| **#9** All-Crash | — | 🔴 | 🔴 | 🟠 (transformiert) |

> **Tabellen-Legende:** 🔴 = kritisch / nicht funktional · 🟡 = Bug aktiv (mittlere Schwere) · 🟠 = teilweiser Fix (Symptom verändert, Crash bleibt) · 🟢 = niedrige Initial-Severity · ✅ = funktioniert · ❌ = Bug-Verhalten unverändert (nicht "gefixt")

**Trendlinie:** Drei aufeinanderfolgende Versionen (v3.1.1 → v3.2.0 → v3.2.1) konnten Bug #9 nicht stabil schließen. Bugs #4, #6 und #8 sind seit Erstausgabe **vier Versionen lang** ohne Bewegung — das deutet auf systematische Vernachlässigung der kosmetischen CLI- und UX-Issues hin.

---

### P0 — Sofort und umfassend: Bug #9 (`--mutation-profile All`-Crash)

Bug #9 ist seit drei Versionen (v3.1.1 → v3.2.0 → v3.2.1) **offen und ungelöst**. Der Versuch in v3.2.1 hat den Crash nicht beseitigt, sondern lediglich die Exception-Klasse von `InvalidCastException` zu `NullReferenceException` getauscht — bei diagnostischer **Verschlechterung** (NRE enthält keinerlei Typ-Information; vorher waren `ParenthesizedExpressionSyntax` und `TypeSyntax` als kollidierende Typen explizit benannt).

**Wir bestehen darauf, dass dieser Bug:**

1. **Unverzüglich** angegangen wird. Das `--mutation-profile All` ist ein Kernfeature des Tools. Sein Ausfall über drei Versionen hinweg unterminiert die Glaubwürdigkeit des Tools im professionellen Einsatz und macht jeden Vergleich zwischen den Profilen (Defaults / Stronger / All) zur Farce.
2. **Nicht durch einen weiteren Hotfix** geschlossen wird, der lediglich das Symptom verschiebt — wie der nicht-funktionale `Cast → as`-Tausch in v3.2.1, der die Exception umlabelt, ohne die Ursache zu adressieren. Wir erwarten stattdessen einen architekturell tiefen Eingriff:

   **a) Ursachen-Analyse statt Stack-Trace-Cosmetik.** Welche Mutator-Operatoren im `All`-Set triggern den Pfad durch `NodeSpecificOrchestrator.OrchestrateChildrenMutation`? Welche Syntax-Knoten landen unerwartet in einer Cast-Position? Die Antwort gehört in den Code-Comment am Fix-Ort und in die Release-Notes.

   **b) Korrekte Behandlung des Nicht-Type-Falls.** Pattern-Matching mit explizitem Skip-Pfad (`if (node is TypeSyntax t) ... else { /* mutator skips this node */ }`), nicht ein silently-null-`as`. Wenn ein Mutator einen Knoten nicht mutieren kann, ist das eine bewusste Designentscheidung, kein Crash-Ausfall.

   **c) Validierungs-Layer vor der Mutation.** Eine zentrale Stelle in der Pipeline, die jede beabsichtigte Mutation auf Syntax-Konsistenz prüft, bevor sie auf den Syntax-Tree angewandt wird. Das verhindert, dass der gleiche Bug-Klasse in anderen Mutatoren erneut auftritt.

   **d) Regression-Tests im stryker-netx-Repo** mit minimalen, gezielten Code-Schnipseln, die `ParenthesizedExpressionSyntax` an Cast-Position triggern:
   ```csharp
   var x = -(a + b);            // unary minus on parenthesized expr
   var y = !(condition);         // logical not on parenthesized expr
   return (predicate ? a : b);   // parenthesized ternary as return value
   var z = (i + 1) * 2;          // parenthesized expr in arithmetic
   ```
   Jeder davon sollte mit `--mutation-profile All` ohne Crash und mit sinnvoller Mutanten-Liste verarbeitet werden.

   **e) Audit aller Mutatoren im `All`-Set** auf ähnliche unbedacht-blind-castende Stellen. Die Tatsache, dass der Fix von v3.2.1 das Symptom verschoben statt beseitigt hat, deutet darauf hin, dass an dieser Code-Stelle nicht sauber gearbeitet wurde — und vermutlich auch an anderen Stellen vergleichbare Schwächen bestehen.

**Eskalation:** Sollte v3.2.2 oder v3.3.x den `All`-Profile-Crash ebenfalls nicht stabil schließen, werden wir das Tool intern als "Defaults / Stronger only" deklarieren und das `All`-Profile aus unserer CI- und Doku-Empfehlung streichen. Im Bug-Report nach außen werden wir diese Schritte transparent machen.

---

### P1 — Lange überfällig: Bugs #4 und #6 (CLI-Hygiene)

Beide Bugs sind seit der Erstausgabe **vier Versionen lang** ohne jegliche Bewegung. Beide sind kosmetisch im Tool-Code, aber **nicht kosmetisch im Eindruck auf Anwender** — vier Versionen ohne triviale CLI-Fixes signalisieren mangelnde Sorgfalt bei der CLI-Pflege.

**Bug #4 — `--version` ohne Argument:** Tool antwortet weiterhin mit `Missing value for option 'version'`. Das verletzt die fest etablierte .NET-Tool-Konvention, dass `dotnet <tool> --version` die Tool-Version ausgibt. Der derzeitige Belegung des Flags durch "Project-Version" (für Dashboard/Baseline-Feature) muss dem Plattform-Standard weichen — wir erwarten:
- `--version` ohne Argument → Tool-Version (z.B. `3.3.0`) auf stdout, Exit-Code 0.
- Project-Version für Dashboard in einen anderen Flag verschoben (`--project-version`, `--baseline-version`).

**Bug #6 — `--reporters` (plural) abgelehnt:** Tool kennt nur `--reporter` (singular), lehnt `--reporters` mit `Unrecognized option`-Fehler ab. Der "Did you mean this? reporter"-Hinweis ist bereits implementiert — die plural-Variante als zusätzlichen Alias zu akzeptieren ist trivial. Wir erwarten:
- `--reporters html` und `--reporter html` als gleichwertig akzeptiert.
- Alternativ: Doku überall zu `--reporter` (singular) korrigiert, einschließlich der externen Tutorials, die das Tool referenzieren.

**Wir erwarten beide Issues in der nächsten Version geschlossen.**

---

### P1 — Endlich: Bug #8 (Multi-Project-Test-Setup-UX)

Bug #8 ist seit Erstausgabe **vier Versionen lang** ungelöst und produktionshemmend für Clean-Architecture-Setups, in denen ein Test-Projekt typischerweise mehrere Source-Projekt-Schichten referenziert (Domain / Infrastructure / Application — bei uns drei, in größeren Projekten oft fünf bis sieben).

Aktuelles Verhalten: Tool bricht bei mehr als einer Source-Project-Reference im Test-Project mit einem Hinweis ab und verlangt manuelles `--project Foo.csproj`. Der Anwender muss pro Layer einen separaten Run starten und die Reports manuell zusammenfügen.

**Wir erwarten** entweder:
- Eine **`--all-projects`-Option**, die alle Source-References sequenziell mutiert und einen kombinierten HTML- / JSON-Report erzeugt, oder
- **Mehrfach-`--project`-Angaben** im selben Aufruf (`--project A.csproj --project B.csproj --project C.csproj`).

Die aktuelle Lösung skaliert nicht und macht das Tool für realistisch geschnittene .NET-Projekte umständlich. Diese Funktionalität gehört in die Standard-CLI, nicht in einen Wrapper-Skript-Workaround.

---

### Zusammenfassung der Forderungen

| Priorität | Bug | Fix-Erwartung | Spätester Termin |
|-----------|-----|---------------|------------------|
| **P0** | #9 | Tiefer architektureller Eingriff (a-e), kein Hotfix | Nächste Version (v3.2.2 oder v3.3.0) |
| P1 | #4 | `--version` als Tool-Version-Flag etablieren | Nächste Version |
| P1 | #6 | `--reporters` (plural) als Alias akzeptieren | Nächste Version |
| P1 | #8 | `--all-projects` oder Mehrfach-`--project`-Angabe | Nächste Version |

Wir sind weiterhin bereit, die Repro-Codebase und den Branch `feature/2.x-stryker-vX.Y.Z-verification` als Test-Subjekt zur Verfügung zu stellen. Eine zeitnahe Rückmeldung zum geplanten Vorgehen würden wir begrüßen.

---

## ⚡ Update für v3.2.1 (2026-05-06, ~7 h nach Erstausgabe)

Das Maintainer-Team hat v3.2.1 als Patch-Release veröffentlicht. NuGet-Push diesmal **nicht vergessen** (gut!) — `dotnet tool search` zeigt 3.2.1 sofort.

| Aspekt | Beobachtung in v3.2.1 |
|--------|------------------------|
| **NuGet-Verfügbarkeit** | ✅ Sofort auffindbar — Bug #7 nicht reproduziert |
| **Banner-Version** | ✅ `Version: 3.2.1` |
| **Defaults-Profile** auf `Calculator.Domain` | ✅ Sauberer Run, 2 testbare Mutanten, Score 66,67 % |
| **Stronger-Profile** auf `Calculator.Domain` | ✅ Sauberer Run, 4 testbare Mutanten, Score 80,00 % |
| **All-Profile** auf `Calculator.Domain` | 🟠 **Crash, aber andere Exception** — siehe Detail unten |
| **`--version`-Flag** | ❌ Weiterhin `Missing value for option 'version'` |
| **`--reporters` (plural)** | ❌ Weiterhin `Unrecognized option '--reporters'` |

### Bug #9 — Detail-Befund in v3.2.1

Die Exception hat sich verändert:

**v3.1.1 / v3.2.0:**
```
System.InvalidCastException: Unable to cast object of type
'Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax' to type
'Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax'.
```

**v3.2.1:**
```
System.AggregateException: One or more errors occurred. (Object reference
not set to an instance of an object.)
 ---> System.NullReferenceException: Object reference not set to an instance of an object.
```

Der **Stack-Trace ist im Wesentlichen identisch** zu v3.1.1 — gleicher Code-Pfad in `Stryker.Core.Mutants.CsharpNodeOrchestrators.NodeSpecificOrchestrator.OrchestrateChildrenMutation` (Zeile 84/85). Es ist also derselbe Mutator-Code, nur an einer Stelle verändert.

**Vermutete Ursache des unvollständigen Fixes:**

Im v3.1.1/v3.2.0-Code stand vermutlich ein expliziter Cast wie `(TypeSyntax)someNode`. Der Fix in v3.2.1 hat das wahrscheinlich zu `someNode as TypeSyntax` geändert — was die `InvalidCastException` beseitigt, aber jetzt liefert `as` schlicht `null` zurück, und der nachfolgende Code dereferenziert das null-Objekt → `NullReferenceException`.

**Kritik aus Anwender-Sicht:**

1. **Diagnostische Verschlechterung.** Die `InvalidCastException` nannte konkret die beiden Typen (`ParenthesizedExpressionSyntax` → `TypeSyntax`) — sehr nützlich zur Eingrenzung. Die `NullReferenceException` sagt **gar nichts** darüber, was null ist oder wo es erwartet wurde. Ein Anwender, der den Crash debuggen will, hat es jetzt schwerer.
2. **Halb-fertiger Fix.** Wenn das Maintainer-Team den Cast-Fehler ernst genommen hat, hätte es entweder einen Pattern-Match (`if (node is TypeSyntax t) ...`) mit Fallback oder einen expliziten Validierungs-Check einbauen müssen. Die `as`-zu-`null`-Variante ist die kürzeste, aber funktional schlechteste Lösung.
3. **Unbekannte Auswirkung auf andere Mutationen.** Wenn an dieser Stelle null zurückkommt und nicht abgefangen wird, ist unklar, ob in **anderen Profile-Konstellationen** (z.B. `Stronger` mit bestimmten Code-Mustern) der gleiche null-Pfad auftreten und schweigend zu falschen Mutationen führen könnte. Stronger-Runs erscheinen erfolgreich — aber wir können nicht ausschließen, dass dort einzelne Mutationen unbemerkt verschluckt wurden.

**Empfehlung an die Maintainer:**

1. **Diagnose-Logging vor dem null-Dereference** — z.B. `if (typeNode is null) throw new InvalidOperationException($"Mutator X expected TypeSyntax at {node.GetLocation()}, got {node.Kind()}");`. Das gibt sowohl der Anwender-Seite als auch dem Maintainer einen brauchbaren Bug-Report.
2. **Pattern-Matching statt `as`** an dieser Stelle: explizite Behandlung von "ist nicht TypeSyntax" als bekannter Fall (z.B. Mutator skipt diesen Knoten).
3. **Test-Case für den `ParenthesizedExpressionSyntax` Fall** — ein minimaler Code-Schnipsel wie `var x = -(a + b);` als Regression-Test im stryker-netx-Repo.

**Aktualisierte Empfehlung (Stand v3.2.1):** Tool **bleibt nicht produktionsreif für `--mutation-profile All`**. Der Fix-Versuch zwischen v3.2.0 und v3.2.1 ist erkennbar, aber unvollständig. Anwender sollten weiterhin `Defaults` oder `Stronger` einsetzen. Beide Versionen (v3.2.0 und v3.2.1) sind im `All`-Bereich gleich unbenutzbar — der Versions-Bump zu v3.2.1 löst aus Anwender-Sicht nichts.

---

## ⚡ Update für v3.2.0 (2026-05-06, ~6 h nach Erstausgabe)

Das Maintainer-Team hat v3.2.0 als "neue, gefixte Version" angekündigt. Verifikation auf identischer Codebase ergibt:

| Aspekt | Beobachtung in v3.2.0 |
|--------|------------------------|
| **NuGet-Verfügbarkeit** | Initial nicht auffindbar (`Version "3.2.0" ... wurde in NuGet-Feeds nicht gefunden`); nach Push-Korrektur durch Maintainer-Team verfügbar. **→ Bug #7 reproduziert sich.** |
| **Banner-Version** | ✅ Banner zeigt `Version: 3.2.0`. |
| **Defaults-Profile** auf `Calculator.Domain` | ✅ Läuft sauber durch (4 Mutanten, 2 tested, 2 killed, Score 66,67 %) — Banner und Output unauffällig. |
| **Stronger-Profile** auf `Calculator.Domain` | ✅ Läuft sauber durch (4 testbare Mutanten, 0 survived, Score 80,00 %) — bestätigt: Profile-Differenzierung weiterhin funktional, Bug #1-Fix nicht regressiert. |
| **All-Profile** auf `Calculator.Domain` | 🔴 **CRASH unverändert**. Identischer Stack-Trace wie in v3.1.1: `System.InvalidCastException: Unable to cast object of type 'ParenthesizedExpressionSyntax' to type 'TypeSyntax'`. Tool bricht ab, kein Report. **Bug #9 bleibt offen.** |
| **`--version`-Flag** | ❌ Weiterhin `Missing value for option 'version'`. **Bug #4 bleibt offen.** |
| **`--reporters` (plural)** | ❌ Weiterhin `Unrecognized option '--reporters'`. **Bug #6 bleibt offen.** |

**Bilanz v3.2.0:** Status quo gegenüber v3.1.1 — keine zusätzlichen Fixes auf der von uns getesteten Bug-Liste. Wenn 3.2.0 als "Bug-Fix-Release" angekündigt wurde, sollten Release-Notes klarstellen, welche Bugs adressiert wurden und welche nicht. Aus unserer Sicht **bleibt Bug #9 der Show-Stopper** für die Profile-Vollständigkeit.

**Aktualisierte Empfehlung:** Wie schon bei v3.1.1: Tool ist mit `Defaults` und `Stronger` brauchbar; `All` weiterhin meiden. Die Versions-Bumps von 3.1.1 → 3.1.2 → 3.2.0 (Patch + Minor) ohne Crash-Fix sind aus Anwender-Sicht irritierend, weil sie semantische Versions-Schritte signalisieren, ohne dass das gefürchtete Feature wieder benutzbar wird.

---

## ⚡ Update für v3.1.1 (2026-05-06, ~3 h nach Erstausgabe)

Das Maintainer-Team hat reagiert und v3.1.1 veröffentlicht. Alle Befunde unten wurden mit dieser Version erneut getestet auf identischer Codebase und identischem Repro-Pfad. Ergebnis-Status:

| Bug | Status in v3.1.1 | Belege |
|-----|------------------|--------|
| **#1** Profile-Flag ohne Effekt | ✅ **GEFIXT** | Defaults erzeugt 271 Mutanten / 203 tested / Score 72,91 %; Stronger erzeugt **420 tested / Score 69,10 %**. Klare und plausibel höhere Mutator-Anzahl bei Stronger. |
| **#2** Banner-Version inkonsistent | ✅ **GEFIXT** | Banner zeigt jetzt korrekt `Version: 3.1.1`. |
| **#3** Falscher Update-Hinweis | ✅ **GEFIXT** | "A new version is available" verschwunden, wenn die installierte Version aktuell ist. |
| **#4** `--version` braucht Argument | ❌ unverändert | `dotnet stryker-netx --version` antwortet weiterhin mit `Missing value for option 'version'`. |
| **#5** `Could not find a valid analysis for target` | ✅ **GEFIXT** | Warning aus dem Standard-Log entfernt; sauberer Run-Output. |
| **#6** `--reporters` (plural) unbekannt | ❌ unverändert | `--reporters html` weiterhin abgelehnt mit `Unrecognized option '--reporters'`. |
| **#7** NuGet-Publishing-Verzögerung | n/a | Beobachtung im Erstreport, nicht reproduzierbar prüfbar (3.1.1 war direkt verfügbar). |
| **#8** Multi-Source-Project-UX | ❌ unverändert | Manuelles `--project` weiterhin nötig. |
| **#9** _NEU_ — Crash mit `--mutation-profile All` | 🔴 **NEU** | `System.InvalidCastException: Unable to cast object of type 'ParenthesizedExpressionSyntax' to type 'TypeSyntax'`. Tool bricht ab, kein Report. Trifft sowohl auf Calculator.Domain als auch auf Calculator.Infrastructure. Details siehe **Bug #9** weiter unten. |

**Bilanz:** 4 von 6 testbaren Bugs gefixt. Aber **Bug #9 ist gravierender als die meisten gefixten Bugs zusammen**, weil das `All`-Profile-Feature jetzt komplett unbenutzbar ist — vorher hat es zumindest "etwas" produziert (auch wenn falsch).

**Aktualisierte Empfehlung:** Tool ist mit `Defaults` und `Stronger` brauchbar (das sollten die meisten Anwender aktivieren). `--mutation-profile All` ist **kaputt** und sollte vor Produktiv-Einsatz nicht eingestellt werden. Ein Hot-Fix v3.1.2 wäre wünschenswert.

---

## TL;DR (Original, 2026-05-06 Erstausgabe, gilt für v3.0.24)

Wir haben `dotnet-stryker-netx` 3.0.24 als Mutation-Testing-Tool auf einer realistischen .NET-10-Codebase (1.700 LOC src, 357 xUnit-Tests, 92,34 % Coverage, `.slnx`-Solution) eingesetzt. Das Tool **läuft grundsätzlich**, **liefert aber an mehreren Stellen falsches oder widersprüchliches Verhalten**. Der gravierendste Befund ist, dass das beworbene Feature `--mutation-profile` (Defaults / Stronger / All) auf unserer Codebase keinerlei Differenzierung zeigt. Daneben gibt es eine Reihe kleinerer, kosmetischer und UX-Schwächen, die in Summe ein unrundes Tool-Erlebnis ergeben.

**Kern-Empfehlung:** Vor Produktiv-Einsatz mindestens Bug #1 (Profile-Flag) und Bug #2 (Version-Banner) fixen; Bugs #3–#7 sind nice-to-have.

---

## 1. Umgebung

| Komponente | Version / Konfiguration |
|------------|-------------------------|
| OS | Windows 11 Pro (10.0.26200) |
| .NET SDK | 10.0.107 |
| `dotnet-stryker-netx` (NuGet) | 3.0.24 |
| `dotnet-stryker-netx` (interner Banner) | `1.0.0-preview.1` ⚠️ siehe Bug #2 |
| Test-Framework | xUnit 2.9.3 + FluentAssertions 8.8.0 + FsCheck.Xunit 3.1.0 |
| Solution-Format | `.slnx` (XML-basiert) |
| Test-Projekt | net10.0, xUnit 2.9.3, 357 Tests grün |
| Source-Projekte | `Calculator.Domain` (classlib), `Calculator.Infrastructure` (classlib), `Calculator` (Console-Exe) |
| Coverage (Coverlet) | 92,34 % Lines, 90,87 % Branches |
| Bash | MSYS Bash auf Windows |

---

## 2. Bug-Liste (sortiert nach Schweregrad)

### 🔴 Bug #1 — `--mutation-profile` zeigt keinen messbaren Effekt

**Schweregrad:** HOCH (beworbenes Feature funktioniert nicht)

**Erwartung** (laut Doku): `Defaults` aktiviert 26 Mutatoren, `Stronger` 44, `All` 52. Auf einer Codebase mit Type-Aware-Targets, Method-Bodies und Receiver-Aufrufen sollte sich daraus eine signifikant unterschiedliche Mutanten-Anzahl ergeben.

**Beobachtung:** Drei Runs auf identischer Codebase, einzige Variation `--mutation-profile`:

```bash
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Defaults  -O StrykerOutput/Inf-Defaults
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Stronger  -O StrykerOutput/Inf-Stronger
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile All       -O StrykerOutput/Inf-All
```

| Profile | Created | CompileError | Ignored | Tested | Killed | Survived | Timeout | Score |
|---------|---------|--------------|---------|--------|--------|----------|---------|-------|
| `Defaults` | **271** | 20 | 48 | 203 | 144 | 55 | 4 | 72,91 % |
| `Stronger` | **271** | 20 | 48 | 203 | 144 | 55 | 4 | 72,91 % |
| `All` | **271** | 20 | 48 | 203 | 143 | 55 | 5 | 72,91 % |

→ **Bitidentische** Mutanten-Anzahl in allen drei Spalten Created/CompileError/Ignored/Tested. Die einzige Differenz zwischen `Defaults`/`Stronger` und `All` (ein Mutant, der von Killed nach Timeout kippt) ist Rauschen aus parallelem Testlauf.

**Hinweis:** Das Tool validiert das Profile-Argument korrekt — `--mutation-profile XYZ` wird abgewiesen mit:
```
The given mutation profile (XYZ) is invalid. Valid options are: [None, Defaults, Stronger, All]
```
Das Argument wird also durchgelassen, aber an der Mutanten-Generierung augenscheinlich nicht angewandt.

**Vermutete Ursache:** Profile-Argument wird validiert und gespeichert, aber der Code-Pfad, der die Mutator-Liste anhand des Profils zusammenstellt, fällt auf einen Default-Set zurück (vermutlich `Defaults`).

**Vorschlag zur Fehlersuche:**
1. Logging der tatsächlich aktivierten Mutator-Liste pro Run hinzufügen (`-V debug`).
2. Unit-Test im stryker-netx-Repo, der nachweist, dass `MutatorLevelOptions.GetActiveMutators(profile)` für die drei Profile unterschiedliche Mengen zurückliefert.

---

### 🟡 Bug #2 — Versions-Banner inkonsistent zur Paket-Version

**Schweregrad:** MITTEL (Trust- und Diagnose-Issue)

**Erwartung:** Tool meldet seine eigene Paket-Version (3.0.24).

**Beobachtung:** Bei jedem Run, im Banner-Output:
```
Version: 1.0.0-preview.1
```

Gleichzeitig:
```bash
$ dotnet tool list -g | grep stryker-netx
dotnet-stryker-netx                    3.0.24          dotnet-stryker-netx
```

Die NuGet-Paket-Version ist 3.0.24, das Tool meldet sich aber als `1.0.0-preview.1`. Das ist nicht nur kosmetisch:

1. **Diagnose** wird schwer — wenn Bug-Reports mit unterschiedlichen Versions-Strings im Banner kommen, weiß man nicht, ob die Reporter unterschiedliche Tool-Versionen haben oder nur unterschiedlich viele Updates verpasst.
2. **Vertrauen** leidet — wenn ein Tool sich nicht über seine eigene Version im Klaren ist, ziehen Anwender berechtigterweise weitere Annahmen über Reife in Zweifel.

**Vermutete Ursache:** Hardcoded `AssemblyInfo.Version = "1.0.0-preview.1"` aus einem frühen Fork-Stand, das beim Build der NuGet-Pakete nicht mit der Paket-Version synchronisiert wird.

**Vorschlag:** `<Version>` und `<AssemblyVersion>` im `.csproj` an gemeinsame Quelle binden (z.B. `Directory.Build.props`).

---

### 🟡 Bug #3 — Update-Hinweis zeigt die bereits installierte Version als "neue verfügbare Version"

**Schweregrad:** MITTEL (UX-Verwirrung)

**Erwartung:** Wenn 3.0.24 installiert ist und 3.0.24 die neueste auf NuGet, sollte kein Update-Hinweis erscheinen.

**Beobachtung:** Bei jedem Run mit installierter 3.0.24:
```
A new version of stryker-netx (3.0.24) is available. Please consider upgrading
using `dotnet tool update -g dotnet-stryker-netx`
```

Das Tool weist sieben Mal in fünf Runs darauf hin, dass eine neue Version verfügbar sei — und zwar genau die, die schon installiert ist.

**Vermutete Ursache:** Der Update-Check vergleicht die NuGet-Latest (3.0.24) mit der internen Banner-Version (`1.0.0-preview.1` aus Bug #2). Da `3.0.24 > 1.0.0-preview.1`, gibt es immer ein "Update verfügbar".

**Vorschlag:** Behebt sich automatisch mit Bug #2 (Banner-Version-Sync), oder der Update-Check vergleicht stattdessen `Assembly.GetEntryAssembly().GetName().Version` gegen NuGet-Latest.

---

### 🟡 Bug #4 — `--version`-Flag liefert nicht die Tool-Version

**Schweregrad:** NIEDRIG (UX-Inkonsistenz)

**Erwartung:** `dotnet stryker-netx --version` zeigt die Tool-Version (Konvention bei .NET-Tools).

**Beobachtung:**
```bash
$ dotnet stryker-netx --version
Specify --help for a list of available options and commands.
Missing value for option 'version'
```

`--version` ist ein bestehender Flag, aber **erwartet ein Argument**. Aus `--help`:
```
-v|--version    Project version used in dashboard reporter
                and baseline feature. | default: ''
```

Der Flag ist also für **Project-Version** (Dashboard-Feature) reserviert, nicht für Tool-Version. Das ist eine konzeptionelle Kollision mit der gängigen .NET-Tool-Konvention.

**Test:** `--version 99.99.99` wird wortlos akzeptiert und der ganze Mutation-Run startet — also keine Validierung des übergebenen Wertes als SemVer.

**Vorschlag:**
- Den Project-Version-Flag in `--project-version` oder `--baseline-version` umbenennen.
- `--version` (oder `-V`) für Tool-Version reservieren.
- Plattform-Konvention: `dotnet stryker-netx --version` sollte einen Einzeiler wie `3.0.24` ausgeben und 0 returnen.

---

### 🟡 Bug #5 — Wiederkehrende Warning `Could not find a valid analysis for target`

**Schweregrad:** NIEDRIG (Log-Rauschen, kosmetisch)

**Beobachtung:** Bei **jedem** Run erscheint im Info-Log:
```
[INF] Could not find a valid analysis for target  for project
'C:\claude_code\stryker-test\tests\Calculator.Tests\Calculator.Tests.csproj'.
Selected version is net10.0.
```

Die zwei Leerzeichen vor "for project" deuten auf ein leeres Format-String-Argument hin (`$"... for target {emptyString} for project ..."`). Tritt auf, obwohl das Test-Projekt klar als `net10.0` aufgelöst wird und Stryker direkt danach verkündet `Found project ... to mutate.` und auch korrekt 357 Tests entdeckt.

**Vermutete Ursache:** MSBuild-Workspace-Loader liefert ein leeres `TargetFramework`-Feld in einer Übergangsphase, und der Code loggt das als Warning, obwohl er anschließend einen Default einsetzt.

**Vorschlag:** Entweder Warning entfernen (wenn der Default-Pfad zuverlässig funktioniert), oder den fehlenden Wert in der Meldung explizit nennen (`for target '' (empty)`).

---

### 🟡 Bug #6 — Inkonsistenz `--reporter` (singular) im Help vs. CLAUDE-Beispiele mit `--reporters` (plural)

**Schweregrad:** NIEDRIG (UX, ggf. Dokumentationsbug)

**Beobachtung:** Mehrere externe Anleitungen (auch unsere [_config/Stryker_NetX_Installation.md](../../_config/Stryker_NetX_Installation.md)) zeigen `--reporters "html"`. Das Tool kennt diesen Flag nicht:

```bash
$ dotnet stryker-netx --reporters html
Specify --help for a list of available options and commands.
Unrecognized option '--reporters'

Did you mean this?
    reporter
    open-report
```

Im `--help` ist nur `-r|--reporter` (singular) dokumentiert, mit `default: ['Progress', 'Html']`. Mehrfach-Werte mit `-r html -r json`.

**Vorschlag:** Entweder
- `--reporters` (plural) als Alias akzeptieren — nahe an Tippfehler-Toleranz, was die "Did you mean"-Hilfe ohnehin schon andeutet.
- Oder die Doku überall an `--reporter` (singular) anpassen.

(Dies ist möglicherweise eher ein Doku-Bug der weiterverbreiteten Tutorials als ein Tool-Bug.)

---

### 🟢 Hinweis #7 — NuGet-Publishing-Verzögerung

**Schweregrad:** INFORMATIONELL

**Beobachtung:** Am 2026-05-05 war das Paket `dotnet-stryker-netx 3.0.24` auf nuget.org **nicht** auffindbar:

```bash
$ dotnet tool install -g dotnet-stryker-netx --version 3.0.24
"Version "3.0.24" des Pakets "dotnet-stryker-netx"" wurde in NuGet-Feeds
"https://api.nuget.org/v3/index.json" nicht gefunden.

$ dotnet tool search stryker
dotnet-stryker          4.14.1   ...
dotnet-stryker-unofficial   3.7.2  ...
# (kein dotnet-stryker-netx)
```

Am 2026-05-06 (~24 h später) war dieselbe Version (3.0.24) ohne weitere Änderungen plötzlich verfügbar und installierbar. Möglicherweise NuGet-Indexing-Latency oder verzögerte Veröffentlichung.

**Wirkung:** Anwender, die der Doku folgen, erhalten zunächst die Fehlermeldung "Paket nicht gefunden" und vermuten, dass das Tool nicht (mehr) existiert. CI-Pipelines fallen scheinbar unmotiviert um.

**Vorschlag:**
- Release-Prozess: nach `dotnet nuget push` mindestens auf Index-Sichtbarkeit warten (`dotnet package search` polling) bevor die Doku/Release-Notes verteilt werden.
- Doku: Hinweis auf typische NuGet-Indexing-Latenz (15–60 min) hinzufügen.

---

### 🟢 Hinweis #8 — Multi-Source-Project-Test-Setup verlangt manuelles `--project`

**Schweregrad:** NIEDRIG (UX)

**Beobachtung:** Wenn das Test-Projekt mehrere `<ProjectReference>` zu Source-Projekten hat (in unserem Fall drei: Domain, Infrastructure, App), bricht Stryker mit:

```
Test project contains more than one project reference. Please set the project
option to specify which project to mutate.
Choose one of the following references:
  C:/.../src/Calculator.Domain/Calculator.Domain.csproj
  C:/.../src/Calculator.Infrastructure/Calculator.Infrastructure.csproj
  C:/.../src/Calculator/Calculator.csproj
```

Der Anwender muss pro Layer einen separaten Run starten — das ist umständlich, wenn ein Projekt mehrere Layer hat (Clean-Architecture-Setups sind so üblich).

**Vorschlag:**
- Zusätzliche Option `--all-projects` o.ä., die alle gefundenen References sequenziell mutiert und einen kombinierten Report generiert.
- Oder eine Mehrfach-`--project`-Angabe (`--project A.csproj --project B.csproj`).

---

### 🔴 Bug #9 — _NEU in v3.1.1_ — `--mutation-profile All` crasht mit `InvalidCastException`

**Schweregrad:** HOCH (komplettes Profile-Feature unbenutzbar)

**Erwartung:** `--mutation-profile All` aktiviert den maximalen Mutator-Set (laut Doku 52 Operatoren) und produziert mehr Mutanten als `Stronger`.

**Beobachtung:** Tool bricht mit Stack-Trace ab, kein Report wird generiert:

```
[ERR] An error occurred during the mutation test run
System.AggregateException: One or more errors occurred. (Unable to cast object
of type 'Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax'
to type 'Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax'.)
 ---> System.InvalidCastException: Unable to cast object of type
'Microsoft.CodeAnalysis.CSharp.Syntax.ParenthesizedExpressionSyntax' to type
'Microsoft.CodeAnalysis.CSharp.Syntax.TypeSyntax'.

Unhandled exception. ...
   at Stryker.Core.Mutants.CsharpNodeOrchestrators.NodeSpecificOrchestrator`2
      .OrchestrateChildrenMutation(...)
   at Stryker.Core.Mutants.MutationContext.Mutate(SyntaxNode node, SemanticModel model)
   at Stryker.Core.Mutants.CsharpMutantOrchestrator.Mutate(SyntaxTree input, ...)
   at Stryker.Core.MutationTest.CsharpMutationProcess.Mutate(...)
   at Stryker.Core.Initialisation.ProjectMutator.MutateProject(...)
   at Stryker.Core.Initialisation.ProjectOrchestrator.MutateProjectsAsync(...)
   at Stryker.Core.StrykerRunner.RunMutationTestAsync(...)
   at Stryker.CLI.StrykerCli.RunStrykerAsync(...)
```

**Reproduzierbarkeit:** 100 % auf zwei verschiedenen Source-Projekten (`Calculator.Domain` mit ~150 LOC, `Calculator.Infrastructure` mit ~1.300 LOC) — also kein code-spezifischer Edge-Case, sondern ein genereller Bug in einem oder mehreren der zusätzlichen `All`-Mutatoren.

**Repro:**
```bash
dotnet stryker-netx --project Calculator.Domain.csproj --mutation-profile All
# Crasht mit obigem Stack-Trace.
```

**Diagnose:** Der Cast `ParenthesizedExpressionSyntax → TypeSyntax` deutet auf einen Mutator hin, der bei Cast-Expressions oder pattern-matching-Konstrukten den syntaktischen Unterschied zwischen `(Type)expr` (CastExpressionSyntax mit Type=TypeSyntax) und `(expr)` (ParenthesizedExpressionSyntax) übersieht und blind auf `TypeSyntax` castet.

Wahrscheinliche Kandidaten unter den "All"-spezifischen Operatoren (laut Doku-Beschreibung):
- `UoiMutator` (unary operator insertion) — könnte bei `-(x + y)` triggern, weil `(x + y)` parenthesized expression ist
- `MethodBodyReplacement` — könnte bei expression-bodied members betroffen sein
- `NakedReceiver` — eher unwahrscheinlich

**Verhalten der anderen Profile in v3.1.1:**

| Profile | Status | Created | Tested | Killed | Survived | Timeout | Score |
|---------|--------|---------|--------|--------|----------|---------|-------|
| `Defaults` | ✅ läuft durch | 271 | 203 | 142 | 55 | 6 | 72,91 % |
| `Stronger` | ✅ läuft durch | (nicht im Output, aber) **420 tested** | 420 | 277 | 127 | 16 | 69,10 % |
| `All` | 🔴 **CRASH** | — | — | — | — | — | — |

**Vorschlag zur Fehlersuche:**
1. Mutator-Liste in Stronger vs All identifizieren — die Differenz-Operatoren sind die Kandidaten.
2. Pro Differenz-Operator einen kleinen Test mit Code-Schnipseln, die `ParenthesizedExpressionSyntax` enthalten (z.B. `var x = -(a + b);`, `return (predicate ? a : b);`, `var y = !(x > 0);`).
3. Cast-Stelle finden, in `is`/`as`/Pattern-Matching ändern.
4. Regression-Test mit minimaler Repro-Codebase im stryker-netx-Repo.

**Vorläufiger Workaround für Anwender:** `--mutation-profile Stronger` statt `All` einsetzen. Stronger funktioniert in v3.1.1 sauber und liefert bereits 420 Mutanten (vs. 203 bei Defaults) — also schon einen substantiellen Mehrwert gegenüber Defaults.

---

## 3. Beobachtungen, die KEIN Bug sind

Zur Fairness:

- **Tool-Crash**: Keiner. Alle Runs liefen sauber durch.
- **HTML-Report**: Self-contained, gut lesbar, klickbare Source-Annotation pro Mutant.
- **JSON-Report**: Validates JSON, vollständige Mutant-Liste, gut maschinell parsebar.
- **Coverage-basierter Test-Lauf**: `--coverage-analysis perTest` ist Default, funktioniert, ~10× schneller als ohne.
- **`.slnx`-Support**: Fehlerlos. Erkennt Test-Projekt, baut es, mutiert die referenzierten Source-Projekte.
- **Survivor-Detection**: 55 Survivors auf Infrastructure-Layer sind realistische Test-Gaps (siehe `comparison.md` Sektion 4 für Klassifikation).
- **Profile-Validierung**: Werte `[None, Defaults, Stronger, All]` werden geprüft, ungültige Werte wie `XYZ` sauber abgelehnt mit Liste der validen Optionen.

---

## 4. Reproduzier-Setup

**Codebase:** Calculator-Demo in C# 14 / .NET 10. Falls gewünscht: Branch `feature/2-mutation-testing` enthält den Stand bei Bug-Discovery; Mutation-Reports unter `tests/Calculator.Tests/StrykerOutput/`.

**Minimaler Repro für Bug #1 (Profile-Flag):**

```bash
# 1. Tool installieren
dotnet tool install -g dotnet-stryker-netx --version 3.0.24

# 2. In Test-Project-Verzeichnis wechseln
cd tests/Calculator.Tests

# 3. Drei Runs mit identischer Source-Project-Auswahl, einzige Variation: Profile
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Defaults -O StrykerOutput/A
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile Stronger -O StrykerOutput/B
dotnet stryker-netx --project Calculator.Infrastructure.csproj --mutation-profile All       -O StrykerOutput/C

# 4. Vergleichen — alle drei Runs zeigen "271 mutants created" und Score 72,91 %
diff <(jq '.files[] | .mutants | length' StrykerOutput/A/reports/mutation-report.json) \
     <(jq '.files[] | .mutants | length' StrykerOutput/B/reports/mutation-report.json)
# (Kein Output → identisch.)
```

**Erwartet:** Unterschiedliche Mutanten-Zahlen pro Profile (analog zur Mutator-Tabelle in der Tool-Doku, 26 / 44 / 52 Operatoren).

**Tatsächlich:** Identische Zahlen, identische Survivor-Listen.

---

## 5. Empfehlung

| Entscheidung | Begründung |
|--------------|------------|
| **Tool nicht im Produktiv-CI einsetzen, bis Bug #1 gefixt ist.** | Wer `--mutation-profile Stronger` setzt, erwartet stärkere Mutanten und höhere Aussagekraft des Mutation-Scores. Die Identität der drei Profile-Outputs macht jede solche Begründung haltlos. |
| **Vor Bug-Fix: nur `Defaults` (= effektives Verhalten) dokumentieren.** | Wer trotzdem das Tool nutzt, sollte wissen, dass `Stronger` und `All` aktuell keine zusätzliche Wirkung entfalten — sonst werden falsche Schlüsse aus Score-Vergleichen gezogen. |
| **Bug #2 (Versions-Banner) ist Trust-relevant.** | Ein Tool, das seine eigene Version nicht kennt, lässt Anwender an der Code-Disziplin zweifeln. Auch wenn rein kosmetisch — der Eindruck zählt. |
| **Bug #3 (Update-Hinweis) selbsterklärend zu fixen, sobald Bug #2 behoben ist.** | Beide haben dieselbe Wurzel. |
| **Bugs #4–#8 nice-to-have.** | Niedrige Priorität, eher Polish und Doku-Sync. |

**Gesamtbewertung:** **Tool ist alpha-/beta-tauglich, aber nicht produktionsreif.** Es liefert wertvolle Survivor-Detektion und integriert sich technisch gut in die .NET-10-Toolchain — was ein echter Fortschritt gegenüber dem inkompatiblen upstream Stryker.NET 4.14.x auf .NET 10 ist. Die Anomalien (insbesondere Bug #1) verhindern aber den Status "verlässliches CI-Tool".

---

## 6. Anhang — vollständige Log-Ausgaben

### Banner & Versionscheck (jeder Run)
```
   _____ _              _               _   _ ______ _______
  / ____| |            | |             | \ | |  ____|__   __|
 | (___ | |_ _ __ _   _| | _____ _ __  |  \| | |__     | |
  \___ \| __| '__| | | | |/ / _ \ '__| | . ` |  __|    | |
  ____) | |_| |  | |_| |   <  __/ |    | |\  | |____   | |
 |_____/ \__|_|   \__, |_|\_\___|_| (_)|_| \_|______|  |_|
                   __/ |
                  |___/

Version: 1.0.0-preview.1                          ← Bug #2
A new version of stryker-netx (3.0.24) is available.  ← Bug #3
Please consider upgrading using `dotnet tool update -g dotnet-stryker-netx`
```

### Standard-Run-Log (gekürzt)
```
[INF] Analysis starting.
[INF] Analyzing 1 test project(s).
[INF] Could not find a valid analysis for target  for project          ← Bug #5
      'C:\...\tests\Calculator.Tests\Calculator.Tests.csproj'.
      Selected version is net10.0.
[INF] Found project C:\...\Calculator.Infrastructure.csproj to mutate.
[INF] Analysis complete.
[INF] Number of tests found: 357 ... Initial test run started.
[INF] 271 mutants created
[INF] Capture mutant coverage using 'CoverageBasedTest' mode.
[INF] 20    mutants got status CompileError.
[INF] 48    mutants got status Ignored.        Reason: Removed by block already covered filter
[INF] 203   total mutants will be tested

Killed:   144
Survived: 55
Timeout:  4

Your html report has been generated at: ...\reports\mutation-report.html
[INF] Time Elapsed 00:01:04
[INF] The final mutation score is 72.91 %
```

### `--version`-Versuch (Bug #4)
```bash
$ dotnet stryker-netx --version
Specify --help for a list of available options and commands.
Missing value for option 'version'

$ dotnet stryker-netx --version 99.99.99
[Banner ...] [Banner says it's running, accepts 99.99.99 silently]
```

### `--reporters`-Versuch (Bug #6)
```bash
$ dotnet stryker-netx --reporters html
Specify --help for a list of available options and commands.
Unrecognized option '--reporters'

Did you mean this?
    reporter
    open-report
```

### `--mutation-profile XYZ` — Validierung funktioniert
```bash
$ dotnet stryker-netx --mutation-profile XYZ --project Calculator.Domain.csproj
The given mutation profile (XYZ) is invalid.
Valid options are: [None, Defaults, Stronger, All]
```

---

## 7. Kontakt / Repo-Verweis

Falls Reproduktion an Originalcode gewünscht: dieses Repository (`stryker-test`) auf `feature/2-mutation-testing`-Branch enthält die exakte Codebase + die fünf Stryker-Output-Verzeichnisse mit HTML- und JSON-Reports (`tests/Calculator.Tests/StrykerOutput/`).

Bei Rückfragen: gerne per Issue im stryker-netx-Repo oder direkt per E-Mail an das Calculator-Testbed-Projekt.
