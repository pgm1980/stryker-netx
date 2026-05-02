using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 77 (v2.63.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class IgnoreBlockMutantFilterTests
{
    [Fact]
    public void ShouldHaveName()
    {
        var sut = new IgnoreBlockMutantFilter();
        sut.DisplayName.Should().Be("block already covered filter");
    }

    [Fact]
    public void Type_ShouldBeIgnoreBlockRemoval()
    {
        var sut = new IgnoreBlockMutantFilter();

        sut.Type.Should().Be(MutantFilter.IgnoreBlockRemoval);
    }

    [Fact]
    public void MutantFilter_WithMutationsInBlock_ShouldIgnoreBlockMutant()
    {
        var source = """
            public void SomeMethod()
            {
                var x = 1 + 1;
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var blockNode = syntaxTree.DescendantNodes().OfType<BlockSyntax>().First();
        var binaryExpressionNode = blockNode.DescendantNodes().OfType<ExpressionSyntax>().First();

        var blockMutant = new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = blockNode,
                ReplacementNode = blockNode,
                DisplayName = "block",
                Type = Mutator.Block,
            },
        };
        var binaryExpressionMutant = new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = binaryExpressionNode,
                ReplacementNode = binaryExpressionNode,
                DisplayName = "expr",
            },
        };

        var sut = new IgnoreBlockMutantFilter();

        var filteredMutants = sut.FilterMutants([blockMutant, binaryExpressionMutant], null!, null!);

        filteredMutants.Should().Contain(binaryExpressionMutant);
        filteredMutants.Should().NotContain(blockMutant);
    }

    [Fact]
    public void MutantFilter_WithNoMutationsInBlock_ShouldNotIgnoreBlockMutant()
    {
        var source = """
            public void SomeMethod()
            {
                var x = 1 + 1;
            }
            """;
        var syntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var blockNode = syntaxTree.DescendantNodes().OfType<BlockSyntax>().First();
        var blockMutant = new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = blockNode,
                ReplacementNode = blockNode,
                DisplayName = "block",
                Type = Mutator.Block,
            },
        };
        var sut = new IgnoreBlockMutantFilter();

        var filteredMutants = sut.FilterMutants([blockMutant], null!, null!);

        filteredMutants.Should().Contain(blockMutant);
        blockMutant.ResultStatus.Should().NotBe(MutantStatus.Ignored);
    }
}
