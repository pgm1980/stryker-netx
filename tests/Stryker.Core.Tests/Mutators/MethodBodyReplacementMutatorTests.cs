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
    public void ApplyMutations_OnNonVoidMethodBody_EmitsReturnDefault()
    {
        var (model, body) = BuildSemanticContext<BlockSyntax>(
            "class C { int M() { return 42; } }");
        var mutations = ApplyTypeAwareMutations<MethodBodyReplacementMutator, BlockSyntax>(new(), body, model);
        mutations.Should().NotBeEmpty();
        mutations[0].ReplacementNode.ToString().Should().Contain("default");
    }

    [Fact]
    public void ApplyMutations_OnVoidMethodBody_EmitsEmptyBody()
    {
        var (model, body) = BuildSemanticContext<BlockSyntax>(
            "class C { void M() { System.Console.WriteLine(\"x\"); } }");
        var mutations = ApplyTypeAwareMutations<MethodBodyReplacementMutator, BlockSyntax>(new(), body, model);
        mutations.Should().NotBeEmpty();
    }

    [Fact]
    public void ApplyMutations_OnAsyncMethodBody_ReturnsNoMutation()
    {
        var (model, body) = BuildSemanticContext<BlockSyntax>(
            "using System.Threading.Tasks; class C { async Task M() { await Task.Delay(1); } }");
        AssertNoMutations(ApplyTypeAwareMutations<MethodBodyReplacementMutator, BlockSyntax>(new(), body, model));
    }

    [Fact]
    public void ApplyMutations_OnNestedBlock_ReturnsNoMutation()
    {
        // INJ-001 (ADR-061): only the direct method-body block is replaced, not arbitrary nested blocks,
        // so the operator keeps its one-mutation-per-method genre.
        var (model, body) = BuildSemanticContext<BlockSyntax>(
            "class C { void M() { if (true) { System.Console.WriteLine(\"x\"); } } }",
            b => b.Parent is IfStatementSyntax);
        AssertNoMutations(ApplyTypeAwareMutations<MethodBodyReplacementMutator, BlockSyntax>(new(), body, model));
    }

    [Fact]
    public void ApplyMutations_TargetsBodyBlock_NotTheMethodDeclaration()
    {
        // INJ-001 (ADR-061): OriginalNode is the body BLOCK (a valid inject frame), so the orchestrator can
        // inject it. The previous MethodDeclaration OriginalNode was a member-level ancestor that no inject
        // frame contained and was dropped to a CompileError soft-fail.
        var (model, body) = BuildSemanticContext<BlockSyntax>(
            "class C { int Echo(int x) { return x; } }");
        var mutation = AssertSingleMutation(
            ApplyTypeAwareMutations<MethodBodyReplacementMutator, BlockSyntax>(new(), body, model));
        mutation.OriginalNode.Should().BeOfType<BlockSyntax>();
    }
}
