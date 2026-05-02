using FluentAssertions;
using Stryker.Abstractions;
using Stryker.TestHelpers;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.TestHelpers;

/// <summary>
/// Sprint 61 (v2.47.0). Validation port: exercises the new
/// <see cref="ProjectAnalysisMockBuilder"/> through real production
/// <see cref="IProjectAnalysisExtensions"/> consumer code. Confirms the
/// builder produces mocks that satisfy the same heuristics
/// upstream-port Initialisation tests will rely on.
/// </summary>
public class ProjectAnalysisMockBuilderExtensionsIntegrationTests
{
    [Fact]
    public void IsValid_WithSucceededAnalysis_ReturnsTrue()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\src\\Foo.csproj")
            .Build();

        analysis.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithFailedAnalysisButSourceFilesAndReferences_ReturnsTrue()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .AsFailed()
            .WithSourceFiles("a.cs")
            .WithReferences("ref.dll")
            .Build();

        analysis.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithFailedAnalysisAndNoFallback_ReturnsFalse()
    {
        var analysis = new ProjectAnalysisMockBuilder().AsFailed().Build();

        analysis.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValidFor_RequiresMatchingTargetFramework()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetFramework("net10.0")
            .WithProjectFilePath("c:\\src\\Foo.csproj")
            .Build();

        analysis.IsValidFor("net10.0").Should().BeTrue();
        analysis.IsValidFor("net8.0").Should().BeFalse();
    }

    [Fact]
    public void GetLanguage_RoutesToLanguagePropertyAndReturnsCsharp()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProperty("Language", "C#")
            .Build();

        analysis.GetLanguage().Should().Be(Language.Csharp);
    }

    [Fact]
    public void GetLanguage_WithFsharpProperty_ReturnsFsharp()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProperty("Language", "F#")
            .Build();

        analysis.GetLanguage().Should().Be(Language.Fsharp);
    }

    [Fact]
    public void TargetsFullFramework_ForNetFramework48_ReturnsTrue()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetFramework("net48")
            .Build();

        analysis.TargetsFullFramework().Should().BeTrue();
    }

    [Fact]
    public void TargetsFullFramework_ForNet10_ReturnsFalse()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetFramework("net10.0")
            .Build();

        analysis.TargetsFullFramework().Should().BeFalse();
    }

    [Fact]
    public void GetReferenceAssemblyPath_WithOutputRefFilePath_PrefersIt()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\src\\Foo.csproj")
            .WithOutputRefFilePath("c:\\obj\\ref\\Foo.dll")
            .Build();

        analysis.GetReferenceAssemblyPath().Should().EndWith("Foo.dll");
        analysis.GetReferenceAssemblyPath().Should().Contain("ref");
    }

    [Fact]
    public void TargetPlatform_WithUnsetProperty_DefaultsToAnyCpu()
    {
        var analysis = new ProjectAnalysisMockBuilder().Build();

        analysis.TargetPlatform().Should().Be("AnyCPU");
    }

    [Fact]
    public void TargetPlatform_WithExplicitProperty_ReturnsValue()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProperty("TargetPlatform", "x64")
            .Build();

        analysis.TargetPlatform().Should().Be("x64");
    }

    [Fact]
    public void IsSignedAssembly_RoutesBooleanProperty()
    {
        var unsigned = new ProjectAnalysisMockBuilder().Build();
        var signed = new ProjectAnalysisMockBuilder().WithProperty("SignAssembly", "true").Build();

        unsigned.IsSignedAssembly().Should().BeFalse();
        signed.IsSignedAssembly().Should().BeTrue();
    }

    [Fact]
    public void GetWarningLevel_DefaultIs4()
    {
        var analysis = new ProjectAnalysisMockBuilder().Build();

        analysis.GetWarningLevel().Should().Be(4);
    }

    [Fact]
    public void GetWarningLevel_WithExplicitProperty_ReturnsParsedValue()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProperty("WarningLevel", "2")
            .Build();

        analysis.GetWarningLevel().Should().Be(2);
    }
}
