using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class DateTimeMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<DateTimeMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("DateTime.Now")]
    [InlineData("DateTime.UtcNow")]
    [InlineData("DateTimeOffset.Now")]
    [InlineData("DateTimeOffset.UtcNow")]
    public void ApplyMutations_OnDateTimeNowUtcNow_EmitsSwap(string source)
    {
        var node = ParseExpression<MemberAccessExpressionSyntax>(source);
        AssertSingleMutation(ApplyMutations<DateTimeMutator, MemberAccessExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnUnrelatedMemberAccess_ReturnsNoMutation()
    {
        var node = ParseExpression<MemberAccessExpressionSyntax>("Foo.Bar");
        AssertNoMutations(ApplyMutations<DateTimeMutator, MemberAccessExpressionSyntax>(new(), node));
    }
}
