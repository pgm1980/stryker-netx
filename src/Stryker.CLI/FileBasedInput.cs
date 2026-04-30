using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class FileBasedInput : IExtraData
{
    // See ProjectInfo: explicit parameterless ctor required for source-gen +
    // [JsonExtensionData] interaction.
    [JsonConstructor]
    public FileBasedInput() { }

    [JsonPropertyName("project-info")]
    public ProjectInfo? ProjectInfo { get; init; }

    [JsonPropertyName("concurrency")]
    public int? Concurrency { get; init; }

    [JsonPropertyName("mutation-level")]
    public string? MutationLevel { get; init; }

    [JsonPropertyName("language-version")]
    public string? LanguageVersion { get; init; }

    [JsonPropertyName("additional-timeout")]
    public int? AdditionalTimeout { get; init; }

    [JsonPropertyName("mutate")]
    public string[]? Mutate { get; init; }

    [JsonPropertyName("solution")]
    public string? Solution { get; init; }

    [JsonPropertyName("configuration")]
    public string? Configuration { get; init; }

    [JsonPropertyName("target-framework")]
    public string? TargetFramework { get; init; }

    [JsonPropertyName("project")]
    public string? Project { get; init; }

    [JsonPropertyName("coverage-analysis")]
    public string? CoverageAnalysis { get; init; }

    [JsonPropertyName("disable-bail")]
    public bool? DisableBail { get; init; }

    [JsonPropertyName("disable-mix-mutants")]
    public bool? DisableMixMutants { get; init; }

    [JsonPropertyName("thresholds")]
    public ThresholdsConfig? Thresholds { get; init; }

    [JsonPropertyName("verbosity")]
    public string? Verbosity { get; init; }

    [JsonPropertyName("reporters")]
    public string[]? Reporters { get; init; }

    [JsonPropertyName("since")]
    public Since? Since { get; init; }

    [JsonPropertyName("baseline")]
    public Baseline? Baseline { get; init; }

    [JsonPropertyName("dashboard-url")]
    public string? DashboardUrl { get; init; }

    [JsonPropertyName("test-projects")]
    public string[]? TestProjects { get; init; }

    [JsonPropertyName("test-case-filter")]
    public string? TestCaseFilter { get; init; }

    [JsonPropertyName("test-runner")]
    public string? TestRunner { get; init; }

    [JsonPropertyName("ignore-mutations")]
    public string[]? IgnoreMutations { get; init; }

    [JsonPropertyName("ignore-methods")]
    public string[]? IgnoreMethods { get; init; }

    [JsonPropertyName("report-file-name")]
    public string? ReportFileName { get; init; }

    [JsonPropertyName("break-on-initial-test-failure")]
    public bool? BreakOnInitialTestFailure { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; set; }
}
