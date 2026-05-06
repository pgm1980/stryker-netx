using System.IO;
using FluentAssertions;
using Stryker.Abstractions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Configuration.Options.Inputs;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options;

/// <summary>Sprint 88 (v2.74.0) port. MSTest → xUnit, Shouldly → FluentAssertions. Block C start.
/// Inherits TestBase: ConcurrencyInput.Validate uses ApplicationLogging.LoggerFactory.</summary>
public class StrykerInputsTests : TestBase
{
    private readonly StrykerInputs _target = BuildInputs();

    private static StrykerInputs BuildInputs() => new()
    {
        AdditionalTimeoutInput = new AdditionalTimeoutInput(),
        AzureFileStorageSasInput = new AzureFileStorageSasInput(),
        AzureFileStorageUrlInput = new AzureFileStorageUrlInput(),
        BaselineProviderInput = new BaselineProviderInput(),
        BasePathInput = new BasePathInput { SuppliedInput = Directory.GetCurrentDirectory() },
        ConcurrencyInput = new ConcurrencyInput(),
        DashboardApiKeyInput = new DashboardApiKeyInput(),
        DashboardUrlInput = new DashboardUrlInput(),
        DiagModeInput = new DiagModeInput(),
        DiffIgnoreChangesInput = new DiffIgnoreChangesInput(),
        DisableBailInput = new DisableBailInput(),
        DisableMixMutantsInput = new DisableMixMutantsInput(),
        IgnoreMutationsInput = new IgnoreMutationsInput(),
        FallbackVersionInput = new FallbackVersionInput(),
        IgnoredMethodsInput = new IgnoreMethodsInput(),
        LanguageVersionInput = new LanguageVersionInput(),
        VerbosityInput = new VerbosityInput(),
        LogToFileInput = new LogToFileInput(),
        ModuleNameInput = new ModuleNameInput(),
        MutateInput = new MutateInput(),
        MutationLevelInput = new MutationLevelInput(),
        CoverageAnalysisInput = new CoverageAnalysisInput(),
        OutputPathInput = new OutputPathInput { SuppliedInput = Directory.GetCurrentDirectory() },
        ProjectNameInput = new ProjectNameInput(),
        SourceProjectNameInput = new SourceProjectNameInput(),
        ProjectVersionInput = new ProjectVersionInput(),
        ReportersInput = new ReportersInput(),
        SinceInput = new SinceInput(),
        SinceTargetInput = new SinceTargetInput(),
        SolutionInput = new SolutionInput(),
        TestProjectsInput = new TestProjectsInput(),
        ThresholdBreakInput = new ThresholdBreakInput(),
        ThresholdHighInput = new ThresholdHighInput(),
        ThresholdLowInput = new ThresholdLowInput(),
        WithBaselineInput = new WithBaselineInput(),
        BreakOnInitialTestFailureInput = new BreakOnInitialTestFailureInput(),
    };

    [Fact]
    public void PerTestInIsolationShouldSetOptimizationFlags()
    {
        _target.CoverageAnalysisInput.SuppliedInput = "perTestInIsolation";
        var result = _target.ValidateAll();

        result.OptimizationMode.HasFlag(OptimizationModes.CoverageBasedTest).Should().BeTrue();
        result.OptimizationMode.HasFlag(OptimizationModes.CaptureCoveragePerTest).Should().BeTrue();
    }

    [Fact]
    public void ShouldSetConfiguration()
    {
        _target.ConfigurationInput.SuppliedInput = "TheRelease";
        var result = _target.ValidateAll();
        result.Configuration.Should().Be("TheRelease");
    }

    [Fact]
    public void ShouldSetConfigurationAndPlatform()
    {
        _target.ConfigurationInput.SuppliedInput = "TheRelease|x64";
        var result = _target.ValidateAll();
        result.Configuration.Should().Be("TheRelease");
        result.Platform.Should().Be("x64");
    }

    [Fact]
    public void ShouldIgnoreExtraInfoInConfiguration()
    {
        _target.ConfigurationInput.SuppliedInput = "TheRelease|x64|Disregarded";
        var result = _target.ValidateAll();
        result.Configuration.Should().Be("TheRelease");
        result.Platform.Should().Be("x64");
    }

    [Fact]
    public void DisableBailShouldSetOptimizationFlags()
    {
        _target.DisableMixMutantsInput.SuppliedInput = true;
        var result = _target.ValidateAll();

        result.OptimizationMode.HasFlag(OptimizationModes.DisableMixMutants).Should().BeTrue();
        result.OptimizationMode.HasFlag(OptimizationModes.CoverageBasedTest).Should().BeTrue();
    }

