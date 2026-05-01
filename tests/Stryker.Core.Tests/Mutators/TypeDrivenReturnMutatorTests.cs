using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class TypeDrivenReturnMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<TypeDrivenReturnMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsAdvanced()
        => AssertMutationLevel<TypeDrivenReturnMutator>(MutationLevel.Advanced);

    // The null-SemanticModel branch is owned by TypeAwareMutatorBase and tested
    // separately in TypeAwareMutatorBaseTests.

    [Fact]
    public void ApplyMutations_OnStringReturn_EmitsStringEmpty()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "class C { string M() { return \"x\"; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        mutations.Should().HaveCountGreaterThanOrEqualTo(1);
        mutations.Should().Contain(m => m.ReplacementNode.ToString().Contains("string.Empty"));
    }

    [Fact]
    public void ApplyMutations_OnBoolReturn_EmitsTrueAndFalse()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "class C { bool M() { return false; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertMutationCount(mutations, 2);
        mutations.Should().Contain(m => m.ReplacementNode.ToString().Contains("true"));
        mutations.Should().Contain(m => m.ReplacementNode.ToString().Contains("false"));
    }

    [Theory]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("double")]
    [InlineData("float")]
    [InlineData("decimal")]
    [InlineData("byte")]
    public void ApplyMutations_OnNumericReturn_EmitsZero(string typeName)
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            $"class C {{ {typeName} M() {{ return 42; }} }}");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertSingleMutation(mutations).ReplacementNode.ToString().Should().Contain("0");
    }

    [Fact]
    public void ApplyMutations_OnTaskReturn_EmitsTaskFromResult()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "using System.Threading.Tasks; class C { Task<int> M() { return Task.FromResult(1); } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertSingleMutation(mutations).ReplacementNode.ToString().Should().Contain("Task.FromResult(default(int))");
    }

    [Fact]
    public void ApplyMutations_OnIEnumerableReturn_EmitsEnumerableEmpty()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "using System.Collections.Generic; using System.Linq; class C { IEnumerable<int> M() { return new[] { 1 }; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertSingleMutation(mutations).ReplacementNode.ToString().Should().Contain("Enumerable.Empty<int>()");
    }

    [Fact]
    public void ApplyMutations_OnListReturn_EmitsNewList()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "using System.Collections.Generic; class C { List<int> M() { return null; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertSingleMutation(mutations).ReplacementNode.ToString().Should().Contain("new System.Collections.Generic.List<int>()");
    }

    [Fact]
    public void ApplyMutations_OnReturnWithoutExpression_ReturnsNoMutation()
    {
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "class C { void M() { return; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertNoMutations(mutations);
    }

    [Fact]
    public void ApplyMutations_OnUnsupportedType_ReturnsNoMutation()
    {
        // A custom class type isn't in the substitution table.
        var (model, returnNode) = BuildSemanticContext<ReturnStatementSyntax>(
            "class Foo { } class C { Foo M() { return null; } }");
        var mutations = ApplyTypeAwareMutations<TypeDrivenReturnMutator, ReturnStatementSyntax>(
            new(), returnNode, model);
        AssertNoMutations(mutations);
    }
}
