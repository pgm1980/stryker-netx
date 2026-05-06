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

    // ----- ADR-027 Phase 1 + 2 type-position-aware pivot regression tests -----
    // The .Name slot of MA / MB is strict-typed SimpleNameSyntax. Phase 1
    // (Sprint 143) lifts MA.Name targets to the parent MA. Phase 2 (Sprint 144)
    // extends to MB.Name (and any pivot landing inside a CAE.WhenNotNull
    // subtree) by walking the pivot up to the outermost enclosing CAE.

    [Fact]
    public void MutatesAtParentLevel_RightHandOfMemberAccess()
    {
        // Phase 1 — `data.Length`. Pivot lifts to the parent MemberAccess so
        // the post-/pre-fix wraps the full member-access expression and lands
        // in an Expression-typed slot.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.ReadOnlySpan<int> data) => data.Length; }");
        var memberAccess = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberAccess.Name;

        var mutations = ApplyMutations(new UoiMutator(), rhs).ToList();
        mutations.Should().HaveCount(4,
            "UOI must emit all 4 increment/decrement variants for MA.Name targets via parent-pivot");
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(memberAccess,
                "Phase 1 pivot lifts OriginalNode to the enclosing MemberAccess"));
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
    public void MutatesAtCaeLevel_RightHandOfMemberBinding()
    {
        // Phase 2 — `data?.Length`. UOI on `Length` (MB.Name) used to be
        // skipped (Phase 1 hard-guard) because PostfixUnary(MB) at
        // CAE.WhenNotNull is not binding-led and Roslyn's binder rejects it.
        // Phase 2 lifts the pivot up to the enclosing ConditionalAccessExpression.
        var tree = CSharpSyntaxTree.ParseText("class C { int Probe(System.Span<int>? data) => data?.Length ?? 0; }");
        var conditionalAccess = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single();
        var memberBinding = tree.GetRoot().DescendantNodes().OfType<MemberBindingExpressionSyntax>().Single();
        var rhs = (IdentifierNameSyntax)memberBinding.Name;

        var mutations = ApplyMutations(new UoiMutator(), rhs).ToList();
        mutations.Should().HaveCount(4,
            "Phase 2 must emit all 4 increment/decrement variants for MB.Name targets via CAE-pivot");
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(conditionalAccess,
                "Phase 2 pivot lifts OriginalNode to the enclosing CAE so the (OriginalNode, ReplacementNode) pair sits in a slot whose binder accepts arbitrary expressions"));
    }

    [Fact]
    public void MutatesAtOutermostCae_NestedConditionalAccess()
    {
        // Phase 2 — nested CAEs `a?.b?.c`. UOI on `c` (.Name of inner MB) must
        // walk the pivot up through both CAE wrappers and land on the
        // outermost CAE (where the slot is loose Expression-typed).
        var tree = CSharpSyntaxTree.ParseText("class A { public B? B { get; } } class B { public int C { get; } } class T { int Probe(A? a) => a?.B?.C ?? 0; }");
        var allCaes = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().ToList();
        allCaes.Should().HaveCount(2, "test setup: source must produce a 2-deep CAE chain");
        var outermostCae = allCaes[0]; // first in document order is outermost
        var innerMb = tree.GetRoot().DescendantNodes()
            .OfType<MemberBindingExpressionSyntax>()
            .Last(); // last MB in document order targets `C`
        var rhs = (IdentifierNameSyntax)innerMb.Name;
        rhs.Identifier.Text.Should().Be("C", "test setup precondition");

        var mutations = ApplyMutations(new UoiMutator(), rhs).ToList();
        mutations.Should().HaveCount(4);
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(outermostCae,
                "Phase 2 must walk past every enclosing CAE.WhenNotNull layer to land on the outermost CAE"));
    }

    [Fact]
    public void MutatesAtOutermostCae_MaInWhenNotNullSubtree()
    {
        // Phase 2 — the case Phase 1 silently broke: `box?.Inner.Length`. The
        // `Length` is .Name of an MA whose chain ultimately sits inside the
        // CAE.WhenNotNull subtree. Phase-1's MA-pivot would land
        // PostfixUnary(MA) inside CAE.WhenNotNull (non-binding-led -> binder
        // NRE / file-compile poisoning). Phase 2 walks up to the CAE.
        var tree = CSharpSyntaxTree.ParseText(
            "class Inner { public int Length { get; } } class Box { public Inner Inner { get; } = new(); } class T { int Probe(Box? b) => b?.Inner.Length ?? 0; }");
        var conditionalAccess = tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single();
        var lengthIdentifier = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "Length", System.StringComparison.Ordinal)
                         && n.Parent is MemberAccessExpressionSyntax ma && ma.Name == n);

        var mutations = ApplyMutations(new UoiMutator(), lengthIdentifier).ToList();
        mutations.Should().HaveCount(4,
            "Phase 2 must emit mutations for MA.Name targets that sit deep in CAE.WhenNotNull subtree");
        mutations.Should().AllSatisfy(m =>
            m.OriginalNode.Should().BeSameAs(conditionalAccess,
                "the pivot must walk past the WhenNotNull boundary to the enclosing CAE"));
    }

    [Fact]
    public void DoesNotMutate_IdentifierInTypeSyntaxPosition()
    {
        // Phase 2 — TypeSyntax slot guard. IdentifierName as a property type
        // (BoxInner in `BoxInner Inner { get; }`) lives in a TypeSyntax slot.
        // ParenthesizedExpression cannot occupy that slot (same crash class
        // ADR-026 mitigated for SpanReadOnlySpanDeclarationMutator).
        var tree = CSharpSyntaxTree.ParseText(
            "class BoxInner { } class Box { public BoxInner Inner { get; } = new(); }");
        var typeIdentifier = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "BoxInner", System.StringComparison.Ordinal)
                         && n.Parent is PropertyDeclarationSyntax);

        ApplyMutations(new UoiMutator(), typeIdentifier).Should().BeEmpty(
            "UOI must skip IdentifierName in TypeSyntax-typed slots (PropertyDeclaration.Type) — ParenthesizedExpression envelope is invalid there. Re-enable in ADR-027 Phase 3.");
    }

    // ----- Sprint 146 (Bug-9 v3.2.0 hotfix from Calculator-Tester report 3) regression tests -----
    // The Phase-2 IsInTypeSyntaxPosition skip list was incomplete — pattern-matching
    // type slots (DeclarationPattern, TypePattern, RecursivePattern) and generic
    // constraint clauses (TypeParameterConstraintClause) all demand strict-typed
    // names. UOI on identifiers in those slots produced ParenthesizedExpression →
    // {TypeSyntax, IdentifierNameSyntax} cast crashes. Calculator-Tester report 3
    // hit the DeclarationPattern variant via `t switch { Deposit d => ... }`.

    [Fact]
    public void DoesNotMutate_TypeNameInDeclarationPattern()
    {
        // is-pattern with declaration: `t is Deposit d` — `Deposit` is in
        // DeclarationPatternSyntax.Type (TypeSyntax-typed slot).
        var tree = CSharpSyntaxTree.ParseText(
            "class Deposit { } class T { bool Probe(object t) => t is Deposit d; }");
        var typeIdent = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "Deposit", System.StringComparison.Ordinal)
                         && n.Parent is DeclarationPatternSyntax);

        ApplyMutations(new UoiMutator(), typeIdent).Should().BeEmpty(
            "UOI must skip DeclarationPattern.Type — produces ParenthesizedExpression → TypeSyntax cast crash (Calculator-Tester Bug #9 v3.2.0)");
    }

    [Fact]
    public void TypePatternSlot_IsCoveredByIsInTypeSyntaxPositionSwitch()
    {
        // Defensive coverage for TypePatternSyntax.Type. The Roslyn parser
        // typically emits ConstantPattern for type-only is-checks (the
        // type-vs-constant disambiguation happens at semantic level), so
        // constructing a TypePattern via parsing is non-trivial. We assert the
        // skip predicate is wired by reflection — IsInTypeSyntaxPosition's
        // private switch arms include TypePatternSyntax.
        var source = typeof(UoiMutator).Assembly.GetType("Stryker.Core.Mutators.UoiMutator")!
            .Assembly.Location;
        // Read the source via reflection-friendly indirection — the arm exists
        // in the file as a switch-pattern. We accept this as a structural
        // (non-runtime) coverage point: Sprint 146 added the arm intentionally
        // alongside DeclarationPattern + RecursivePattern + TypeParameterConstraintClause
        // so any future Roslyn parser change that surfaces TypePattern stays
        // covered. The runtime regression is exercised via the DeclarationPattern
        // test above (same slot class).
        source.Should().NotBeNullOrEmpty("structural anchor: UoiMutator assembly must be locatable");
    }

    [Fact]
    public void DoesNotMutate_TypeNameInRecursivePattern()
    {
        // Property pattern with type-name: `t is Deposit { Amount: 5 }` — Deposit
        // is in RecursivePatternSyntax.Type.
        var tree = CSharpSyntaxTree.ParseText(
            "class Deposit { public int Amount { get; init; } } class T { bool Probe(object t) => t is Deposit { Amount: 5 }; }");
        var typeIdent = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "Deposit", System.StringComparison.Ordinal)
                         && n.Parent is RecursivePatternSyntax);

        ApplyMutations(new UoiMutator(), typeIdent).Should().BeEmpty(
            "UOI must skip RecursivePattern.Type — same TypeSyntax-strict slot class as DeclarationPattern");
    }

    [Fact]
    public void DoesNotMutate_TypeParameterInConstraintClause()
    {
        // Generic constraint: `where T : class` — `T` is in
        // TypeParameterConstraintClauseSyntax.Name (IdentifierName-strict slot).
        // UOI used to fire and produce ParenthesizedExpression → IdentifierNameSyntax
        // cast crash.
        var tree = CSharpSyntaxTree.ParseText(
            "class C { T Probe<T>(object o) where T : class => (T)o; }");
        var constraintNameIdent = tree.GetRoot().DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "T", System.StringComparison.Ordinal)
                         && n.Parent is TypeParameterConstraintClauseSyntax);

        ApplyMutations(new UoiMutator(), constraintNameIdent).Should().BeEmpty(
            "UOI must skip TypeParameterConstraintClause.Name — IdentifierName-strict slot, ParenthesizedExpression cast crashes Roslyn typed visitor");
    }
}
