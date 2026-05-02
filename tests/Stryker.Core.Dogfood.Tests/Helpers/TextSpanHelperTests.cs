using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Stryker.Utilities.Helpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Helpers;

/// <summary>
/// Sprint 46 (v2.33.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Helpers/TextSpanHelperTests.cs.
/// Framework conversion: MSTest [DataTestMethod]/[DataRow] → xUnit [Theory]/[InlineData], Shouldly → FluentAssertions.
/// </summary>
public class TextSpanHelperTests
{
    [Theory]
    [InlineData(new int[0], new int[0])]
    [InlineData(new[] { 5, 5 }, new int[0])]
    [InlineData(new[] { 5, 10 }, new[] { 5, 10 })]
    [InlineData(new[] { 5, 10, 5, 10 }, new[] { 5, 10 })]
    [InlineData(new[] { 5, 10, 10, 15 }, new[] { 5, 15 })]
    [InlineData(new[] { 5, 10, 10, 15, 15, 20 }, new[] { 5, 20 })]
    [InlineData(new[] { 5, 15, 10, 20 }, new[] { 5, 20 })]
    [InlineData(new[] { 5, 10, 15, 25 }, new[] { 5, 10, 15, 25 })]
    [InlineData(new[] { 5, 10, 10, 15, 20, 25, 25, 30 }, new[] { 5, 15, 20, 30 })]
    public void Reduce_should_reduce_correctly(int[] inputSpans, int[] outputSpans)
    {
        var textSpans = ConvertToSpans(inputSpans);

        var result = textSpans.Reduce();

        result.SequenceEqual(ConvertToSpans(outputSpans)).Should().BeTrue();
    }

    [Theory]
    [InlineData(new int[0], new int[0], new int[0])]
    [InlineData(new[] { 5, 15 }, new[] { 5, 10 }, new[] { 10, 15 })]
    [InlineData(new[] { 5, 25 }, new[] { 5, 10, 10, 15 }, new[] { 15, 25 })]
    [InlineData(new[] { 5, 25 }, new[] { 5, 10, 15, 25 }, new[] { 10, 15 })]
    [InlineData(new[] { 5, 25 }, new[] { 5, 10, 15, 20 }, new[] { 10, 15, 20, 25 })]
    [InlineData(new[] { 5, 10 }, new[] { 5, 10 }, new int[0])]
    [InlineData(new[] { 5, 10 }, new[] { 0, 100 }, new int[0])]
    [InlineData(new[] { 5, 25, 50, 100 }, new[] { 20, 75 }, new[] { 5, 20, 75, 100 })]
    public void Remove_overlap_should_remove_overlap_correctly(int[] leftSpans, int[] rightSpans, int[] outputSpans)
    {
        var result = ConvertToSpans(leftSpans).RemoveOverlap(ConvertToSpans(rightSpans));

        result.SequenceEqual(ConvertToSpans(outputSpans)).Should().BeTrue();
    }

    private static IEnumerable<TextSpan> ConvertToSpans(int[] positions) =>
        Enumerable.Range(0, positions.Length)
            .GroupBy(i => Math.Floor(i / 2d))
            .Select(x => TextSpan.FromBounds(positions[x.First()], positions[x.Skip(1).First()]));
}
