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

    /// <summary>Sprint 111 (v2.97.0): consolidated 3 individual FullRunScenario [Fact(Skip)] tests
    /// (ShouldCallExecutorForEveryCoveredMutant, ShouldCallExecutorForEveryMutantWhenNoOptimization,
    /// ShouldHandleCoverage) into 1 architectural-deferral. Each upstream test wires up
    /// FullRunScenario + ICoverageAnalyser + IMutationTestExecutor mock chain producing
    /// TestRunResult/CoverageRunResult — heavy mock-builder infrastructure not yet ported.
    /// Belongs in dedicated MutationTestProcess deep-port sprint with mock-builders.</summary>
    [Fact(Skip = "ARCHITECTURAL DEFERRAL: heavy FullRunScenario + ICoverageAnalyser + IMutationTestExecutor mock chain (3 upstream tests consolidated). Re-port = TestRunResult/CoverageRunResult mock-builder infrastructure. MutationTestProcess deep-port sprint required.")]
    public void MutationTestProcess_FullRunScenarioArchitecturalDeferral() { /* permanently skipped */ }
}
