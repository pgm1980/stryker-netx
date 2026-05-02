using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>
/// Sprint 46 (v2.33.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/MutantFilters/ExcludeMutationMutantFilterTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift (Sprint 2): Mutation has required members OriginalNode/ReplacementNode/DisplayName.
/// </summary>
public class ExcludeMutationMutantFilterTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance",
        Justification = "Test asserts behavior of IMutantFilter interface; perf-not-test-concern (Sprint 28 lesson).")]
    public void ShouldHaveName()
    {
        var target = new IgnoreMutationMutantFilter() as IMutantFilter;
        target!.DisplayName.Should().Be("mutation type filter");
    }

    [Theory]
    [InlineData(Mutator.Arithmetic, true)]
    [InlineData(Mutator.Assignment, false)]
    public void MutantFilter_ShouldSkipMutationsForExcludedMutatorType(Mutator excludedMutation, bool skipped)
    {
        var placeholderNode = SyntaxFactory.IdentifierName("_");
        var mutant = new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = placeholderNode,
                ReplacementNode = placeholderNode,
                DisplayName = "test-mutation",
                Type = Mutator.Arithmetic,
            },
        };

        var sut = new IgnoreMutationMutantFilter();

        var options = new StrykerOptions { ExcludedMutations = [excludedMutation] };

        var filteredMutants = sut.FilterMutants([mutant], null!, options);

        if (skipped)
        {
            filteredMutants.Should().NotContain(mutant);
        }
        else
        {
            filteredMutants.Should().Contain(mutant);
        }
    }
}
