using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using FsCheck.Xunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Stryker.Abstractions;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Properties;

/// <summary>
/// v2.6.0 (Sprint 19, Item C / ToT property P4 + P6): pipeline-OR
/// semantics + filter idempotence.
/// </summary>
public class FilterPipelineProperties : MutatorTestBase
{
    [Property(MaxTest = 30)]
    public bool Pipeline_IsEquivalent_EqualsAnyOfMockedFilters(bool[] results)
    {
        results ??= [];
        var mocks = new List<IEquivalentMutantFilter>(results.Length);
        for (var i = 0; i < results.Length; i++)
        {
            var r = results[i];
            var m = new Mock<IEquivalentMutantFilter>();
            m.Setup(f => f.FilterId).Returns($"F{i}");
            m.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(r);
            mocks.Add(m.Object);
        }
        var pipeline = new EquivalentMutantFilterPipeline(mocks);
        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        return pipeline.IsEquivalent(mutation, semanticModel: null) == results.Any(r => r);
    }

    [Fact]
    public void IdentityArithmeticFilter_IsIdempotent()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("x + 0");
        var mutation = BuildMutation(node, node);
        var filter = new IdentityArithmeticFilter();
        var first = filter.IsEquivalent(mutation, semanticModel: null);
        var second = filter.IsEquivalent(mutation, semanticModel: null);
        first.Should().Be(second);
    }

    [Fact]
    public void IdempotentBooleanFilter_IsIdempotent()
    {
        var original = ParseExpression<PrefixUnaryExpressionSyntax>("!!x");
        var replacement = ParseExpression<IdentifierNameSyntax>("x");
        var mutation = BuildMutation(original, replacement);
        var filter = new IdempotentBooleanFilter();
        filter.IsEquivalent(mutation, null).Should().Be(filter.IsEquivalent(mutation, null));
    }

    [Fact]
    public void RoslynDiagnosticsFilter_IsIdempotent()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        var filter = new RoslynDiagnosticsEquivalenceFilter();
        filter.IsEquivalent(mutation, null).Should().Be(filter.IsEquivalent(mutation, null));
    }
}
