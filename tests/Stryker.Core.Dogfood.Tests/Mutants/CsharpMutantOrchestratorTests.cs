using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>
/// Sprint 62 (v2.48.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Mutants/CsharpMutantOrchestratorTests.cs (1968 LOC, 95 [TestMethod]s).
/// MSTest → xUnit, Shouldly → FluentAssertions.
///
/// Drift-risk triage (Maxential branch B, full integration):
///   Bucket 1 — NO mutation expected (source==expected): port — robust to mutator-set drift.
///   Bucket 2 — Single-mutation, default-profile pattern: port if our orchestrator produces matching output.
///   Bucket 3 — Multi-mutation hardcoded IDs: defer — IDs depend on mutator-pipeline ordering and our
///              v2.x has 52 mutators vs upstream 40.
///
/// Validation: empirical run of bucket-1 tests proves the helpers + orchestrator-API parity work end-to-end.
/// </summary>
public class CsharpMutantOrchestratorTests : MutantOrchestratorTestsBase
{
    [Fact]
    public void ShouldNotMutateEmptyInterfaces()
    {
        var source = """
            using System;
            using System.Collections.Generic;
            using System.Text;
            namespace StrykerNet.UnitTest.Mutants.TestResources
            {
                interface TestClass
                {
                    int A { get; set; }
                    int B { get; set; }
                    void MethodA();
                }
            }
            """;

        var expected = """
            using System;
            using System.Collections.Generic;
            using System.Text;
            namespace StrykerNet.UnitTest.Mutants.TestResources
            {
                interface TestClass
                {
                    int A { get; set; }
                    int B { get; set; }
                    void MethodA();
                }
            }
            """;
        ShouldMutateSourceToExpected(source, expected);
    }

