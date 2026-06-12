using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace Stryker.Benchmarks;

/// <summary>
/// Sprint 184 (issue #287, G-22): RegisterCoverage runs in the USER test process on every
/// IsActive hit during coverage capture. The former List.Contains paid O(covered mutants)
/// per hit — quadratic overall. This benchmark contrasts the two shapes the way the hot
/// loop sees them: N distinct mutants, each hit M times.
/// </summary>
[MemoryDiagnoser]
public class RegisterCoverageBenchmarks
{
    private const int HitsPerMutant = 20;

    [Params(1_000, 10_000)]
    public int CoveredMutants { get; set; }

    [Benchmark(Baseline = true)]
    public int ListContains()
    {
        var covered = new List<int>();
        var gate = new System.Threading.Lock();
        for (var hit = 0; hit < HitsPerMutant; hit++)
        {
            for (var id = 0; id < CoveredMutants; id++)
            {
                lock (gate)
                {
                    if (!covered.Contains(id))
                    {
                        covered.Add(id);
                    }
                }
            }
        }

        return covered.Count;
    }

    [Benchmark]
    public int HashSetAdd()
    {
        var covered = new HashSet<int>();
        var gate = new System.Threading.Lock();
        for (var hit = 0; hit < HitsPerMutant; hit++)
        {
            for (var id = 0; id < CoveredMutants; id++)
            {
                lock (gate)
                {
                    covered.Add(id);
                }
            }
        }

        return covered.Count;
    }
}
