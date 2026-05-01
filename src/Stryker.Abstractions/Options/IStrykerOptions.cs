using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.ProjectComponents;

namespace Stryker.Abstractions.Options;

public interface IStrykerOptions
{
    int AdditionalTimeout { get; init; }
    string? AzureFileStorageSas { get; init; }
    string? AzureFileStorageUrl { get; init; }
    string? S3BucketName { get; init; }
    string? S3Endpoint { get; init; }
    string? S3Region { get; init; }
    BaselineProvider BaselineProvider { get; init; }
    bool BreakOnInitialTestFailure { get; set; }
    int Concurrency { get; init; }
    string? Configuration { get; init; }
    string? DashboardApiKey { get; init; }
    string? DashboardUrl { get; init; }
    bool DiagMode { get; init; }
    IEnumerable<IExclusionPattern> DiffIgnoreChanges { get; init; }
    IEnumerable<LinqExpression> ExcludedLinqExpressions { get; init; }
    IEnumerable<Mutator> ExcludedMutations { get; init; }
    string? FallbackVersion { get; init; }
    IEnumerable<Regex> IgnoredMethods { get; init; }
    bool IsSolutionContext { get; }
    LanguageVersion LanguageVersion { get; init; }
    ILogOptions? LogOptions { get; init; }
    string? ModuleName { get; init; }
    string? MsBuildPath { get; init; }
    IEnumerable<IFilePattern> Mutate { get; init; }
    MutationLevel MutationLevel { get; init; }

    /// <summary>
    /// v2.0.0 (ADR-018): orthogonal mutation-profile axis. Defaults to
    /// <see cref="Stryker.Abstractions.MutationProfile.Defaults"/> if not set.
    /// </summary>
    MutationProfile MutationProfile { get; init; }

    /// <summary>
    /// <b>Obsolete in v2.2.0 — deprecated per ADR-021.</b> Originally introduced
    /// in v2.0.0 (ADR-016) as the mutation execution engine selector. The
    /// HotSwap engine was removed because the underlying ADR-016 was based on
    /// a wrong mental model of Stryker.NET's cost structure. This property
    /// remains as a deprecated shim for v2.x source compatibility.
    /// </summary>
    // S1133 + CS0618 deferred to v3.0 per ADR-021 — see MutationEngine enum and IMutationEngine interface.
#pragma warning disable CS0618, S1133
    [System.Obsolete("Deprecated in v2.2.0 (ADR-021): HotSwap engine was based on a wrong mental model. The property is a shim; v3.0 may remove it.")]
    MutationEngine MutationEngine { get; init; }
#pragma warning restore CS0618, S1133
    OptimizationModes OptimizationMode { get; init; }
    string? OutputPath { get; init; }
    string? Platform { get; }
    string? ProjectName { get; set; }
    string? ProjectPath { get; init; }
    string? ProjectVersion { get; set; }
    IEnumerable<Reporter> Reporters { get; init; }
    string? ReportFileName { get; init; }
    string ReportPath { get; }
    ReportType? ReportTypeToOpen { get; init; }
    bool Since { get; init; }
    string? SinceTarget { get; init; }
    string? SolutionPath { get; init; }
    string? SourceProjectName { get; init; }
    string? TargetFramework { get; init; }
    string? TestCaseFilter { get; init; }
    IEnumerable<string> TestProjects { get; init; }
    TestRunner TestRunner { get; init; }
    IThresholds Thresholds { get; init; }
    bool WithBaseline { get; init; }
    string? WorkingDirectory { get; init; }
    IProvideId? MutantIdProvider { get; set; }
}
