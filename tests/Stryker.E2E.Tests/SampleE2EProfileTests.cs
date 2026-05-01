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
/// Sprint 22 (v2.9.0): three additional profile-comparison Facts at
/// <c>--mutation-level Advanced</c> exercise the newly-wired
/// <c>--mutation-profile</c> CLI flag. Each profile gets its own cached run.
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

    [Fact]
    public void All_TotalIsStrictlyGreaterThan_Defaults()
    {
        // Sprint 22: with --mutation-profile All --mutation-level Advanced the
        // additional Stronger/All-only operators (AOD, InlineConstants, …) fire on
        // Sample.Library and produce strictly more mutants than Defaults at the same
        // level. Concretely on the current Calculator code: 5 (Defaults) vs 13 (All).
        var defaults = _cache.GetDefaultsRunAtAdvancedLevel();
        var all = _cache.GetAllRunAtAdvancedLevel();
        var defaultsTotal = defaults.Report!.SummariseMutants().Total;
        var allTotal = all.Report!.SummariseMutants().Total;
        allTotal.Should().BeGreaterThan(defaultsTotal,
            "the All profile must enable Stronger-only and All-only operators that Defaults excludes");
    }

    [Fact]
    public void Stronger_TotalIsAtLeast_Defaults()
    {
        // Sprint 22: Stronger ⊇ Defaults by membership-flag construction —
        // every Defaults-only mutator is also a Stronger member, plus the PIT-Stronger
        // additions. The total must therefore be >= the Defaults total.
        var defaults = _cache.GetDefaultsRunAtAdvancedLevel();
        var stronger = _cache.GetStrongerRunAtAdvancedLevel();
        var defaultsTotal = defaults.Report!.SummariseMutants().Total;
        var strongerTotal = stronger.Report!.SummariseMutants().Total;
        strongerTotal.Should().BeGreaterThanOrEqualTo(defaultsTotal,
            "Stronger is a superset of Defaults by [MutationProfileMembership] flag construction");
    }

    [Fact]
    public void All_FileMap_ContainsEveryDefaultsFile()
    {
        // Sprint 22: every file Stryker reports under Defaults at Advanced level
        // must also appear under All at Advanced level. The All profile only adds
        // mutators — it never removes them — so the per-file report cannot lose entries.
        var defaults = _cache.GetDefaultsRunAtAdvancedLevel();
        var all = _cache.GetAllRunAtAdvancedLevel();
        var defaultsFiles = defaults.Report!.Files.Keys;
        var allFiles = all.Report!.Files.Keys;
        allFiles.Should().Contain(defaultsFiles,
            "the All profile must report on every file the Defaults profile reports on");
    }

    [Fact]
    public void All_AtCompleteLevel_DoesNotCrash()
    {
        // Sprint 23 regression: the Complete level combined with the All profile
        // used to crash with an unhandled InvalidCastException coming out of
        // Roslyn's qualified-name visitor. Root cause was the unary-operator
        // mutator firing on namespace identifiers and the conditional placer
        // wrapping the mutation in a parenthesised expression which then sat
        // in a name-syntax slot. Fixed by per-mutator parent-context skip and
        // a global no-mutate orchestrator on the qualified-name node kind.
        var run = _cache.GetAllRunAtCompleteLevel();
        run.ExitCode.Should().Be(0,
            "Complete + All must complete the run cleanly (no unhandled InvalidCastException)");
        run.Report.Should().NotBeNull("the JSON report must be produced");
        run.Report!.Files.Should().NotBeEmpty(
            "even with profile All + level Complete the file map must contain Calculator.cs");
        run.Report.SummariseMutants().Total.Should().BeGreaterThan(
            _cache.GetAllRunAtAdvancedLevel().Report!.SummariseMutants().Total,
            "Complete level admits Complete-only operators that Advanced excludes — total must grow");
    }
}
