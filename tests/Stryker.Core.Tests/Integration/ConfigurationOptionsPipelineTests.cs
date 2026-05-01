using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.Core.Tests.Integration;

[Trait("Category", "Integration")]
public class ConfigurationOptionsPipelineTests : IntegrationTestBase
{
    [Fact]
    public void BuildStrykerOptions_DefaultProfile_IsDefaults()
        => BuildStrykerOptions().MutationProfile.Should().Be(MutationProfile.Defaults);

    [Theory]
    [InlineData(MutationProfile.Defaults)]
    [InlineData(MutationProfile.Stronger)]
    [InlineData(MutationProfile.All)]
    public void BuildStrykerOptions_ProfileFlag_RoundtripsCorrectly(MutationProfile profile)
        => BuildStrykerOptions(profile).MutationProfile.Should().Be(profile);

    [Theory]
    [InlineData(MutationLevel.Basic)]
    [InlineData(MutationLevel.Standard)]
    [InlineData(MutationLevel.Advanced)]
    [InlineData(MutationLevel.Complete)]
    public void BuildStrykerOptions_MutationLevel_RoundtripsCorrectly(MutationLevel level)
        => BuildStrykerOptions(level: level).MutationLevel.Should().Be(level);

    [Fact]
    public void BuildStrykerOptions_LanguageVersion_DefaultsToLatest()
        => BuildStrykerOptions().LanguageVersion.Should().Be(LanguageVersion.Latest);

    [Fact]
    public void BuildStrykerOptions_Concurrency_DefaultsToOne()
        => BuildStrykerOptions().Concurrency.Should().Be(1);

    [Fact]
    public void StrykerOptions_DefaultMutationProfile_IsDefaults()
        => new StrykerOptions().MutationProfile.Should().Be(MutationProfile.Defaults);

    [Fact]
    public void StrykerOptions_DefaultMutationEngine_IsRecompile()
    {
#pragma warning disable CS0618 // ADR-021 deprecated shim
        new StrykerOptions().MutationEngine.Should().Be(MutationEngine.Recompile);
#pragma warning restore CS0618
    }

    [Fact]
    public void StrykerOptions_DefaultThresholds_AreSensible()
    {
        var thresholds = new StrykerOptions().Thresholds;
        thresholds.High.Should().Be(80);
        thresholds.Low.Should().Be(60);
        thresholds.Break.Should().Be(0);
    }

    [Fact]
    public void StrykerOptions_DefaultTestProjects_IsEmpty()
        => new StrykerOptions().TestProjects.Should().BeEmpty();

    [Fact]
    public void StrykerOptions_DefaultReporters_IsEmpty()
        => new StrykerOptions().Reporters.Should().BeEmpty();
}
