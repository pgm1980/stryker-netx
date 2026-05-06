using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class UoiMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<UoiMutator>(MutationProfile.All);

    [Fact]
    public void Type_IsUoiMutator()
        => typeof(UoiMutator).Should().NotBeNull();

    [Fact]
    public void DoesNotMutate_IdentifierInsideQualifiedName()
    {
        // Sprint 23 regression: a namespace declaration's qualified name has
        // two identifier children. UOI used to fire on those children; the
        // conditional placer then produced a parenthesised expression in the
        // name-syntax slot, which Roslyn's qualified-name visitor cannot accept.
        // Reproducer was the Complete level combined with the All profile.
        var tree = CSharpSyntaxTree.ParseText("namespace Sample.Library; class C {}");
        var qualifiedName = tree.GetRoot().DescendantNodes().OfType<QualifiedNameSyntax>().Single();
        var library = (IdentifierNameSyntax)qualifiedName.Right;
        var sample = (IdentifierNameSyntax)qualifiedName.Left;

        var mutator = new UoiMutator();
        ApplyMutations(mutator, library).Should().BeEmpty(
            "UOI must skip NameSyntax-typed slots (right-hand of QualifiedName)");
        ApplyMutations(mutator, sample).Should().BeEmpty(
            "UOI must skip NameSyntax-typed slots (left-hand of QualifiedName)");
    }

    [Fact]
    public void DoesNotMutate_IdentifierInsideAliasQualifiedName()
    {
        // global::Foo — `Foo` lives in an AliasQualifiedNameSyntax slot that
        // also requires NameSyntax. Same crash class as QualifiedName.
        var tree = CSharpSyntaxTree.ParseText("class C { global::System.String s; }");
        var aliasQualified = tree.GetRoot().DescendantNodes().OfType<AliasQualifiedNameSyntax>().FirstOrDefault();
        aliasQualified.Should().NotBeNull("test setup: source must contain an alias-qualified name");
        var nameUnderAlias = aliasQualified!.Name;
        if (nameUnderAlias is IdentifierNameSyntax id)
        {
            ApplyMutations(new UoiMutator(), id).Should().BeEmpty(
                "UOI must skip the right-hand of an AliasQualifiedName");
        }
    }

    // ----- Sprint 142 (Bug #9 from Calculator-tester report) regression tests -----
    // The .Name slot on MemberAccess and MemberBinding has the same crash class as
    // QualifiedName: Roslyn's typed visitor rejects ParenthesizedExpression in a
    // SimpleNameSyntax slot. Sprint 142 fix: UoiMutator.IsSafeToWrap() skips both.

    [Fact]
    public void DoesNotMutate_RightHandOfMemberAccess()
    {
        // Repro: `data.Length` — UoiMutator used to fire on `Length`, producing
        // ParenthesizedExpression in MemberAccess.Name slot → InvalidCastException
        // at OrchestrateChildrenMutation.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.ReadOnlySpan<int> data) => data.Length; }");
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberAccess.Name;

        ApplyMutations(new UoiMutator(), rhs).Should().BeEmpty(
            "UOI must skip MemberAccess.Name slots (right-hand) — producing parenthesized control there crashes Roslyn's typed visitor (Bug #9)");
    }

    [Fact]
    public void StillMutates_LocalIdentifierInExpression()
    {
        // Sanity-check that the new MemberAccess.Name skip did not over-block:
        // a plain local identifier in expression position must still receive
        // four UOI mutations (postfix and prefix increment plus decrement).
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(int x) => x; }");
        var localRef = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "x", System.StringComparison.Ordinal) && n.Parent is ArrowExpressionClauseSyntax);

        ApplyMutations(new UoiMutator(), localRef).Should().HaveCount(4,
            "UOI must still emit all 4 increment/decrement variants on plain local references");
    }

    [Fact]
    public void DoesNotMutate_RightHandOfMemberBinding()
    {
        // The conditional-access dual: `data?.Length` — `Length` lives on a
        // MemberBindingExpression.Name slot. Same crash class as MemberAccess.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.ReadOnlySpan<int>? data) => data?.Length ?? 0; }");
        var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberBinding.Name;

        ApplyMutations(new UoiMutator(), rhs).Should().BeEmpty(
            "UOI must skip MemberBinding.Name slots (right-hand of conditional access) — same crash class as MemberAccess (Bug #9)");
    }
}
