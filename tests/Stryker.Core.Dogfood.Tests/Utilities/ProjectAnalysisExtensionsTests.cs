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
}
