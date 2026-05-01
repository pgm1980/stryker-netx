using System.Globalization;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.InjectedHelpers;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// L4 integration layer (Sprint 20): the
/// <see cref="MutantPlacer"/> ↔ orchestrator integration. The placer is the
/// component that injects the runtime selector (`if (Stryker.Active.IsActive(N))
/// mutated; else original;` and ternary equivalents) and stamps mutant IDs as
/// SyntaxAnnotations onto the produced nodes. This layer's invariants are
/// transitively exercised by L1 (any mutation flows through the placer), so
/// L4 stays focused on the placer's distinguishing behaviour: annotations are
/// recoverable, mutant IDs survive, the mutated tree parses cleanly, and the
/// placer's selector expression is observable in the produced tree.
/// </summary>
[Trait("Category", "Integration")]
public class MutantPlacerInjectionTests : IntegrationTestBase
{
    private const string SimpleSource = "class C { int M(int a, int b) => a + b; }";

    [Fact]
    public void Mutate_ProducesInjectorAnnotationOnEveryMutantSite()
    {
        var (mutants, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        mutants.Should().NotBeEmpty();

        // Every mutant should leave an Injector annotation somewhere in the tree.
        var injectorNodes = mutatedTree.GetRoot().GetAnnotatedNodes(MutantPlacer.Injector).ToList();
        injectorNodes.Should().NotBeEmpty(
            "MutantPlacer must annotate the injected node with the Injector marker");
    }

    [Fact]
    public void Mutate_AnnotatesEachInjectedNodeWithMutantId()
    {
        var (mutants, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        var injectorNodes = mutatedTree.GetRoot().GetAnnotatedNodes(MutantPlacer.Injector).ToList();
        var mutantIdsFromTree = injectorNodes
            .Select(MutantPlacer.FindAnnotations)
            .Where(info => info.Id is >= 0)
            .Select(info => info.Id!.Value)
            .ToHashSet();
        var mutantIdsFromList = mutants.Select(m => m.Id).ToHashSet();
        mutantIdsFromTree.IsSubsetOf(mutantIdsFromList).Should().BeTrue(
            "every Mutant ID found via tree annotations must originate from the orchestrator's mutant list");
    }

    [Fact]
    public void Mutate_FindAnnotationsCarriesEngineAndType()
    {
        var (_, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        var first = mutatedTree.GetRoot().GetAnnotatedNodes(MutantPlacer.Injector).First();
        var info = MutantPlacer.FindAnnotations(first);
        info.Id.Should().NotBeNull(
            "MutantPlacer.FindAnnotations must surface a parsed mutant ID for an injected node");
        info.Id!.Value.Should().BeGreaterThanOrEqualTo(0);
        info.Engine.Should().NotBeNullOrEmpty(
            "MutantPlacer.FindAnnotations must surface the engine ID for downstream rollback");
        info.Type.Should().NotBeNullOrEmpty(
            "MutantPlacer.FindAnnotations must surface the mutator-type marker");
    }

    [Fact]
    public void Mutate_TreeContainsRuntimeSelectorExpression()
    {
        // The CodeInjection injects a `StrykerXxxxxxx.MutantControl.IsActive(N)`
        // selector at every mutant site. The runtime helper namespace carries a
        // per-process random suffix (drift-resistance), so we assert on the
        // stable parts of the selector — that's how the runtime decides whether
        // the mutated branch executes.
        var (_, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        var emitted = mutatedTree.ToString();
        emitted.Should().Contain("MutantControl.IsActive",
            "the placer must reference the runtime selector helper in the emitted tree");
        emitted.Should().Contain("Stryker",
            "the helper namespace prefix must appear in the emitted tree");
    }

    [Fact]
    public void Mutate_MutatedTreeProducesValidCSharp()
    {
        var (_, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        var roundtrip = CSharpSyntaxTree.ParseText(mutatedTree.ToString());
        roundtrip.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty("the placer's emitted tree must be syntactically valid C#");
    }

    [Fact]
    public void Mutate_MultipleMutantsOnOneStatement_ChainsControlFlow()
    {
        // A statement with several mutator hits (e.g. `int x = a + b;` triggers
        // BinaryExpressionMutator AND others). The placer chains the conditional
        // selectors, so the number of injected annotations is at least the
        // number of mutants on that statement.
        var (mutants, mutatedTree) = RunOrchestratorOnSource(
            "class C { int M(int a, int b) { int x = a + b; return x; } }");
        mutants.Should().NotBeEmpty();
        var injectorCount = mutatedTree.GetRoot().GetAnnotatedNodes(MutantPlacer.Injector).Count();
        injectorCount.Should().BeGreaterThanOrEqualTo(mutants.Count,
            "each mutant must leave at least one Injector-annotated node in the tree");
    }

    [Fact]
    public void Mutate_MutationIdsOnTreeAreParseableIntegers()
    {
        // The MutationIdMarker stores the ID as InvariantCulture int. Must round-trip.
        var (_, mutatedTree) = RunOrchestratorOnSource(SimpleSource);
        var nodesWithMutationId = mutatedTree.GetRoot().GetAnnotatedNodes("MutationId").ToList();
        nodesWithMutationId.Should().NotBeEmpty();
        nodesWithMutationId.Should().AllSatisfy(n =>
        {
            var data = n.GetAnnotations("MutationId").First().Data;
            int.TryParse(data, NumberStyles.Integer, CultureInfo.InvariantCulture, out _).Should().BeTrue(
                $"MutationId annotation '{data}' must be a parseable invariant-culture int");
        });
    }

    [Fact]
    public void Mutate_MutationMarkersExposedThroughPublicSurface()
    {
        // The exposed MutationMarkers list (driving rollback / annotation lookup)
        // must contain at least Injector + MutationId + MutationType; integration
        // tests should fail loudly if a marker is silently removed because every
        // downstream tool (rollback, reporter) keys off these names.
        MutantPlacer.MutationMarkers.Should().Contain(MutantPlacer.Injector);
        MutantPlacer.MutationMarkers.Should().Contain("MutationId");
        MutantPlacer.MutationMarkers.Should().Contain("MutationType");
    }
}
