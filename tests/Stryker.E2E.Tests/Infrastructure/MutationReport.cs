using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Minimal projection of the mutation-testing-elements-schema-v1 emitted by
/// Stryker's JsonReporter at <c>StrykerOutput/&lt;timestamp&gt;/reports/mutation-report.json</c>.
/// Only the fields the E2E tests assert on are modelled — Sprint 21 keeps this
/// surface tight to avoid coupling tests to the full schema.
/// </summary>
public sealed class MutationReport
{
    [JsonPropertyName("schemaVersion")]
    public string? SchemaVersion { get; init; }

    [JsonPropertyName("thresholds")]
    public ReportThresholds? Thresholds { get; init; }

    [JsonPropertyName("files")]
    public IDictionary<string, FileReport> Files { get; init; } = new Dictionary<string, FileReport>(System.StringComparer.Ordinal);

    /// <summary>Sums every mutant across every file by status; used by tests to derive totals.</summary>
    public MutantTotals SummariseMutants()
    {
        int killed = 0, survived = 0, noCoverage = 0, timeout = 0, runtimeError = 0, compileError = 0, ignored = 0, pending = 0;
        foreach (var file in Files.Values)
        {
            foreach (var mutant in file.Mutants)
            {
                switch (mutant.Status)
                {
                    case "Killed": killed++; break;
                    case "Survived": survived++; break;
                    case "NoCoverage": noCoverage++; break;
                    case "Timeout": timeout++; break;
                    case "RuntimeError": runtimeError++; break;
                    case "CompileError": compileError++; break;
                    case "Ignored": ignored++; break;
                    case "Pending": pending++; break;
                }
            }
        }
        return new MutantTotals(killed, survived, noCoverage, timeout, runtimeError, compileError, ignored, pending);
    }
}
