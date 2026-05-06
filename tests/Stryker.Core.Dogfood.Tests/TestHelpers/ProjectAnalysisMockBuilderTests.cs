using System.IO;
using FluentAssertions;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.TestHelpers;

/// <summary>
/// Sprint 61 (v2.47.0). Validation tests for <see cref="ProjectAnalysisMockBuilder"/>.
/// Covers default values, single .With*() configuration, derivations
/// (TargetFileName, TargetDir, OutputFilePath, BuildsAnAssembly),
/// composables (.WithProperty / .WithItemPaths / .WithReferenceAlias)
/// and the .BuildMock() vs .Build() entrypoints.
/// </summary>
public class ProjectAnalysisMockBuilderTests
{
    [Fact]
    public void Build_WithoutConfiguration_ReturnsSensibleDefaults()
    {
        var analysis = new ProjectAnalysisMockBuilder().Build();

        analysis.ProjectFilePath.Should().BeEmpty();
        analysis.TargetFramework.Should().Be("net10.0");
        analysis.AssemblyName.Should().BeEmpty();
        analysis.TargetFileName.Should().BeEmpty();
        analysis.TargetDir.Should().BeEmpty();
        analysis.OutputFilePath.Should().BeEmpty();
        analysis.OutputRefFilePath.Should().BeEmpty();
        analysis.Language.Should().Be("C#");
        analysis.IsTestProject.Should().BeFalse();
        analysis.Succeeded.Should().BeTrue();
        analysis.BuildsAnAssembly.Should().BeFalse();
        analysis.SourceFiles.Should().BeEmpty();
        analysis.References.Should().BeEmpty();
        analysis.ProjectReferences.Should().BeEmpty();
        analysis.EmbeddedResourcePaths.Should().BeEmpty();
        analysis.AnalyzerAssemblyPaths.Should().BeEmpty();
        analysis.ReferenceAliases.Should().BeEmpty();
    }

    // Sprint 152 (ADR-036, CI matrix flake fix): cross-platform paths via Path.Combine
    // instead of hardcoded Windows-style "c:\\src\\…" — backslash is not a path separator
    // on Linux/macOS, so Path.GetFileNameWithoutExtension treated the whole string as a
    // single filename and the test failed on Ubuntu/macOS CI runners.
    private static readonly string s_srcProjectFilePath = Path.Combine("src", "MyProject.csproj");
    private static readonly string s_outBinDir = Path.Combine("out", "bin");
    private static readonly string s_customFilePath = Path.Combine("custom", "Foo.dll");

    [Fact]
    public void WithProjectFilePath_DerivesAssemblyNameAndTargetFileNameAndTargetDir()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath(s_srcProjectFilePath)
            .Build();

