# Sprachfeatures von C# 10 bis C# 14

Eine Übersicht aller Sprachfeatures, die mit den C#-Versionen 10 bis 14 neu eingeführt wurden. Die Reihenfolge folgt der Veröffentlichung; jede Version ist mit der zugehörigen .NET-Version und der offiziellen Microsoft-Learn-Quelle versehen.

---

## C# 10 (November 2021, .NET 6)

1. **Record Structs** – Records gibt es jetzt auch als Werttyp: `record struct Point(double X, double Y)` (mit Equality-by-Value), zusätzlich `readonly record struct` für unveränderliche Varianten. Synthetisiert dieselben Member wie `record class`. Bestehende Records bleiben Referenztypen; `record class` ist als explizite Schreibweise neu zugelassen.

2. **Verbesserungen bei Structs** – Structs dürfen jetzt parameterlose Konstruktoren und Field-Initializer haben. Achtung: Beim Erzeugen via `default(T)` oder als nicht initialisiertem Array-Element wird der Konstruktor *nicht* aufgerufen, die Initializer werden also umgangen.

3. **`with`-Expressions auf Structs und anonymen Typen** – Die nicht-destruktive Mutation per `with`, bisher nur für Records erlaubt, funktioniert jetzt auch für beliebige `struct`-Typen und für anonyme Typen.

4. **Global using Directives** – `global using System;` in einer einzelnen Datei macht das Using projektweit verfügbar. Außerdem hat das SDK ab .NET 6 das Konzept der „Implicit Usings", die per MSBuild-Property (`<ImplicitUsings>enable</ImplicitUsings>`) automatisch typische Namespaces global einbinden.

5. **File-scoped Namespace Declaration** – `namespace MyApp;` als Anweisung statt eines Blocks. Spart eine Einrückungsebene und macht den Code kompakter, wenn die ganze Datei zu einem Namespace gehört.

6. **Extended Property Patterns** – Verschachtelte Properties dürfen mit Punktnotation gematcht werden: `obj is { Address.City: "Berlin" }` statt `obj is { Address: { City: "Berlin" } }`.

7. **Natural Type für Lambda-Ausdrücke** – Der Compiler kann jetzt einen Delegate-Typ aus einer Lambda oder Method Group ableiten, sodass `var f = () => 42;` funktioniert (mit Inferenz auf `Func<int>`).

8. **Explizite Return-Typen für Lambdas** – Wenn der Compiler den Rückgabetyp nicht eindeutig herleiten kann (oder man ihn explizit festlegen will), darf er angegeben werden: `var f = object () => "hi";`.

9. **Attribute auf Lambda-Ausdrücken** – Lambdas dürfen jetzt Attribute auf Parametern, Rückgabewert oder der Lambda selbst tragen, z. B. `([NotNull] string? s) => ...`. Wichtig u. a. für Minimal APIs.

10. **Interpolated String Handlers** – Neuer Mechanismus, der das Erzeugen interpolierter Strings über einen benutzerdefinierten Handler-Typ steuern lässt. Erlaubt z. B., dass `Debug.Assert($"...")` den String nur dann tatsächlich zusammenbaut, wenn die Assertion fehlschlägt – ein erheblicher Performance-Gewinn in Hot Paths (in der BCL z. B. bei `StringBuilder.Append($"...")` und `ILogger`).

11. **Konstante interpolierte Strings** – Ein `const string`-Wert darf jetzt per Interpolation initialisiert werden, sofern alle eingesetzten Platzhalter selbst konstante Strings sind.

12. **`sealed` für `ToString()` in Records** – In einem `record`-Typ darf der Override von `ToString()` als `sealed` markiert werden, sodass abgeleitete Records die Implementierung nicht weiter überschreiben können (nützlich, um die kompilergenerierte Erzeugung in Subtypen zu verhindern).

13. **Genauere Definite-Assignment- und Null-State-Analyse** – Der Compiler erkennt mehr sichere Muster und reduziert dadurch falsche Warnungen bei definitiver Zuweisung sowie bei der Nullable-Reference-Type-Analyse.

14. **Gemischte Deklaration und Zuweisung in Dekonstruktion** – Innerhalb eines einzelnen Dekonstruktions-Tupels dürfen jetzt sowohl neue Variablen deklariert als auch bestehende Variablen zugewiesen werden, z. B. `(existingX, int newY) = point;`. Vorher musste man sich auf eine der beiden Formen festlegen.

