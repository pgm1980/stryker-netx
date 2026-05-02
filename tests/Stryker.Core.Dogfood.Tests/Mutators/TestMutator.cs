using Stryker.Abstractions;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 75 (v2.61.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Mutators/TestMutator.cs.
/// Test-only enum used by IgnoreMutationsInputTests.ShouldIgnoreBasedOnEitherDescription
/// to verify that <see cref="MutatorDescriptionAttribute"/> matching uses BOTH descriptions
/// when an enum member carries multiple attribute instances.
/// </summary>
public enum TestMutator
{
    [MutatorDescription("Simple mutator")]
    Simple,
    [MutatorDescription("Two descriptions mutator")]
    [MutatorDescription("Multi-description mutator")]
    MultipleDescriptions,
}
