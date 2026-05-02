using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>
/// Sprint 60 (v2.46.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Subset port: ShouldHaveDisplayName + ShouldCountForStats. Other tests defer
/// (TestIdentifierList ctor signature drift requires investigation).
/// </summary>
public class MutantTests
{
    [Fact]
    public void ShouldHaveDisplayName()
    {
        var placeholder = SyntaxFactory.IdentifierName("_");
        var mutant = new Mutant
        {
            Id = 1,
            Mutation = new Mutation
            {
                OriginalNode = placeholder,
                ReplacementNode = placeholder,
                DisplayName = "test mutation",
            },
        };

        mutant.DisplayName.Should().Be("1: test mutation");
    }

    [Theory]
    [InlineData(MutantStatus.CompileError, false)]
    [InlineData(MutantStatus.Ignored, false)]
    [InlineData(MutantStatus.Killed, true)]
    [InlineData(MutantStatus.NoCoverage, true)]
    [InlineData(MutantStatus.Pending, true)]
    [InlineData(MutantStatus.Survived, true)]
    [InlineData(MutantStatus.Timeout, true)]
    public void ShouldCountForStats(MutantStatus status, bool doesCount)
    {
        var mutant = new Mutant { ResultStatus = status };
        mutant.CountForStats.Should().Be(doesCount);
    }
}
