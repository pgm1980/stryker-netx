using System;
using FluentAssertions;
using Sample.Library;
using Xunit;

namespace Sample.Tests;

/// <summary>
/// Sprint 143 repro coverage. Drives the <c>data.Length</c> /
/// <c>data?.Length</c> shapes through Stryker's mutation pipeline so the
/// regression for Bug-9 (Sprint 142 hotfix superseded by ADR-027 Phase 1
/// type-position-aware pivot) stays covered when running with
/// <c>--mutation-profile All --mutation-level Complete</c>.
/// </summary>
public sealed class SpanTesterTests
{
    [Fact]
    public void FirstOrZero_NonEmpty_ReturnsFirst() =>
        SpanTester.FirstOrZero(new ReadOnlySpan<int>([42, 1, 2])).Should().Be(42);

    [Fact]
    public void FirstOrZero_Empty_ReturnsZero() =>
        SpanTester.FirstOrZero(ReadOnlySpan<int>.Empty).Should().Be(0);

    [Fact]
    public void ConditionalLengthOrZero_Null_ReturnsZero() =>
        SpanTester.ConditionalLengthOrZero(null).Should().Be(0);

    [Fact]
    public void ConditionalLengthOrZero_NonEmpty_ReturnsLength() =>
        SpanTester.ConditionalLengthOrZero([1, 2, 3]).Should().Be(3);
}
