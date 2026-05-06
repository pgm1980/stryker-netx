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

    // ----- Sprint 143 (ADR-027 Phase 1 — type-position-aware pivot) regression tests -----
    // The .Name slot on MemberAccess is strict-typed (SimpleNameSyntax). Sprint 142 mitigated
    // the resulting crash via a hard skip; Sprint 143 supersedes that with a parent-pivot:
    // the mutation now lands on the enclosing MA so the engine's ParenthesizedExpression
    // envelope ends up in an Expression-typed slot instead of a SimpleName-typed one.
    // The MB.Name twin (data?.Length) still requires the legacy skip — see Phase 2.

    [Fact]
    public void MutatesAtParentLevel_RightHandOfMemberAccess()
    {
        // Sprint 143 Phase 1 — pivot for member-access right-hand. UOI now emits 4
        // mutations whose OriginalNode is the parent MemberAccess and whose
        // ReplacementNode wraps the full member-access expression in postfix or
        // prefix increment / decrement.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.ReadOnlySpan<int> data) => data.Length; }");
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberAccess.Name;

        var mutations = ApplyMutations(new UoiMutator(), rhs).ToList();
        mutations.Should().HaveCount(4,
            "UOI must emit all 4 increment/decrement variants for MA.Name targets via parent-pivot");
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(memberAccess,
                "Sprint 143 pivot lifts OriginalNode to the enclosing MemberAccess so the (OriginalNode, ReplacementNode) pair is structurally valid for an Expression-typed slot"));
    }

    [Fact]
    public void StillMutates_LocalIdentifierInExpression()
    {
        // Sanity-check that the parent-pivot logic does not over-block plain local
        // identifiers in expression position — they must still receive four UOI
        // mutations rooted at the identifier itself (no pivot needed).
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(int x) => x; }");
        var localRef = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "x", System.StringComparison.Ordinal) && n.Parent is ArrowExpressionClauseSyntax);

        var mutations = ApplyMutations(new UoiMutator(), localRef).ToList();
        mutations.Should().HaveCount(4,
            "UOI must still emit all 4 increment/decrement variants on plain local references");
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(localRef,
                "no parent-pivot for plain identifier — OriginalNode stays at the IdentifierName itself"));
    }

    [Fact]
    public void DoesNotMutate_RightHandOfMemberBinding()
    {
        // Sprint 143 Phase 1 deferred: `data?.Length` — pivoting `Length` to its
        // MemberBinding parent would put the post-/pre-fix in CAE.WhenNotNull,
        // which the binder rejects (must be binding-led: `.` or `[`). The legacy
        // hard-skip remains in place until Phase 2 lifts the pivot to the
        // enclosing ConditionalAccessExpression.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.ReadOnlySpan<int>? data) => data?.Length ?? 0; }");
        var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberBinding.Name;

        ApplyMutations(new UoiMutator(), rhs).Should().BeEmpty(
            "UOI must still skip MemberBinding.Name slots — Phase 1 limits pivot to MA.Name only; MB pivot is Phase 2 (CAE-aware lifting)");
    }
}
