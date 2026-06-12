using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// Sprint 182 (issue #277b, 360°-Analyse): the orchestrator loop had no guard around
/// <c>mutator.Mutate(...)</c> — a single buggy mutator (e.g. the former RegexMutator
/// InvalidCastException on interpolated patterns) killed the whole run with exit 127
/// and no report. A mutator bug must never end the run: skip the mutator for that node,
/// log a warning, keep every other mutator's output.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MutatorRobustnessTests : IntegrationTestBase
{
    [Fact]
    public void Mutate_WhenAMutatorThrows_ContinuesWithRemainingMutators()
    {
        var throwingMutator = new Mock<IMutator>();
        throwingMutator
            .Setup(m => m.Mutate(It.IsAny<SyntaxNode>(), It.IsAny<SemanticModel>(), It.IsAny<IStrykerOptions>()))
            .Throws(new InvalidCastException("simulated mutator bug"));
        var orchestrator = BuildOrchestrator(customMutators:
            [throwingMutator.Object, new Stryker.Core.Mutators.BinaryExpressionMutator()]);

        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        var compilation = CSharpCompilation.Create("RobustnessTestAssembly", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var act = () => orchestrator.Mutate(tree, compilation.GetSemanticModel(tree));

        act.Should().NotThrow("a mutator bug must never kill the whole run");
        orchestrator.Mutants.Should().NotBeEmpty(
            "the remaining healthy mutators must still produce their mutations");
        orchestrator.Mutants.Select(m => m.Mutation.Type).Should().Contain(Mutator.Arithmetic);
    }

    [Fact]
    public void Mutate_WhenAMutatorThrowsOperationCanceled_Propagates()
    {
        var cancellingMutator = new Mock<IMutator>();
        cancellingMutator
            .Setup(m => m.Mutate(It.IsAny<SyntaxNode>(), It.IsAny<SemanticModel>(), It.IsAny<IStrykerOptions>()))
            .Throws(new OperationCanceledException());
        var orchestrator = BuildOrchestrator(customMutators: [cancellingMutator.Object]);

        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        var compilation = CSharpCompilation.Create("RobustnessTestAssembly", [tree],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var act = () => orchestrator.Mutate(tree, compilation.GetSemanticModel(tree));

        act.Should().Throw<OperationCanceledException>("cancellation must not be swallowed");
    }
}
