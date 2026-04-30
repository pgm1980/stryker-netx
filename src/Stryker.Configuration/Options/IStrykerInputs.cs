using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;

namespace Stryker.Configuration.Options;

public interface IStrykerInputs
{
    AdditionalTimeoutInput AdditionalTimeoutInput { get; init; }
    AzureFileStorageSasInput AzureFileStorageSasInput { get; init; }
    S3BucketNameInput S3BucketNameInput { get; init; }
    S3EndpointInput S3EndpointInput { get; init; }
    S3RegionInput S3RegionInput { get; init; }
    AzureFileStorageUrlInput AzureFileStorageUrlInput { get; init; }
    BaselineProviderInput BaselineProviderInput { get; init; }
    BasePathInput BasePathInput { get; init; }
    ConcurrencyInput ConcurrencyInput { get; init; }
    ConfigurationInput ConfigurationInput { get; init; }
    CoverageAnalysisInput CoverageAnalysisInput { get; init; }
    DashboardApiKeyInput DashboardApiKeyInput { get; init; }
    DashboardUrlInput DashboardUrlInput { get; init; }
    DiagModeInput DiagModeInput { get; init; }
    DiffIgnoreChangesInput DiffIgnoreChangesInput { get; init; }
    DisableBailInput DisableBailInput { get; set; }
    DisableMixMutantsInput DisableMixMutantsInput { get; set; }
    IgnoreMutationsInput IgnoreMutationsInput { get; init; }
    FallbackVersionInput FallbackVersionInput { get; init; }
    IgnoreMethodsInput IgnoredMethodsInput { get; init; }
    LanguageVersionInput LanguageVersionInput { get; init; }
    LogToFileInput LogToFileInput { get; init; }
    ModuleNameInput ModuleNameInput { get; init; }
    MutateInput MutateInput { get; init; }
    MutationLevelInput MutationLevelInput { get; init; }
    MsBuildPathInput MsBuildPathInput { get; init; }
    OutputPathInput OutputPathInput { get; init; }
    ReportFileNameInput ReportFileNameInput { get; init; }
    ProjectNameInput ProjectNameInput { get; init; }
    SourceProjectNameInput SourceProjectNameInput { get; init; }
    ProjectVersionInput ProjectVersionInput { get; init; }
    ReportersInput ReportersInput { get; init; }
    SinceInput SinceInput { get; init; }
    SinceTargetInput SinceTargetInput { get; init; }
    SolutionInput SolutionInput { get; init; }
    TargetFrameworkInput TargetFrameworkInput { get; init; }
    TestProjectsInput TestProjectsInput { get; init; }
    TestCaseFilterInput TestCaseFilterInput { get; init; }
    ThresholdBreakInput ThresholdBreakInput { get; init; }
    ThresholdHighInput ThresholdHighInput { get; init; }
    ThresholdLowInput ThresholdLowInput { get; init; }
    VerbosityInput VerbosityInput { get; init; }
    WithBaselineInput WithBaselineInput { get; init; }
    OpenReportInput OpenReportInput { get; init; }
    OpenReportEnabledInput OpenReportEnabledInput { get; init; }
    BreakOnInitialTestFailureInput BreakOnInitialTestFailureInput { get; init; }
    TestRunnerInput TestRunnerInput { get; init; }

    IStrykerOptions ValidateAll();
}
