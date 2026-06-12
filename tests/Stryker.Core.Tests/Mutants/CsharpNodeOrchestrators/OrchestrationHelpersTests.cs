using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutants.CsharpNodeOrchestrators;
using Xunit;

namespace Stryker.Core.Tests.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Sprint 151 (ADR-032, Bug-Report 5 Bug #9 systemic audit): unit tests for
/// the per-child slot-validation orchestration helper that catches the
/// <c>ParenthesizedExpressionSyntax → IdentifierNameSyntax</c> Roslyn typed-visitor
/// crash class at orchestration time (where the Sprint-147 injection-time validator
/// was blind).
/// </summary>
public class OrchestrationHelpersTests
{
    [Fact]
    public void ReplaceChildrenValidated_NoMutations_ReturnsOriginalUnchanged()
    {
        // When the lambda returns the same instance for every child, ReplaceChildrenValidated
        // must short-circuit to the original parent — no allocation, no replacement, no logging.
        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        var add = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();

        var result = OrchestrationHelpers.ReplaceChildrenValidated(add, add.ChildNodes(),
            original => original);

        ReferenceEquals(result, add).Should().BeTrue("identity preservation when no child mutates");
    }

    [Fact]
    public void ReplaceChildrenValidated_CompatibleMutation_AppliesIt()
    {
        // Replacing `a + b` with `a - b` is a structurally compatible mutation. ReplaceChildrenValidated
        // must NOT drop it; the result must reflect the change.
        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        var add = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
        var newRight = SyntaxFactory.IdentifierName("zzz").WithTriviaFrom(add.Right);

        var result = OrchestrationHelpers.ReplaceChildrenValidated(add, add.ChildNodes(),
            original => ReferenceEquals(original, add.Right) ? newRight : original);

        result.ToString().Should().Contain("zzz",
            "compatible child mutations must propagate through ReplaceChildrenValidated");
    }

    // NOTE: a third unit-test case for "slot-incompatible mutation dropped silently" was
    // attempted but proved hard to construct at unit-test level — Roslyn's public APIs
    // (ReplaceNode, WithType, etc.) crash inside their own typed-visitor cascade BEFORE
    // ReplaceChildrenValidated can even receive the bogus replacement. The orchestration-
    // pipeline is the only realistic surface that produces such replacements organically
    // (via recursive context.Mutate calls), so the regression coverage for "slot-
    // incompatible mutation dropped silently" lives in OrchestrationSlotValidationTests
    // (integration layer) instead. See Sprint 151 ADR-032 for the rationale.
    // Sprint 181 (issue #286, G-15): a DIRECT lambda — without any Roslyn API in the
    // arrangement — CAN hand the helper a bogus replacement; that is exactly how the
    // recursive pipeline delivers them. Used below to pin the dropped-mutation callback.

    // Sprint 181 (issue #286, 360-Grad-Analyse G-15): verworfene Subtrees enthalten
    // bereits injizierte, registrierte Mutanten. Ohne Rueckmeldung enden die als Geister
    // (False Survivor/NoCoverage). Der Helper meldet jeden verworfenen mutierten Subtree.
    [Fact]
    public void ReplaceChildrenValidated_OnSlotIncompatibleMutation_ReportsDroppedSubtree()
    {
        // An ExpressionSyntax in a Block's typed statement list is the historical Bug-9
        // crash class the slot validator demonstrably catches (typed-list cast).
        var tree = CSharpSyntaxTree.ParseText("class C { int M() { return 1; } }");
        var block = tree.GetRoot().DescendantNodes().OfType<BlockSyntax>().Single();
        var bogus = SyntaxFactory.IdentifierName("y")
            .WithAdditionalAnnotations(new SyntaxAnnotation("MutationId", "42"));
        var dropped = new List<SyntaxNode>();

        var result = OrchestrationHelpers.ReplaceChildrenValidated(block, block.ChildNodes(),
            original => original is ReturnStatementSyntax ? bogus : original,
            subtree => dropped.Add(subtree));

        ReferenceEquals(result, block).Should().BeTrue(
            "the slot-incompatible mutation must be dropped, not applied");
        dropped.Should().ContainSingle("the dropped mutated subtree must reach the caller")
            .Which.GetAnnotations("MutationId").Should().ContainSingle(a => a.Data == "42");
    }

    [Fact]
    public void ReplaceChildrenValidated_OnCompatibleMutation_DoesNotReportDrops()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        var add = tree.GetRoot().DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
        var newRight = SyntaxFactory.IdentifierName("zzz").WithTriviaFrom(add.Right);
        var dropped = new List<SyntaxNode>();

        _ = OrchestrationHelpers.ReplaceChildrenValidated(add, add.ChildNodes(),
            original => ReferenceEquals(original, add.Right) ? newRight : original,
            subtree => dropped.Add(subtree));

        dropped.Should().BeEmpty("applied mutations are not drops");
    }
}
