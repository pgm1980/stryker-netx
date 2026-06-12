using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// Sprint 181 (issue #286, 360°-Analyse G-15): the ADR-032 safety net
/// (<c>OrchestrationHelpers.ReplaceChildrenValidated</c>) drops slot-incompatible mutated
/// subtrees. The mutants wrapped inside those subtrees are already registered and consumed
/// from the store — without follow-up they never reach the assembly and end as ghosts:
/// NoCoverage in coverage mode, Survived without coverage. Both silently corrupt the score.
/// The orchestrator must mark them as CompileError instead.
/// </summary>
[Trait("Category", "Integration")]
public sealed class GhostMutantTests : IntegrationTestBase
{
    [Fact]
    public void FlagDroppedMutants_MarksPendingMutantsOfDroppedSubtreeAsCompileError()
    {
        var orchestrator = BuildOrchestrator();
        var pending = BuildRegisteredMutant(42, MutantStatus.Pending);
        var killed = BuildRegisteredMutant(43, MutantStatus.Killed);
        orchestrator.Mutants.Add(pending);
        orchestrator.Mutants.Add(killed);
        var droppedSubtree = SyntaxFactory.IdentifierName("x").WithAdditionalAnnotations(
            new SyntaxAnnotation("MutationId", "42"),
            new SyntaxAnnotation("MutationId", "43"),
            new SyntaxAnnotation("MutationId", "999"));

        orchestrator.FlagDroppedMutants(droppedSubtree);

        pending.ResultStatus.Should().Be(MutantStatus.CompileError,
            "a dropped pending mutant never reaches the assembly and must not be tested");
        pending.ResultStatusReason.Should().NotBeNullOrEmpty();
        killed.ResultStatus.Should().Be(MutantStatus.Killed,
            "mutants with a settled result must not be rewritten");
    }

    [Fact]
    public void FlagDroppedMutants_OnSubtreeWithoutMutationAnnotations_ChangesNothing()
    {
        var orchestrator = BuildOrchestrator();
        var pending = BuildRegisteredMutant(7, MutantStatus.Pending);
        orchestrator.Mutants.Add(pending);

        orchestrator.FlagDroppedMutants(SyntaxFactory.IdentifierName("x"));

        pending.ResultStatus.Should().Be(MutantStatus.Pending);
    }

    private static Mutant BuildRegisteredMutant(int id, MutantStatus status)
    {
        var node = SyntaxFactory.ParseExpression("1 + 1");
        return new Mutant
        {
            Id = id,
            ResultStatus = status,
            Mutation = new Mutation
            {
                OriginalNode = node,
                ReplacementNode = SyntaxFactory.ParseExpression("1 - 1"),
                Type = Mutator.Arithmetic,
                DisplayName = "test",
            },
        };
    }
}
