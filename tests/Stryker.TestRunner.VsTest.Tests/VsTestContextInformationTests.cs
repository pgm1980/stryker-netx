using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer;
using Microsoft.TestPlatform.VsTestConsole.TranslationLayer.Interfaces;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Moq;
using Serilog.Events;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Stryker.TestRunner.VsTest.Helpers;
using Stryker.Utilities;
using Xunit;
using VsTestObjModel = Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 28 (v2.15.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.VsTest.UnitTest/VsTextContextInformationTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// IAnalyzerResult → IProjectAnalysis via Sprint-25 TestHelper adapter.
/// Tests cover VsTestContextInformation discovery / cleanup / param-setup +
/// log-level mapping (Theory ×7).
/// </summary>
public class VsTestContextInformationTests : TestBase
{
    private readonly string _testAssemblyPath;
    private readonly TestProjectsInfo _testProjectsInfo;
    private readonly MockFileSystem _fileSystem;
    private readonly Uri _msTestExecutorUri;
    private readonly Uri _executorUri;
    private ConsoleParameters _consoleParameters = null!;

    public VsTestContextInformationTests()
    {
        var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var filesystemRoot = Path.GetPathRoot(currentDirectory)!;

        var sourceFile = File.ReadAllText(currentDirectory + "/TestResources/ExampleSourceFile.cs");
        var testProjectPath = FilePathUtils.NormalizePathSeparators(Path.Combine(filesystemRoot, "TestProject", "TestProject.csproj"))!;
        var projectUnderTestPath = FilePathUtils.NormalizePathSeparators(Path.Combine(filesystemRoot, "ExampleProject", "ExampleProject.csproj"))!;
        const string DefaultTestProjectFileContents = """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>netcoreapp2.0</TargetFramework>
                    <IsPackable>false</IsPackable>
                </PropertyGroup>
                <ItemGroup>
                    <PackageReference Include="Microsoft.NET.Test.Sdk" Version = "15.5.0" />
                    <PackageReference Include="xunit" Version="2.3.1" />
                    <PackageReference Include="xunit.runner.visualstudio" Version="2.3.1" />
                    <DotNetCliToolReference Include="dotnet-xunit" Version="2.3.1" />
                </ItemGroup>
                <ItemGroup>
                    <ProjectReference Include="..\ExampleProject\ExampleProject.csproj" />
                </ItemGroup>
            </Project>
            """;
        _testAssemblyPath = FilePathUtils.NormalizePathSeparators(Path.Combine(filesystemRoot, "_firstTest", "bin", "Debug", "TestApp.dll"))!;
        _executorUri = new Uri("exec://nunit");
        _msTestExecutorUri = new Uri("exec://mstestV2");
        var firstTest = BuildCase("T0");
        var secondTest = BuildCaseMsTest("T1");

        var content = new CsharpFolderComposite();
        _fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(StringComparer.Ordinal)
        {
            [projectUnderTestPath] = new MockFileData(DefaultTestProjectFileContents),
            [Path.Combine(filesystemRoot, "ExampleProject", "Recursive.cs")!] = new MockFileData(sourceFile),
            [Path.Combine(filesystemRoot, "ExampleProject", "OneFolderDeeper", "Recursive.cs")!] = new MockFileData(sourceFile),
            [testProjectPath] = new MockFileData(DefaultTestProjectFileContents),
            [_testAssemblyPath] = new MockFileData("Bytecode"),
            [Path.Combine(filesystemRoot, "app", "bin", "Debug", "AppToTest.dll")!] = new MockFileData("Bytecode"),
        });
        content.Add(new CsharpFileLeaf());
        _testProjectsInfo = new TestProjectsInfo(_fileSystem)
        {
            TestProjects =
            [
                new TestProject(_fileSystem,
                    TestHelper.SetupProjectAnalyzerResult(
                        properties: new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            ["TargetDir"] = Path.GetDirectoryName(_testAssemblyPath)!,
                            ["TargetFileName"] = Path.GetFileName(_testAssemblyPath),
                        },
                        targetFramework: "netcoreapp2.1").Object),
            ],
        };

        TestCases = [firstTest, secondTest];
    }

    // CA1859 (prefer concrete over abstract for perf) and MA0016 (prefer abstraction over impl) disagree
    // on the same surface. ICollection<T> is the right level here: tests.Count is used; perf is not the concern
    // in a test-only path. Suppress CA1859 explicitly.
#pragma warning disable CA1859
    private static void DiscoverTests(ITestDiscoveryEventsHandler discoveryEventsHandler, ICollection<VsTestObjModel.TestCase> tests, bool aborted)