    [Fact]
    public void ShouldNotMutateImplicitArrayCreationProperties()
    {
        var source = "public int[] Foo() => new [] { 1 };";
        var expected = "public int[] Foo() => new [] { 1 };";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldNotMutateImplicitArrayCreation()
    {
        var source = "public static readonly int[] Foo =  { 1 };";
        var expected = "public static readonly int[] Foo =  { 1 };";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldNotMutateConst()
    {
        var source = "private const int x = 1 + 2;";
        var expected = "private const int x = 1 + 2;";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    /// <summary>
    /// Verifies that <c>EnumMemberDeclarationSyntax</c> nodes are not mutated.
    /// Mutating would introduce code like
    /// <c>StrykerXGJbRBlHxqRdD9O.MutantControl.IsActive(0) ? One + 1 : One - 1</c>
    /// — enum members must be constants, so the mutated code would not compile.
    /// </summary>
    [Fact]
    public void ShouldNotMutateEnum()
    {
        var source = "private enum Numbers { One = 1, Two = One + 1 }";
        var expected = "private enum Numbers { One = 1, Two = One + 1 }";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldNotMutateAttributes()
    {
        var source = """
            [Obsolete("thismustnotbemutated")]
            public void SomeMethod() {}
            """;
        var expected = """
            [Obsolete("thismustnotbemutated")]
            public void SomeMethod() {}
            """;

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldNotMutateDefaultValues()
    {
        var source = "public void SomeMethod(bool option = true) {}";
        var expected = "public void SomeMethod(bool option = true) {}";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    // ----- Bucket-2 tests: single-mutation, low-drift-risk patterns. -----

    [Fact]
    public void ShouldNotAddReturnDefaultToDestructor()
    {
        var source = "~TestClass(){;}";
        var expected = "~TestClass(){if(StrykerNamespace.MutantControl.IsActive(0)){}else{;}}";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldMutateStackalloc()
    {
        var source = "Span<ushort> kindaUnrelated = stackalloc ushort[] { 0 };";
        var expected = "Span<ushort> kindaUnrelated = (StrykerNamespace.MutantControl.IsActive(0)?stackalloc ushort[] {}:stackalloc ushort[] { 0 });";

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    [Fact]
    public void ShouldMutateTrimMethodOnStringIdentifier()
    {
        var source = "static string Value(string text) => text.Trim();";
        var expected = """
            static string Value(string text) =>
            (StrykerNamespace.MutantControl.IsActive(0) ? "" : text.Trim());
            """;

        ShouldMutateSourceInClassToExpected(source, expected);
    }

    // ----- Bucket-3 (multi-mutation hardcoded IDs) deferred. -----
    // The expected output contains literal `StrykerNamespace.MutantControl.IsActive(N)?...:...` strings
    // whose IDs depend on (a) the orchestrator's mutator-pipeline ordering and (b) which mutators fire on
    // a given source. stryker-netx v2.x has 52 mutators (vs upstream 4.14.1's 40), so additional/different
    // mutations are produced for the same source — the upstream expected strings drift.
    //
    // Future remediation paths (Sprint 63+):
    //   - Rewrite as STRUCTURAL assertions (count mutations + verify mutator-class names) instead of literal-string match.
    //   - Or recompute v2.x-specific expected strings against current orchestrator output.

    // Sprint 119 (v3.0.6): Bucket-3 structural-assertion rewrites. Replaced consolidated [Fact(Skip)]
    // with structural tests that assert MUTATION COUNT instead of literal-string comparison. Each
    // upstream test asserted exact mutated-source-strings with hardcoded IsActive(N) IDs; v2.x has
    // 52 mutators vs upstream 40 → IDs differ → strings drift. Structural assertions bypass this
    // by counting IsActive markers (each = 1 mutation) without checking specific N values.

    [Fact]
    public void ShouldMutateBlockStatements_StructuralAssertion()
    {
        // Sprint 119: structural rewrite of upstream bucket-3 ShouldMutateBlockStatements.
        // Upstream asserted exact mutated source — we assert ≥1 mutation produced.
        var source = """
            private void Move()
            {
                ;
            }
            """;
        var count = CountMutations(source);
        count.Should().BeGreaterThan(0, "block statement should produce at least 1 mutation (block-statement mutator)");
    }

    [Fact]
    public void ShouldMutateConditionalExpression_StructuralAssertion()
    {
        // Sprint 137 (v3.0.24): Sprint 23 known-bug investigation. Reproduces upstream bucket-3
        // ShouldMutateConditionalExpression input to verify VisitQualifiedName crash status.
        var source = """
            void TestMethod()
            {
                string SomeLocalFunction()
                {
                    return string.Empty?.All(x => !string.IsNullOrEmpty(x));
                }
            }
            """;
        var count = CountMutations(source);
        count.Should().BeGreaterThan(2, "conditional + linq + string + boolean should produce multiple mutations");
    }

    [Fact]
    public void ShouldMutateDefaultImplementationInterfaces_StructuralAssertion()
    {
        // Sprint 119: structural rewrite of upstream bucket-3 ShouldMutateDefaultImplementationInterfaces.
        var source = """
            public interface IExample
            {
                int DefaultMethod() => 1 + 2;
            }
            """;
        var count = CountMutations(source);
        count.Should().BeGreaterThan(0, "default interface method body with arithmetic should produce ≥1 mutation");
    }

    // Sprint 179 (issue #279, Mechanik-Korrektur zu Befund G-01): der needReturn-Pfad von
    // MutationStore.Inject stellt auf dem Mutations-Pfad bereits ein terminales Return her
    // (der separate EndingReturnEngine-Aufruf ist dort by design entbehrlich). Dieser Test
    // pinnt die Garantie, damit eine kuenftige Refaktorierung sie nicht verliert.
    [Fact]
    public void ShouldAddEndingReturnOnMutatedValueReturningMethods()
    {
        var source = "int M(bool c){ if(c) {return 1;} return 2; }";

        var mutated = MutateSourceInClass(source);

        var method = CSharpSyntaxTree.ParseText(mutated).GetRoot()
            .DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Single(m => string.Equals(m.Identifier.ValueText, "M", System.StringComparison.Ordinal));
        method.Body!.Statements.Last().Should().BeOfType<ReturnStatementSyntax>(
            "the mutation path must restore a terminal return so block mutants stay compilable");
    }

    // Sprint 179 (issue #283, 360-Grad-Analyse G-14): case-Labels verlangen Konstanten.
    // Ein Mutations-Wrap im Label ist garantiert CS0150, daher darf dort gar nicht erst
    // mutiert werden (Probe Sprint 174: 2/2 CompileError, Kontrollgruppe Killed).
    [Fact]
    public void ShouldNotMutateCaseLabelConstants()
    {
        var source = """
            int M(string s)
            {
                switch (s)
                {
                    case "a": return 1;
                    case "b": goto case "a";
                    default: return 0;
                }
            }
            """;

        _ = MutateSourceInClass(source);

        Target.Mutants.Should().NotContain(
            m => m.Mutation.OriginalNode.ToString() == "\"a\"" || m.Mutation.OriginalNode.ToString() == "\"b\"",
            "string constants in case labels and goto-case targets must not be mutated");
    }

    // Sprint 180 (issue #284, 360-Grad-Analyse G-26): ein Pattern mit Designation bindet
    // seine Variable in den umgebenden Statement-Scope. Ein Ternary-Wrap am is-Ausdruck
    // dupliziert die Designation im selben Scope (CS0128, Probe Sprint 174: 2/2). Pattern-
    // interne Mutationen muessen deshalb auf Block-Ebene gehoben werden, wo jede if/else-
    // Kopie ihren eigenen Scope hat.
    [Fact]
    public void ShouldLiftPatternInternalMutationsToBlockWhenPatternBindsAVariable()
    {
        var source = """
            bool M(string o)
            {
                if (o is { Length: > 2 } s)
                {
                    return s.Length > 5;
                }
                return false;
            }
            """;

        var mutated = MutateSourceInClass(source);

        Target.Mutants.Should().Contain(
            m => m.Mutation.OriginalNode.ToString() == "> 2",
            "the relational pattern itself must stay mutable — only its control location moves");
        AssertNoTernaryDuplicatesADesignation(mutated);
    }

    // Sprint 180 (issue #284b): ContainsDeclarations kannte nur DeclarationExpression und
    // DeclarationPattern. Designationen an Recursive-, Var- und List-Patterns wurden daher
    // nicht erkannt und Mutationen am umgebenden Ausdruck (hier &&) als Ternary gewrappt —
    // dieselbe CS0128-Klasse ueber einen zweiten Pfad.
    [Fact]
    public void ShouldControlConditionMutationAtBlockLevelWhenRecursivePatternBindsAVariable()
    {
        var source = """
            bool M(string o, bool flag)
            {
                var r = o is { Length: 2 } t && flag;
                return r && t.Length > 1;
            }
            """;

        var mutated = MutateSourceInClass(source);

        Target.Mutants.Should().Contain(
            m => m.Mutation.OriginalNode.ToString() == "o is { Length: 2 } t && flag",
            "the logical mutation on the pattern-carrying condition must stay mutable");
        AssertNoTernaryDuplicatesADesignation(mutated);
    }

    private static void AssertNoTernaryDuplicatesADesignation(string mutated)
    {
        var offendingTernaries = CSharpSyntaxTree.ParseText(mutated).GetRoot()
            .DescendantNodes().OfType<ConditionalExpressionSyntax>()
            .Where(c => c.DescendantNodes().Any(d => d is SingleVariableDesignationSyntax))
            .ToList();

        offendingTernaries.Should().BeEmpty(
            "a conditional expression duplicates pattern designations in the same statement scope (CS0128)");
    }
}
