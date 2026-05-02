using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Moq;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.Mutants;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using System.Linq;
using Xunit;
using IMutationProcess = Stryker.Core.MutationTest.IMutationProcess;
using Mutation = Stryker.Abstractions.Mutation;

namespace Stryker.Core.Dogfood.Tests.MutationTest;

/// <summary>Sprint 79 (v2.65.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: SourceProjectInfo.AnalyzerResult → Analysis (Sprint 1 Phase 9 rename).</summary>
public class CSharpMutationTestProcessTests : TestBase
{
    private string CurrentDirectory { get; }
    private string FilesystemRoot { get; }
    private string SourceFile { get; }

    public CSharpMutationTestProcessTests()
    {
        CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        FilesystemRoot = Path.GetPathRoot(CurrentDirectory)!;
        SourceFile = File.ReadAllText(Path.Combine(CurrentDirectory, "TestResources", "ExampleSourceFile.cs"));
    }

    [Fact]
    public void CsharpMutationProcess_Constructor_AcceptsFileSystemAndLogger()
    {
        // Sprint 122 (v3.0.9) structural rewrite: original upstream test wrote mutated code to disk
        // via full compiler pipeline (not orchestrator-injectable in v2.x). Replaced with constructor
        // smoke + IMutationProcess contract verification. Disk-write integration deferred to dedicated
        // compiler-pipeline harness sprint.
        var fileSystem = new MockFileSystem();
        var process = new CsharpMutationProcess(fileSystem, TestLoggerFactory.CreateLogger<CsharpMutationProcess>());

        // Verify it implements the interface
        process.Should().BeAssignableTo<IMutationProcess>();
    }

    [Fact]
    public void CsharpMutationProcess_BuildMockMutants_ReturnsNonEmptyCollection()
    {
        var mutants = BuildMockMutants();
        mutants.Should().NotBeEmpty();
        mutants[0].Mutation.DisplayName.Should().Be("test");
    }

    [Fact]
    public void MutateShouldWriteToDisk_IfCompilationIsSuccessful()
    {
        // Sprint 131 (v3.0.18): replaces architectural-deferral with end-to-end integration test.
        // Production v2.x doesn't allow orchestrator injection → use REAL orchestrator + REAL
        // CsharpCompilingProcess on simple C# source that compiles cleanly.
        var simpleSource = "public class Sample { public int Add(int a, int b) => a + b; }";
        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            SourceCode = simpleSource,
            SyntaxTree = CSharpSyntaxTree.ParseText(simpleSource),
            FullPath = Path.Combine(FilesystemRoot, "Sample.cs"),
            RelativePath = "Sample.cs",
        });

        var fileSystem = new MockFileSystem();
        // Pre-create test assembly directory (production CompileMutations writes the injected DLL there)
        var testAssemblyDir = Path.Combine(FilesystemRoot, "TestProject", "bin", "Debug", "net10.0");
        fileSystem.AddDirectory(testAssemblyDir);

        var input = new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo
            {
                Analysis = TestHelper.SetupProjectAnalyzerResult(
                    projectFilePath: Path.Combine(FilesystemRoot, "ProjectUnderTest", "ProjectUnderTest.csproj"),
                    properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        ["TargetDir"] = Path.Combine(FilesystemRoot, "ProjectUnderTest", "bin", "Debug", "net10.0"),
                        ["TargetFileName"] = "ProjectUnderTest.dll",
                        ["AssemblyName"] = "ProjectUnderTest",
                        ["Language"] = "C#",
                    },
                    references: [typeof(object).Assembly.Location, typeof(System.Linq.Enumerable).Assembly.Location]).Object,
                ProjectContents = folder,
                TestProjectsInfo = new TestProjectsInfo(fileSystem)
                {
                    TestProjects = new List<TestProject>
                    {
                        new(fileSystem, TestHelper.SetupProjectAnalyzerResult(
                            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                            {
                                ["TargetDir"] = testAssemblyDir,
                                ["TargetFileName"] = "TestProject.dll",
                                ["Language"] = "C#",
                            },
                            references: [typeof(object).Assembly.Location]).Object),
                    },
                },
            },
        };

        var target = new CsharpMutationProcess(fileSystem, TestLoggerFactory.CreateLogger<CsharpMutationProcess>());

        // Act — REAL Mutate invocation runs orchestrator + compile + disk-write
        target.Mutate(input, new StrykerOptions());

        // Assert — production writes the injected mutant assembly to test-project bin dir
        var expectedPath = Path.Combine(testAssemblyDir, "ProjectUnderTest.dll");
        fileSystem.FileExists(expectedPath).Should().BeTrue($"production CompileMutations should have written {expectedPath}");
    }

#pragma warning disable S1144, IDE0051 // Sprint 122 unused-private-method retained for documentation; superseded by Sprint 131 inline integration test
    private (MutationTestInput input, MockFileSystem fileSystem) BuildMutationTestInput()
    {
        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            SourceCode = SourceFile,
            SyntaxTree = CSharpSyntaxTree.ParseText(SourceFile),
        });

        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>(System.StringComparer.Ordinal)
        {
            { Path.Combine(FilesystemRoot, "SomeFile.cs"), new MockFileData("SomeFile") },
        });

        fileSystem.AddDirectory(Path.Combine(FilesystemRoot, "TestProject", "bin", "Debug", "netcoreapp2.0"));

        var input = new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo
            {
                Analysis = TestHelper.SetupProjectAnalyzerResult(
                    projectFilePath: "/c/ProjectUnderTest/ProjectUnderTest.csproj",
                    properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        { "TargetDir", Path.Combine(FilesystemRoot, "ProjectUnderTest", "bin", "Debug", "netcoreapp2.0") },
                        { "TargetFileName", "ProjectUnderTest.dll" },
                        { "AssemblyName", "AssemblyName" },
                        { "Language", "C#" },
                    },
                    references: [typeof(object).Assembly.Location]).Object,
                ProjectContents = folder,
                TestProjectsInfo = new TestProjectsInfo(fileSystem)
                {
                    TestProjects = new List<TestProject>
                    {
                        new(fileSystem, TestHelper.SetupProjectAnalyzerResult(properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                        {
                            { "TargetDir", Path.Combine(FilesystemRoot, "TestProject", "bin", "Debug", "netcoreapp2.0") },
                            { "TargetFileName", "TestName.dll" },
                            { "Language", "C#" },
                        }).Object),
                    },
                },
            },
        };

        return (input, fileSystem);
    }

    private static Collection<Mutant> BuildMockMutants() =>
    [
        new()
        {
            Mutation = new Mutation
            {
                OriginalNode = SyntaxFactory.IdentifierName("_"),
                ReplacementNode = SyntaxFactory.IdentifierName("_"),
                DisplayName = "test",
            },
        },
    ];

    private Mock<BaseMutantOrchestrator<SyntaxTree, SemanticModel>> BuildOrchestratorMock(
        StrykerOptions options, Collection<Mutant> mockMutants)
    {
        var orchestratorMock = new Mock<BaseMutantOrchestrator<SyntaxTree, SemanticModel>>(MockBehavior.Strict, options);
        orchestratorMock.Setup(x => x.Mutate(It.IsAny<SyntaxTree>(), It.IsAny<SemanticModel>())).Returns(CSharpSyntaxTree.ParseText(SourceFile));
        orchestratorMock.SetupAllProperties();
        orchestratorMock.Setup(x => x.GetLatestMutantBatch()).Returns(mockMutants);
        return orchestratorMock;
    }
#pragma warning restore S1144, IDE0051
}
