using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Stryker.Core.Dogfood.Tests.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 75 (v2.61.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class IgnoreMutationsInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new IgnoreMutationsInput();
        target.HelpText.Should().Be(
            "The given mutators will be excluded for this mutation testrun.\n    This argument takes a json array as value. Example: ['string', 'logical'] | default: []"
                .Replace("\n", System.Environment.NewLine, StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldValidateExcludedMutation()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = ["gibberish"] };

        var act = () => target.Validate<Mutator>();

        act.Should().Throw<InputException>()
            .WithMessage("Invalid excluded mutation (gibberish). The excluded mutations options are [Statement, Arithmetic, Block, Equality, Boolean, Logical, Assignment, Unary, Update, Checked, Linq, String, Bitwise, Initializer, Regex, NullCoalescing, Math, StringMethod, Conditional, CollectionExpression]");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = [] };

        var result = target.Validate<Mutator>();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldIgnoreMutatorWithOptions()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = ["linq.Sum", "string.empty", "logical.equal"] };

        var result = target.Validate<Mutator>();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnMultipleMutators()
    {
        var target = new IgnoreMutationsInput
        {
            SuppliedInput =
            [
                Mutator.String.ToString(),
                Mutator.Regex.ToString(),
            ],
        };

        var result = target.Validate<Mutator>().ToList();

        result.Should().HaveCount(2);
        result[0].Should().Be(Mutator.String);
        result[1].Should().Be(Mutator.Regex);
    }

    private static IEnumerable<LinqExpression> AllLinqExpressions { get; } =
        [.. Enum.GetValues<LinqExpression>().Where(w => w != LinqExpression.None)];

    [Fact]
    public void ShouldReturnEmptyLinqExpressionsWithNonLinqOptions()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = ["gibberish"] };
        var linqExpressions = target.ValidateLinqExpressions();
        linqExpressions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("linq.nothing")]
    [InlineData("linq.test")]
    [InlineData("linq.first.default")]
    public void ShouldValidateExcludedLinqExpression(string method)
    {
        var target = new IgnoreMutationsInput { SuppliedInput = [method] };

        var act = () => target.ValidateLinqExpressions();

        act.Should().Throw<InputException>()
            .WithMessage($"Invalid excluded linq expression ({string.Join(".", method.Split('.').Skip(1))}). The excluded linq expression options are [{string.Join(", ", AllLinqExpressions)}]");
    }

    [Fact]
    public void ShouldHaveDefaultLinqExpressions()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = [] };

        var linqExpressions = target.ValidateLinqExpressions();

        linqExpressions.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnMultipleLinqExpressions()
    {
        var target = new IgnoreMutationsInput
        {
            SuppliedInput =
            [
                "linq.FirstOrDefault",
                "linq.First",
            ],
        };

        var linqExpressions = target.ValidateLinqExpressions().ToList();

        linqExpressions.Should().HaveCount(2);
        linqExpressions[0].Should().Be(LinqExpression.FirstOrDefault);
        linqExpressions[1].Should().Be(LinqExpression.First);
    }

    [Fact]
    public void ShouldIgnoreIncorrectFormatWhenValidateLinqExpressions()
    {
        var target = new IgnoreMutationsInput
        {
            SuppliedInput =
            [
                "linq.Max",
                "linq.Sum",
                "test",
            ],
        };

        var linqExpressions = target.ValidateLinqExpressions().ToList();

        linqExpressions.Should().HaveCount(2);
        linqExpressions[0].Should().Be(LinqExpression.Max);
        linqExpressions[1].Should().Be(LinqExpression.Sum);
    }

    /// <summary>
    /// This test is needed as other mutators also have "statement" in their name.
    /// It should pick the right mutator.
    /// </summary>
    [Fact]
    public void ShouldIgnoreStatementMutator()
    {
        var target = new IgnoreMutationsInput { SuppliedInput = ["statement"] };

        var mutators = target.Validate<Mutator>();

        mutators.Should().ContainSingle().Which.Should().Be(Mutator.Statement);
    }

    [Fact]
    public void ShouldIgnoreBasedOnEitherDescription()
    {
        var targetWithFirstDescription = new IgnoreMutationsInput { SuppliedInput = ["Multi-description mutator"] };
        var targetWithSecondDescription = new IgnoreMutationsInput { SuppliedInput = ["Two descriptions mutator"] };

        var mutatorsWithFirstDescription = targetWithFirstDescription.Validate<TestMutator>();
        var mutatorsWithSecondDescription = targetWithSecondDescription.Validate<TestMutator>();

        mutatorsWithFirstDescription.Should().ContainSingle().Which.Should().Be(TestMutator.MultipleDescriptions);
        mutatorsWithSecondDescription.Should().ContainSingle().Which.Should().Be(TestMutator.MultipleDescriptions);
    }
}
