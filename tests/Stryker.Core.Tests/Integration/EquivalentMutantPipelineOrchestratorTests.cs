using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// L3 integration layer (Sprint 20): the
/// <see cref="EquivalentMutantFilterPipeline"/> ↔
/// <see cref="CsharpMutantOrchestrator"/> integration. Sprint 7 (ADR-017)
/// wired the pipeline into the orchestrator: any filter that classifies a
/// mutation as "equivalent" causes the orchestrator to skip emitting the
/// mutant. Property tests (Sprint 19) cover OR-semantics + idempotence; this
/// layer covers Default-pipeline composition, ID-uniqueness, and end-to-end
/// orchestrator integration.
/// </summary>
[Trait("Category", "Integration")]
public class EquivalentMutantPipelineOrchestratorTests : IntegrationTestBase
{
    [Fact]
    public void DefaultPipeline_HasFiveFilters()
    {
        // Sprint 7 + 9 + 14 + 17 cumulative: 5 filters in the Default chain.
        EquivalentMutantFilterPipeline.Default.Filters.Should().HaveCount(5,
            "the Default pipeline must include all v2.0–v2.4 equivalent-filter rules");
    }

    [Fact]
    public void DefaultPipeline_ContainsAllExpectedFilterIds()
    {
        var ids = EquivalentMutantFilterPipeline.Default.Filters
            .Select(f => f.FilterId).ToList();
        ids.Should().Contain("IdentityArithmetic");
        ids.Should().Contain("IdempotentBoolean");
        ids.Should().Contain("RoslynDiagnostics");
    }

    [Fact]
    public void DefaultPipeline_FilterIdsAreUnique()
    {
        // FilterIds are surfaced in diagnostic logs and (eventually) reports,
        // so duplicates would make it impossible to attribute a skip.
        var ids = EquivalentMutantFilterPipeline.Default.Filters
            .Select(f => f.FilterId).ToList();
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void DefaultPipeline_ContainsConservativeDefaultsEqualityFilter()
    {
        // Sprint 9 (cargo-mutants companion): conservative-defaults filter
        // must be present in every default run.
        EquivalentMutantFilterPipeline.Default.Filters
            .OfType<ConservativeDefaultsEqualityFilter>()
            .Should().HaveCount(1);
    }

    [Fact]
    public void DefaultPipeline_ContainsRoslynSemanticDiagnosticsFilter()
    {
        // Sprint 17 (semantic-error pre-filter): Roslyn semantic-diag filter
        // must be present.
        EquivalentMutantFilterPipeline.Default.Filters
            .OfType<RoslynSemanticDiagnosticsEquivalenceFilter>()
            .Should().HaveCount(1);
    }

    [Fact]
    public void Orchestrator_OnSimpleArithmetic_DoesNotSpuriouslySkipAllMutants()
    {
        // Sanity check: an `a + b` expression must produce mutants under the
        // default pipeline — the filters should NOT be over-eager.
        var (mutants, _) = RunOrchestratorOnSource("class C { int M(int a, int b) => a + b; }");
        mutants.Should().NotBeEmpty(
            "the equivalent-mutant pipeline must not over-suppress on a plain add expression");
    }

    [Fact]
    public void Orchestrator_OnBooleanLiteral_DoesNotSpuriouslySkipAllMutants()
    {
        var (mutants, _) = RunOrchestratorOnSource("class C { bool M() => true; }");
        mutants.Should().NotBeEmpty(
            "BooleanMutator's `true → false` must not be filtered as equivalent");
    }

    [Fact]
    public void RoslynDiagnosticsFilter_OnReplacementWithErrorDiagnostic_ReturnsTrue()
    {
        // Construct a Mutation whose replacement carries a parser-error
        // diagnostic — the filter must classify it as equivalent (skip).
        var tree = CSharpSyntaxTree.ParseText("class C { int M() { return ?; } }");
        var bad = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().First(e => string.Equals(e.ToString(), "?", System.StringComparison.Ordinal));
        bad.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error).Should().BeTrue(
            "test setup: the constructed node must carry a parser error");

        var ok = SyntaxFactory.ParseExpression("a + b");
        var mutation = BuildMutation(ok, bad);
        var filter = new RoslynDiagnosticsEquivalenceFilter();
        filter.IsEquivalent(mutation, semanticModel: null).Should().BeTrue(
            "a replacement node carrying a parser-error diagnostic must be classified equivalent");
    }

    [Fact]
    public void RoslynDiagnosticsFilter_OnCleanReplacement_ReturnsFalse()
    {
        // Counterpart to the previous test: a syntactically clean replacement
        // must NOT be flagged.
        var ok1 = SyntaxFactory.ParseExpression("a + b");
        var ok2 = SyntaxFactory.ParseExpression("a - b");
        var mutation = BuildMutation(ok1, ok2);
        var filter = new RoslynDiagnosticsEquivalenceFilter();
        filter.IsEquivalent(mutation, semanticModel: null).Should().BeFalse(
            "a clean parser-valid replacement must not be classified equivalent");
    }

    [Fact]
    public void Pipeline_FindEquivalentFilter_ReturnsFilterIdOnHit()
    {
        var ok = SyntaxFactory.ParseExpression("a + b");
        var bad = CSharpSyntaxTree.ParseText("class C { int M() { return ?; } }")
            .GetRoot().DescendantNodes().OfType<ExpressionSyntax>().First(e => string.Equals(e.ToString(), "?", System.StringComparison.Ordinal));
        var mutation = BuildMutation(ok, bad);
        var hit = EquivalentMutantFilterPipeline.Default.FindEquivalentFilter(mutation, semanticModel: null);
        hit.Should().Be("RoslynDiagnostics",
            "FindEquivalentFilter must return the matching filter's ID for downstream attribution");
    }

    [Fact]
    public void Pipeline_FindEquivalentFilter_ReturnsNullWhenAllAbstain()
    {
        var ok1 = SyntaxFactory.ParseExpression("a + b");
        var ok2 = SyntaxFactory.ParseExpression("a - b");
        var mutation = BuildMutation(ok1, ok2);
        var hit = EquivalentMutantFilterPipeline.Default.FindEquivalentFilter(mutation, semanticModel: null);
        hit.Should().BeNull("no filter should classify a + b → a - b as equivalent");
    }

    [Fact]
    public void Pipeline_IsEquivalent_AgreesWithFindEquivalentFilter()
    {
        // Contract: IsEquivalent(...) returns (FindEquivalentFilter(...) is not null).
        var ok1 = SyntaxFactory.ParseExpression("a + b");
        var ok2 = SyntaxFactory.ParseExpression("a - b");
        var mutation = BuildMutation(ok1, ok2);
        var pipeline = EquivalentMutantFilterPipeline.Default;
        pipeline.IsEquivalent(mutation, semanticModel: null)
            .Should().Be(pipeline.FindEquivalentFilter(mutation, semanticModel: null) is not null);
    }
}
