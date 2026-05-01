using System.IO;
using System.Linq;
using FluentAssertions;
using Stryker.E2E.Tests.Infrastructure;
using Xunit;

namespace Stryker.E2E.Tests;

/// <summary>
/// Sprint 21 — slow E2E tests against the Sample.slnx with the Defaults
/// profile. The two cached runs (json-only, json+html) are shared across
/// every <c>[Fact]</c> via <see cref="StrykerRunCacheFixture"/>.
///
/// v2.7.0 design note: profile-comparison tests (Defaults vs Stronger vs All)
/// are deferred — see <see cref="StrykerRunCacheFixture"/> for the rationale.
/// </summary>
[Collection(E2ETestCollection.Name)]
public class SampleE2EProfileTests
{
    private readonly StrykerRunCacheFixture _cache;

    public SampleE2EProfileTests(StrykerRunCacheFixture cache) => _cache = cache;

    [Fact]
    public void Defaults_ProducesExpectedTotalAndScore()
    {
        var run = _cache.GetDefaultsRunWithJsonReporter();
        var totals = run.Report!.SummariseMutants();
        totals.Total.Should().Be(5,
            "the Sample.Library has produced 5 mutants under the Defaults profile in every release of v1.x and v2.x");
        totals.Survived.Should().Be(0, "Sample.Tests must kill every mutant");
        totals.Killed.Should().Be(5);
        totals.MutationScorePercent.Should().Be(100.0);
    }

    [Fact]
    public void Defaults_ProducesParseableJsonReport()
    {
        var run = _cache.GetDefaultsRunWithJsonReporter();
        run.JsonReportPath.Should().NotBeNull("--reporter json must produce a report file");
        File.Exists(run.JsonReportPath!).Should().BeTrue();
        run.Report.Should().NotBeNull("the JSON report must deserialise into the MutationReport schema");
        run.Report!.SchemaVersion.Should().NotBeNullOrEmpty(
            "the report must declare a schemaVersion (mutation-testing-elements compatibility)");
    }

    [Fact]
    public void Defaults_JsonReportLandsUnderStrykerOutputReportsDir()
    {
        var run = _cache.GetDefaultsRunWithJsonReporter();
        run.StrykerOutputRunDir.Should().NotBeNull(
            "the run must produce a timestamped StrykerOutput sub-directory");
        var reportsDir = Path.Combine(run.StrykerOutputRunDir!, "reports");
        Directory.Exists(reportsDir).Should().BeTrue();
        run.JsonReportPath.Should().StartWith(reportsDir,
            "the JSON report must live under <run-dir>/reports/");
    }

    [Fact]
    public void Defaults_FileMapContainsCalculatorAndItsMutants()
    {
        // E2E spot-check: the per-file report block must include Calculator.cs
        // (the Sample.Library production code) with a non-empty mutants list.
        var run = _cache.GetDefaultsRunWithJsonReporter();
        run.Report!.Files.Keys.Should().Contain(k => k.EndsWith("Calculator.cs", System.StringComparison.OrdinalIgnoreCase),
            "Calculator.cs must appear in the file-level report");
        var calc = run.Report.Files.First(kvp =>
            kvp.Key.EndsWith("Calculator.cs", System.StringComparison.OrdinalIgnoreCase)).Value;
        calc.Mutants.Should().NotBeEmpty("Calculator.cs must contribute at least one mutant entry");
        calc.Language.Should().Be("cs", "the Stryker JsonReporter declares C# as 'cs'");
    }

    [Fact]
    public void Defaults_EveryReportedMutantHasKnownStatus()
    {
        // Defensive: every mutant in the per-file report must carry a status string
        // matching the documented mutation-testing-elements vocabulary. Catches
        // status-string regressions in JsonReporter.
        var run = _cache.GetDefaultsRunWithJsonReporter();
        var knownStatuses = new[] { "Killed", "Survived", "NoCoverage", "Timeout", "RuntimeError", "CompileError", "Ignored", "Pending" };
        foreach (var file in run.Report!.Files.Values)
        {
            foreach (var mutant in file.Mutants)
            {
                mutant.Status.Should().BeOneOf(knownStatuses,
                    $"mutant {mutant.Id} ({mutant.MutatorName}) reported unexpected status '{mutant.Status}'");
            }
        }
    }

    [Fact]
    public void DefaultsWithJsonAndHtml_BothReportsLand()
    {
        // Multi-reporter check: requesting --reporter json + --reporter html must
        // produce both a JSON and an HTML file under <run>/reports/. Catches the
        // class-of-bug we hit during Sprint-20-validation: misnamed reporter
        // (clear-text vs cleartext) led to silent reporter-set degradation.
        var run = _cache.GetDefaultsRunWithJsonAndHtmlReporters();
        run.JsonReportPath.Should().NotBeNull();
        run.StrykerOutputRunDir.Should().NotBeNull();
        var reportsDir = Path.Combine(run.StrykerOutputRunDir!, "reports");
        Directory.Exists(reportsDir).Should().BeTrue();
        var htmlReport = Directory.GetFiles(reportsDir, "*.html").FirstOrDefault();
        htmlReport.Should().NotBeNull(
            "--reporter html must produce a .html file in the reports directory");
    }
}
