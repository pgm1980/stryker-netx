using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 48 (v2.35.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Tests the abstract base MutatorBase&lt;T&gt; via a custom ExampleMutator.
/// </summary>
public class AbstractMutatorTests
{
    internal sealed class ExampleMutator : MutatorBase<BinaryExpressionSyntax>
    {
        public ExampleMutator(MutationLevel mutationLevel) => MutationLevel = mutationLevel;

        public override MutationLevel MutationLevel { get; }

        public override IEnumerable<Mutation> ApplyMutations(BinaryExpressionSyntax node, SemanticModel semanticModel) =>
            throw new NotSupportedException("Test marker — used to verify the base class invoked ApplyMutations.");
    }

    [Fact]
    public void Mutator_ShouldCallApplyMutations_OnExpectedType()
    {
        var originalNode = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));

        var target = new ExampleMutator(MutationLevel.Basic);

        Action act = () => target.Mutate(originalNode, null!, new StrykerOptions());
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Mutator_ShouldNotCallApplyMutations_OnWrongType()
    {
        var originalNode = SyntaxFactory.ReturnStatement(
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var target = new ExampleMutator(MutationLevel.Basic);

        var result = target.Mutate(originalNode, null!, new StrykerOptions());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Mutator_ShouldNotCallApplyMutations_OnWrongType2()
    {
        var originalNode = SyntaxFactory.AssignmentExpression(
            SyntaxKind.SimpleAssignmentExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(100)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(5)));

        var target = new ExampleMutator(MutationLevel.Basic);

        var result = target.Mutate(originalNode, null!, new StrykerOptions());

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateIfMutationLevelIsLow()
    {
        var originalNode = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));

        var target = new ExampleMutator(MutationLevel.Complete);

        var options = new StrykerOptions { MutationLevel = MutationLevel.Standard };
        var result = target.Mutate(originalNode, null!, options);

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateIfLevelIsEqual()
    {
        var originalNode = SyntaxFactory.BinaryExpression(
            SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));
        var options = new StrykerOptions { MutationLevel = MutationLevel.Complete };
        var target = new ExampleMutator(MutationLevel.Complete);

        Action act = () => target.Mutate(originalNode, null!, options);
        act.Should().Throw<NotSupportedException>();
    }
}
