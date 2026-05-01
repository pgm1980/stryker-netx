using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Stryker.Solutions;
using Xunit;

namespace Stryker.Solutions.Tests;

/// <summary>
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Solutions.Test/SolutionFileShould.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Test logic preserved 1:1 with upstream; the few path constants are
/// adjusted because our flat repo structure replaces upstream's
/// src/&lt;Module&gt;/&lt;Module&gt;/ nesting with src/&lt;Module&gt;/.
/// </summary>
public sealed class SolutionFileShould
{
    // Use the upstream-shape clone in _references/ as the test target for the
    // ProvideProjectListForGivenConfiguration test, because that test asserts
    // on the upstream nested layout (Stryker.CLI/Stryker.CLI/Stryker.CLI.csproj).
    private static readonly string UpstreamSlnxPath =
        Path.Combine("..", "..", "..", "..", "..", "_references", "stryker-net", "src", "Stryker.slnx");

    [Fact]
    public void LoadStrykerSlnFile()
    {
        var solution = SolutionFile.GetSolution(UpstreamSlnxPath);
        solution.Should().NotBeNull();
    }

    [Fact]
    public void IdentifyStrykerBuildTypes()
    {
        var solution = SolutionFile.GetSolution(UpstreamSlnxPath);
        solution.GetBuildTypes().Should().Equal("Debug", "Release");
    }

    [Fact]
    public void IdentifyStrykerPlatform()
    {
        var solution = SolutionFile.GetSolution(UpstreamSlnxPath);
        solution.ConfigurationExists("Debug", "Any CPU").Should().BeTrue();
    }

    [Theory]
    [InlineData("Any CPU")]
    [InlineData("AnyCPU")]
    [InlineData("Z80")]
    public void DetectPlatformIfNotSpecified(string platform)
    {
        List<string> projects = ["Project.csproj", "Test.csproj"];
        var solution = SolutionFile.BuildFromProjectList(projects, [platform]);

        solution.GetProjectsWithDetails("Debug").Should().Equal(projects.Select(p => (p, "Debug", platform)));
    }

    [Fact]
    public void DefaultPlatformToAnyCpuIfNotSpecified()
    {
        List<string> projects = ["Project.csproj", "Test.csproj"];
        var solution = SolutionFile.BuildFromProjectList(projects, ["Z80", "Any CPU"]);

        solution.GetProjectsWithDetails("Debug").Should().Equal(projects.Select(p => (p, "Debug", "Any CPU")));
    }

    [Fact]
    public void DefaultPlatformToFirstIfAnyCpuNotProvided()
    {
        List<string> projects = ["Project.csproj", "Test.csproj"];
        var solution = SolutionFile.BuildFromProjectList(projects, ["Z80", "6502"]);

        solution.GetProjectsWithDetails("Debug").Should().Equal(projects.Select(p => (p, "Debug", "Z80")));
    }

    [Fact]
    public void ProvideProjectListForGivenConfiguration()
    {
        var solution = SolutionFile.GetSolution(UpstreamSlnxPath);
        solution.ConfigurationExists("Debug", "Any CPU").Should().BeTrue();

        // Upstream-shape Stryker.slnx: report all production + UnitTest projects in the upstream nested layout.
        var expectedProjects = new List<string>
        {
            Path.Combine("Stryker.CLI", "Stryker.CLI", "Stryker.CLI.csproj"),
            Path.Combine("Stryker.CLI", "Stryker.CLI.UnitTest", "Stryker.CLI.UnitTest.csproj"),
            Path.Combine("Stryker.Core", "Stryker.Core", "Stryker.Core.csproj"),
            Path.Combine("Stryker.Core", "Stryker.Core.UnitTest", "Stryker.Core.UnitTest.csproj"),
            Path.Combine("Stryker.DataCollector", "Stryker.DataCollector.csproj"),
            Path.Combine("Stryker.RegexMutators", "Stryker.RegexMutators", "Stryker.RegexMutators.csproj"),
            Path.Combine("Stryker.RegexMutators", "Stryker.RegexMutators.UnitTest", "Stryker.RegexMutators.UnitTest.csproj"),
            Path.Combine("Stryker.Abstractions", "Stryker.Abstractions.csproj"),
            Path.Combine("Stryker.Configuration", "Stryker.Configuration.csproj"),
            Path.Combine("Stryker.Utilities", "Stryker.Utilities.csproj"),
            Path.Combine("Stryker.TestRunner", "Stryker.TestRunner.csproj"),
            Path.Combine("Stryker.TestRunner.VsTest", "Stryker.TestRunner.VsTest.csproj"),
            Path.Combine("Stryker.TestRunner.VsTest.UnitTest", "Stryker.TestRunner.VsTest.UnitTest.csproj"),
            Path.Combine("Stryker.Solutions", "Stryker.Solutions.csproj"),
            Path.Combine("Stryker.Solutions.Test", "Stryker.Solutions.Test.csproj"),
            Path.Combine("Stryker.TestRunner.MicrosoftTestPlatform", "Stryker.TestRunner.MicrosoftTestPlatform.csproj"),
            Path.Combine("Stryker.TestRunner.MicrosoftTestPlatform.UnitTest", "Stryker.TestRunner.MicrosoftTestPlatform.UnitTest.csproj"),
        };
        AssertProjectListEndsWith(solution.GetProjects("Debug"), expectedProjects);
    }

