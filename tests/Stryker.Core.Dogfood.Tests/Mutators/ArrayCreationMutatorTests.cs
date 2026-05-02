using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 47 (v2.34.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Sprint 97 (v2.83.0) un-skipped 2 tests via real StrykerOptions instance (production drift:
/// IMutator.Mutate now requires non-null IStrykerOptions).
/// </summary>
public class ArrayCreationMutatorTests
{
    private static readonly StrykerOptions DefaultOptions = new() { MutationLevel = MutationLevel.Standard };

    [Fact]
    public void ShouldBeMutationLevelStandard()
    {
        var target = new ArrayCreationMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Fact]
    public void ShouldRemoveValuesFromArrayCreation()
    {
        var expressionSyntax = (SyntaxFactory.ParseExpression("new int[] { 1, 3 }") as ArrayCreationExpressionSyntax)!;

        var target = new ArrayCreationMutator();

        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Array initializer mutation");

        var replacement = mutation.ReplacementNode.Should().BeOfType<ArrayCreationExpressionSyntax>().Which;
        replacement.Initializer!.Expressions.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotRemoveValuesFromImplicitArrayCreation()
    {
        var expressionSyntax = (SyntaxFactory.ParseExpression("new [] { 1, 3 }") as ImplicitArrayCreationExpressionSyntax)!;

        var target = new ArrayCreationMutator();

        // ImplicitArrayCreation is NOT ArrayCreation, so the mutator should not mutate it.
        // Pass through ApplyMutations on an ArrayCreation type only — but since the upstream test passes
        // ImplicitArrayCreationExpressionSyntax (not ArrayCreationExpressionSyntax) we can't call directly.
        // Use the IRegexMutator/IMutator dispatch via Mutate with cast:
        var result = ((IMutator)target).Mutate(expressionSyntax, null!, DefaultOptions).ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void ShouldNotMutateEmptyInitializer()
    {
        var arrayCreationExpression = (SyntaxFactory.ParseExpression("new int[] { }") as ArrayCreationExpressionSyntax)!;
        var implicitArrayCreationExpression = (SyntaxFactory.ParseExpression("new int[] { }") as ArrayCreationExpressionSyntax)!;

        var target = new ArrayCreationMutator();

        var result1 = target.ApplyMutations(arrayCreationExpression, null!).ToList();
        var result2 = target.ApplyMutations(implicitArrayCreationExpression, null!).ToList();

        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
    }

    [Fact]
    public void ShouldMutateStackallocArrays()
    {
        var stackallocArrayCreationExpression = (SyntaxFactory.ParseExpression("stackalloc int[] { 1 }") as StackAllocArrayCreationExpressionSyntax)!;

        var target = new ArrayCreationMutator();

        // Same type-mismatch issue as ImplicitArray — use Mutate (interface dispatch).
        var result = ((IMutator)target).Mutate(stackallocArrayCreationExpression, null!, DefaultOptions).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Array initializer mutation");

        var replacement = mutation.ReplacementNode.Should().BeOfType<StackAllocArrayCreationExpressionSyntax>().Which;
        replacement.Initializer!.Expressions.Should().BeEmpty();
    }
}