#pragma warning restore CA1859
    {
        ArgumentNullException.ThrowIfNull(tests);

        _ = Task.Run(() => discoveryEventsHandler.HandleDiscoveredTests(tests))
            .ContinueWith((_, u) => discoveryEventsHandler.HandleDiscoveryComplete((int)u!, null, aborted), tests.Count, TaskScheduler.Default);
    }

    public IList<VsTestObjModel.TestCase> TestCases { get; set; }

    private VsTestObjModel.TestCase BuildCase(string name) => new(name, _executorUri, _testAssemblyPath) { Id = Guid.NewGuid() };

    private VsTestObjModel.TestCase BuildCaseMsTest(string name) => new(name, _msTestExecutorUri, _testAssemblyPath) { Id = Guid.NewGuid() };

    private VsTestContextInformation BuildVsTextContext(IStrykerOptions options, out Mock<IVsTestConsoleWrapper> mockedVsTestConsole)
    {
        mockedVsTestConsole = new Mock<IVsTestConsoleWrapper>(MockBehavior.Strict);
        mockedVsTestConsole.Setup(x => x.StartSession());
        mockedVsTestConsole.Setup(x => x.InitializeExtensions(It.IsAny<IEnumerable<string>>()));
        mockedVsTestConsole.Setup(x => x.EndSession());
        mockedVsTestConsole.Setup(x =>
            x.DiscoverTests(
                It.Is<IEnumerable<string>>(d => d.Any(e => string.Equals(e, _testAssemblyPath, StringComparison.Ordinal))),
                It.IsAny<string>(),
                It.IsAny<ITestDiscoveryEventsHandler>())).Callback(
            (IEnumerable<string> _, string _, ITestDiscoveryEventsHandler discoveryEventsHandler) =>
                DiscoverTests(discoveryEventsHandler, TestCases, false));

        var vsTestConsoleWrapper = mockedVsTestConsole.Object;
        return new VsTestContextInformation(
            options,
            new Mock<IVsTestHelper>().Object,
            _fileSystem,
            parameters =>
            {
                _consoleParameters = parameters;
                return vsTestConsoleWrapper;
            },
            null,
            NullLogger.Instance);
    }

    [Fact]
    public void InitializeAndDiscoverTests()
    {
        using var runner = BuildVsTextContext(new StrykerOptions(), out _);
        foreach (var testAssembly in _testProjectsInfo.GetTestAssemblies())
        {
            runner.AddTestSource(testAssembly);
        }
        runner.VsTests.Count.Should().Be(2);
    }

    [Fact]
    public void CleanupProperly()
    {
        using var runner = BuildVsTextContext(new StrykerOptions(), out var mock);
        foreach (var testAssembly in _testProjectsInfo.GetTestAssemblies())
        {
            runner.AddTestSource(testAssembly);
        }
        runner.Dispose();
        mock.Verify(m => m.EndSession(), Times.Once);
    }

    [Fact]
    public void InitializeAndSetParameters()
    {
        using var runner = BuildVsTextContext(new StrykerOptions(), out _);
        runner.AddTestSource(_testAssemblyPath);
        _consoleParameters.TraceLevel.Should().Be(TraceLevel.Off);
        _consoleParameters.LogFilePath.Should().BeNull();
    }

    [Fact]
    public void InitializeAndSetParametersAccordingToOptions()
    {
        using var runner = BuildVsTextContext(new StrykerOptions { LogOptions = new LogOptions { LogToFile = true } }, out _);
        runner.AddTestSource(_testAssemblyPath);
        _consoleParameters.TraceLevel.Should().Be(TraceLevel.Info);
        _consoleParameters.LogFilePath.Should().Be($"\"logs{_fileSystem.Path.DirectorySeparatorChar}TestDiscoverer-log.txt\"");
        _fileSystem.AllDirectories.Last().Should().MatchRegex(".*logs$");
    }

    [Theory]
    [InlineData(LogEventLevel.Debug, TraceLevel.Verbose)]
    [InlineData(LogEventLevel.Verbose, TraceLevel.Verbose)]
    [InlineData(LogEventLevel.Information, TraceLevel.Info)]
    [InlineData(LogEventLevel.Warning, TraceLevel.Warning)]
    [InlineData(LogEventLevel.Error, TraceLevel.Error)]
    [InlineData(LogEventLevel.Fatal, TraceLevel.Error)]
    [InlineData((LogEventLevel)(-1), TraceLevel.Off)]
    public void InitializeAndSetProperLogLevel(LogEventLevel setLevel, TraceLevel expectedLevel)
    {
        using var runner = BuildVsTextContext(new StrykerOptions { LogOptions = new LogOptions { LogLevel = setLevel } }, out _);
        runner.AddTestSource(_testAssemblyPath);
        _consoleParameters.TraceLevel.Should().Be(expectedLevel);
    }
}
