using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// L1 integration layer (Sprint 20): exercises the
/// <see cref="CsharpMutantOrchestrator"/> end-to-end on real source — the
/// orchestrator owns the mutator pipeline, MutationContext flow, equivalent-
/// mutant filter dispatch, and duplicate suppression. Unit tests for
/// individual mutators don't cover the wiring; these do.
/// </summary>
[Trait("Category", "Integration")]
public class OrchestratorMutatorPipelineTests : IntegrationTestBase
{
    private const string SimpleAdd = "class C { int M(int a, int b) => a + b; }";

    [Fact]
    public void Mutate_SimpleAddExpression_ProducesAtLeastOneMutant()
    {
        var (mutants, _) = RunOrchestratorOnSource(SimpleAdd);
        mutants.Should().NotBeEmpty("BinaryExpressionMutator must fire on `a + b`");
    }

    [Fact]
    public void Mutate_SimpleAddExpression_MutatedTreeStillParses()
    {
        var (_, mutatedTree) = RunOrchestratorOnSource(SimpleAdd);
        var roundtrip = CSharpSyntaxTree.ParseText(mutatedTree.ToString());
        roundtrip.GetDiagnostics().Should().BeEmpty(
            "the mutated tree must produce valid C# (MutantPlacer injection failed otherwise)");
    }

    [Fact]
    public void Mutate_SimpleAddExpression_AllMutantsHaveUniqueIds()
    {
        var (mutants, _) = RunOrchestratorOnSource(SimpleAdd);
        var ids = mutants.Select(m => m.Id).ToList();
        ids.Should().OnlyHaveUniqueItems("CsharpMutantOrchestrator.GetNextId must be monotonic per orchestrator instance");
    }

    [Fact]
    public void Mutate_SimpleAddExpression_AllMutantsHaveOriginalAndReplacement()
    {
        var (mutants, _) = RunOrchestratorOnSource(SimpleAdd);
        mutants.Should().AllSatisfy(m =>
        {
            m.Mutation.Should().NotBeNull();
            m.Mutation.OriginalNode.Should().NotBeNull();
            m.Mutation.ReplacementNode.Should().NotBeNull();
        });
    }

    [Fact]
    public void Mutate_SimpleAddExpression_AllMutantsStartPending()
    {
        var (mutants, _) = RunOrchestratorOnSource(SimpleAdd);
        mutants.Should().AllSatisfy(m =>
            m.ResultStatus.Should().Be(MutantStatus.Pending,
                "fresh mutants emitted by the orchestrator must start in Pending status"));
    }

    [Fact]
    public void Mutate_ConstField_ProducesNoMutants()
    {
        // DoNotMutateOrchestrator<FieldDeclarationSyntax>(t => t.Modifiers.Any(ConstKeyword))
        // must skip the entire const declaration — including the initialiser expression.
        var (mutants, _) = RunOrchestratorOnSource("class C { const int X = 5 + 3; }");
        mutants.Should().BeEmpty("const-field initialisers are filtered by DoNotMutateOrchestrator");
    }

    [Fact]
    public void Mutate_PropertyExpressionBody_ProducesMutants()
    {
        // Sanity counterpart to the const-field test: a regular expression-bodied
        // property over the same `5 + 3` IS mutable.
        var (mutants, _) = RunOrchestratorOnSource("class C { int X => 5 + 3; }");
        mutants.Should().NotBeEmpty("expression-bodied properties carry mutable expressions");
    }

    [Fact]
    public void Mutate_AttributeArgument_NotMutated()
    {
        // DoNotMutateOrchestrator<AttributeListSyntax>: the entire attribute list
        // (and therefore its argument expression) must be skipped.
        const string source = """
            using System;
            [Obsolete("hi" + "there")]
            class C { void M() { var s = "hi" + "there"; } }
            """;
        var (mutants, _) = RunOrchestratorOnSource(source);
        // The body's `"hi" + "there"` is mutable, so we expect at least one mutant.
        // None of them may originate inside the AttributeList span.
        mutants.Should().NotBeEmpty();
        mutants.Should().AllSatisfy(m =>
            m.Mutation.OriginalNode.Ancestors()
                .Any(a => a is Microsoft.CodeAnalysis.CSharp.Syntax.AttributeListSyntax)
                .Should().BeFalse("no mutant may originate from inside an AttributeList"));
    }