    [Fact]
    public void DisableMixMutantsShouldSetOptimizationFlags()
    {
        _target.DisableBailInput.SuppliedInput = true;
        var result = _target.ValidateAll();

        result.OptimizationMode.HasFlag(OptimizationModes.DisableBail).Should().BeTrue();
        result.OptimizationMode.HasFlag(OptimizationModes.CoverageBasedTest).Should().BeTrue();
    }

    [Fact]
    public void AllShouldSetOptimizationFlags()
    {
        _target.CoverageAnalysisInput.SuppliedInput = "all";
        var result = _target.ValidateAll();
        result.OptimizationMode.HasFlag(OptimizationModes.SkipUncoveredMutants).Should().BeTrue();
    }

    [Fact]
    public void OffShouldSetOptimizationFlags()
    {
        _target.CoverageAnalysisInput.SuppliedInput = "off";
        var result = _target.ValidateAll();
        result.OptimizationMode.HasFlag(OptimizationModes.None).Should().BeTrue();
    }

    [Fact]
    public void OptimizationFlagsShouldHaveDefaultCoverageBasedTest()
    {
        _target.CoverageAnalysisInput.SuppliedInput = null!;
        var result = _target.ValidateAll();
        result.OptimizationMode.HasFlag(OptimizationModes.CoverageBasedTest).Should().BeTrue();
    }

    [Fact]
    public void UsingDashboardReporterShouldEnableDashboardApiKey()
    {
        _target.DashboardApiKeyInput.SuppliedInput = "dashboard_api_key";
        _target.ReportersInput.SuppliedInput = ["dashboard"];
        var result = _target.ValidateAll();
        result.DashboardApiKey.Should().Be("dashboard_api_key");
    }

    [Fact]
    public void UsingDashboardBaselineStorageWithBaselineShouldEnableDashboardApiKey()
    {
        _target.DashboardApiKeyInput.SuppliedInput = "dashboard_api_key";
        _target.ReportersInput.SuppliedInput = ["html"];
        _target.BaselineProviderInput.SuppliedInput = "dashboard";
        _target.WithBaselineInput.SuppliedInput = true;
        _target.ProjectVersionInput.SuppliedInput = "develop";
        var result = _target.ValidateAll();
        result.DashboardApiKey.Should().Be("dashboard_api_key");
    }

    [Fact]
    public void NotUsingDashboardBaselineStorageWithBaselineOrDashboardReporterShouldDisableDashboardApiKey()
    {
        _target.DashboardApiKeyInput.SuppliedInput = "dashboard_api_key";
        _target.ReportersInput.SuppliedInput = ["html"];
        _target.BaselineProviderInput.SuppliedInput = "disk";
        _target.WithBaselineInput.SuppliedInput = true;
        _target.ProjectVersionInput.SuppliedInput = "develop";
        var result = _target.ValidateAll();
        result.DashboardApiKey.Should().BeNull();
    }

    [Fact]
    public void WithBaselineAndSinceShouldBeMutuallyExclusive()
    {
        _target.WithBaselineInput.SuppliedInput = true;
        _target.SinceInput.SuppliedInput = true;
        var act = () => _target.ValidateAll();
        act.Should().Throw<InputException>().WithMessage("The since and baseline features are mutually exclusive.");
    }

    [Fact]
    public void WithBaselineShouldNotThrow_2743()
    {
        _target.ProjectVersionInput.SuppliedInput = "1";
        _target.WithBaselineInput.SuppliedInput = true;
        var act = () => _target.ValidateAll();
        act.Should().NotThrow();
    }

    [Fact]
    public void BaseLineOptionsShouldBeSetToDefaultWhenBaselineIsDisabled()
    {
        _target.WithBaselineInput.SuppliedInput = false;
        _target.BaselineProviderInput.SuppliedInput = "azurefilestorage";
        _target.AzureFileStorageSasInput.SuppliedInput = "sasCredential";
        _target.AzureFileStorageUrlInput.SuppliedInput = "azureUrl";
        var result = _target.ValidateAll();

        result.WithBaseline.Should().BeFalse();
        result.BaselineProvider.Should().Be(BaselineProvider.Disk);
        result.AzureFileStorageSas.Should().BeEmpty();
        result.AzureFileStorageUrl.Should().BeEmpty();
    }

    // ----- Sprint 140 (ADR-025) Mutation-Profile Auto-Bump Tests -----
    // Closes Bug #1 silent-no-op from the post-v3.0.24 Calculator-tester real-life
    // bug-report. ToT + Maxential converged on B-AutoBump (= D-lite). Test matrix:
    // 3 profiles × 3 level-settings = 9 cases. Implementation in
    // src/Stryker.Configuration/Options/StrykerInputs.cs::ResolveMutationLevel().

