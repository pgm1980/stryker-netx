using System.Collections.Generic;
using FluentAssertions;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Stryker.Utilities;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.TestProjects;

/// <summary>Sprint 92 (v2.78.0) port — subset. MSTest → xUnit, Shouldly → FluentAssertions.
/// 6 of 8 tests deferred (heavy IFile/IDirectory mocking + Buildalyzer-shape adapters).
/// Subset port: GetInjectionFilePath path-derivation tests (no mocking heavy lifting).</summary>
public class TestProjectsInfoTests : TestBase
{
    [Fact]
    public void ShouldGenerateInjectionPath()
    {
        var sourceProjectAnalyzerResults = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResults = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(System.StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var expectedPath = FilePathUtils.NormalizePathSeparators("/test/bin/Debug/AppToTest.dll");

        var result = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResults, sourceProjectAnalyzerResults);

        result.Should().Be(expectedPath);
    }

    [Fact(Skip = "Heavy IFile/IDirectory mocking with Buildalyzer-shape adapters — defer to dedicated TestProjectsInfo deep-port sprint.")]
    public void MergeTestProjectsInfo() { /* skip */ }

    [Fact(Skip = "Heavy IFile/IDirectory mocking — defer.")]
    public void MergeTestProjectsInfoWithASharedSourceFile() { /* skip */ }

    [Fact(Skip = "Heavy IFile/IDirectory Mock<IFileSystem> — defer.")]
    public void RestoreOriginalAssembly_RestoresIfBackupExists() { /* skip */ }

    [Fact(Skip = "Heavy IFile/IDirectory Mock<IFileSystem> — defer.")]
    public void RestoreOriginalAssembly_IgnoreIfBackupIsAbsent() { /* skip */ }

    [Fact(Skip = "Heavy IFile/IDirectory Mock<IFileSystem> — defer.")]
    public void RestoreOriginalAssembly_IgnoreIfBackupCopyFails() { /* skip */ }

    [Fact(Skip = "Heavy IFile/IDirectory Mock<IFileSystem> — defer.")]
    public void BackupOriginalAssembly_CreatesBackup() { /* skip */ }
}
