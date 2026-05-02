using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Stryker.Utilities;
using Stryker.Utilities.MSBuild;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.TestProjects;

/// <summary>Sprint 95 (v2.81.0) deep-port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: RestoreOriginalAssembly/BackupOriginalAssembly take IProjectAnalysis (Sprint 1
/// Phase 9 rename from IAnalyzerResult). All 6 previously-skipped tests now real ports.
/// Inherits TestBase: TestProjectsInfo ctor uses ApplicationLogging.LoggerFactory.</summary>
public class TestProjectsInfoTests : TestBase
{
    [Fact]
    public void ShouldGenerateInjectionPath()
    {
        var sourceProjectAnalyzerResults = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResults = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var expectedPath = FilePathUtils.NormalizePathSeparators("/test/bin/Debug/AppToTest.dll");

        var result = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResults, sourceProjectAnalyzerResults);

        result.Should().Be(expectedPath);
    }

    [Fact]
    public void MergeTestProjectsInfo()
    {
        var fileSystem = new MockFileSystem();
        var rootPath = Path.Combine("c", "TestProject");
        var fileAPath = Path.Combine(rootPath, "ExampleTestFileA.cs");
        var fileBPath = Path.Combine(rootPath, "ExampleTestFileB.cs");
        fileSystem.AddDirectory(rootPath);
        var fileA = File.ReadAllText(Path.Combine(".", "TestResources", "ExampleTestFileA.cs"));
        var fileB = File.ReadAllText(Path.Combine(".", "TestResources", "ExampleTestFileB.cs"));
        fileSystem.AddFile(fileAPath, new MockFileData(fileA));
        fileSystem.AddFile(fileBPath, new MockFileData(fileB));
        var testProjectAnalyzerResultAMock = TestHelper.SetupProjectAnalyzerResult(references: [], sourceFiles: [fileAPath]);
        var testProjectAnalyzerResultBMock = TestHelper.SetupProjectAnalyzerResult(references: [], sourceFiles: [fileBPath]);

        var testProjectA = new TestProject(fileSystem, testProjectAnalyzerResultAMock.Object);
        var testProjectB = new TestProject(fileSystem, testProjectAnalyzerResultBMock.Object);
        testProjectA.TestFiles.First().AddTest(Guid.NewGuid().ToString(), "test1", SyntaxFactory.Block());
        testProjectA.TestFiles.First().AddTest(Guid.NewGuid().ToString(), "test2", SyntaxFactory.Block());
        testProjectB.TestFiles.First().AddTest(Guid.NewGuid().ToString(), "test3", SyntaxFactory.Block());

        var testProjectsInfoA = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectA] };
        var testProjectsInfoB = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectB] };
        var testProjectsInfoC = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectB] };

        var testProjectsInfoABC = testProjectsInfoA + testProjectsInfoB + testProjectsInfoC;

        testProjectsInfoABC.TestFiles.Count().Should().Be(2);
        testProjectsInfoABC.TestFiles.ElementAt(0).Tests.Count.Should().Be(2);
        testProjectsInfoABC.TestFiles.ElementAt(1).Tests.Count.Should().Be(1);
    }

    [Fact]
    public void MergeTestProjectsInfoWithASharedSourceFile()
    {
        var fileSystem = new MockFileSystem();
        var rootPath = Path.Combine("c", "TestProject");
        var fileAPath = Path.Combine(rootPath, "ExampleTestFile.cs");
        fileSystem.AddDirectory(rootPath);
        var fileA = File.ReadAllText(Path.Combine(".", "TestResources", "ExampleTestFileA.cs"));
        fileSystem.AddFile(fileAPath, new MockFileData(fileA));
        var testProjectAnalyzerResultAMock = TestHelper.SetupProjectAnalyzerResult(references: [], sourceFiles: [fileAPath]);
        var testProjectAnalyzerResultBMock = TestHelper.SetupProjectAnalyzerResult(references: [], sourceFiles: [fileAPath]);

        var testProjectA = new TestProject(fileSystem, testProjectAnalyzerResultAMock.Object);
        var testProjectB = new TestProject(fileSystem, testProjectAnalyzerResultBMock.Object);
        testProjectA.TestFiles.First().AddTest(Guid.NewGuid().ToString(), "test1", SyntaxFactory.Block());
        testProjectA.TestFiles.First().AddTest(Guid.NewGuid().ToString(), "test2", SyntaxFactory.Block());

        var testProjectsInfoA = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectA] };
        var testProjectsInfoB = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectB] };
        var testProjectsInfoC = new TestProjectsInfo(fileSystem) { TestProjects = [testProjectB] };

        var testProjectsInfoABC = testProjectsInfoA + testProjectsInfoB + testProjectsInfoC;

        testProjectsInfoABC.TestFiles.Count().Should().Be(1);
    }

    [Fact]
    public void RestoreOriginalAssembly_RestoresIfBackupExists()
    {
        var fileSystem = Mock.Of<IFileSystem>(MockBehavior.Strict);
        var file = Mock.Of<IFile>(MockBehavior.Strict);
        Mock.Get(fileSystem).Setup(f => f.File).Returns(file);

        var sourceProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var testProject = new TestProject(fileSystem, testProjectAnalyzerResult);
        var testProjectsInfo = new TestProjectsInfo(fileSystem) { TestProjects = [testProject] };

        var injectionPath = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResult, sourceProjectAnalyzerResult);
        var backupPath = injectionPath + ".stryker-unchanged";

        Mock.Get(file).Setup(f => f.Exists(backupPath)).Returns(true);
        Mock.Get(file).Setup(f => f.Copy(backupPath, injectionPath, true));

        testProjectsInfo.RestoreOriginalAssembly(sourceProjectAnalyzerResult);

        Mock.Get(file).VerifyAll();
    }

    [Fact]
    public void RestoreOriginalAssembly_IgnoreIfBackupIsAbsent()
    {
        var fileSystem = Mock.Of<IFileSystem>(MockBehavior.Strict);
        var file = Mock.Of<IFile>(MockBehavior.Strict);
        Mock.Get(fileSystem).Setup(f => f.File).Returns(file);

        var sourceProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var testProject = new TestProject(fileSystem, testProjectAnalyzerResult);
        var testProjectsInfo = new TestProjectsInfo(fileSystem) { TestProjects = [testProject] };

        var injectionPath = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResult, sourceProjectAnalyzerResult);
        var backupPath = injectionPath + ".stryker-unchanged";

        Mock.Get(file).Setup(f => f.Exists(backupPath)).Returns(false);

        testProjectsInfo.RestoreOriginalAssembly(sourceProjectAnalyzerResult);

        Mock.Get(file).VerifyAll();
    }

    [Fact]
    public void RestoreOriginalAssembly_IgnoreIfBackupCopyFails()
    {
        var fileSystem = Mock.Of<IFileSystem>(MockBehavior.Strict);
        var file = Mock.Of<IFile>(MockBehavior.Strict);
        Mock.Get(fileSystem).Setup(f => f.File).Returns(file);

        var sourceProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var testProject = new TestProject(fileSystem, testProjectAnalyzerResult);
        var testProjectsInfo = new TestProjectsInfo(fileSystem) { TestProjects = [testProject] };

        var injectionPath = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResult, sourceProjectAnalyzerResult);
        var backupPath = injectionPath + ".stryker-unchanged";

        Mock.Get(file).Setup(f => f.Exists(backupPath)).Returns(true);
        Mock.Get(file).Setup(f => f.Copy(backupPath, injectionPath, true)).Throws(new IOException("copy failed"));

        testProjectsInfo.RestoreOriginalAssembly(sourceProjectAnalyzerResult);

        Mock.Get(file).VerifyAll();
    }

    [Fact]
    public void BackupOriginalAssembly_CreatesBackup()
    {
        var fileSystem = Mock.Of<IFileSystem>(MockBehavior.Strict);
        var directory = Mock.Of<IDirectory>(MockBehavior.Strict);
        var file = Mock.Of<IFile>(MockBehavior.Strict);

        Mock.Get(fileSystem).Setup(f => f.Directory).Returns(directory);
        Mock.Get(fileSystem).Setup(f => f.File).Returns(file);

        var sourceProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/app/bin/Debug/" },
                { "TargetFileName", "AppToTest.dll" },
            }).Object;

        var testProjectAnalyzerResult = TestHelper.SetupProjectAnalyzerResult(
            properties: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "TargetDir", "/test/bin/Debug/" },
                { "TargetFileName", "TestName.dll" },
            }).Object;

        var testProject = new TestProject(fileSystem, testProjectAnalyzerResult);
        var testProjectsInfo = new TestProjectsInfo(fileSystem, NullLogger<TestProjectsInfo>.Instance) { TestProjects = [testProject] };

        var injectionPath = TestProjectsInfo.GetInjectionFilePath(testProjectAnalyzerResult, sourceProjectAnalyzerResult);
        var backupPath = injectionPath + ".stryker-unchanged";

        Mock.Get(directory).Setup(d => d.Exists(sourceProjectAnalyzerResult.GetAssemblyDirectoryPath())).Returns(true);

        Mock.Get(file).Setup(f => f.Exists(injectionPath)).Returns(true);
        Mock.Get(file).Setup(f => f.Exists(backupPath)).Returns(false);
        Mock.Get(file).Setup(f => f.Move(injectionPath, backupPath, false));

        testProjectsInfo.BackupOriginalAssembly(sourceProjectAnalyzerResult);

        Mock.Get(directory).Verify(d => d.CreateDirectory(testProjectAnalyzerResult.GetAssemblyDirectoryPath()), Times.Never);
        Mock.Get(file).VerifyAll();
    }
}