    [Fact]
    public void AutoBump_ProfileDefaults_LevelImplicit_KeepsStandard()
    {
        // Profile=Defaults + no explicit level → keep Standard (today's behaviour).
        // Auto-bump must NOT fire because profile is Defaults.
        _target.MutationProfileInput.SuppliedInput = null!;
        _target.MutationLevelInput.SuppliedInput = null!;
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Standard, "Defaults-profile must keep Standard-level (no auto-bump)");
        result.MutationProfile.Should().Be(MutationProfile.Defaults);
    }

    [Fact]
    public void AutoBump_ProfileStronger_LevelImplicit_BumpsToAdvanced()
    {
        // Profile=Stronger + no explicit level → Auto-bump to Advanced (ADR-025).
        // The core bug-fix scenario from the Calculator-tester real-life report.
        _target.MutationProfileInput.SuppliedInput = "Stronger";
        _target.MutationLevelInput.SuppliedInput = null!;
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Advanced, "Stronger-profile + implicit level must auto-bump to Advanced");
        result.MutationProfile.Should().Be(MutationProfile.Stronger);
    }

    [Fact]
    public void AutoBump_ProfileAll_LevelImplicit_BumpsToComplete()
    {
        // Profile=All + no explicit level → Auto-bump to Complete (ADR-025).
        _target.MutationProfileInput.SuppliedInput = "All";
        _target.MutationLevelInput.SuppliedInput = null!;
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Complete, "All-profile + implicit level must auto-bump to Complete");
        result.MutationProfile.Should().Be(MutationProfile.All);
    }

    [Fact]
    public void AutoBump_ProfileDefaults_LevelExplicit_RespectsUserChoice()
    {
        // Profile=Defaults + explicit level=Advanced → keep user's Advanced.
        _target.MutationProfileInput.SuppliedInput = "Defaults";
        _target.MutationLevelInput.SuppliedInput = "Advanced";
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Advanced, "explicit level must always win");
        result.MutationProfile.Should().Be(MutationProfile.Defaults);
    }

    [Fact]
    public void AutoBump_ProfileStronger_LevelExplicitStandard_RespectsUserChoice()
    {
        // Profile=Stronger + explicit level=Standard → DO NOT auto-bump (user chose Standard
        // explicitly, possibly to test the conjunctive-filter behaviour deliberately).
        _target.MutationProfileInput.SuppliedInput = "Stronger";
        _target.MutationLevelInput.SuppliedInput = "Standard";
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Standard, "explicit Standard must always win, even with Stronger profile");
        result.MutationProfile.Should().Be(MutationProfile.Stronger);
    }

    [Fact]
    public void AutoBump_ProfileStronger_LevelExplicitAdvanced_KeepsAdvanced()
    {
        // Profile=Stronger + explicit level=Advanced → keep Advanced (user explicit, no bump needed).
        _target.MutationProfileInput.SuppliedInput = "Stronger";
        _target.MutationLevelInput.SuppliedInput = "Advanced";
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Advanced);
        result.MutationProfile.Should().Be(MutationProfile.Stronger);
    }

    [Fact]
    public void AutoBump_ProfileStronger_LevelExplicitComplete_KeepsComplete()
    {
        // Profile=Stronger + explicit level=Complete → keep Complete (user wants more aggressive than profile-default).
        _target.MutationProfileInput.SuppliedInput = "Stronger";
        _target.MutationLevelInput.SuppliedInput = "Complete";
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Complete);
        result.MutationProfile.Should().Be(MutationProfile.Stronger);
    }

    [Fact]
    public void AutoBump_ProfileAll_LevelExplicitBasic_KeepsBasic()
    {
        // Profile=All + explicit level=Basic → keep Basic (user wants minimal mutations from All-pool).
        _target.MutationProfileInput.SuppliedInput = "All";
        _target.MutationLevelInput.SuppliedInput = "Basic";
        var result = _target.ValidateAll();
        result.MutationLevel.Should().Be(MutationLevel.Basic);
        result.MutationProfile.Should().Be(MutationProfile.All);
    }

    [Fact]
    public void AutoBump_ProfileNotSupplied_LevelImplicit_DefaultsBoth()
    {
        // No explicit profile, no explicit level → both at default (Defaults + Standard).
        // Verifies the auto-bump does NOT silently override the natural defaults.
        _target.MutationProfileInput.SuppliedInput = null!;
        _target.MutationLevelInput.SuppliedInput = null!;
        var result = _target.ValidateAll();
        result.MutationProfile.Should().Be(MutationProfile.Defaults);
        result.MutationLevel.Should().Be(MutationLevel.Standard);
    }
}
