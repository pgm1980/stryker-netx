#pragma warning disable IDE0028 // suppress: List initializer cast to IEnumerable for GetEnumerator (collection-expression breaks target-type inference)
using System;
using System.Collections.Generic;
using FluentAssertions;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.DashboardCompare;

/// <summary>Sprint 102 (v2.88.0) full upstream port from
/// src/Stryker.Core/Stryker.Core.UnitTest/DashboardCompare/GitInfoProviderTests.cs (replaces
/// Sprint 93 placeholder). Production GitInfoProvider uses LibGit2Sharp IRepository directly,
/// mocked via Moq with Mock&lt;IRepository&gt; + Mock&lt;BranchCollection&gt; + Mock&lt;Branch&gt; pattern.
/// MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class GitInfoProviderTests : TestBase
{
    [Fact]
    public void WhenProvidedReturnsRepositoryPath()
    {
        var repository = new Mock<IRepository>(MockBehavior.Strict);

        var options = new StrykerOptions { Since = true };
        var target = new GitInfoProvider(options, repository.Object, "path", Mock.Of<ILogger<GitInfoProvider>>());

        target.RepositoryPath.Should().Be("path");
    }

    [Fact]
    public void DoesNotCheckForRepositoryPathWhenDisabled()
    {
        var repository = new Mock<IRepository>(MockBehavior.Strict);

        var options = new StrykerOptions { Since = false };
        var target = new GitInfoProvider(options, repository.Object);

        target.Repository.Should().BeNull();
    }

    [Fact]
    public void DoesNotCreateNewRepositoryWhenPassedIntoConstructor()
    {
        var options = new StrykerOptions { ProjectPath = "C:\\" };
        var repository = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        branchCollectionMock.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<Branch>(MockBehavior.Loose).Object);
        repository.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        Action act = () => _ = new GitInfoProvider(options, repository.Object);

        act.Should().NotThrow();
    }

    [Fact]
    public void ThrowsExceptionIfNoCurrentBranchOrProjectVersionSet()
    {
        var options = new StrykerOptions();
        var repository = new Mock<IRepository>(MockBehavior.Loose);
        var target = new GitInfoProvider(options, repository.Object);

        Action act = () => target.GetCurrentBranchName();

        act.Should().Throw<InputException>();
    }

    [Fact]
    public void ReturnsCurrentBranch()
    {
        var options = new StrykerOptions { Since = true };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();

        branchCollectionMock.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<Branch>(MockBehavior.Loose).Object);
        branchMock.SetupGet(x => x.IsCurrentRepositoryHead).Returns(true);
        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchCollectionMock
            .Setup(x => x.GetEnumerator())
            .Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var res = target.GetCurrentBranchName();

        res.Should().Be("master");
        repositoryMock.Verify();
    }

    [Fact]
    public void ReturnsCurrentBranchWhenMultipleBranches()
    {
        var options = new StrykerOptions { Since = true };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();
        var branchMock2 = new Mock<Branch>();

        branchCollectionMock.Setup(x => x.Add(It.IsAny<string>(), It.IsAny<string>())).Returns(new Mock<Branch>(MockBehavior.Loose).Object);
        branchMock.SetupGet(x => x.IsCurrentRepositoryHead).Returns(true);
        branchMock2.SetupGet(x => x.IsCurrentRepositoryHead).Returns(false);
        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchMock2.SetupGet(x => x.FriendlyName).Returns("dev");
        branchCollectionMock
            .Setup(x => x.GetEnumerator())
            .Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var res = target.GetCurrentBranchName();

        res.Should().Be("master");
        repositoryMock.Verify();
    }

    [Fact]
    public void CreateRepository_Throws_InputException_When_RepositoryPath_Empty()
    {
        Action act = () => _ = new GitInfoProvider(new StrykerOptions { Since = true }, repositoryPath: string.Empty);

        act.Should().Throw<InputException>()
            .WithMessage("Could not locate git repository. Unable to determine git diff to filter mutants. Did you run inside a git repo? If not please disable the 'since' feature.");
    }

    [Fact]
    public void DetermineCommitThrowsStrykerInputException()
    {
        var options = new StrykerOptions { Since = true, SinceTarget = "main" };
        var repository = new Mock<IRepository>();

        var branchCollectionMock = new Mock<BranchCollection>();
        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch>()).GetEnumerator());
        repository.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var tagCollectionMock = new Mock<TagCollection>();
        tagCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Tag>)new List<Tag>()).GetEnumerator());
        repository.SetupGet(x => x.Tags).Returns(tagCollectionMock.Object);

        var target = new GitInfoProvider(options, repository.Object);
        Action act = () => target.DetermineCommit();

        act.Should().Throw<InputException>();
    }

    [Fact]
    public void LooksUpCommitWhenGitSourceIsFortyCharacters()
    {
        var sha = "5a6940131b31f6958007ecbc0c51cbc35177f4e0";
        var options = new StrykerOptions { Since = true, SinceTarget = sha };
        var commitMock = new Mock<Commit>();
        var repositoryMock = new Mock<IRepository>();
        var branchCollectionMock = new Mock<BranchCollection>();
        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch>()).GetEnumerator());

        var tagCollectionMock = new Mock<TagCollection>();
        tagCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Tag>)new List<Tag>()).GetEnumerator());

        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);
        repositoryMock.SetupGet(x => x.Tags).Returns(tagCollectionMock.Object);
        repositoryMock.Setup(x => x.Lookup(It.IsAny<ObjectId>())).Returns(commitMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var result = target.DetermineCommit();

        result.Should().NotBeNull();
        repositoryMock.Verify(x => x.Lookup(It.Is<ObjectId>(o => o.Sha == sha)), Times.Once);
    }

    [Fact]
    public void ReturnsTip_When_Canonical_Name_Is_GitSource()
    {
        var options = new StrykerOptions { Since = true, SinceTarget = "origin/master" };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();
        var commitMock = new Mock<Commit>();

        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchMock.SetupGet(x => x.CanonicalName).Returns("origin/master");
        branchMock.SetupGet(x => x.UpstreamBranchCanonicalName).Returns("refs/heads/master");
        branchMock.SetupGet(x => x.Tip).Returns(commitMock.Object);

        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var res = target.DetermineCommit();

        res.Should().NotBeNull();
        res.Should().BeSameAs(commitMock.Object);
        repositoryMock.Verify();
    }

    [Fact]
    public void GetTargetCommit_Does_Not_Throw_NullReferenceException_When_Branch_Is_Null()
    {
        var options = new StrykerOptions { Since = true, SinceTarget = "origin/master" };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();

        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var tagCollectionMock = new Mock<TagCollection>(MockBehavior.Strict);
        var tagMock = new Mock<Tag>();
        tagCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Tag>)new List<Tag> { tagMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Tags).Returns(tagCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        Action act = () => target.DetermineCommit();

        act.Should().Throw<InputException>();
    }

    [Fact]
    public void ReturnsTip_When_Friendly_Name_Is_GitSource()
    {
        var options = new StrykerOptions { Since = true, SinceTarget = "master" };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();
        var commitMock = new Mock<Commit>();

        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchMock.SetupGet(x => x.CanonicalName).Returns("origin/master");
        branchMock.SetupGet(x => x.UpstreamBranchCanonicalName).Returns("refs/heads/master");
        branchMock.SetupGet(x => x.Tip).Returns(commitMock.Object);

        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var res = target.DetermineCommit();

        res.Should().NotBeNull();
        res.Should().BeSameAs(commitMock.Object);
        repositoryMock.Verify();
    }

    [Fact]
    public void ReturnsTip_When_Upstream_Branch_Canonical_Name_Is_GitSource()
    {
        var options = new StrykerOptions { Since = true, SinceTarget = "refs/heads/master" };
        var repositoryMock = new Mock<IRepository>(MockBehavior.Strict);
        var branchCollectionMock = new Mock<BranchCollection>(MockBehavior.Strict);
        var branchMock = new Mock<Branch>();
        var commitMock = new Mock<Commit>();

        branchMock.SetupGet(x => x.FriendlyName).Returns("master");
        branchMock.SetupGet(x => x.CanonicalName).Returns("origin/master");
        branchMock.SetupGet(x => x.UpstreamBranchCanonicalName).Returns("refs/heads/master");
        branchMock.SetupGet(x => x.Tip).Returns(commitMock.Object);

        branchCollectionMock.Setup(x => x.GetEnumerator()).Returns(((IEnumerable<Branch>)new List<Branch> { branchMock.Object }).GetEnumerator());
        repositoryMock.SetupGet(x => x.Branches).Returns(branchCollectionMock.Object);

        var target = new GitInfoProvider(options, repositoryMock.Object);
        var res = target.DetermineCommit();

        res.Should().NotBeNull();
        res.Should().BeSameAs(commitMock.Object);
        repositoryMock.Verify();
    }
}
