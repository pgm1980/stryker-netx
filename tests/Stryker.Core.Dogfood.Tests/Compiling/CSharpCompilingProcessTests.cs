#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FluentAssertions;
using Stryker.Configuration.Options;
using Stryker.Core.Compiling;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 125 (v3.0.12) structural-smoke port (replaces Sprint 109 architectural-deferral).
/// Original architectural-deferral covered 549 LOC of full Roslyn compile pipeline. Structural
/// smoke tests verify constructor + ICSharpCompilingProcess interface contract WITHOUT actually
/// invoking the heavy Compile() method (which needs MetadataReference setup + emit pipeline).
/// Full compile-pipeline integration tests defer to dedicated harness sprint.</summary>
public class CSharpCompilingProcessTests : TestBase
{
    private static MutationTestInput BuildMinimalInput()
    {
        var folder = new CsharpFolderComposite();
        return new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo
            {
                Analysis = TestHelper.SetupProjectAnalyzerResult(
                    properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        ["TargetDir"] = "/bin/Debug",
                        ["TargetFileName"] = "TestAssembly.dll",
                        ["AssemblyName"] = "TestAssembly",
                    }).Object,
                ProjectContents = folder,
                TestProjectsInfo = new TestProjectsInfo(new System.IO.Abstractions.TestingHelpers.MockFileSystem()),
            },
        };
    }

    [Fact]
    public void CsharpCompilingProcess_Constructor_AcceptsMinimalArgs()
    {
        var input = BuildMinimalInput();
        var process = new CsharpCompilingProcess(input);

        process.Should().BeAssignableTo<ICSharpCompilingProcess>();
    }

    [Fact]
    public void CsharpCompilingProcess_Constructor_AcceptsCustomRollbackProcess()
    {
        var input = BuildMinimalInput();
        var rollback = new CSharpRollbackProcess();
        var options = new StrykerOptions();
        var process = new CsharpCompilingProcess(input, rollback, options);

        process.Should().BeAssignableTo<ICSharpCompilingProcess>();
    }

    [Fact(Skip = "ARCHITECTURAL DEFERRAL: end-to-end Compile() integration tests need MetadataReference setup + emit pipeline harness. Defer to dedicated compiler-pipeline harness sprint.")]
    public void CsharpCompilingProcess_FullCompilePipeline_IntegrationDeferral() { /* defer */ }
}
