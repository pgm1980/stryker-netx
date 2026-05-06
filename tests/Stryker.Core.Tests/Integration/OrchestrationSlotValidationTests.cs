using System;
using FluentAssertions;
using Stryker.Abstractions;
using Xunit;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// Sprint 151 (ADR-032, Bug #9 systemic audit from Calculator-Tester Bug-Report 5):
/// regression tests for the OrchestrationHelpers.ReplaceChildrenValidated path that
/// was introduced to extend the Sprint-147 SyntaxSlotValidator coverage from injection
/// to the orchestration phase. The Calculator-Tester reproduced
/// <c>InvalidCastException: ParenthesizedExpressionSyntax → IdentifierNameSyntax</c>
/// on Calculator.Infrastructure with <c>--mutation-profile All</c>; the user's
/// repro patterns (parenthesised receiver, unary on parenthesised binary, etc.) are
/// exercised here through the full orchestrator pipeline.
///
/// <para>
/// Each test runs the full orchestrator with <see cref="MutationProfile.All"/> and
/// <see cref="MutationLevel.Complete"/> — the Calculator-Tester's exact configuration
/// — and asserts that:
/// <list type="number">
///   <item>The orchestration completes without an unhandled exception.</item>
///   <item>The resulting tree is well-formed (re-parses cleanly).</item>
/// </list>
/// Whether mutations are produced, dropped silently, or marked as CompileError is
/// secondary: the primary contract is "no crash escapes the orchestrator under any
/// input shape an end-user can produce".
/// </para>
/// </summary>
[Trait("Category", "Integration")]
public sealed class OrchestrationSlotValidationTests : IntegrationTestBase
{
    [Theory]
    // Sprint 147 patterns (Bug-Report 4 closed, kept as regression baseline):
    [InlineData("class C { int M(int a, int b) => -(a + b); }", "unary-minus on parenthesised binary")]
    [InlineData("class C { bool M(bool x) => !(x && true); }", "logical-not on parenthesised binary")]
    [InlineData("class C { int M(int x, int y, bool p) => p ? x : y; }", "ternary as return-value (no extra parens)")]
    [InlineData("class C { int M(int i) => (i + 1) * 2; }", "parenthesised-binary in arithmetic")]
    // Sprint 151 patterns (Bug-Report 5 — newly closed):
    [InlineData("class C { int M(C c) { return (c).GetHashCode(); } }", "parenthesised receiver in invocation (Bug-Report 5 hint)")]
    [InlineData("class C { int M(C c) => (c).GetHashCode(); }", "parenthesised receiver in expression-bodied member")]
    [InlineData("class C { void M(C c) { _ = (c)?.ToString(); } }", "parenthesised receiver in conditional access")]
    [InlineData("class C { int X; int M() { return (this).X; } }", "parenthesised this-receiver in field access")]
    public void Mutate_AllProfile_DoesNotCrashOnParenthesisedExpressionPatterns(string source, string scenario)
    {
        // The historical Bug #9 (v3.1.1 → v3.2.5) and its Bug-Report-5 sequel both
        // surface as a Roslyn typed-visitor InvalidCastException during orchestration.
        // The Sprint 151 fix routes child mutations through SyntaxSlotValidator, so the
        // orchestrator drops slot-incompatible mutations silently instead of crashing.
        Action act = () =>
        {
            var (_, mutatedTree) = RunOrchestratorOnSource(source, MutationProfile.All);
            // re-parsing the mutated tree must succeed: the ConditionalInstrumentationEngine
            // envelope and the validated child mutations together produce well-formed C#.
            mutatedTree.GetDiagnostics().Should().BeEmpty(
                $"the All-profile orchestrator on the {scenario} pattern must produce a well-formed mutated tree");
        };

        act.Should().NotThrow(
            $"Sprint 151 ADR-032 OrchestrationHelpers.ReplaceChildrenValidated must catch slot-incompatible " +
            $"child mutations and drop them silently — the {scenario} pattern was a Calculator-Tester repro.");
    }

    [Theory]
    // The exact Calculator-Tester Bug-Report 5 pattern hint — `(x).Method()`:
    [InlineData("class C { int M(string s) { return (s).Length; } }")]
    [InlineData("class C { int M(string s) => (s).Length; }")]
    public void Mutate_AllProfile_HandlesParenthesisedReceiverWithoutCrash(string source)
    {
        // Calculator-Tester Bug-Report 5 explicitly cited "(x).Method() mit Klammern um den Receiver"
        // as the surfacing pattern for the second-class cast crash
        // (ParenthesizedExpressionSyntax → IdentifierNameSyntax).
        Action act = () => RunOrchestratorOnSource(source, MutationProfile.All);
        act.Should().NotThrow("Bug-Report 5 closure: parenthesised receivers must not crash the All-profile orchestrator");
    }
}
