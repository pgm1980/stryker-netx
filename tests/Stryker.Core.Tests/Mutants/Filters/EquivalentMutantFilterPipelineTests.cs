using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Stryker.Abstractions;
using Stryker.Core.Mutants.Filters;
using Xunit;

namespace Stryker.Core.Tests.Mutants.Filters;

public class EquivalentMutantFilterPipelineTests : MutatorTestBase
{
    [Fact]
    public void Default_ContainsAllShippedFilters()
    {
        var pipeline = EquivalentMutantFilterPipeline.Default;
        pipeline.Filters.Should().HaveCountGreaterThanOrEqualTo(4);
        pipeline.Filters.Select(f => f.FilterId).Should().Contain([
            "IdentityArithmetic",
            "IdempotentBoolean",
            "ConservativeDefaultsEquality",
            "RoslynDiagnostics",
            "RoslynSemanticDiagnostics",
        ]);
    }

    [Fact]
    public void IsEquivalent_WhenAnyFilterReturnsTrue_ReturnsTrue()
    {
        var trueFilter = new Mock<IEquivalentMutantFilter>();
        trueFilter.Setup(f => f.FilterId).Returns("True");
        trueFilter.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(true);

        var falseFilter = new Mock<IEquivalentMutantFilter>();
        falseFilter.Setup(f => f.FilterId).Returns("False");
        falseFilter.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(false);

        var pipeline = new EquivalentMutantFilterPipeline([falseFilter.Object, trueFilter.Object]);

        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        pipeline.IsEquivalent(mutation, semanticModel: null).Should().BeTrue();
    }

    [Fact]
    public void IsEquivalent_WhenAllFiltersReturnFalse_ReturnsFalse()
    {
        var falseFilter = new Mock<IEquivalentMutantFilter>();
        falseFilter.Setup(f => f.FilterId).Returns("False");
        falseFilter.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(false);

        var pipeline = new EquivalentMutantFilterPipeline([falseFilter.Object, falseFilter.Object]);

        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        pipeline.IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void IsEquivalent_OnEmptyPipeline_ReturnsFalse()
    {
        var pipeline = new EquivalentMutantFilterPipeline([]);
        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        pipeline.IsEquivalent(mutation, semanticModel: null).Should().BeFalse();
    }

    [Fact]
    public void FindEquivalentFilter_ReturnsFirstMatchingFilterId()
    {
        var trueFilter1 = new Mock<IEquivalentMutantFilter>();
        trueFilter1.Setup(f => f.FilterId).Returns("First");
        trueFilter1.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(true);

        var trueFilter2 = new Mock<IEquivalentMutantFilter>();
        trueFilter2.Setup(f => f.FilterId).Returns("Second");
        trueFilter2.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(true);

        var pipeline = new EquivalentMutantFilterPipeline([trueFilter1.Object, trueFilter2.Object]);

        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        pipeline.FindEquivalentFilter(mutation, semanticModel: null).Should().Be("First");
    }

    [Fact]
    public void FindEquivalentFilter_OnNoMatch_ReturnsNull()
    {
        var falseFilter = new Mock<IEquivalentMutantFilter>();
        falseFilter.Setup(f => f.FilterId).Returns("False");
        falseFilter.Setup(f => f.IsEquivalent(It.IsAny<Mutation>(), It.IsAny<SemanticModel?>())).Returns(false);

        var pipeline = new EquivalentMutantFilterPipeline([falseFilter.Object]);
        var node = ParseExpression<BinaryExpressionSyntax>("a + b");
        var mutation = BuildMutation(node, node);
        pipeline.FindEquivalentFilter(mutation, semanticModel: null).Should().BeNull();
    }
}
