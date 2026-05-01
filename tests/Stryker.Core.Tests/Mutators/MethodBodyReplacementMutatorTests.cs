using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class MethodBodyReplacementMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<MethodBodyReplacementMutator>(MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnNonVoidMethod_EmitsReturnDefault()
    {
        var (model, method) = BuildSemanticContext<MethodDeclarationSyntax>(
            "class C { int M() { return 42; } }");
        var mutations = ApplyTypeAwareMutations<MethodBodyReplacementMutator, MethodDeclarationSyntax>(
            new(), method, model);
        mutations.Should().NotBeEmpty();
        mutations[0].ReplacementNode.ToString().Should().Contain("default");
    }

    [Fact]
    public void ApplyMutations_OnVoidMethod_EmitsEmptyBody()
    {
        var (model, method) = BuildSemanticContext<MethodDeclarationSyntax>(
            "class C { void M() { System.Console.WriteLine(\"x\"); } }");
        var mutations = ApplyTypeAwareMutations<MethodBodyReplacementMutator, MethodDeclarationSyntax>(
            new(), method, model);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnAsyncMethod_ReturnsNoMutation()
    {
        var (model, method) = BuildSemanticContext<MethodDeclarationSyntax>(
            "using System.Threading.Tasks; class C { async Task M() { await Task.Delay(1); } }");
        var mutations = ApplyTypeAwareMutations<MethodBodyReplacementMutator, MethodDeclarationSyntax>(
            new(), method, model);
        AssertNoMutations(mutations);
    }
}
