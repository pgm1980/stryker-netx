# Mutation Testing Frameworks — Vergleichsmatrix

**PIT (Java) · cargo-mutants (Rust) · mutmut (Python) · Stryker.NET (C#)**

Stand: April 2026 — basierend auf den jeweils offiziellen Dokumentationen und Quellen.

---

## 1. Framework-Steckbriefe

| Framework | Sprache | Mutation-Ebene | Aktive Version | Besonderheit |
|---|---|---|---|---|
| **PIT (PITest)** | Java/JVM | Bytecode (ASM) | 1.19.x | Granulare Operatoren, Gruppen (DEFAULTS/STRONGER/ALL), umfangreiche experimentelle Operatoren aus akademischer Forschung |
| **cargo-mutants** | Rust | AST (syn) | 25.x | Stark typgetrieben — kennt `Result`, `Option`, Collections; ersetzt ganze Funktionskörper plus feingranulare Operatoren |
| **mutmut** | Python | CST (libcst) | 3.x | AST-treues, sehr „subtiles" Mutieren; Trampoline-basierte Ausführung (mutiert nur aufgerufene Funktionen) |
| **Stryker.NET** | C# | Roslyn AST | 4.x | Breitester Operator-Katalog im Direkt-Vergleich (LINQ, Math, Regex, String-Methods, Pattern Matching) |

---

## 2. Vergleichsmatrix nach Mutationskategorie

Legende: ✅ = unterstützt · ⚠️ = teilweise/experimentell · ❌ = nicht unterstützt · — = nicht anwendbar in der Sprache

### 2.1 Arithmetische Operatoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `+ ↔ -` | ✅ MATH | ✅ | ✅ | ✅ arithmetic |
| `* ↔ /` | ✅ MATH | ✅ | ✅ | ✅ arithmetic |
| `% → *` / `% → /` | ✅ MATH | ✅ `% → /, +` | ✅ | ✅ arithmetic |
| `+ → *`, `- → /` (cross) | ⚠️ AOR (exp.) | ✅ | ❌ | ❌ |
| AOD (Operator Deletion: `a+b → a`, `a+b → b`) | ⚠️ AOD (exp.) | ❌ | ❌ | ❌ |

### 2.2 Bitweise Operatoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `& ↔ \|` | ⚠️ OBBN (exp.) | ✅ | ✅ | ✅ bitwise |
| `^` Mutation | ✅ MATH (`^→&`) | ✅ `^ → &, \|` | ✅ | ✅ bitwise (`a^b → ~(a^b)`) |
| `<< ↔ >>` | ✅ MATH | ✅ | ✅ | ✅ bitwise |
| `>>>` (unsigned shift) | ✅ MATH | — | — | — |
| Bitwise Operator Deletion | ⚠️ OBBN | ❌ | ❌ | ❌ |
| `&=`, `\|=`, `^=` | ❌ | ✅ assignment | ✅ | ✅ assignment |

### 2.3 Relationale / Vergleichsoperatoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Boundary (`<→<=`, `>→>=`) | ✅ CONDITIONALS_BOUNDARY | ❌ (zu viele False Positives) | ✅ | ✅ equality |
| Negation (`==↔!=`, `<↔>=`) | ✅ NEGATE_CONDITIONALS | ✅ | ✅ | ✅ equality |
| Vollständige ROR-Matrix (5 Submutators) | ⚠️ ROR (exp.) | ❌ | ❌ | ⚠️ teilweise |
| `is ↔ is not` (Pattern Matching) | — | — | ✅ | ✅ equality |

### 2.4 Logische Operatoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `&& ↔ \|\|` | ❌ direkt (nur via NEGATE) | ✅ | ✅ (`and ↔ or`) | ✅ logical |
| `&& → ==`, `\|\| → !=` | ❌ | ✅ | ❌ | ❌ |
| `and ↔ or` Pattern Matching | — | — | — | ✅ logical |
| `^` als Logical XOR mutieren | ❌ | ❌ | ❌ | ✅ logical (`^→==`) |

### 2.5 Unäre Operatoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Negation invertieren (`-x → x`) | ✅ INVERT_NEGS | ✅ (Deletion) | ✅ | ✅ unary (`-↔+`) |
| Boolean negation (`!x → x`) | ❌ | ✅ (Deletion) | ✅ | ✅ boolean |
| Unary Insertion (`a → a++`, `++a` etc.) | ⚠️ UOI (exp.) | ❌ | ✅ | ❌ |
| Negation einfügen (`x → -x`) | ⚠️ ABS (exp.) | ❌ | ✅ | ❌ |
| Bitwise NOT `~x → x` | ❌ | ❌ | ❌ | ✅ unary |

### 2.6 Increment/Decrement & Update

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `++ ↔ --` | ✅ INCREMENTS | — (kein `++` in Rust) | — | ✅ update |
| `+= ↔ -=` etc. | ✅ INCREMENTS | ✅ assignment | ✅ | ✅ assignment |
| Increment entfernen | ⚠️ REMOVE_INCREMENTS | ❌ | ❌ | ⚠️ via statement removal |
| Postfix ↔ Prefix (`a++ ↔ ++a`) | ❌ | — | — | ✅ update |

### 2.7 Konstanten und Literale

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Inline Integer Constants (Inkrementieren) | ✅ INLINE_CONSTS | ❌ | ✅ (n→n+1) | ❌ |
| Constant Replacement (CRCR: `c→0,1,-1,c+1,c-1,-c`) | ⚠️ CRCR (exp.) | ❌ | ⚠️ teilweise | ❌ |
| Float Constants | ✅ INLINE_CONSTS | ❌ | ✅ | ❌ |
| Boolean Literal Toggle | ✅ TRUE/FALSE_RETURNS | ✅ | ✅ | ✅ boolean |
| String Literal `"foo" → ""` | ❌ | ✅ | ✅ | ✅ string |
| String `"" → "Stryker was here!"` | ❌ | ✅ (`"" → "xyzzy"`) | ❌ | ✅ string |

### 2.8 Return Values (Rückgabewerte)

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Empty Returns (`""`, `0`, `emptyList()`, …) | ✅ EMPTY_RETURNS | ✅ FnValue | ❌ | ⚠️ `return default` |
| `false` Returns | ✅ FALSE_RETURNS | ✅ | ❌ | ✅ boolean |
| `true` Returns | ✅ TRUE_RETURNS | ✅ | ❌ | ✅ boolean |
| `null` Returns | ✅ NULL_RETURNS | ✅ (`Option::None`) | ❌ | ❌ |
| Primitive Default Returns (0) | ✅ PRIMITIVE_RETURNS | ✅ | ❌ | ❌ |
| Typgetriebene Werte (`Result::Ok`, `Vec::new`, `HashMap` etc.) | ❌ | ✅ **stark** | ❌ | ❌ |

### 2.9 Methodenaufrufe

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Void Method Call entfernen | ✅ VOID_METHOD_CALLS | ✅ FnValue (Body-Replace) | ✅ statement deletion | ✅ statement removal |
| Non-Void Call → Default-Wert | ✅ NON_VOID_METHOD_CALLS | ✅ | ❌ | ⚠️ block removal |
| Constructor → null | ✅ CONSTRUCTOR_CALLS | ❌ | ❌ | ❌ |
| Naked Receiver (`a.foo(b) → a`) | ⚠️ EXP_NAKED_RECEIVER | ❌ | ❌ | ❌ |
| Argument Propagation (`foo(a,b) → a`) | ⚠️ EXP_ARGUMENT_PROPAGATION | ❌ | ❌ | ❌ |

### 2.10 Kontrollfluss

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `if` immer wahr/falsch | ✅ REMOVE_CONDITIONALS | ❌ | ❌ | ✅ conditional (`?:`) |
| Switch/Match-Arme entfernen | ⚠️ EXP_SWITCH | ✅ (mit Wildcard) | ❌ | ❌ |
| Match Guards `→ true/false` | — | ✅ | ❌ | ❌ |
| `break` entfernen | ❌ | ❌ | ✅ (→`continue`) | ✅ removal |
| `continue` entfernen | ❌ | ❌ | ❌ | ✅ removal |
| `return` entfernen | ❌ | ❌ | ✅ | ✅ removal |
| `throw`, `goto`, `yield` entfernen | ❌ | ❌ | ❌ | ✅ removal |

### 2.11 String-Methoden / String-API

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `StartsWith ↔ EndsWith` | ❌ | ❌ | ❌ | ✅ stringmethod |
| `IndexOf ↔ LastIndexOf` | ❌ | ❌ | ❌ | ✅ stringmethod |
| `ToLower ↔ ToUpper` (+ Invariant-Varianten) | ❌ | ❌ | ❌ | ✅ stringmethod |
| `TrimStart ↔ TrimEnd` | ❌ | ❌ | ❌ | ✅ stringmethod |
| `PadLeft ↔ PadRight` | ❌ | ❌ | ❌ | ✅ stringmethod |
| `Substring → ""`, `Trim → ""` | ❌ | ❌ | ❌ | ✅ stringmethod |
| `IsNullOrEmpty/IsNullOrWhiteSpace` Equivalents | ❌ | ❌ | ❌ | ✅ string |

### 2.12 Collections / LINQ / iteratoren

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Empty Collection Returns | ✅ EMPTY_RETURNS (Java Collections) | ✅ (`vec![]`, `HashSet::new()`) | ❌ | ❌ |
| One-element Collections | ❌ | ✅ | ❌ | ❌ |
| Initializer-Listen leeren (`{1,2}→{}`) | ❌ | ❌ | ❌ | ✅ initializer |
| Collection Expression (`[1,2,3]→[]`) | — | — | — | ✅ collectionexpression |
| LINQ-Methoden-Swaps (32+ Stück: `All↔Any`, `First↔Last`, `Min↔Max`, `OrderBy↔OrderByDescending`, `Skip↔Take`, …) | ❌ | ❌ | ❌ | ✅ linq **(Alleinstellungsmerkmal)** |
| BigInteger-Methoden swaps | ⚠️ EXP_BIG_INTEGER | ❌ | ❌ | ❌ |

### 2.13 Math-Methoden

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `Sin ↔ Cos`, `Sin ↔ Sinh`, `Sin ↔ Tan` etc. (50+ Mappings) | ❌ | ❌ | ❌ | ✅ math **(einzigartig)** |
| `Floor ↔ Ceiling` | ❌ | ❌ | ❌ | ✅ math |
| `Log ↔ Exp`, `Log ↔ Pow` | ❌ | ❌ | ❌ | ✅ math |
| `Min ↔ Max`, `MinBy ↔ MaxBy` | ❌ | ❌ | ❌ | ✅ math/linq |

### 2.14 Null-Handling / Optional

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `?? ` Operator (`a??b → b??a`, `→a`, `→b`) | — | — | — | ✅ nullcoalescing |
| `??=` | — | — | — | ✅ assignment |
| `Option::Some ↔ None` | — | ✅ | — | — |
| `Result::Ok ↔ Err` | — | ✅ konfigurierbar | — | — |
| Optional Chaining (`?.`) | — | — | — | ❌ (Stryker**JS** ja, .NET nein) |

### 2.15 Member Variables / Felder / Struct

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Member-Assignment entfernen (Default-Wert) | ⚠️ EXP_MEMBER_VARIABLE | ❌ | ❌ | ❌ |
| Struct-Literal-Feld löschen (mit `..Default::default()`) | — | ✅ | — | — |
| Object-Initializer leeren (`new Foo{X=1}→new Foo{}`) | — | — | — | ✅ initializer |

### 2.16 Regex

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| Regex-Literal Mutationen (Quantifier, Anker, Klassen, Lookaheads etc.) | ❌ | ❌ | ❌ (TODO im Code) | ✅ regex (umfangreich, eigene Doku) |

### 2.17 Sprachspezifische Konstrukte

| Mutation | PIT | cargo-mutants | mutmut | Stryker.NET |
|---|---|---|---|---|
| `checked()`-Block entfernen (Overflow-Check) | — | — | — | ✅ checked |
| `unsafe`-Funktionen ausschließen | — | ✅ (auto skip) | — | ❌ |
| Async/Await Mutationen | ❌ | ❌ | ❌ | ❌ |
| Pattern-Match-Decomposition | ❌ | ⚠️ | ❌ | ⚠️ |
| Lambdas/Closures | ⚠️ | ⚠️ | ✅ (`lambda: x → lambda: None`) | ⚠️ |

---

## 3. Alleinstellungsmerkmale je Framework

**PIT** ist stark bei:
- Granular konfigurierbaren Operator-**Gruppen** (DEFAULTS / STRONGER / ALL)
- **Equivalent-Mutant-Filterung** (z.B. EMPTY_RETURNS überspringt Methoden, die ohnehin leer zurückgeben)
- Akademisch fundiertem **erweiterten Set** (AOR, AOD, ROR, UOI, CRCR, OBBN, ABS) — das Pattern eignet sich gut als Vorlage für eine Kategorienachse
- **Inkrementeller Analyse** (History-File)

**cargo-mutants** ist stark bei:
- **Typgetriebener Mutation**: `Result<T>`, `Option<T>`, `Vec<T>`, `HashMap<K,V>`, `Cow`, `Arc`, `Box` — Mutanten matchen die Signatur
- **Funktionskörper-Replacement** als ganzheitlicher Ansatz (FnValue-Genre)
- Konservativer Default (z.B. `==` wird *nicht* zu `<` mutiert wegen False-Positives bei unsigned)
- **Match-Arm-Deletion** mit Wildcard-Aware-Logik
- **Struct-Literal-Field-Deletion** (testet, ob Felder wirklich geprüft werden)

**mutmut** ist stark bei:
- **Subtilen** Mutationen (Designprinzip: möglichst wenig disruptiv)
- **Trampoline-basierter Mutant-Auswahl** (nur aufgerufene Funktionen)
- **Type-Checker-Integration** (mypy/pyrefly filtern unviable Mutanten)
- Lambda- und Comprehension-Mutationen

**Stryker.NET** ist stark bei:
- **Breite des Katalogs**: einziges Framework mit dediziertem `linq`-, `stringmethod`- und `math`-Operator
- **Regex-Mutationen** (sehr umfangreich, eigenes Subsystem)
- **Pattern-Matching-Awareness** (`is`/`is not`, `and`/`or` in Mustern)
- **Null-Coalescing** (`??`, `??=`)
- **Checked-Statement-Mutator**

---

## 4. Was Stryker.NET fehlt — Lücken für deinen „Allrounder"

Wenn das Ziel ein C#-Framework ist, das **alles kann**, dann sind das die konkreten Gaps gegenüber den anderen drei:

### 4.1 Aus PIT übernehmenswert

| Gap | Beschreibung |
|---|---|
| **Operator-Gruppen-Konzept** | DEFAULTS / STRONGER / ALL als Profile statt nur Include/Exclude |
| **Inline Constants Mutator** | Numerische Literale verändern (1→0, 5→6, 42→43). Stryker.NET hat das gar nicht für Zahlen. |
| **CRCR-Set** (Constant Replacement) | `c → 0, 1, -1, c+1, c-1, -c` als systematische Achse |
| **Empty/Null/Primitive Returns** | Spezifische Default-Returns für `IEnumerable<T>`, `List<T>`, `Dictionary<K,V>`, `string`, `int`, `bool`, `Task<T>`, `ValueTask<T>` |
| **Constructor Call → null** | `new Foo() → null` |
| **Argument Propagation** | `foo.Bar(a, b) → a` (Methode durch Argument ersetzen, wenn Typ passt) |
| **Naked Receiver** | `a.Method(b) → a` |
| **Member Variable Mutator** | Field-Assignments zu Default-Werten zurücksetzen |
| **AOD (Arithmetic Operator Deletion)** | `a + b → a` und `a + b → b` als zwei Submutators |
| **ROR Vollmatrix** | Alle 5 Replacements pro Vergleichsoperator (statt nur Boundary + Negation) |
| **UOI (Unary Operator Insertion)** | `a → a++`, `a → ++a`, `a → a--`, `a → --a` |
| **Equivalent-Mutant-Filtering** | Statisch erkennen, wenn Mutation semantisch identisch ist |

### 4.2 Aus cargo-mutants übernehmenswert

| Gap | Beschreibung |
|---|---|
| **Typgetriebene Return-Werte** | `Task<Result<T>>` → `Task.FromResult(Result.Ok(default))`, `IEnumerable<int>` → `Enumerable.Empty<int>()` und `new[] { 0 }`, etc. — *kategorisch das größte Differenzial-Feature* |
| **Function-Body-Replacement** als eigene Genre | Ergänzt feingranulare Operatoren um „grobe" Mutation |
| **Match-Arm-Deletion** für `switch`-Expressions in C# (bei `_`-Default) | C# 8+ Pattern Matching wird von Stryker.NET kaum exploitiert |
| **Match-Guard-Mutation** (`when`-Klausel → `true`/`false`) | C# `case … when …` Patterns |
| **Object-Initializer mit `with`-Expression** (Records) | Felder einzeln aus `with { … }` löschen |
| **Konservative Defaults** | `==` *nicht* zu `<` für `uint`, `byte` etc. |

### 4.3 Aus mutmut übernehmenswert

| Gap | Beschreibung |
|---|---|
| **Type-Checker-Integration** (Roslyn-Diagnostics als Filter) | Mutanten verwerfen, die nicht typchecken — schnellere Iteration |
| **Trampoline-Approach** | Mutanten-Switching zur Laufzeit, statt für jeden Mutanten neu zu kompilieren — das wäre ein massiver Performance-Boost gegenüber Stryker.NET |
| **Coverage-gesteuerte Mutation** | Nur Zeilen mutieren, die wirklich getestet werden |

### 4.4 Eigene Erweiterungen, die noch niemand hat (für „alles können")

- **Async/Await-Mutationen**: `await x → x.Result`, `Task.WhenAll → Task.WhenAny`, `ConfigureAwait(false) → ConfigureAwait(true)`
- **Span/Memory-Mutationen**: `Span<T>` ↔ `ReadOnlySpan<T>`, `AsSpan() → AsMemory()`
- **DateTime-Mutationen**: `AddDays(n) ↔ AddDays(-n)`, `Now ↔ UtcNow`
- **Exception-Type-Swap**: `throw new ArgumentNullException → throw new ArgumentException`
- **Access-Modifier-Mutation** (kontrovers): `private ↔ public` (für Reflexions-Tests)
- **Generic-Constraint-Loosening**: `where T : class → where T : new()` etc. — als Compile-Time-Mutationen

---

## 5. Architekturhinweise für die Umsetzung

Aus dem Vergleich ergeben sich ein paar Designentscheidungen, die du früh treffen solltest:

1. **AST oder IL?** Stryker.NET arbeitet auf Roslyn-AST, PIT auf JVM-Bytecode. Roslyn ist die richtige Wahl für C#, aber für einige Mutationen (z. B. `checked`, Inline-Constants) gewinnt man durch zusätzliche IL-Sicht. Hybrider Ansatz möglich.

2. **Operator-Modell**: PIT's Konzept *Operator → Sub-Operator → Gruppen* skaliert besser als Stryker.NETs Flat-List. Plane das von Anfang an als Hierarchie.

3. **Type-Awareness**: cargo-mutants zeigt, dass *typgetriebene* Mutationen die größte Aussagekraft haben. In C# bedeutet das: stütze dich auf `SemanticModel` von Roslyn, nicht nur auf den Syntax-Tree.

4. **Performance-Modell**: Stryker.NET kompiliert pro Mutant neu (langsam). mutmut nutzt Trampolines. PIT lädt mutierte Klassen via Custom-ClassLoader. Für C# wäre das Äquivalent **AssemblyLoadContext** mit Hot-Swap der mutierten Methode — das wäre der größte Wettbewerbsvorteil.

5. **Equivalent-Mutant-Filterung**: PIT zeigt, dass das Filtering-Layer fast genauso wichtig ist wie der Operator-Layer. Plane es als first-class Citizen ein, nicht als Nachgedanke.

6. **Mutation-Levels** (StrykerJS Innovation): Konzept aus dem Stryker-Universum, das es in .NET noch nicht gibt — Mutationen nach Aussagekraft/Stabilität in Levels einteilen.

---

## Quellen

- PIT: <https://pitest.org/quickstart/mutators/>
- cargo-mutants: <https://mutants.rs/mutants.html>
- mutmut: <https://mutmut.readthedocs.io/>
- Stryker.NET: <https://stryker-mutator.io/docs/stryker-net/mutations/>
