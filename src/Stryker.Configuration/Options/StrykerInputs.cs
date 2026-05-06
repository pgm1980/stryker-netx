using Microsoft.Extensions.Logging;
using System.IO.Abstractions;
using Stryker.Abstractions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Stryker.Utilities.Logging;

namespace Stryker.Configuration.Options;

public partial class StrykerInputs : IStrykerInputs
{
    private IStrykerOptions? _strykerOptionsCache;
    private readonly IFileSystem _fileSystem;

    public StrykerInputs(IFileSystem? fileSystem = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public DiagModeInput DiagModeInput { get; init; } = new();
    public BasePathInput BasePathInput { get; init; } = new();
    public OutputPathInput OutputPathInput { get; init; } = new();
    public ReportFileNameInput ReportFileNameInput { get; init; } = new();
    public SolutionInput SolutionInput { get; init; } = new();
    public ConfigurationInput ConfigurationInput { get; init; } = new();
    public TargetFrameworkInput TargetFrameworkInput { get; init; } = new();
    public VerbosityInput VerbosityInput { get; init; } = new();
    public LogToFileInput LogToFileInput { get; init; } = new();
    public MutationLevelInput MutationLevelInput { get; init; } = new();
    /// <summary>v2.0.0 (ADR-018): mutation profile selector (Defaults / Stronger / All).</summary>
    public MutationProfileInput MutationProfileInput { get; init; } = new();
    /// <summary>
    /// <b>Obsolete in v2.2.0 — deprecated per ADR-021.</b> Originally introduced in
    /// v2.0.0 (ADR-016) for the planned HotSwap engine selector. Kept as CLI shim
    /// so existing scripts/configs don't break; emits a deprecation warning when
    /// the user explicitly supplies <c>--engine</c>.
    /// </summary>
#pragma warning disable CS0618 // Reference to obsolete MutationEngineInput — this is the deprecated shim itself.
    public MutationEngineInput MutationEngineInput { get; init; } = new();
#pragma warning restore CS0618
    public ThresholdBreakInput ThresholdBreakInput { get; init; } = new();
    public ThresholdHighInput ThresholdHighInput { get; init; } = new();
    public ThresholdLowInput ThresholdLowInput { get; init; } = new();
    public AdditionalTimeoutInput AdditionalTimeoutInput { get; init; } = new();
    public LanguageVersionInput LanguageVersionInput { get; init; } = new();
    public ConcurrencyInput ConcurrencyInput { get; init; } = new();
    public SourceProjectNameInput SourceProjectNameInput { get; init; } = new();
    public TestProjectsInput TestProjectsInput { get; init; } = new();
    public TestCaseFilterInput TestCaseFilterInput { get; init; } = new();
    public WithBaselineInput WithBaselineInput { get; init; } = new();
    public ReportersInput ReportersInput { get; init; } = new();
    public BaselineProviderInput BaselineProviderInput { get; init; } = new();
    public AzureFileStorageUrlInput AzureFileStorageUrlInput { get; init; } = new();
    public AzureFileStorageSasInput AzureFileStorageSasInput { get; init; } = new();
    public S3BucketNameInput S3BucketNameInput { get; init; } = new();
    public S3EndpointInput S3EndpointInput { get; init; } = new();
    public S3RegionInput S3RegionInput { get; init; } = new();
    public DashboardUrlInput DashboardUrlInput { get; init; } = new();
    public DashboardApiKeyInput DashboardApiKeyInput { get; init; } = new();
    public ProjectNameInput ProjectNameInput { get; init; } = new();
    public SinceInput SinceInput { get; init; } = new();
    public SinceTargetInput SinceTargetInput { get; init; } = new();
    public DiffIgnoreChangesInput DiffIgnoreChangesInput { get; init; } = new();
    public FallbackVersionInput FallbackVersionInput { get; init; } = new();
    public ProjectVersionInput ProjectVersionInput { get; init; } = new();
    public ModuleNameInput ModuleNameInput { get; init; } = new();
    public MutateInput MutateInput { get; init; } = new();
    public IgnoreMethodsInput IgnoredMethodsInput { get; init; } = new();
    public IgnoreMutationsInput IgnoreMutationsInput { get; init; } = new();
    public CoverageAnalysisInput CoverageAnalysisInput { get; init; } = new();
    public DisableBailInput DisableBailInput { get; set; } = new();
    public DisableMixMutantsInput DisableMixMutantsInput { get; set; } = new();
    public MsBuildPathInput MsBuildPathInput { get; init; } = new();
    public OpenReportInput OpenReportInput { get; init; } = new();
    public OpenReportEnabledInput OpenReportEnabledInput { get; init; } = new();
    public BreakOnInitialTestFailureInput BreakOnInitialTestFailureInput { get; init; } = new();
    public TestRunnerInput TestRunnerInput { get; init; } = new();

    public IStrykerOptions ValidateAll()
    {
        if (_strykerOptionsCache is not null)
        {
            return _strykerOptionsCache;
        }

        var basePath = BasePathInput.Validate(_fileSystem);
        var outputPath = OutputPathInput.Validate(_fileSystem);
        var withBaseline = WithBaselineInput.Validate();
        var reporters = ReportersInput.Validate(withBaseline);
        var baselineProvider = BaselineProviderInput.Validate(reporters, withBaseline);
        var sinceEnabled = SinceInput.Validate(WithBaselineInput.SuppliedInput);
        var sinceTarget = SinceTargetInput.Validate(sinceEnabled);
        var projectVersion = ProjectVersionInput.Validate(reporters, withBaseline);

        _strykerOptionsCache = BuildStrykerOptions(
            basePath,
            outputPath,
            withBaseline,
            reporters,
            baselineProvider,
            sinceEnabled,
            sinceTarget,
            projectVersion);
        return _strykerOptionsCache;
    }

    private StrykerOptions BuildStrykerOptions(
        string? basePath,
        string? outputPath,
        bool withBaseline,
        System.Collections.Generic.IEnumerable<Reporter> reporters,
        BaselineProvider baselineProvider,
        bool sinceEnabled,
        string? sinceTarget,
        string? projectVersion)
    {
#pragma warning disable CS0618 // MutationEngine + MutationEngineInput are deprecated v2.2.0 (ADR-021); the assignment here is the shim that keeps --engine flag accepting input.
        return new StrykerOptions()
        {
            ProjectPath = basePath,
            OutputPath = outputPath,
            ReportFileName = ReportFileNameInput.Validate(),
            Concurrency = ConcurrencyInput.Validate(),
            MutationLevel = ResolveMutationLevel(MutationProfileInput, MutationLevelInput),
            MutationProfile = MutationProfileInput.Validate(),
            MutationEngine = MutationEngineInput.Validate(),
            DiagMode = DiagModeInput.Validate(),
            MsBuildPath = MsBuildPathInput.Validate(_fileSystem),
            SolutionPath = SolutionInput.Validate(basePath, _fileSystem),
            Configuration = ConfigurationInput.Validate(),
            TargetFramework = TargetFrameworkInput.Validate(),
            Thresholds = new Thresholds
            {
                High = ThresholdHighInput.Validate(ThresholdLowInput.SuppliedInput),
                Low = ThresholdLowInput.Validate(ThresholdBreakInput.SuppliedInput, ThresholdHighInput.SuppliedInput),
                Break = ThresholdBreakInput.Validate(ThresholdLowInput.SuppliedInput),
            },
            Reporters = reporters,
            LogOptions = new LogOptions
            {
                LogLevel = VerbosityInput.Validate(),
                LogToFile = LogToFileInput.Validate(outputPath)
            },
            SourceProjectName = SourceProjectNameInput.Validate(),
            AdditionalTimeout = AdditionalTimeoutInput.Validate(),
            ExcludedMutations = IgnoreMutationsInput.Validate<Mutator>(),
            ExcludedLinqExpressions = IgnoreMutationsInput.ValidateLinqExpressions(),
            IgnoredMethods = IgnoredMethodsInput.Validate(),
            Mutate = MutateInput.Validate(),
            LanguageVersion = LanguageVersionInput.Validate(),
            OptimizationMode = CoverageAnalysisInput.Validate() | DisableBailInput.Validate() | DisableMixMutantsInput.Validate(),
            TestProjects = TestProjectsInput.Validate(),
            TestCaseFilter = TestCaseFilterInput.Validate(),
            DashboardUrl = DashboardUrlInput.Validate(),
            DashboardApiKey = DashboardApiKeyInput.Validate(withBaseline, baselineProvider, reporters),
            ProjectName = ProjectNameInput.Validate(),
            ModuleName = ModuleNameInput.Validate(),
            ProjectVersion = ProjectVersionInput.Validate(reporters, withBaseline),
            DiffIgnoreChanges = DiffIgnoreChangesInput.Validate(),
            AzureFileStorageSas = AzureFileStorageSasInput.Validate(baselineProvider, withBaseline),
            AzureFileStorageUrl = AzureFileStorageUrlInput.Validate(baselineProvider, withBaseline),
            S3BucketName = S3BucketNameInput.Validate(baselineProvider, withBaseline),
            S3Endpoint = S3EndpointInput.Validate(baselineProvider, withBaseline),
            S3Region = S3RegionInput.Validate(baselineProvider, withBaseline),
            WithBaseline = withBaseline,
            BaselineProvider = baselineProvider,
            FallbackVersion = FallbackVersionInput.Validate(withBaseline, projectVersion, sinceTarget),
            Since = sinceEnabled,
            SinceTarget = sinceTarget,
            ReportTypeToOpen = OpenReportInput.Validate(OpenReportEnabledInput.Validate()),
            BreakOnInitialTestFailure = BreakOnInitialTestFailureInput.Validate(),
            TestRunner = TestRunnerInput.Validate(),
            MutantIdProvider = new BasicIdProvider()
        };
#pragma warning restore CS0618
    }

    /// <summary>
    /// v3.1.0 (Sprint 140, ADR-025): resolve <see cref="MutationLevel"/> with auto-bump
    /// based on <see cref="MutationProfile"/> when the user did not explicitly supply
    /// <c>--mutation-level</c>. Closes the silent-no-op caused by the conjunctive
    /// Profile × Level filter when only the profile is set.
    /// </summary>
    /// <remarks>
    /// Mapping (profile → bumped level) when level is not explicitly supplied:
    /// <list type="bullet">
    ///   <item><c>Stronger</c> → <see cref="MutationLevel.Advanced"/></item>
    ///   <item><c>All</c> → <see cref="MutationLevel.Complete"/></item>
    ///   <item><c>Defaults</c> / <c>None</c> → unchanged (validated default = Standard).</item>
    /// </list>
    /// When the user explicitly supplies <c>--mutation-level</c> (any value, including
    /// <c>Standard</c>), it always wins — auto-bump only kicks in for the implicit case.
    /// </remarks>
    private static MutationLevel ResolveMutationLevel(MutationProfileInput profileInput, MutationLevelInput levelInput)
    {
        var validatedLevel = levelInput.Validate();

        // Explicit user-set level always wins, no auto-bump.
        if (levelInput.SuppliedInput is not null)
        {
            return validatedLevel;
        }

        var profile = profileInput.Validate();
        var bumpedLevel = profile switch
        {
            MutationProfile.Stronger => MutationLevel.Advanced,
            MutationProfile.All => MutationLevel.Complete,
            _ => validatedLevel,
        };

        if (bumpedLevel != validatedLevel)
        {
            var logger = ApplicationLogging.LoggerFactory.CreateLogger<StrykerInputs>();
            LogAutoBumpedMutationLevel(logger, bumpedLevel, profile);
        }

        return bumpedLevel;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "mutation-level auto-set to {AutoBumpedLevel} based on mutation-profile={Profile} (no explicit --mutation-level supplied). Override with --mutation-level if needed. (ADR-025)")]
    private static partial void LogAutoBumpedMutationLevel(ILogger logger, MutationLevel autoBumpedLevel, MutationProfile profile);
}
