# 360°-Analyse — Sprint 173: Mutatoren-Katalog (Findings-Register)

## Executive Summary

**Abdeckung:** 55/55 Core-Mutatoren + 20/20 RegexMutators einzeln vollständig gelesen; 5 Live-Proben auf v3.3.3 (Scratch-Projekt außerhalb des Repos). **41 Findings**, davon 13 gemessen/code-pfad-verifiziert.

**Top-Befunde → Issues:**
| Sev | Finding | Issue |
|-----|---------|-------|
| **P1** | F-34: RegexMutator-CRASH auf `new Regex($"…")` bei Level ≥ Advanced (Unhandled InvalidCastException, Exit 127; Orchestrator-Schleife ohne try/catch) — zweifach gemessen | **#277** |
| **P1-Kandidat** | F-29: is-Pattern-Negation erzeugt CS0165-Mutanten im DEFAULT-Profil (jede `is T name`-Stelle mit Nutzung) | **#278** |
| **P2-Epic** | F-06/07/09/14/23/26/35: typ-/flow-blinde Mutatoren = 56 % CE-Rate auf 15-LOC-Probe unter All (UOI 20/20, ROR 8/8, Block 2/2, async-Return, struct→null, AsSpanAsMemory=Reporter-B-Wurzel) | **#279** |
| **P2** | F-01/F-33: Konstanten-Mutatoren als `Mutator.Linq`; Statement als Sammelkategorie — ignore-mutations/Reports betroffen | **#280** |

**Schlüssel-Erkenntnisse für die Folge-Sprints:** (a) Der Semantik-Pre-Filter fängt Method-Binding-Failures, aber keine Operator-/Flow-/Slot-Fehler — Erweiterungshebel F-08. (b) Die if/else-Wrap-Konstruktion erzeugt selbst Flow-CEs; `AddEndingReturn`-Mechanik (F-14) ist Sprint-174-Pflichtlektüre. (c) Die Fix-Blaupause existiert im eigenen Code (NullCoalescing/ArgumentPropagation/MemberVariable als Positiv-Referenzen). (d) Drei Doc-Kommentare behaupten fälschlich „classified as killed".