    [Theory]
    [InlineData("MicrosoftTestPlatform.sln")]
    [InlineData("MicrosoftTestPlatform.slnx")]
    public void ProvideProjectListForGivenConfigurationOnSolutionWithMultiplePlatforms(string solutionFile)
    {
        var solution = SolutionFile.GetSolution(Path.Combine("..", "..", "..", "..", "..", "integrationtest", "TargetProjects", solutionFile));

        var expectedProjects = new List<string>
        {
            Path.Combine("NetCore", "TargetProject", "TargetProject.csproj"),
            Path.Combine("NetCore", "Library", "Library.csproj"),
            Path.Combine("MicrosoftTestPlatform", "UnitTests.MSTest", "UnitTests.MSTest.csproj"),
            Path.Combine("MicrosoftTestPlatform", "UnitTests.XUnit", "UnitTests.XUnit.csproj"),
            Path.Combine("MicrosoftTestPlatform", "UnitTests.NUnit", "UnitTests.NUnit.csproj"),
            Path.Combine("MicrosoftTestPlatform", "UnitTests.TUnit", "UnitTests.TUnit.csproj"),
        };
        AssertProjectListEndsWith(solution.GetProjects("Debug"), expectedProjects);

        var projectsWithDetails = solution.GetProjectsWithDetails("Debug").ToList();
        AssertProjectListEndsWith(projectsWithDetails.Select(x => x.file), expectedProjects);
        projectsWithDetails.Should().AllSatisfy(x => x.buildType.Should().Be("Debug"));
        projectsWithDetails.Should().AllSatisfy(x =>
            x.platform.Should().BeOneOf("Any CPU", "AnyCPU",
                $"platform '{x.platform}' must be either 'Any CPU' or 'AnyCPU'"));
    }

    [Fact]
    public void PickExactMatch()
    {
        var solution = SolutionFile.BuildFromProjectList(["Project.csproj", "Test.csproj"], ["x86", "x64"]);

        var match = solution.GetMatching("Debug", "x64");
        match.Should().Be(("Debug", "x64"));
    }

    [Fact]
    public void FallBackOnDebug()
    {
        var solution = SolutionFile.BuildFromProjectList(["Project.csproj", "Test.csproj"], ["x86", "x64"]);

        var match = solution.GetMatching("Stryker", "x64");
        match.Should().Be(("Debug", "x64"));
    }

    [Fact]
    public void FallBackOnAnyCPU()
    {
        var solution = SolutionFile.BuildFromProjectList(["Project.csproj", "Test.csproj"], ["AnyCPU"]);

        var match = solution.GetMatching("Debug", "x64");
        match.Should().Be(("Debug", "AnyCPU"));
    }

    [Fact]
    public void PickFirstIfNoMatch()
    {
        var solution = SolutionFile.BuildFromProjectList(["Project.csproj", "Test.csproj"], ["AnyCPU"]);

        var match = solution.GetMatching("Stryker", "x64");
        match.Should().Be(("Debug", "AnyCPU"));
    }

    /// <summary>
    /// Helper for project-list assertions: stryker-netx's <see cref="SolutionFile.GetProjects"/>
    /// returns absolute paths (Sprint 1 Workspaces.MSBuild port), where upstream returned
    /// solution-relative paths. Asserting on suffixes preserves the upstream test intent
    /// (correct project-set for this configuration) without coupling to the path-shape
    /// difference. Document the behaviour change for v2.x users.
    /// </summary>
    private static void AssertProjectListEndsWith(IEnumerable<string> actual, List<string> expectedRelativeSuffixes)
    {
        var actualList = actual.ToList();
        actualList.Should().HaveCount(expectedRelativeSuffixes.Count);
        foreach (var expected in expectedRelativeSuffixes)
        {
            actualList.Should().Contain(p => p.EndsWith(expected, System.StringComparison.Ordinal),
                $"the actual project list should contain a path ending with '{expected}'");
        }
    }
}
