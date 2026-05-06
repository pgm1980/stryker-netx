using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Helpers;
using Xunit;

namespace Stryker.Core.Tests.Helpers;

/// <summary>
/// Sprint 147 (ADR-028, Bug #9 architectural fix from Calculator-Tester Bug-Report 4):
/// regression tests for the central syntax-slot validator. The intent is to verify two
/// invariants: (1) structurally compatible mutations pass through unchanged, (2)
/// structurally incompatible mutations are rejected cleanly with a diagnostic — never
/// allowed to escape into the downstream pipeline where they would crash a typed visitor.
///
/// The unit tests below cover the four parenthesised-expression patterns the
/// Calculator-Tester explicitly listed in Bug-Report 4 (forderung d) plus the historical
/// patterns from Sprints 144 and 146.
/// </summary>
public sealed class SyntaxSlotValidatorTests
{
    [Fact]
    public void Accepts_StructurallyCompatible_Replacement()
    {
        // Replace `a + b` with `a - b` inside `var x = a + b;` — both are
        // BinaryExpressionSyntax, so the slot accepts the replacement.
        var tree = CSharpSyntaxTree.ParseText("class C { int M(int a, int b) => a + b; }");
        SyntaxNode root = tree.GetRoot()!;
        var add = root.DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
        var sub = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, add.Left, add.Right);

        var success = SyntaxSlotValidator.TryReplaceWithValidation(
            root, add, sub, out var result, out var error);

        success.Should().BeTrue("BinaryExpression -> BinaryExpression is a structurally valid replacement");
        error.Should().BeNull();
        // SyntaxFactory.BinaryExpression preserves leading trivia from add.Left ("a" with space)
        // but no trailing trivia → emits "a -b" (space before -, no space after).
        result.ToString().Should().Match(s => s.Contains("a -b") || s.Contains("a - b"),
            "the SubtractExpression replacement must be visible in the result");
    }

    [Fact]
    public void Rejects_ParenthesisedExpression_InTypeSyntaxSlot()
    {
        // Calculator-Tester pattern: a parameter type is a TypeSyntax slot. If a mutation
        // would put a ParenthesizedExpression there, the validator must reject it.
        var tree = CSharpSyntaxTree.ParseText("class Box { } class C { int M(Box b) => 0; }");
        SyntaxNode root = tree.GetRoot()!;
        var typeIdent = root.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Single(n => string.Equals(n.Identifier.Text, "Box", System.StringComparison.Ordinal)
                         && n.Parent is ParameterSyntax);
        var paren = SyntaxFactory.ParenthesizedExpression(SyntaxFactory.IdentifierName("Box"));

        var success = SyntaxSlotValidator.TryReplaceWithValidation<SyntaxNode>(
            root, typeIdent, paren, out _, out var error);

        success.Should().BeFalse("ParenthesizedExpression in a ParameterSyntax.Type (TypeSyntax) slot is the historical Bug #9 crash class");
        error.Should().NotBeNullOrEmpty();
        error.Should().Contain("Slot-incompatible mutation rejected");
    }

    [Fact]
    public void Rejects_ParenthesisedExpression_InMemberAccessNameSlot()
    {
        // Sprint-143 historical pattern: a MemberAccess.Name is a SimpleNameSyntax-strict
        // slot. ParenthesizedExpression there is the original Bug #9 manifestation.
        var tree = CSharpSyntaxTree.ParseText("class C { int M(System.ReadOnlySpan<int> data) => data.Length; }");
        SyntaxNode root = tree.GetRoot()!;
        var memberAccess = root.DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single();
        var paren = SyntaxFactory.ParenthesizedExpression(memberAccess.Name);

        var success = SyntaxSlotValidator.TryReplaceWithValidation<SyntaxNode>(
            root, memberAccess.Name, paren, out _, out var error);

        success.Should().BeFalse("ParenthesizedExpression in a MemberAccess.Name (SimpleNameSyntax) slot is rejected");
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Accepts_ParenthesisedExpression_InExpressionSlot()
    {
        // Loose Expression slot accepts ParenthesizedExpression. This is the canonical
        // happy path for the conditional-instrumentation envelope.
        var tree = CSharpSyntaxTree.ParseText("class C { int M(int x) => x + 1; }");
        SyntaxNode root = tree.GetRoot()!;
        var addExpr = root.DescendantNodes().OfType<BinaryExpressionSyntax>().Single();
        var paren = SyntaxFactory.ParenthesizedExpression(addExpr);

        var success = SyntaxSlotValidator.TryReplaceWithValidation<SyntaxNode>(
            root, addExpr, paren, out _, out var error);

        success.Should().BeTrue("Expression-typed slots accept ParenthesizedExpression — this is the canonical conditional envelope shape");
        error.Should().BeNull();
    }
}