    [Fact]
    public void Mutate_StaticConstructorBody_MutantsMarkedStatic()
    {
        // StaticConstructorOrchestrator calls EnterStatic() before descending,
        // so MutationContext.InStaticValue propagates into Mutant.IsStaticValue.
        const string source = "class C { static int X; static C() { X = 1 + 2; } }";
        var (mutants, _) = RunOrchestratorOnSource(source);
        mutants.Should().NotBeEmpty();
        mutants.Should().AllSatisfy(m =>
            m.IsStaticValue.Should().BeTrue("mutants emitted under a static-constructor MUST carry IsStaticValue=true"));
    }

    [Fact]
    public void Mutate_NonStaticMethodBody_MutantsNotMarkedStatic()
    {
        // Control: ordinary instance method has no EnterStatic on the way down.
        var (mutants, _) = RunOrchestratorOnSource(SimpleAdd);
        mutants.Should().NotBeEmpty();
        mutants.Should().AllSatisfy(m =>
            m.IsStaticValue.Should().BeFalse("instance-method mutants must NOT carry IsStaticValue=true"));
    }

    [Fact]
    public void Mutate_BooleanLiteral_BooleanMutatorFires()
    {
        // BooleanMutator is in the Defaults profile; a `true` literal must produce
        // a mutant whose replacement is `false` (and vice versa).
        var (mutants, _) = RunOrchestratorOnSource("class C { bool M() => true; }");
        mutants.Should().NotBeEmpty("BooleanMutator must fire on a literal `true`");
        mutants.Should().Contain(m =>
            m.Mutation.ReplacementNode.ToString() == "false");
    }

    [Fact]
    public void Mutate_StringLiteral_StringMutatorFires()
    {
        // StringMutator → "hello" becomes empty string "".
        var (mutants, _) = RunOrchestratorOnSource("""class C { string M() => "hello"; }""");
        mutants.Should().NotBeEmpty("StringMutator must fire on a literal string");
        mutants.Should().Contain(m => m.Mutation.ReplacementNode.ToString() == "\"\"");
    }

    [Fact]
    public void Mutate_EmptyClass_ProducesNoMutants()
    {
        var (mutants, _) = RunOrchestratorOnSource("class C { }");
        mutants.Should().BeEmpty("a class with no body has no mutable nodes");
    }

    [Fact]
    public void Mutate_TwoMethods_BothProduceMutants()
    {
        const string source = """
            class C {
                int A(int x) => x + 1;
                int B(int x) => x - 1;
            }
            """;
        var (mutants, _) = RunOrchestratorOnSource(source);
        // Mutants from `+` AND mutants from `-` must both be present.
        var originals = mutants.Select(m => m.Mutation.OriginalNode.ToString()).ToList();
        originals.Should().Contain(o => o == "x + 1");
        originals.Should().Contain(o => o == "x - 1");
    }

    [Fact]
    public void Mutate_AllProfile_LoadsAtLeastAsManyMutatorsAsDefaults()
    {
        var defaultsOrch = BuildOrchestrator(MutationProfile.Defaults);
        var allOrch = BuildOrchestrator(MutationProfile.All);
        var defaults = GetActiveMutators(defaultsOrch);
        var all = GetActiveMutators(allOrch);
        all.Count.Should().BeGreaterThanOrEqualTo(defaults.Count,
            "the All profile must include every mutator the Defaults profile carries");
    }

    [Fact]
    public void Mutate_StrongerProfile_LoadsMoreMutatorsThanDefaults()
    {
        // Sprint 9-11 added several Stronger-tier operators; the Stronger profile
        // must expand strictly past Defaults.
        var defaultsOrch = BuildOrchestrator(MutationProfile.Defaults);
        var strongerOrch = BuildOrchestrator(MutationProfile.Stronger);
        var defaults = GetActiveMutators(defaultsOrch);
        var stronger = GetActiveMutators(strongerOrch);
        stronger.Count.Should().BeGreaterThan(defaults.Count,
            "Stronger profile must include Defaults plus extra Stronger-only mutators");
    }

    [Fact]
    public void Mutate_NullCoalescing_ProducesMutants()
    {
        // NullCoalescingExpressionMutator runs in the Defaults profile; ensures
        // the orchestrator wires it onto a real BinaryExpressionSyntax(?? kind).
        var (mutants, _) = RunOrchestratorOnSource("class C { string M(string s) => s ?? \"x\"; }");
        mutants.Should().NotBeEmpty("NullCoalescingExpressionMutator must fire on `s ?? \"x\"`");
    }
}
