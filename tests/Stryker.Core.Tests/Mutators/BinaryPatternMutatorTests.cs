using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class BinaryPatternMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<BinaryPatternMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnAndPattern_EmitsAtLeastOneMutation()
    {
        var pattern = ParseExpression<IsPatternExpressionSyntax>("x is > 0 and < 10").Pattern;
        var binPattern = pattern.Should().BeOfType<BinaryPatternSyntax>().Subject;
        var mutations = ApplyMutations<BinaryPatternMutator, BinaryPatternSyntax>(new(), binPattern);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnOrPattern_EmitsAtLeastOneMutation()
    {
        var pattern = ParseExpression<IsPatternExpressionSyntax>("x is 1 or 2").Pattern;
        var binPattern = pattern.Should().BeOfType<BinaryPatternSyntax>().Subject;
        var mutations = ApplyMutations<BinaryPatternMutator, BinaryPatternSyntax>(new(), binPattern);
        mutations.Should().NotBeEmpty();
    }
}
