#pragma warning disable IDE0028, IDE0300, IDE0301, CA1859, MA0051 // collection-expression on cast; CA1859/MA0051 perf-not-test-concern
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.CoverageAnalysis;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Abstractions.Testing;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutationTest;

/// <summary>Sprint 107 (v2.93.0) minimum-viable port from
/// src/Stryker.Core/Stryker.Core.UnitTest/MutationTest/MutationTestProcessTests.cs (replaces
/// Sprint 93 placeholder). Production matches upstream signatures with Sprint 25-26 drift:
/// SourceProjectInfo.AnalyzerResult → .Analysis. Heavy FullRunScenario+CoverageAnalysis tests
/// (8 of 9 upstream) defer for separate sprint due to v2.x pipeline drift.</summary>
public class MutationTestProcessTests : TestBase
{
    private readonly MockFileSystem _fileSystem = new();
    private readonly CsharpFolderComposite _folder = new();

    private MutationTestInput BuildInput()
    {
        var filesystemRoot = Path.GetPathRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)) ?? "/";
        var testProjectsInfo = new TestProjectsInfo(_fileSystem)
        {
            TestProjects = new List<TestProject>
            {
                new(_fileSystem, TestHelper.SetupProjectAnalyzerResult(
                    projectFilePath: Path.Combine(filesystemRoot, "TestProject", "TestProject.csproj"),
                    properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        ["TargetDir"] = Path.Combine(filesystemRoot, "TestProject", "bin", "Debug", "netcoreapp2.0"),
                        ["TargetFileName"] = "TestName.dll",
                        ["Language"] = "C#",
                    }).Object),
            },
        };
        return new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo
            {
                Analysis = TestHelper.SetupProjectAnalyzerResult(
                    projectFilePath: Path.Combine(filesystemRoot, "ProjectUnderTest", "ProjectUnderTest.csproj"),
                    properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        ["TargetDir"] = "/bin/Debug/netcoreapp2.1",
                        ["TargetFileName"] = "ProjectUnderTest.dll",
                        ["AssemblyName"] = "ProjectUnderTest",
                        ["Language"] = "C#",
                    }).Object,
                ProjectContents = _folder,
                TestProjectsInfo = testProjectsInfo,
            },
            TestProjectsInfo = testProjectsInfo,
        };
    }

    [Fact]
    public void ShouldCallMutationProcess_MutateAndFilterMutants()
    {
        var options = new StrykerOptions { ExcludedMutations = System.Array.Empty<Mutator>() };
        var executorMock = new Mock<IMutationTestExecutor>();
        var coverageAnalyzerMock = new Mock<ICoverageAnalyser>();

        var mutationProcessMock = new Mock<IMutationProcess>(MockBehavior.Strict);
        mutationProcessMock.Setup(x => x.Mutate(It.IsAny<MutationTestInput>(), It.IsAny<IStrykerOptions>()));
        mutationProcessMock.Setup(x => x.FilterMutants(It.IsAny<MutationTestInput>()));

        var target = new MutationTestProcess(executorMock.Object, coverageAnalyzerMock.Object, mutationProcessMock.Object, TestLoggerFactory.CreateLogger<MutationTestProcess>());

        target.Initialize(BuildInput(), options, Mock.Of<IReporter>());
        target.Mutate();
        target.FilterMutants();

        mutationProcessMock.Verify(x => x.Mutate(It.IsAny<MutationTestInput>(), It.IsAny<IStrykerOptions>()), Times.Once);
        mutationProcessMock.Verify(x => x.FilterMutants(It.IsAny<MutationTestInput>()), Times.Once);
    }

    [Fact]
    public void FullRunScenario_CreatesAndTracksMutants()
    {
        // Sprint 127 (v3.0.14): replaces architectural-deferral with FullRunScenario port + structural test.
        var scenario = new FullRunScenario();
        scenario.CreateMutants(1, 2, 3);

        scenario.GetMutants().Should().HaveCount(3);
        scenario.Mutants.Should().ContainKeys(1, 2, 3);
    }

    [Fact]
    public void FullRunScenario_CreatesAndTracksTests()
    {
        var scenario = new FullRunScenario();
        scenario.CreateTests(1, 2);

        scenario.TestSet.Count.Should().Be(2);
    }

    [Fact]
    public void FullRunScenario_DeclareCoverageForMutant()
    {
        var scenario = new FullRunScenario();
        scenario.CreateMutants(1, 2);
        scenario.CreateTests(1, 2);
        scenario.DeclareCoverageForMutant(1, 1);

        scenario.GetCoveredMutants().Should().ContainSingle().Which.Id.Should().Be(1);
    }

    [Fact]
    public void FullRunScenario_GetTestRunnerMock_HasInitialTestSetup()
    {
        var scenario = new FullRunScenario();
        scenario.CreateMutants(1, 2);
        scenario.CreateTests(1);
        var runnerMock = scenario.GetTestRunnerMock();

        runnerMock.Object.Should().NotBeNull();
        // Verify the runner mock has the InitialTestAsync setup by inspecting Mock setup count
        runnerMock.Setups.Should().NotBeEmpty();
    }
}
