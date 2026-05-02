using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.TestProjects;

/// <summary>Sprint 80 (v2.66.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: TestHelper.SetupProjectAnalyzerResult does not have a preprocessorSymbols
/// parameter (Sprint 25 helper) — instead pass via properties["DefineConstants"] which our
/// IProjectAnalysisCSharpExtensions.GetPreprocessorSymbols() reads (semicolon-separated).</summary>
public class TestProjectTests
{
    [Fact]
    public void TestProjectEqualsWhenAllPropertiesEqual()
    {
        var fileSystem = new MockFileSystem();
        var rootPath = Path.Combine("c", "TestProject");
        var fileAPath = Path.Combine(rootPath, "ExampleTestFileA.cs");
        fileSystem.AddDirectory(rootPath);
        var fileA = File.ReadAllText(Path.Combine(".", "TestResources", "ExampleTestFileA.cs"));
        fileSystem.AddFile(fileAPath, new MockFileData(fileA));
        var testProjectAnalyzerResultMock = TestHelper.SetupProjectAnalyzerResult(
            references: [],
            sourceFiles: [fileAPath]);

        var testProjectA = new TestProject(fileSystem, testProjectAnalyzerResultMock.Object);
        var testProjectB = new TestProject(fileSystem, testProjectAnalyzerResultMock.Object);

        testProjectA.Should().Be(testProjectB);
        testProjectA.GetHashCode().Should().Be(testProjectB.GetHashCode());
    }

    [Fact]
    public void TestProjectsNotEqualWhenAnalyzerResultsNotEqual()
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
        var testProjectAnalyzerResultAMock = TestHelper.SetupProjectAnalyzerResult(
            references: [],
            sourceFiles: [fileAPath]);
        var testProjectAnalyzerResultBMock = TestHelper.SetupProjectAnalyzerResult(
            references: [],
            sourceFiles: [fileBPath]);

        var testProjectA = new TestProject(fileSystem, testProjectAnalyzerResultAMock.Object);
        var testProjectB = new TestProject(fileSystem, testProjectAnalyzerResultBMock.Object);

        testProjectA.Should().NotBe(testProjectB);
    }

    [Fact]
    public void TestProject_ParseTestFile_WithCsharpParseOptions()
    {
        var fileSystem = new MockFileSystem();
        var rootPath = Path.Combine("c", "TestProject");
        var filePath = Path.Combine(rootPath, "ExampleTestFilePreprocessorSymbols.cs");
        fileSystem.AddDirectory(rootPath);

        var testProjectAnalyzerResultMock = TestHelper.SetupProjectAnalyzerResult(
            references: [],
            sourceFiles: [filePath],
            properties: new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "DefineConstants", "NET6_0_OR_GREATER" },
            });
        var file = File.ReadAllText(Path.Combine(".", "TestResources", "ExampleTestFilePreprocessorSymbols.cs"));
        fileSystem.AddFile(filePath, new MockFileData(file));

        var testProject = new TestProject(fileSystem, testProjectAnalyzerResultMock.Object);

        testProject.TestFiles.First().SyntaxTree.GetRoot().DescendantNodes()
            .Count(n => n is MethodDeclarationSyntax).Should().Be(4);
    }
}