> **Programm:** 6 Analyse-Sprints (173–178), Findings-only (Issue #276). Dieses Register
> wird batch-weise fortgeschrieben; jeder Eintrag trägt Status `VERDACHT` (unverifiziert),
> `BESTÄTIGT` (Trace/Repro liegt vor, Issue angelegt), `ENTKRÄFTET` (geprüft, kein Bug —
> bleibt als Doku stehen) oder `NOTIZ` (kein Bug, aber bemerkenswert: Schwachstelle in
> Tests, Doku-Lücke, Upstream-Abweichung).
>
> **Severity:** P0 = Crash/falsches Mutationsergebnis im Default-Pfad · P1 = Edge-Case-Bug
> mit realem Trigger · P2 = Robustheit/UX/irreführendes Verhalten · P3 = Smell/Test-Lücke.
>
> **Methodik:** jede Datei vollständig gelesen; Checkliste: Emission-Typdisziplin
> (ADR-047/049-Klasse) · Slot-Kompatibilität (ADR-027/028/032-Klasse) · Guard-Lücken
> (unsigned, nullable, const, checked/unchecked, Expression-Trees, ref/in/out, async) ·
> Äquivalenz-Unfälle (Mutation ≡ Original) · Profile-Membership-Konsistenz ·
> Kultur/Format · PIT-/cargo-mutants-/mutmut-Semantik-Treue · Tests-als-Orakel.

## Abdeckungs-Protokoll

| Batch | Dateien | Status |
|-------|---------|--------|
| 1 | BinaryExpressionMutator, InlineConstantsMutator, ConstantReplacementMutator (+ Mutator.cs-Enum, IgnoreMutationMutantFilter/ExcludeLinqExpressionFilter als Konsumenten-Verifikation) | ✅ gelesen |
| 2 | AodMutator, RorMatrixMutator, UoiMutator (+ RoslynSemanticDiagnosticsEquivalenceFilter vorgezogen als Severity-Abhängigkeit) + **End-to-End-Probe** (15-LOC-Projekt, Profile All, echtes v3.3.3) | ✅ gelesen + gemessen |
| 3 | BlockMutator, MethodBodyReplacementMutator, StatementMutator, NegateConditionMutator, ConditionalExpressionMutator (+ MutantPlacer.AddEndingReturn / BaseFunctionOrchestrator als F-09/F-10-Mechanik-Vorgriff) | ✅ gelesen |
| 4 | BooleanMutator, CheckedMutator, StringMutator, StringEmptyMutator, StringMethodMutator, StringMethodToConstantMutator, InterpolatedStringMutator, MathMutator | ✅ gelesen |
| 5 | LinqMutator, PrefixUnaryMutator, PostfixUnaryMutator, NullCoalescingExpressionMutator | ✅ gelesen |
| 6 | MutatorBase, TypeAwareMutatorBase, AsSpanAsMemoryMutator, NakedReceiverMutator, TypeDrivenReturnMutator, ArgumentPropagationMutator, MemberVariableMutator | ✅ gelesen |
| 7 | AsyncAwaitMutator, AsyncAwaitResultMutator, ConfigureAwaitMutator, TaskWhenAllToWhenAnyMutator, MatchGuardMutator, WithExpressionMutator, IsPatternExpressionMutator, SwitchArmDeletionMutator (+ **2. Probe-Messung: is-Pattern-Negation CE im Default-Profil**) | ✅ gelesen + gemessen |
| 8 | BinaryPattern, RelationalPattern, ObjectCreation, ConstructorNull, Initializer, ArrayCreation | ✅ gelesen |
| 9 | AssignmentExpression, CollectionExpression, DateTime, DateTimeAddSign, ExceptionSwap | ✅ gelesen |
| 10 | GenericConstraint, GenericConstraintLoosen, SpanMemory, SpanReadOnlySpanDeclaration, RegexMutator, MathExpression (+ **3. Probe-Messung: RegexMutator-CRASH** auf interpoliertem Pattern, Level Advanced) — **damit alle 55 Core-Mutator-Dateien vollständig gelesen** | ✅ gelesen + gemessen |

| 11 | **RegexMutators komplett** (20 Dateien): Orchestrator, Mutation, IRegexMutator, RegexMutatorBase + 16 Mutatoren (Anchor, CharClass×7, Quantifier×6, Group, LookAround) | ✅ gelesen |
| 12 | Verifikations-Proben 4+5: F-23 async-Return + F-35 struct-Ctor (+ unfreiwillige #277-Zweitbestätigung: Probe-eigener `new Regex($)` killte den Stronger-Lauf mit Exit 127) | ✅ gemessen |

**ABDECKUNG VOLLSTÄNDIG: 55/55 Core-Mutatoren + 20/20 RegexMutators einzeln gelesen; 5 Live-Proben.**

## Findings

| # | Status | Sev | Datei | Kurzbefund |
|---|--------|-----|-------|------------|
| F-01 | BESTÄTIGT | P2 | InlineConstantsMutator.cs:108, ConstantReplacementMutator.cs:190 | Beide Konstanten-Mutatoren melden `Type = Mutator.Linq` — `ignore-mutations: ["linq"]` deaktiviert still die Konstanten-Mutationen; Reports kategorisieren sie als „Linq methods"; ExcludeLinqExpressionFilter inspiziert Literal-Nodes |
| F-02 | NOTIZ | P3 | BinaryExpressionMutator.cs:56 | String-`+`-Skip ist syntax-heuristisch (`IsAStringExpression` auf Left/Right); `stringRet() + stringRet()` ohne syntaktische String-Signale → `-`-Mutante → CS0019 → Rollback-Noise. SemanticModel-Param verfügbar, ungenutzt. Gleiches Muster: `DateTime - DateTime` → `+` |
| F-03 | NOTIZ | P3 | InlineConstantsMutator.cs:47 | Literal in byte/sbyte/short-Slot (Token.Value=int): Randwerte (`byte b = 255` → `256`) erzeugen CS0031 → Rollback-Noise; SemanticModel-Slot-Check würde es vermeiden (bewusste ADR-047-Entscheidung syntax-only — als Verbesserungs-Kandidat notiert) |
| F-04 | NOTIZ | P3 | ConstantReplacementMutator.cs:67 | Literal `2147483648` (Token.Value=**uint**) unter unärem Minus (= int.MinValue-Schreibweise): uint-Pfad emittiert `0U`/`1U` → `-(0U)` ist **long** → CS0266 in int-Slot → Rollback-Noise (Doppel-Edge, real selten) |
| F-05 | NOTIZ | P3 | ConstantReplacementMutator.cs:122 | float/double `→-c` bei `v=0`: `-0f`-Mutante ist semantisch quasi-äquivalent (`0f == -0f`) → unkillbare Mutante (dokumentierter S1244-Trade-off; double hat zusätzlich KEINEN Equality-Skip → `0.0→0` ebenfalls No-op-Mutante) |
| F-06 | **BESTÄTIGT (gemessen)** | **P2** | RorMatrixMutator.cs:86 | Volle Ordnungs-Matrix feuert auf JEDEN `==`/`!=` — auch Referenztypen: `name == null` → `<,<=,>,>=` = 4 CE pro Null-Check. **Probe: 8/8 ROR-Mutanten CE.** SemanticModel-Param verfügbar/ungenutzt; PIT wendet ROR nur auf primitive Vergleiche an |
| F-07 | **BESTÄTIGT (gemessen)** | **P2/P1-Kandidat** | UoiMutator.cs:64 | UOI ist komplett TYP-blind: `++/--` auf string/object/Array-Identifiern kompiliert nie — **Probe: 20/20 UOI-Mutanten CE** (string-Param, object-Params, string[]-Param, get-only `Length`). Unter `--profile All` plausibel dominanter Treiber der Reporter-31-%-CE-Rate. ADR-027 löste die CRASH-Klasse, nicht die CE-Klasse |
| F-08 | BESTÄTIGT (gemessen, präzisiert) | P3 | Mutants/Filters/RoslynSemanticDiagnosticsEquivalenceFilter.cs:104 | Pre-Filter fängt Symbol-Binding-Failures — **inkl. Method-Overload-Failures** (Gegen-Probe: `xs.Any→xs.All` wird gefiltert ✓ Design-Klasse funktioniert). NICHT gefangen: Operator-Typfehler (CS0019/CS0023 — `name<null`, `name++`), Readonly-Verletzungen (CS1656/CS0200), Flow-Fehler (CS0161) — Probe: 0 von 31 CEs dieser Klassen vorgefiltert. Erweiterungs-Richtung: `GetSpeculativeTypeInfo`/Operator-Resolution zusätzlich abfragen |
| F-09 | BESTÄTIGT (gemessen) | P2 | BlockMutator.cs (Batch 3 zu lesen) | Block-Removal auf Methodenkörpern non-void-Methoden ⇒ CS0161 garantiert — **exakt die Reporter-D-Klasse** (PluginManager, BUG_REPORT_9 honest-deferred). Probe: 2/2 Block-CEs. Guard „non-void-Methodenbody" fehlt (Detailprüfung Batch 3) |
| F-10 | VERDACHT | P2 | MethodBodyReplacementMutator.cs (Batch 3) | Probe: `SumLengths` → `{ return default; }` ist CE, obwohl `return default` im int-Methodenkörper kompilieren müsste — Mechanik unklar (Emission? Slot? Cascade?) — Batch-3-Untersuchung |
| F-11 | NOTIZ | P3 | UoiMutator.cs:191 | `IsRefOrOutArgument` prüft nur ref/out — `in`-Keyword-Argumente fehlen (`M(in x)` → `M(in x++)` = CE); ebenso `nameof(x)`-Argumente (CS8081) und const-Initializer (CS0133) — alles CE-Noise-Klassen |
| F-12 | NOTIZ | P3 | AodMutator.cs:24 | AOD skippt String-Konkat NICHT (anders als BinaryExpressionMutator): `"a" + 5` → keep-right `5` im string-Slot = CE; gemischte Operanden-Typen (DateTime+TimeSpan keep-right) gleiche Klasse |
| F-13 | NOTIZ (Architektur-Einsicht) | — | Instrumentation allgemein | Re-Kalibrierung der Analyse: Conditional-Instrumentation hält Original+Mutante NEBENEINANDER (`if/else` bzw. Ternary) — CE-Klassen entstehen durch untypbare MUTATIONS-ARME bzw. durch die Wrap-Konstruktion (Fall-Through), nie durch „Wegfall des einzigen yield/return". StatementMutators Guards sind dahingehend konsistent |
| F-14 | VERDACHT (Mechanik) | P2 | Mutants/CsharpNodeOrchestrators/BaseFunctionOrchestrator.cs:117–142 | Asymmetrie: No-Mutations-Pfad ruft `MutantPlacer.AddEndingReturn` explizit (CS0161-Mitigation, Zeile 130–135), Mutations-Pfad delegiert an `context.InjectMutations(..., !returnType.IsVoid())` — Probe beweist, dass die Mitigation für Block-Removal-Mutanten NICHT wirkt (CS0161-Klasse 2/2 CE). Wirkkette in MutationContext/Engines = Sprint-B-Pflichtlektüre; gemeinsame Wurzel-Kandidatin für F-09 + Reporter-D |
| F-15 | NOTIZ | P3 | StatementMutator.cs:70 | Return-Guard prüft nur DeclarationPattern im umgebenden if-CONDITION; Guards wirken solide (Probe: 0 Statement-CEs). `foreach`-Pattern-Skips u.ä. von Instrumentation getragen (F-13) |
| F-16 | NOTIZ | P3 | NegateConditionMutator.cs:31 | Deckt nur if/while-Conditions; do-while-, for-, Ternary-Conditions werden nicht negiert (Upstream-Parität vermutet — als Katalog-Lücke notiert, kein Bug) |
| F-17 | BESTÄTIGT (gemessen, abgestuft) | P3 | LinqMutator.cs:126–138 | `FindEnclosingInvocation` strukturell tot (Schleife verlangt `current is MA/MB`; bei `list.Any()` ist Parent direkt die Invocation → immer null → Guard greift nie). **Probe-Beweis:** `xs.Any()`→`All()` wird emittiert, aber vom RoslynSemanticDiagnostics-Filter gefressen (DBG: „Equivalent mutant skipped … xs.Any -> xs.All") → kein User-Schaden, nur toter Code + verschwendete Emission + unbeabsichtigte Downstream-Abhängigkeit. Test-Orakel-Lücke: Dogfood-Test deckt NUR den verketteten Fall (`?.a.b.All`), in dem die Schleife zufällig läuft |
| F-18 | VERDACHT | P3 | MathMutator.cs:36 | `Log→Pow` ohne Arity-Check: `Math.Log(x)` (1 Arg) → `Pow(x)` = CS7036 (Pow braucht 2) — nur 2-Arg-Log wäre valide; ferner MathF komplett ungemutiert (Symbol-Check nur `System.Math`) — Katalog-Lücke modernes C# |
| F-19 | VERDACHT | P3 | StringMethodToConstantMutator.cs:54 | `ElementAt`→`'\0'`: emittiert Char-Token in `SyntaxKind.StringLiteralExpression`-Hülle — Kind/Token-Mismatch (korrekt: CharacterLiteralExpression); kompiliert vermutlich (Token-Value zählt), aber off-spec für Kind-basierte Konsumenten |
| F-20 | NOTIZ | P3 | StringMutator.cs:21 | Regex/Guid-Ctor-Skip via fixer 3-Ebenen-Parent-Walk — Parenthesen/named-args verschieben die Tiefe → Pattern-Strings werden doch mutiert (laufzeit-noisy, nicht CE); zudem `"…"u8`-Literale (Utf8StringLiteralExpression) generell ungemutiert |
| F-21 | NOTIZ | P3 | StringMethodMutator.cs:23, MathMutator.cs:65, NullCoalescing:65 | Mehrere Mutatoren haben explizite `semanticModel == null`-Lax-Pfade („compatibility with existing tests") — Produktion liefert immer ein Model; Tests exerzieren teils den laxeren Pfad = Orakel-Diskrepanz (Test-Infrastruktur-Schuld) |
| F-22 | NOTIZ (Positiv-Referenz) | — | NullCoalescingExpressionMutator.cs | Vorbildlich semantik-bewusst (Nullability-FlowState als Typ-Kompatibilitäts-Proxy, Throw-Guard, CollectionExpression-Sonderfall) — Referenz-Implementierung für die F-06/F-07-Fix-Richtung |
| F-23 | VERDACHT (stark) | **P2** | TypeDrivenReturnMutator.cs:43+67 | Kein Async-Guard: `async Task<int> M(){ return 5; }` → Mutation `return Task.FromResult(default(int))` = CS4016 (in async muss T, nicht Task<T> returned werden) — async-Returns sind in modernem Code allgegenwärtig → CE-Klasse unter Stronger/All |
| F-24 | VERDACHT | P2 | TypeAwareMutatorBase.cs:61 (GetReturnType) | Ancestor-Walk behandelt nur ParenthesizedLambda MIT explizitem ReturnType; Simple-/Anonymous-Lambdas ohne Typ-Arm werden ÜBERSPRUNGEN → `return` in Lambda-Block-Body erbt den äußeren METHODEN-Return-Typ → falsch typisierte Replacements (CE oder falsch-typte Mutante). Auch Indexer-Accessoren fehlen |
| F-25 | NOTIZ | P3 | TypeDrivenReturnMutator.cs:88 | Kein Skip-wenn-identisch: `return 0;`/`return false;`/`return string.Empty;` erzeugen No-op-Mutanten (Original ≡ Replacement) — unkillbar; Equivalence-Pipeline-Abdeckung in Sprint B prüfen (ConservativeDefaultsEquality?) |
| F-26 | BESTÄTIGT (Design + Reporter-Evidenz) | P2 | AsSpanAsMemoryMutator.cs | Namens-Swap ohne Slot-/Receiver-Typprüfung = Reporter-B.1/B.2-Wurzel (Span-Slot ← Memory-Resultat = CS-Kaskade; Semantik-Filter fängt nur Binding der Replacement-Expression SELBST, nicht den Slot-Kontext). Doc-Kommentar behauptet „non-compiling mutants classified as killed" — real ist CompileError/Rollback (Doc-Irrtum). Bonus: `AsReadOnlySpan`/`AsReadOnlyMemory`-Map-Einträge existieren in der BCL nicht (tote Einträge) |
| F-27 | BESTÄTIGT (gemessen) | P3 | NakedReceiverMutator.cs:39 | Typ-blind (`list.Any()` → `list` im bool-Slot — Probe 1/1 CE); Doc nennt foreach-in-Skip, Code prüft nur await/throw (Doc/Code-Drift); Type=Mutator.Statement für Expression-Mutator (Kategorisierungs-Smell wie F-01) |
| F-28 | NOTIZ (Positiv-Referenz) | — | ArgumentPropagationMutator.cs, MemberVariableMutator.cs | Saubere ADR-015-Implementierungen: Symbol-Lookup, ClassifyConversion implicit-only bzw. Instanz-Member-Gate — zweite Referenz neben F-22 |
| **F-29** | **BESTÄTIGT (gemessen)** | **P1-Kandidat** | IsPatternExpressionMutator.cs:35 | Negation von DECLARATION-Patterns ohne Designation-Guard: `if (o is string s) { use(s); }` → `is not string s` macht `s` im True-Zweig unassigned → **CS0165. Probe: 1/1 CE — im DEFAULT-Profil, MutationLevel Basic** (kein Opt-in!). Prime-Verdächtiger für Reporter-D-CS0165-Sites (bisher Block-Removal zugeschrieben). Ironie: NegateConditionMutator skippt IsPattern explizit („can't mutate without breaking build") — die Pattern-Gefahr war bekannt, nur nicht im Pattern-Mutator selbst. Fix: Skip (oder not-Wrap nur) wenn Pattern Designationen enthält, deren Variablen im True-Pfad gelesen werden (konservativ: bei JEDER Designation skippen) |
| F-30 | NOTIZ | P3 | AsyncAwaitResultMutator.cs:32, TaskWhenAllToWhenAnyMutator.cs:22, AsSpanAsMemory (F-26) | Wiederkehrender DOC-IRRTUM „non-compiling mutants classified as killed" (3 Dateien, beruft sich auf GenericConstraint-Präzedenz) — real: CompileError/Rollback. `await task`(non-generic)→`.Result` wird vermutlich vom Semantik-Filter gefressen (Member-Binding-Failure, Any→All-Klasse) — verifizierbar; WhenAll→WhenAny-Slot-Fehler dagegen nicht (Slot-Kontext-Klasse) |
| F-31 | NOTIZ | P3 | SwitchArmDeletionMutator.cs:45, MatchGuardMutator.cs:39 | Exhaustiveness-Kante: Discard-Arm MIT `when`-Guard gilt als Discard (Arm-Drop kann CS8509 auslösen); `when false` auf letztem geguardetem Arm gleiche Klasse — unter TreatWarningsAsErrors wird die Warnung zum CE. Guard: `WhenClause is null` prüfen |
| F-32 | NOTIZ (Positiv) | — | ConfigureAwaitMutator, AsyncAwaitMutator, WithExpressionMutator, MatchGuardMutator (true-Arm), SwitchArmDeletion (Kern) | Saubere, kompilierfeste Designs; `x with { }`-Leerfall valide (Clone-only-Mutante legitim) |
| F-33 | NOTIZ (Querschnitt) | P3 | diverse | `Type = Mutator.Statement` als Sammelkategorie für Expression-Mutatoren (AsSpanAsMemory, NakedReceiver, AsyncAwait×2, TaskWhenAll, TypeDrivenReturn, ArgumentPropagation, DateTime×2, SpanMemory, GenericConstraint×2) — gleiche Kategorisierungs-Schuld wie F-01 |
| **F-34** | **BESTÄTIGT (gemessen)** | **P1** | RegexMutator.cs:44 + Helpers/RoslynHelper.cs:18 | **Tool-CRASH**: `IsAStringExpression()` bejaht InterpolatedStringExpression, direkt danach Hard-Cast auf LiteralExpressionSyntax → `new Regex($"…")` + Level ≥ Advanced (auch via Profile-Auto-Bump ADR-025) ⇒ **Unhandled InvalidCastException, Exit 127, kein Report** (Probe-Beweis). Orchestrator-Schleife hat KEIN try/catch um `mutator.Mutate` (CsharpMutantOrchestrator.cs:223) — Einzel-Mutator-Bug tötet Gesamtlauf. → **Issue #277** |
| F-35 | VERDACHT (stark) | P2 | ConstructorNullMutator.cs:25 | Doc behauptet „Type-aware: only emits when context permits null" — Code prüft NUR throw/ctor-initializer, keinerlei Typ-Kontext: `new Point(1,2)` (struct!) → `null` = CS0037 für JEDE Struct-Konstruktion (Stronger/All); Semantik-Filter fängt null-Literal-Binding nicht |
| F-36 | NOTIZ | P3 | BinaryPatternMutator.cs:18, GenericConstraintLoosenMutator.cs:94 | and↔or-Swap ohne Designation-Guard (`is T x and …` → or = CS8780-Familie wie F-29); `class`→`new()` in Multi-Constraint-Klausel verletzt new()-muss-letzt-Regel (CS0401) |
| F-37 | NOTIZ | P3 | DateTimeMutator.cs:30, ArrayCreationMutator.cs:28 | `System.DateTime.Now` (qualifizierter Receiver) wird nicht erkannt (nur IdentifierName) — Katalog-Lücke; explizit dimensionierte Arrays `new int[2]{1,2}` → `{}` = CS0847-Kante |
| F-38 | NOTIZ (Positiv) | — | CollectionExpressionMutator.cs, RegexMutator.cs:51 (Validierungs-Idee), ExceptionSwapMutator, DateTimeAddSignMutator, AssignmentExpressionMutator, SpanMemoryMutator | Qualitäts-Designs: Collection-Leerung mit Typ-Cast-Erhalt; Regex-Replacement-Validierung mit 200-ms-Timeout; Whitelist-Swaps mit Signatur-Kompatibilität; Minus-Drop statt Doppel-Negation |
| F-39 | NOTIZ (Doku) | P3 | SpanReadOnlySpanDeclarationMutator.cs:58 | Profile.None final (ADR-027) — Katalog zählt de facto 51 aktive von „52 Mutatoren"; README/Marketing-Zahl bei Gelegenheit präzisieren |
| F-40 | NOTIZ | P3 | RegexMutators/Mutators/LookAroundMutator.cs:17 | Copy-Paste-DisplayName: Lookaround-Flip meldet sich als „Regex greedy quantifier quantity mutation" in Reports |
| F-41 | NOTIZ (Positiv) | — | RegexMutators-Projekt gesamt | Hohe Qualität: Bounds-validierte Range-Mutationen, Lazy-Node-Längen-Handling, [\w\W]-Äquivalenz-Skip, explizites CanHandle-Override für Reluctant-Addition; Parse-Fehler → stiller Skip; Upstream-Validierungsschicht (200-ms-Regex-Compile) fängt invalide Outputs. Mikro: ToAnyChar-Skip hängt an `is List<RegexNode>`-Typtest |

## Status-Upgrades nach Verifikations-Proben 4+5

- **F-23 → BESTÄTIGT (gemessen):** `Type-driven return: Task<int> → Task.FromResult(default(int))` = CompileError (Probe, Profile Stronger) — Async-Guard fehlt.
- **F-35 → BESTÄTIGT (gemessen):** `Constructor → null: 'new Point(...)' → 'null'` = CompileError (Probe, Profile All) — Doc-behauptete Typ-Awareness existiert nicht.
- **F-34/#277-Zweitbestätigung:** Probe-eigenes `new Regex($"…")` killte den Stronger-Lauf (Exit 127) — jeder Codebase-weite Stronger/All-Lauf über Code mit interpolierten Regex-Patterns stirbt am Crash.

## Detail-Einträge

### F-01 (BESTÄTIGT, P2): Konstanten-Mutatoren als `Mutator.Linq` kategorisiert

**Evidenz:**
- `src/Stryker.Abstractions/Mutator.cs` — Enum ist Upstream-1:1, hat KEINEN `Number`/`Constant`-Member; die netx-neuen Konstanten-Mutatoren (Sprint 10/14) wählten `Linq` als Verlegenheits-Typ.
- `src/Stryker.Core/MutantFilters/IgnoreMutationMutantFilter.cs:19` — Ausschluss via `options.ExcludedMutations.Contains(mutant.Mutation.Type)` ⇒ `ignore-mutations: ["linq"]` entfernt auch alle Inline-Constants-/CRCR-Mutanten (still, undokumentiert).
- `src/Stryker.Core/MutantFilters/ExcludeLinqExpressionFilter.cs:24` — Vorselektion `Type == Mutator.Linq` schickt Literal-OriginalNodes in `IsIgnoreExpression` (LINQ-Invocation-Inspektion auf Nicht-Invocations; Robustheit in Sprint B/Filter-Analyse prüfen).
- Reports gruppieren nach `Mutation.Type`-Description ⇒ Konstanten erscheinen als „Linq methods".

**Fix-Richtung (für Fix-Backlog Sprint 178):** additiver Enum-Member `Number` (`[MutatorDescription("Number literals")]`), beide Mutatoren umstellen; additiv = config-kompatibel (bestehende Namen unverändert; Upstream-Schema-Treue bleibt, da `ignore-mutations` freie Strings validiert). Tests: IgnoreMutation-Filter-Fall + Report-Kategorisierung.

**Tests-als-Orakel:** ConstantReplacementMutatorTests/InlineConstantsMutatorTests prüfen `Type` nicht — Lücke schloss das Sichtbarwerden aus.

### Mess-Protokoll: End-to-End-Probe (Grundlage F-06…F-10)

**Setup:** `%TEMP%/stryker-probe` — 15-LOC-Classlib (string-Null-Check `==`, object-`!=`, foreach über string[] mit `item.Length`) + 3 xunit-Tests; echtes Tool (Release-CLI HEAD = v3.3.3-Stand), `--mutation-profile All`, Voll-Lauf mit JSON-Report.

**Ergebnis:** 55 Mutanten gesamt, **31 CompileError (56 %)**:

| Quelle | CE/Total | Klasse |
|--------|----------|--------|
| UOI (×5 Identifier × 4 Varianten) | 20/20 | `++/--` auf string/object/string[]-Identifiern + get-only `Length` — typ-blind |
| ROR-Matrix (`==`,`!=` auf Referenztypen) | 8/8 | `<,<=,>,>=` auf string/object — CS0019 |
| Block removal | 2/2 | CS0161 non-void ohne return — Reporter-D-Klasse |
| MethodBodyReplacement `{ return default; }` | 1/1 | unerwartet (F-10, Batch 3) |

**Schlussfolgerungen:** (a) Der RoslynSemanticDiagnostics-Pre-Filter hat 0/31 gefangen — Operator-/Flow-Typfehler sind keine Binding-Failures (F-08). (b) Die CE-Klassen sind user-sichtbar (Rollback-Log „31 mutants got status CompileError") und erklären strukturell die 31-%-CE-Rate des Reporters unter `All`. (c) Fix-Hebel mit größtem Effekt: Typ-Gates via vorhandenem (ungenutztem!) `semanticModel`-Parameter in UOI + ROR; Return-Flow-Guard in BlockMutator.