15. **`AsyncMethodBuilder` auf Methoden** – Das `[AsyncMethodBuilder(typeof(...))]`-Attribut darf jetzt nicht nur auf Typen, sondern auch auf einzelne Methoden angewendet werden, um den State-Machine-Builder pro Methode festzulegen.

16. **`CallerArgumentExpression`-Attribut** – Mit `[CallerArgumentExpression("paramName")]` kann ein optionaler Parameter automatisch den Quellcode-Ausdruck eines anderen Arguments als String erhalten. Sehr nützlich für Guard-/Assertion-APIs (`ArgumentNullException.ThrowIfNull` nutzt das), Logging und Test-Frameworks.

17. **Erweitertes `#line`-Pragma** – Neue Form von `#line`, mit der Spalten- *und* Zeilenbereiche samt Offsets angegeben werden können. Vor allem für DSLs und Source Generators (z. B. Razor) gedacht, um Diagnose- und Debug-Informationen präzise auf den Originalquelltext zu mappen.

Zusätzlich wurden in C# 10 zwei Features als **Preview** ausgeliefert, die erst in C# 11 stabilisiert wurden: **Generic Attributes** und **Static Abstract Members in Interfaces**.

Quelle: [Microsoft Learn – What's new in C# 10](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-10)

---

## C# 11 (November 2022, .NET 7)

1. **Raw String Literals** – Strings, die mit drei oder mehr aufeinanderfolgenden Anführungszeichen (`"""..."""`) eingeleitet und beendet werden. Sie benötigen kein Escaping für `\`, `"` oder Zeilenumbrüche und eignen sich hervorragend für JSON, XML, Regex oder eingebettete Code-Snippets. Auch interpoliert nutzbar mit `$"""..."""`, wobei die Anzahl der `$`-Zeichen die Anzahl der `{}`-Klammern für Interpolations-Holes definiert.

2. **Generic Math Support / Static Virtual und Abstract Members in Interfaces** – Interfaces dürfen jetzt statische, virtuelle und abstrakte Member sowie Operatoren deklarieren. Damit lassen sich numerische Algorithmen einmalig generisch über alle Number-Typen schreiben (Basis für `INumber<T>`, `IAdditionOperators<T,T,T>` etc. in der BCL).

3. **Generic Attributes** – Attribute dürfen jetzt generische Typparameter verwenden: `class TypeAttribute<T> : Attribute`. Anwendung als `[Type<int>]`. Bestimmte Typargumente sind allerdings nicht erlaubt (`dynamic`, Nullable-Reference-Types, Tupel-Syntax).

4. **UTF-8 String Literals** – Mit dem Suffix `u8` (z. B. `"GET"u8`) wird ein String-Literal als UTF-8-Bytefolge vom Typ `ReadOnlySpan<byte>` interpretiert. Vermeidet die Konvertierung von UTF-16 nach UTF-8 zur Laufzeit – nützlich für HTTP, Web-Standards und andere Byte-orientierte Protokolle.

5. **Zeilenumbrüche in Interpolations-Holes** – Innerhalb von `{ ... }` in interpolierten Strings dürfen Ausdrücke jetzt über mehrere Zeilen verteilt werden, was lange LINQ-Ausdrücke deutlich lesbarer macht.

6. **List Patterns** – Pattern Matching für Arrays und Listen: `array is [1, 2, 3]`. Inklusive Discard-Pattern `_` (einzelnes Element) und **Slice-Pattern** `..` (null oder mehr Elemente). Auch mit Bezeichnern kombinierbar: `[1, .. var rest]`.

7. **File-local Types** – Neuer Sichtbarkeitsmodifizierer `file`. Ein `file class Foo` ist nur innerhalb der jeweiligen Quelldatei sichtbar – primär gedacht für Source Generators, um Namenskonflikte mit nutzergeschriebenem Code zu vermeiden.

8. **Required Members** – Modifizierer `required` für Properties und Felder. Aufrufer müssen den Member im Object Initializer setzen, sonst Compilerfehler. Zusammen mit `[SetsRequiredMembers]` an Konstruktoren kombinierbar, um die Pflicht über den Konstruktor zu erfüllen.

9. **Auto-default Structs** – Der Compiler initialisiert in Struct-Konstruktoren automatisch alle Felder/Properties, die im Konstruktor nicht explizit gesetzt werden, mit ihrem Default-Wert. Vorher gab es einen Compilerfehler.

10. **Pattern Match `Span<char>` / `ReadOnlySpan<char>` gegen String-Literal** – `span is "hello"` ist jetzt direkt möglich, ohne dass eine Allokation oder Konvertierung von Span nach String entsteht.

11. **Erweiterter `nameof`-Scope** – `nameof` darf in Attributen, die auf eine Methode oder einen Methodenparameter angewendet werden, jetzt auf die Parameter dieser Methode verweisen, z. B. `[NotNullIfNotNull(nameof(input))]`.

12. **`nint` und `nuint` als echte Sprach-Keywords** – `nint` und `nuint` sind ab C# 11 Aliase für `System.IntPtr` bzw. `System.UIntPtr`, mit voller Unterstützung für arithmetische Operatoren und Konvertierungen.

13. **`ref`-Felder und `scoped ref`** – `ref struct`-Typen dürfen jetzt `ref`-Felder enthalten (Grundlage für die echte Implementierung von `Span<T>` in der Runtime). Mit `scoped` (an `ref`-Parametern oder `ref`-Locals) kann die Lebensdauer einer Referenz auf den aktuellen Methodenrahmen beschränkt werden – der Compiler erzwingt dies per statischer Analyse.

14. **Unsigned Right-Shift Operator `>>>`** – Logisches Right-Shift, das das Vorzeichenbit nicht weiterträgt (im Gegensatz zu `>>`, das bei signed Typen das Vorzeichen erhält). Auch als überladbarer User-defined Operator verfügbar.

15. **Checked User-defined Operators** – Benutzerdefinierte arithmetische Operatoren können jetzt sowohl in einer regulären als auch in einer `checked`-Variante (`public static T operator checked +(...)`) implementiert werden, die in `checked`-Kontexten aufgerufen wird.

16. **Verbesserte Method-Group-zu-Delegate-Konvertierung** – Der Compiler darf jetzt das aus einer Method Group erzeugte Delegate cachen, statt es bei jeder Konvertierung neu zu allozieren – eine reine Performance-Verbesserung ohne Syntaxänderung.

17. **Warning Wave 7** – Neue Warnungen, u. a. eine Warnung für Typennamen, die nur aus Kleinbuchstaben bestehen (Konflikt mit zukünftigen Sprach-Keywords).

Quelle: [Microsoft Learn – What's new in C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-11)

---

## C# 12 (November 2023, .NET 8)

1. **Primary Constructors für Klassen und Structs** – Bisher waren Primary Constructors auf Records beschränkt; in C# 12 dürfen jede `class` und jede `struct` Parameter direkt im Typ-Header deklarieren. Die Parameter sind im gesamten Typkörper sichtbar. Im Gegensatz zu Records erzeugt der Compiler bei normalen Klassen/Structs keine öffentlichen Properties – die Parameter werden zu privaten Feldern, was sich vor allem für Dependency Injection eignet. Explizit deklarierte Konstruktoren müssen den Primary Constructor mit `this(...)` aufrufen.

2. **Collection Expressions** – Neue, unifizierte Syntax mit eckigen Klammern: `int[] arr = [1, 2, 3];`. Funktioniert für Arrays, `Span<T>`, `ReadOnlySpan<T>`, `List<T>` und alle Typen, die Collection-Initializer unterstützen. Inklusive **Spread-Operator** `..`, mit dem bestehende Collections eingebunden werden können: `int[] combined = [..first, ..second, 99];`. Der Compiler wählt die effizienteste Erzeugungsstrategie für den Zieltyp.

3. **`ref readonly` Parameter** – Neuer Parameter-Modifizierer als Ergänzung zu `ref` und `in`. Gedacht für APIs, die per Referenz übergeben wollen, ohne den Wert zu modifizieren – aber explizit eine Variable als Argument verlangen (im Gegensatz zu `in`, das auch Literale akzeptiert). Erlaubt unter anderem die saubere Aktualisierung alter `ref`-APIs, ohne Aufrufer zu brechen.

4. **Default-Parameter für Lambda-Ausdrücke** – Lambda-Parameter dürfen jetzt Default-Werte haben, analog zu Methoden: `var increment = (int x, int y = 1) => x + y;`. Auch `params`-Arrays sind in Lambdas zulässig.

5. **Alias Any Type** – `using`-Aliase funktionieren jetzt für beliebige Typen, nicht nur für benannte Typen und Namespaces. Damit lassen sich Tupel-Typen, Pointer-Typen, Array-Typen, nullable Typen und unsafe Typen mit kurzen Aliassen versehen, z. B. `using Point = (int X, int Y);`. Offene generische Typen sind jedoch nicht erlaubt.

6. **Inline Arrays** – Mit dem Attribut `[System.Runtime.CompilerServices.InlineArray(N)]` lassen sich Struct-Typen als Fixed-Size-Buffer mit `N` Elementen deklarieren, die wie ein Array indizierbar sind. Bietet sicheren Zugriff auf Speicherpuffer ohne `unsafe`-Code – primär gedacht für die Runtime, BCL und Performance-kritische Szenarien (z. B. Game-Development).

7. **`ExperimentalAttribute`** – Das neue `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` markiert Typen, Methoden oder ganze Assemblies als experimentell. Bei deren Nutzung gibt der Compiler eine Diagnose mit individueller ID aus, die per `#pragma` oder Compileroption gezielt unterdrückt werden kann.

8. **Interceptors (experimentelle Preview)** – Mechanismus, mit dem ein Source Generator Aufrufe einer bestimmten Methode an einer konkreten Quelltextposition durch eine andere Methode „abfangen" und ersetzen kann. Aktivierung erfordert die MSBuild-Property `<InterceptorsPreviewNamespaces>`. Ausdrücklich nicht für Produktivcode empfohlen; Syntax und Implementierung können sich noch ändern.

Quelle: [Microsoft Learn – What's new in C# 12](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12)

---

## C# 13 (November 2024, .NET 9)

1. **`params` Collections** – Der `params`-Modifizierer ist nicht mehr auf Arrays beschränkt. Erlaubt sind jetzt `Span<T>`, `ReadOnlySpan<T>`, `IEnumerable<T>`, `List<T>`, `IReadOnlyCollection<T>` und alle Typen, die `IEnumerable<T>` implementieren und eine passende `Add`-Methode besitzen. Der Compiler wählt dabei die effizienteste Variante (z. B. Stack-Allokation bei `ReadOnlySpan<T>` statt Heap-Array).

2. **Neuer `Lock`-Typ mit eigener Semantik** – Wenn das Ziel einer `lock`-Anweisung ein `System.Threading.Lock` ist, generiert der Compiler Code, der `Lock.EnterScope()` aufruft, statt der klassischen `Monitor`-basierten Implementierung. Das ist effizienter und besser typisiert; vorhandener Quellcode muss nur den Typ des Lock-Objekts ändern.

3. **Neue Escape-Sequenz `\e`** – Steht für das ESCAPE-Zeichen (U+001B). Ersetzt die bisherigen Schreibweisen `\u001b` bzw. `\x1b`, wobei `\x1b` problematisch war, weil nachfolgende Hex-Zeichen versehentlich Teil der Sequenz wurden.

4. **`ref struct` darf Interfaces implementieren** – `ref struct`-Typen können jetzt Interfaces implementieren. Eine Konvertierung in den Interface-Typ bleibt aus Gründen der Ref-Safety (Boxing) verboten; der Zugriff funktioniert nur über generische Typparameter mit der entsprechenden Constraint.

5. **`allows ref struct` Generic Constraint** – Neue Generic Constraint, die es erlaubt, `ref struct`-Typen als Typargumente zu verwenden. Damit lassen sich generische APIs für `Span<T>` & Co. schreiben.

6. **Partielle Properties und Indexer** – Das aus C# 9 bekannte `partial`-Konzept gilt jetzt auch für Properties und Indexer. Eine deklarierende und eine implementierende Deklaration; vor allem für Source Generators gedacht.

7. **Overload Resolution Priority** – Das neue Attribut `OverloadResolutionPriorityAttribute` (im Namespace `System.Runtime.CompilerServices`) erlaubt es Bibliotheksautoren, einen Overload gegenüber einem anderen explizit zu bevorzugen. Hilft beim Hinzufügen besserer (z. B. performanterer) Overloads, ohne bestehende Aufrufer zu brechen.

8. **`ref` Locals und `unsafe`-Kontexte in Iteratoren und Async-Methoden** – `ref`-Locals und `unsafe`-Code dürfen jetzt in `async`-Methoden und Iteratoren verwendet werden – allerdings nur in Bereichen, die nicht über ein `await` bzw. `yield` hinweg leben. Damit funktionieren z. B. lokale `ReadOnlySpan<T>`-Variablen in `async`-Methoden.

9. **Implizite Index-Verwendung in Object Initializers** – Der `^`-Operator (Index-from-end) ist nun innerhalb von Object Initializers erlaubt, z. B. `new Buffer { [^1] = 0 }`. Vorher musste man dafür auf separate Anweisungen ausweichen.

10. **Verbesserte Method Group Natural Type** – Bei der Bestimmung des „natürlichen Typs" einer Method Group filtert der Compiler offensichtlich ungeeignete Kandidaten (falsche Arity, nicht erfüllte Constraints) frühzeitig aus. Reduziert Mehrdeutigkeitsfehler und verbessert die Performance der Overload-Auflösung.

11. **`field` Keyword (Preview)** – Das kontextbezogene `field`-Schlüsselwort wurde ab Visual Studio 17.12 als Preview-Feature eingeführt, ist aber erst in C# 14 final stabilisiert worden. In C# 13 nur unter `<LangVersion>preview</LangVersion>` nutzbar.

Quelle: [Microsoft Learn – What's new in C# 13](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-13)

---

## C# 14 (November 2025, .NET 10)

1. **Extension Members** – Erweitert das bisherige Konzept der Extension Methods um Extension Properties, Extension Operators und statische Extension Member. Definiert wird das Ganze in einem neuen `extension`-Block, was thematisch zusammengehörige Erweiterungen pro Empfängertyp gruppiert.

2. **Das `field`-Schlüsselwort** – Ein kontextbezogenes Keyword innerhalb von Property-Accessoren, das auf das vom Compiler synthetisierte Backing-Field zugreift. Damit entfällt die manuelle Deklaration eines privaten Feldes nur, um etwa eine Null-Prüfung im Setter unterzubringen.

3. **First-Class Support für `Span<T>` und `ReadOnlySpan<T>`** – Neue implizite Konvertierungen zwischen `T[]`, `Span<T>` und `ReadOnlySpan<T>`. Span-Typen können zudem als Receiver für Extension Methods dienen und nehmen an generischer Typinferenz teil. Aufrufe wie `Process(values)` für eine `ReadOnlySpan<int>`-Signatur funktionieren ohne explizites `AsSpan()`.

4. **Null-Conditional Assignment** – Die Operatoren `?.` und `?[]` dürfen jetzt auf der linken Seite einer Zuweisung bzw. Compound-Zuweisung stehen, z. B. `customer?.Order = GetCurrentOrder();`. Die Zuweisung wird nur ausgeführt, wenn der Empfänger nicht null ist.

5. **Lambda-Parameter-Modifizierer ohne Typangabe** – In Lambda-Ausdrücken können jetzt `ref`, `out`, `in` und `scoped` direkt verwendet werden, ohne dass der Parametertyp explizit notiert werden muss.

6. **`nameof` mit ungebundenen generischen Typen** – `nameof(List<>)` ist jetzt zulässig und liefert `"List"`. Bisher waren nur geschlossene generische Typen wie `nameof(List<int>)` erlaubt.

7. **Partielle Konstruktoren und partielle Events** – Das `partial`-Konzept wurde auf Instanzkonstruktoren und Events ausgeweitet (Ergänzung zu den partial Properties/Indexern aus C# 13). Genau eine definierende und eine implementierende Deklaration sind nötig – besonders nützlich im Zusammenspiel mit Source Generators.

8. **Benutzerdefinierte Compound-Assignment-Operatoren** – Typen können jetzt Operatoren wie `+=`, `-=` etc. eigenständig überladen, statt sich auf den vom binären `+` abgeleiteten Default zu verlassen. Erlaubt In-Place-Mutation und ist vor allem bei großen Werten wie `BigInteger` oder Vektor-Typen ein spürbarer Performance-Gewinn.

Quelle: [Microsoft Learn – What's new in C# 14](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
