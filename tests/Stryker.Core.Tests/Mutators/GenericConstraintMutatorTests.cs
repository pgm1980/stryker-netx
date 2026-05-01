using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class GenericConstraintMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<GenericConstraintMutator>(MutationProfile.All);

    [Fact]
    public void MutationLevel_IsComplete()
        => AssertMutationLevel<GenericConstraintMutator>(MutationLevel.Complete);

    [Fact]
    public void ApplyMutations_OnMethodWithSingleConstraint_DropsAllConstraints()
    {
        var node = ParseMember<MethodDeclarationSyntax>("void Foo<T>() where T : class { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        var mutation = AssertSingleMutation(mutations);
        var replacement = mutation.ReplacementNode.Should().BeOfType<MethodDeclarationSyntax>().Subject;
        replacement.ConstraintClauses.Should().BeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnMethodWithMultipleConstraints_DropsAllAtOnce()
    {
        var node = ParseMember<MethodDeclarationSyntax>(
            "void Foo<T, U>() where T : class where U : struct { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        var mutation = AssertSingleMutation(mutations);
        var replacement = mutation.ReplacementNode.Should().BeOfType<MethodDeclarationSyntax>().Subject;
        replacement.ConstraintClauses.Should().BeEmpty();
    }

    [Fact]
    public void ApplyMutations_DisplayName_FormatsWithMethodIdentifier()
    {
        var node = ParseMember<MethodDeclarationSyntax>("void Foo<T>() where T : class { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        var mutation = AssertSingleMutation(mutations);
        mutation.DisplayName.Should().Contain("Foo");
        mutation.DisplayName.Should().Contain("Generic constraints dropped");
    }

    [Fact]
    public void ApplyMutations_OnMethodWithoutConstraints_ReturnsNoMutation()
    {
        var node = ParseMember<MethodDeclarationSyntax>("void Foo<T>() { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnNonGenericMethod_ReturnsNoMutation()
    {
        var node = ParseMember<MethodDeclarationSyntax>("void Foo() { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_TypeIsStatementMutator()
    {
        var node = ParseMember<MethodDeclarationSyntax>("void Foo<T>() where T : class { }");
        var mutations = ApplyMutations<GenericConstraintMutator, MethodDeclarationSyntax>(new(), node);
        AssertSingleMutation(mutations).Type.Should().Be(Mutator.Statement);
    }
}
