using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Initialisation;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest;
using Xunit;
using TestCase = Microsoft.VisualStudio.TestPlatform.ObjectModel.TestCase;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 84 (v2.70.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: SourceProjectInfo.AnalyzerResult → Analysis (Sprint 1 Phase 9).
/// Uses ProjectAnalysisMockBuilder (Sprint 61) for cleaner test setup.</summary>
public class ProjectMutatorTests : TestBase
{
    private readonly Mock<IMutationTestProcess> _mutationTestProcessMock = new(MockBehavior.Strict);
    private readonly Mock<IReporter> _reporterMock = new(MockBehavior.Strict);
    // CA1859: kept as IFileSystem to match production InputFileResolver signature; concrete MockFileSystem is implementation detail.
#pragma warning disable CA1859
    private readonly IFileSystem _fileSystemMock = new MockFileSystem();
#pragma warning restore CA1859
    private const string TestFilePath = "c:\\mytestfile.cs";
    private const string TestFileContents = """
        using Microsoft.VisualStudio.TestTools.UnitTesting;

        namespace ExtraProject.XUnit
        {
            public class UnitTest1
            {
                [TestMethod]
                public void Test1()
                {
                    // example test
                }

                [TestMethod]
                public void Test2()
                {
                    // example test
                }
            }
        }
        """;

    private readonly MutationTestInput _mutationTestInput;

    public ProjectMutatorTests()
    {
        _mutationTestProcessMock.Setup(x => x.Mutate());
        _mutationTestProcessMock.Setup(x => x.Initialize(It.IsAny<MutationTestInput>(), It.IsAny<IStrykerOptions>(), It.IsAny<IReporter>()));
        _fileSystemMock.File.WriteAllText(TestFilePath, TestFileContents);

        var sourceAnalysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\project.csproj")
            .WithTargetFramework("netcoreapp3.1")
            .WithAssemblyName("TestProject")
            .WithTargetDir("c:\\bin\\Debug\\netcoreapp3.1")
            .WithTargetFileName("TestProject.dll")
            .WithLanguage("C#")
            .Build();

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            FullPath = "c:\\TestClass.cs",
            SyntaxTree = CSharpSyntaxTree.ParseText("class TestClass { }"),
        });

        var testAnalysis = new ProjectAnalysisMockBuilder()
            .WithProjectFilePath("c:\\testproject.csproj")
            .WithTargetFramework("netcoreapp3.1")
            .WithSourceFiles(TestFilePath)
            .Build();

        _mutationTestInput = new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo
            {
                Analysis = sourceAnalysis,
                ProjectContents = folder,
            },
            TestProjectsInfo = new TestProjectsInfo(_fileSystemMock)
            {
                TestProjects = new List<TestProject>
                {
                    new(_fileSystemMock, testAnalysis),
                },
            },
        };
    }

    [Fact]
    public void ShouldInitializeEachProjectInSolution()
    {
        var options = new StrykerOptions();
        var serviceProviderMock = new Mock<IServiceProvider>();
        var target = new ProjectMutator(TestLoggerFactory.CreateLogger<ProjectMutator>(), serviceProviderMock.Object);

        var testCase1 = new VsTestCase(new TestCase("mytestname1", new Uri(TestFilePath), TestFileContents)
        {
            Id = Guid.NewGuid(),
            CodeFilePath = TestFilePath,
            LineNumber = 7,
        });
        var failedTest = testCase1.Id;
        var testCase2 = new VsTestCase(new TestCase("mytestname2", new Uri(TestFilePath), TestFileContents)
        {
            Id = Guid.NewGuid(),
            CodeFilePath = TestFilePath,
            LineNumber = 13,
        });
        var successfulTest = testCase2.Id;
        var tests = new List<VsTestDescription> { new(testCase1), new(testCase2) };
        var initialTestRunResult = new TestRunResult(
            vsTestDescriptions: tests,
            executedTests: new TestIdentifierList(failedTest, successfulTest),
            failedTests: new TestIdentifierList(failedTest),
            timedOutTest: TestIdentifierList.NoTest(),
            message: "testrun successful",
            messages: [],
            timeSpan: TimeSpan.FromSeconds(2));

        var initialTestRun = new InitialTestRun(initialTestRunResult, new TimeoutValueCalculator(500));

        _mutationTestInput.InitialTestRun = initialTestRun;

        var result = target.MutateProject(options, _mutationTestInput, _reporterMock.Object, _mutationTestProcessMock.Object);

        result.Should().NotBeNull();
        var testFile = _mutationTestInput.TestProjectsInfo.TestFiles.Should().ContainSingle().Subject;
        testFile.Tests.Count.Should().Be(2);
    }
}
