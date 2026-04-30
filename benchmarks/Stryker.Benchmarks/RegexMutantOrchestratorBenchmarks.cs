using System.Linq;
using BenchmarkDotNet.Attributes;
using Stryker.RegexMutators;

namespace Stryker.Benchmarks;

/// <summary>
/// Hot-path 2: regex-mutation orchestration. Runs a regex through the full
/// pipeline of regex-mutators (anchor-removal, character-class negation, etc.).
/// Reflects per-string-literal cost during a full mutation-run.
/// </summary>
[MemoryDiagnoser]
public class RegexMutantOrchestratorBenchmarks
{
    [Params(
        @"^[a-z]+$",
        @"\d{3}-\d{4}",
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})$",
        @"(?:https?:\/\/)?(?:www\.)?[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}(?:\/[^\s]*)?")]
    public string Pattern { get; set; } = string.Empty;

    [Benchmark]
    public int Mutate()
    {
        var orchestrator = new RegexMutantOrchestrator(Pattern);
        return orchestrator.Mutate().Count();
    }
}
