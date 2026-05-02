#pragma warning disable IDE0028, IDE0300, CA1859, MA0051 // collection-expression on cast; CA1859/MA0051 perf-not-test-concern
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LibGit2Sharp;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.DiffProviders;
using Stryker.TestHelpers;
using Stryker.Utilities;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.DiffProviders;

/// <summary>Sprint 105 (v2.91.0) full upstream port from
/// src/Stryker.Core/Stryker.Core.UnitTest/DiffProviders/GitDiffProviderTests.cs (replaces
/// Sprint 94 placeholder, 691 LOC). Production matches upstream LibGit2Sharp IRepository +
/// IGitInfoProvider signatures. Helper `BuildScanDiffTarget` extracts the 60-LOC mock-builder
/// boilerplate so each test boils down to options + paths + assert.</summary>
public class GitDiffProviderTests : TestBase
{
    /// <summary>Helper extracted from upstream's per-test 60-LOC mock chain. Builds a
    /// GitDiffProvider with mocked IRepository whose Patch.GetEnumerator returns the supplied
    /// paths. Each path is wrapped in a Mock&lt;PatchEntryChanges&gt;.</summary>
    private static GitDiffProvider BuildScanDiffTarget(StrykerOptions options, params string?[] patchPaths)
    {
        var gitInfoMock = new Mock<IGitInfoProvider>();
        var repositoryMock = new Mock<IRepository>(MockBehavior.Loose);
        var commitMock = new Mock<Commit>(MockBehavior.Loose);
        var branchMock = new Mock<Branch>(MockBehavior.Strict);
        var patchMock = new Mock<Patch>(MockBehavior.Strict);

        commitMock.SetupGet(x => x.Tree).Returns(new Mock<Tree>(MockBehavior.Loose).Object);
        branchMock.SetupGet(x => x.Tip).Returns(commitMock.Object);
        branchMock.SetupGet(x => x.UpstreamBranchCanonicalName).Returns("origin/branch");
        branchMock.SetupGet(x => x.CanonicalName).Returns("refs/heads/branch");
        branchMock.SetupGet(x => x.FriendlyName).Returns("branch");

        repositoryMock
            .Setup(x => x.Branches.GetEnumerator())
            .Returns(new List<Branch> { branchMock.Object }.GetEnumerator())
            .Verifiable();

        var patchEntries = new List<PatchEntryChanges>();
        foreach (var path in patchPaths)
        {
            var patchEntry = new Mock<PatchEntryChanges>(MockBehavior.Strict);
            patchEntry.SetupGet(x => x.Path).Returns(path ?? string.Empty);
            patchEntries.Add(patchEntry.Object);
        }
        patchMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<PatchEntryChanges>)patchEntries).GetEnumerator());

        repositoryMock.Setup(x => x.Diff.Compare<Patch>(It.IsAny<Tree>(), DiffTargets.WorkingDirectory)).Returns(patchMock.Object);
        repositoryMock.Setup(x => x.Lookup(It.IsAny<ObjectId>())).Returns(commitMock.Object);

        gitInfoMock.Setup(x => x.DetermineCommit()).Returns(commitMock.Object);
        gitInfoMock.SetupGet(x => x.Repository).Returns(repositoryMock.Object);
        gitInfoMock.SetupGet(x => x.RepositoryPath).Returns("/c/Path/To/Repo");

        return new GitDiffProvider(options, new Stryker.TestRunner.Tests.TestSet(), gitInfoMock.Object);
    }

    [Fact]
    public void DoesNotCreateNewRepositoryWhenPassedIntoConstructor()
    {
        var options = new StrykerOptions { ProjectPath = "C:\\" };
        var gitInfoProvider = new Mock<IGitInfoProvider>(MockBehavior.Strict);

        Action act = () => _ = new GitDiffProvider(options, new Stryker.TestRunner.Tests.TestSet(), gitInfoProvider.Object);

        act.Should().NotThrow();
        gitInfoProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "C:\\",
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
        };
        var target = BuildScanDiffTarget(options, "file.cs");
        var res = target.ScanDiff();

        res.ChangedSourceFiles.Should().HaveCount(1);
        res.ChangedTestFiles.Should().HaveCount(0);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_IgnoreFolderWithSameStartName()
    {
        var options = new StrykerOptions
        {
            ProjectPath = "C:\\",
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
        };
        var target = BuildScanDiffTarget(options, "/c/Users/JohnDoe/Project/Tests-temp/file.cs");
        var res = target.ScanDiff();

        res.ChangedSourceFiles.Should().HaveCount(1);
        res.ChangedTestFiles.Should().HaveCount(0);
    }

    [Fact]
    public void ScanDiff_Throws_Stryker_Input_Exception_When_Commit_null()
    {
        var options = new StrykerOptions { SinceTarget = "branch" };
        var repositoryMock = new Mock<IRepository>();
        var branchCollectionMock = new Mock<BranchCollection>();
        var branchMock = new Mock<Branch>();

        branchCollectionMock.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<Branch>(MockBehavior.Loose).Object);
        branchMock.SetupGet(x => x.IsCurrentRepositoryHead).Returns(true);
        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchCollectionMock.Setup(x => x.GetEnumerator())
            .Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var gitInfoMock = new Mock<IGitInfoProvider>();
        gitInfoMock.Setup(x => x.DetermineCommit()).Returns((Commit)null!);
        gitInfoMock.SetupGet(x => x.Repository).Returns(repositoryMock.Object);
        var target = new GitDiffProvider(options, new Stryker.TestRunner.Tests.TestSet(), gitInfoMock.Object);

        Action act = () => target.ScanDiff();
        act.Should().Throw<InputException>();
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_ExcludingTestFilesInDiffIgnoreFiles()
    {
        var diffIgnoreFiles = new IExclusionPattern[] { new ExclusionPattern("/c/Users/JohnDoe/Project/Tests/Test.cs") };
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
            DiffIgnoreChanges = diffIgnoreFiles,
        };
        var target = BuildScanDiffTarget(options, "file.cs", FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Test.cs"));
        var res = target.ScanDiff();

        res.ChangedTestFiles.Should().HaveCount(0);
        res.ChangedSourceFiles.Should().HaveCount(1);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_ExcludingTestFilesInDiffIgnoreFiles_Single_Asterisk()
    {
        var diffIgnoreFiles = new IExclusionPattern[] { new ExclusionPattern("/c/Users/JohnDoe/Project/*/Test.cs") };
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
            DiffIgnoreChanges = diffIgnoreFiles,
        };
        var target = BuildScanDiffTarget(options, "file.cs", FilePathUtils.NormalizePathSeparators($"{basePath}/Test.cs"));
        var res = target.ScanDiff();

        res.ChangedTestFiles.Should().HaveCount(0);
        res.ChangedSourceFiles.Should().HaveCount(1);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_ExcludingTestFilesInDiffIgnoreFiles_Multi_Asterisk()
    {
        var diffIgnoreFiles = new IExclusionPattern[] { new ExclusionPattern("**/Test.cs") };
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
            DiffIgnoreChanges = diffIgnoreFiles,
        };
        var target = BuildScanDiffTarget(options, "file.cs", FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Test.cs"));
        var res = target.ScanDiff();

        res.ChangedTestFiles.Should().HaveCount(0);
        res.ChangedSourceFiles.Should().HaveCount(1);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_ExcludingFilesInDiffIgnoreFiles_Multi_Asterisk()
    {
        var diffIgnoreFiles = new IExclusionPattern[] { new ExclusionPattern("**/file.cs") };
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
            DiffIgnoreChanges = diffIgnoreFiles,
        };
        var target = BuildScanDiffTarget(options, "/c/Users/JohnDoe/Project/file.cs");
        var res = target.ScanDiff();

        res.ChangedSourceFiles.Should().HaveCount(0);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_ShouldCorrectlyAssignTestAndSourceFiles()
    {
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Source");
        var test1Path = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests1");
        var test2Path = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests2");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            TestProjects = new[] { test1Path, test2Path }!,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
        };
        var target = BuildScanDiffTarget(options,
            "file.cs",
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Source/Category/Source1.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/RootFile.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests1/Test.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests2/CategoryA/Test2.cs"));
        var res = target.ScanDiff();

        res.ChangedTestFiles.Should().HaveCount(2);
        res.ChangedSourceFiles.Should().HaveCount(3);
    }

    [Fact]
    public void ScanDiffReturnsListOfFiles_WithoutTestProjects_ShouldCorrectlyAssignTestAndSourceFiles()
    {
        var basePath = FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests1");
        var options = new StrykerOptions
        {
            ProjectPath = basePath,
            SinceTarget = "d670460b4b4aece5915caf5c68d12f560a9fe3e4",
        };
        var target = BuildScanDiffTarget(options,
            "file.cs",
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Source/Category/Source1.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/RootFile.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests1/Test.cs"),
            FilePathUtils.NormalizePathSeparators("/c/Users/JohnDoe/Project/Tests/Tests2/CategoryA/Test2.cs"));
        var res = target.ScanDiff();

        res.ChangedTestFiles.Should().HaveCount(1);
        res.ChangedSourceFiles.Should().HaveCount(4);
    }
}
