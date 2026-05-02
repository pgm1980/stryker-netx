using System.IO;
using FluentAssertions;
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
}
