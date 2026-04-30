using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Text;
using Stryker.Utilities.Helpers;

namespace Stryker.Benchmarks;

/// <summary>
/// Hot-path 3: <see cref="TextSpanHelper.Reduce"/> is invoked on every covered
/// source-file during coverage analysis to merge intersecting line-spans.
/// O(n^2) algorithm; scales with the number of covered text-spans.
/// </summary>
[MemoryDiagnoser]
public class TextSpanHelperBenchmarks
{
    private List<TextSpan> _spans = [];

    [Params(10, 100, 500)]
    public int SpanCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _spans = new List<TextSpan>(SpanCount);
        for (var i = 0; i < SpanCount; i++)
        {
            // Alternating overlapping and non-overlapping spans to exercise both branches.
            var start = i * 50;
            var length = i % 3 == 0 ? 80 : 20;
            _spans.Add(new TextSpan(start, length));
        }
    }

    [Benchmark]
    public IReadOnlyCollection<TextSpan> Reduce() => _spans.Reduce();
}
