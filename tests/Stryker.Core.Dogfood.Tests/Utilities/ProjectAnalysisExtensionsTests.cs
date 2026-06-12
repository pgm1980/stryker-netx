using System.Collections.Generic;
using FluentAssertions;
using Stryker.TestHelpers;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Utilities;

/// <summary>
/// Sprint 182 (360°-Analyse H-19): MSBuild-Properties sind User-Input. <c>bool.Parse</c>
/// warf eine FormatException auf gebräuchlichen Werten wie "yes", <c>int.Parse</c> auf
/// nicht-numerischen WarningLevels — beide rissen den Lauf. Unparsbare Werte fallen jetzt
/// auf den Default zurück.
/// </summary>
public class ProjectAnalysisExtensionsTests : TestBase
{
    [Theory]
    [InlineData("yes")]
    [InlineData("Enabled")]
    public void GetPropertyOrDefault_OnNonBooleanValue_FallsBackToDefault(string value)
    {
        var analysis = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                ["IsPackable"] = value,
            }).Object;

        analysis.GetPropertyOrDefault("IsPackable", defaultBoolean: false).Should().BeFalse(
            "an unparsable boolean property must fall back instead of crashing the run");
    }

    [Fact]
    public void GetPropertyOrDefault_OnBooleanValue_ParsesIt()
    {
        var analysis = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                ["IsPackable"] = "true",
            }).Object;

        analysis.GetPropertyOrDefault("IsPackable", defaultBoolean: false).Should().BeTrue();
    }

    [Fact]
    public void GetWarningLevel_OnGarbageValue_FallsBackToFour()
    {
        var analysis = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                ["WarningLevel"] = "high",
            }).Object;

        analysis.GetWarningLevel().Should().Be(4,
            "an unparsable warning level must fall back instead of crashing the run");
    }

    [Fact]
    public void GetWarningLevel_OnNumericValue_ParsesIt()
    {
        var analysis = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                ["WarningLevel"] = "7",
            }).Object;

        analysis.GetWarningLevel().Should().Be(7);
    }

    // Sprint 183 (issue #291, 360-Grad-Analyse H-18): Multi-TFM-Projekte tragen in der
    // aeusseren Evaluation nur die TargetFrameworks-LISTE. Die Rohliste lief in den
    // Framework-Parser und endete als irrefuehrende InputException — der erste Eintrag
    // ist das, was die Roslyn-Workspace ohnehin laedt.
    [Theory]
    [InlineData("netstandard2.0;net10.0", "netstandard2.0")]
    [InlineData("net10.0", "net10.0")]
    [InlineData(" net10.0 ; net8.0 ", "net10.0")]
    public void FirstTargetFrameworkFrom_PicksTheFirstListEntry(string list, string expected)
        => RoslynProjectAnalysis.FirstTargetFrameworkFrom(list).Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FirstTargetFrameworkFrom_PassesBlankInputThrough(string? input)
        => RoslynProjectAnalysis.FirstTargetFrameworkFrom(input).Should().Be(input);

    // Sprint 183 (issue #290, 360-Grad-Analyse H-17): die DI baut den Workspace-Provider
    // BEVOR die Optionen existieren — Konfigurations-Pins erreichten die Roslyn-Sicht nie.
    // ForProperties liefert einen Provider, dessen Workspace die Pins traegt; ohne Pins
    // bleibt es dieselbe Instanz.
    [Fact]
    public void ForProperties_WithPins_CreatesConfiguredWorkspace()
    {
        using var bare = new MSBuildWorkspaceProvider();
        var pins = new Dictionary<string, string>(System.StringComparer.Ordinal)
        {
            ["Configuration"] = "Release",
        };

        using var configured = bare.ForProperties(pins);

        configured.Should().NotBeSameAs(bare);
        configured.Properties.Should().Contain("Configuration", "Release");
    }

    [Fact]
    public void ForProperties_WithoutPins_ReturnsSameInstance()
    {
        using var bare = new MSBuildWorkspaceProvider();

        bare.ForProperties(null).Should().BeSameAs(bare);
        bare.ForProperties(new Dictionary<string, string>(System.StringComparer.Ordinal)).Should().BeSameAs(bare);
    }
}
