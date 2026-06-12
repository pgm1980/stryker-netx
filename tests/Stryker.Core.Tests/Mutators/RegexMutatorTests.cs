using System;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class RegexMutatorTests : MutatorTestBase
{
    public RegexMutatorTests() =>
        // RegexMutator resolves its logger from ApplicationLogging at construction time,
        // and MutatorTestBase does not seed that factory the way TestBase does.
        Stryker.Utilities.Logging.ApplicationLogging.LoggerFactory = new Microsoft.Extensions.Logging.LoggerFactory();

    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<RegexMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void Type_IsRegexMutator()
        => typeof(RegexMutator).Should().NotBeNull();

    // Sprint 182 (issue #277a): IsAStringExpression bejaht absichtlich auch interpolierte
    // Strings, direkt danach wurde aber hart auf LiteralExpressionSyntax gecastet — die
    // InvalidCastException riss den GESAMTEN Lauf (Exit 127, kein Report). Interpolierte
    // Patterns sind nicht statisch mutierbar und werden uebersprungen.
    [Theory]
    [InlineData("""new System.Text.RegularExpressions.Regex($"^{prefix}")""")]
    [InlineData("""new System.Text.RegularExpressions.Regex(pattern: $"{a}{b}")""")]
    public void ApplyMutations_OnInterpolatedPattern_YieldsNothingInsteadOfCrashing(string source)
    {
        var node = ParseExpression<ObjectCreationExpressionSyntax>(source);

        var act = () => ApplyMutations<RegexMutator, ObjectCreationExpressionSyntax>(new(), node);

        act.Should().NotThrow<InvalidCastException>();
        act().Should().BeEmpty("interpolated patterns cannot be mutated statically");
    }

    [Fact]
    public void ApplyMutations_OnLiteralPattern_StillYieldsMutations()
    {
        var node = ParseExpression<ObjectCreationExpressionSyntax>(
            """new System.Text.RegularExpressions.Regex("^abc")""");

        var mutations = ApplyMutations<RegexMutator, ObjectCreationExpressionSyntax>(new(), node);

        mutations.Should().NotBeEmpty("literal patterns must stay mutable");
    }
}