        analysis.ProjectFilePath.Should().Be(s_srcProjectFilePath);
        analysis.AssemblyName.Should().Be("MyProject");
        analysis.TargetFileName.Should().Be("MyProject.dll");
        analysis.TargetDir.Should().EndWith(Path.Combine("bin", "Debug", "net10.0"));
        analysis.BuildsAnAssembly.Should().BeTrue();
    }

    [Fact]
    public void WithExplicitAssemblyName_OverridesProjectFileNameDerivation()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath(s_srcProjectFilePath)
            .WithAssemblyName("CustomAssembly")
            .Build();

        analysis.AssemblyName.Should().Be("CustomAssembly");
        analysis.TargetFileName.Should().Be("CustomAssembly.dll");
    }

    [Fact]
    public void WithExplicitTargetDirAndTargetFileName_ComputesOutputFilePath()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetDir(s_outBinDir)
            .WithTargetFileName("Foo.dll")
            .Build();

        analysis.TargetDir.Should().Be(s_outBinDir);
        analysis.TargetFileName.Should().Be("Foo.dll");
        analysis.OutputFilePath.Should().Be(Path.Combine(s_outBinDir, "Foo.dll"));
    }

    [Fact]
    public void WithExplicitOutputFilePath_OverridesDerivation()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetDir(s_outBinDir)
            .WithTargetFileName("Foo.dll")
            .WithOutputFilePath(s_customFilePath)
            .Build();

        analysis.OutputFilePath.Should().Be(s_customFilePath);
    }

    [Fact]
    public void WithCollectionsAndFlags_PropagatesToInterface()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithTargetFramework("net8.0")
            .WithLanguage("Visual Basic")
            .AsTestProject()
            .AsFailed()
            .WithSourceFiles("a.cs", "b.cs")
            .WithReferences("ref1.dll", "ref2.dll")
            .WithProjectReferences("p1.csproj")
            .WithEmbeddedResources("r.resx")
            .WithAnalyzerAssemblies("an.dll")
            .WithOutputRefFilePath("c:\\ref\\Foo.dll")
            .Build();

        analysis.TargetFramework.Should().Be("net8.0");
        analysis.Language.Should().Be("Visual Basic");
        analysis.IsTestProject.Should().BeTrue();
        analysis.Succeeded.Should().BeFalse();
        analysis.SourceFiles.Should().Equal("a.cs", "b.cs");
        analysis.References.Should().Equal("ref1.dll", "ref2.dll");
        analysis.ProjectReferences.Should().Equal("p1.csproj");
        analysis.EmbeddedResourcePaths.Should().Equal("r.resx");
        analysis.AnalyzerAssemblyPaths.Should().Equal("an.dll");
        analysis.OutputRefFilePath.Should().Be("c:\\ref\\Foo.dll");
    }

    [Fact]
    public void WithProperty_RoutesToGetPropertyOrDefault()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProperty("RootNamespace", "MyProject")
            .WithProperty("LangVersion", "preview")
            .Build();

        analysis.GetPropertyOrDefault("RootNamespace").Should().Be("MyProject");
        analysis.GetPropertyOrDefault("LangVersion").Should().Be("preview");
        analysis.GetPropertyOrDefault("Missing").Should().BeNull();
        analysis.GetPropertyOrDefault("Missing", "fallback").Should().Be("fallback");
    }

    [Fact]
    public void WithItemPaths_RoutesToGetItemPaths()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithItemPaths("EmbeddedResource", "c:\\res\\strings.resx", "c:\\res\\images.resx")
            .WithItemPaths("Compile", "c:\\src\\Foo.cs")
            .Build();

        analysis.GetItemPaths("EmbeddedResource").Should().Equal("c:\\res\\strings.resx", "c:\\res\\images.resx");
        analysis.GetItemPaths("Compile").Should().Equal("c:\\src\\Foo.cs");
        analysis.GetItemPaths("MissingItem").Should().BeEmpty();
    }

    [Fact]
    public void WithReferenceAlias_PopulatesReferenceAliasesDictionary()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithReferenceAlias("c:\\ref\\Foo.dll", "FooAlias")
            .WithReferenceAlias("c:\\ref\\Bar.dll", "BarAlias", "BarAlias2")
            .Build();

        analysis.ReferenceAliases.Should().HaveCount(2);
        analysis.ReferenceAliases["c:\\ref\\Foo.dll"].Should().Equal("FooAlias");
        analysis.ReferenceAliases["c:\\ref\\Bar.dll"].Should().Equal("BarAlias", "BarAlias2");
    }

    [Fact]
    public void WithBuildsAnAssemblyFalse_OverridesDefaultDerivation()
    {
        var analysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\src\\Foo.csproj")
            .WithBuildsAnAssembly(false)
            .Build();

        // Without the override, BuildsAnAssembly would default to true (TargetFileName derived).
        analysis.BuildsAnAssembly.Should().BeFalse();
    }

    [Fact]
    public void BuildMock_ReturnsMoqCompatibleMockForVerification()
    {
        var builder = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\src\\Foo.csproj")
            .WithSourceFiles("a.cs");

        var mock = builder.BuildMock();
        var analysis = mock.Object;

        // Touch a property -> Moq records the call -> Verify must succeed.
        _ = analysis.SourceFiles;
        mock.Verify(x => x.SourceFiles, Moq.Times.Once);
    }
}
