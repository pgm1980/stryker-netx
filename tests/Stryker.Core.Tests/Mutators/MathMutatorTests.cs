using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class MathMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllProfiles()
        => AssertProfileMembership<MathMutator>(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All);

    [Theory]
    [InlineData("Math.Sin(x)")]
    [InlineData("Math.Cos(x)")]
    [InlineData("Math.Floor(x)")]
    [InlineData("Math.Ceiling(x)")]
    [InlineData("Math.Log(x)")]
    [InlineData("Math.Exp(x)")]
    public void ApplyMutations_OnMathMethod_EmitsMutation(string source)
    {
        var node = ParseExpression<InvocationExpressionSyntax>(source);
        var mutations = ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnNonMathMethod_ReturnsNoMutation()
    {
        var node = ParseExpression<InvocationExpressionSyntax>("Foo.Bar()");
        AssertNoMutations(ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node));
    }

    [Fact]
    public void ApplyMutations_OnMathMemberCall_WithSemanticModel_EmitsMutation()
    {
        // MAT-001 (external 360 test): with a REAL semantic model the idiomatic member-call form must
        // still mutate (Ceiling to Floor). The member path checked the ContainingType of the receiver,
        // but the receiver binds to the top-level System.Math type whose ContainingType is null, so the
        // check failed and produced zero mutants. The existing tests pass a null model (text-fallback
        // path) which masked the bug; the using-static direct-call form was never affected.
        var (model, node) = BuildSemanticContext<InvocationExpressionSyntax>(
            "using System; class C { double M(double x) => Math.Ceiling(x); }");
        var mutations = ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node, model);
        mutations.Should().NotBeEmpty("Math.Ceiling must mutate even with a real semantic model present");
    }

    [Fact]
    public void ApplyMutations_OnNonSystemMathMemberCall_WithSemanticModel_ReturnsNoMutation()
    {
        // Control: a same-named method on a different type must NOT mutate — the receiver symbol is
        // compared to System.Math, not merely the method name.
        var (model, node) = BuildSemanticContext<InvocationExpressionSyntax>(
            "static class Other { public static double Ceiling(double x) => x; } class C { double M(double x) => Other.Ceiling(x); }");
        AssertNoMutations(ApplyMutations<MathMutator, InvocationExpressionSyntax>(new(), node, model));
    }
}
