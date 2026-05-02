using System;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.TestProjects;

/// <summary>Sprint 76 (v2.62.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: TestFile.SyntaxTree is now a required init member (Sprint 2 modernization).</summary>
public class TestFileTests
{
    [Fact]
    public void MergeTestFiles()
    {
        var testCase1Id = Guid.NewGuid().ToString();
        var node = SyntaxFactory.Block();
        var syntaxTree = CSharpSyntaxTree.ParseText("class C{}");
        var fileA = new TestFile
        {
            SyntaxTree = syntaxTree,
            FilePath = "/c/",
            Source = "bla",
        };
        fileA.AddTest(testCase1Id, "test1", node);
        var fileB = new TestFile
        {
            SyntaxTree = syntaxTree,
            FilePath = "/c/",
            Source = "bla",
        };
        fileB.AddTest(testCase1Id, "test1", node);

        fileA.Should().Be(fileB);
        fileA.GetHashCode().Should().Be(fileB.GetHashCode());
    }
}
