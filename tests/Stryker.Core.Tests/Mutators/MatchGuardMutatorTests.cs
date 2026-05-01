using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class MatchGuardMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<MatchGuardMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnWhenClause_EmitsTrueAndFalse()
    {
        var member = ParseMember<SwitchStatementSyntax>("void M(int x) { switch (x) { case 1 when x > 0: break; default: break; } }");
        var whenClause = member.DescendantNodes().OfType<WhenClauseSyntax>().FirstOrDefault();
        whenClause.Should().NotBeNull();
        var mutations = ApplyMutations<MatchGuardMutator, WhenClauseSyntax>(new(), whenClause!);
        AssertMutationCount(mutations, 2);
    }
}
