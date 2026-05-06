using System;

namespace Sample.Library;

/// <summary>
/// Sprint 143 repro fixture (ADR-027 Phase 1).
/// Exercises the MemberAccess.Name and MemberBinding.Name slot path through the
/// mutation pipeline so we can confirm that <c>data.Length</c> /
/// <c>data?.Length</c> targets now produce well-formed UOI mutations
/// (e.g. <c>data.Length++</c>) instead of crashing the
/// ConditionalInstrumentationEngine with the Bug-9
/// <c>InvalidCastException(ParenthesizedExpression -&gt; SimpleNameSyntax)</c>.
///
/// Drives <c>--mutation-profile All</c> + <c>--mutation-level Complete</c>;
/// kept tiny on purpose — only the syntactic shapes matter.
/// </summary>
public static class SpanTester
{
    /// <summary>Member-access right-hand: <c>data.Length</c>.</summary>
    public static int FirstOrZero(ReadOnlySpan<int> data) => data.Length > 0 ? data[0] : 0;

    /// <summary>Conditional-access right-hand: <c>data?.Length</c>.</summary>
    public static int ConditionalLengthOrZero(int[]? data) => data?.Length ?? 0;
}
