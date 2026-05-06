using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class SpanReadOnlySpanDeclarationMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsNone_AsOfSprint142()
    {
        // Sprint 142 (ADR-026, Bug #9 from Calculator-tester report): the mutator
        // emits GenericNameSyntax replacements exclusively in TypeSyntax positions,
        // which the ConditionalInstrumentationEngine cannot wrap without producing
        // an InvalidCastException (ParenthesizedExpression -> TypeSyntax). Disabled
        // from all profiles via Profile.None pending an engine-side fix that
        // supports type-position-aware mutation control.
        AssertProfileMembership<SpanReadOnlySpanDeclarationMutator>(MutationProfile.None);
    }

    [Fact]
    public void Type_IsSpanReadOnlySpanDeclarationMutator()
        => typeof(SpanReadOnlySpanDeclarationMutator).Should().NotBeNull();
}
