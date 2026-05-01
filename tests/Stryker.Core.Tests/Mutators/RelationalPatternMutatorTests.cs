using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class RelationalPatternMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<RelationalPatternMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("x is < 10")]
    [InlineData("x is <= 10")]
    [InlineData("x is > 10")]
    [InlineData("x is >= 10")]
    public void ApplyMutations_OnRelationalPattern_EmitsAtLeastOneMutation(string source)
    {
        var pattern = ParseExpression<IsPatternExpressionSyntax>(source).Pattern;
        var relPattern = pattern.Should().BeOfType<RelationalPatternSyntax>().Subject;
        var mutations = ApplyMutations<RelationalPatternMutator, RelationalPatternSyntax>(new(), relPattern);
        mutations.Should().NotBeEmpty();
    }
}
