using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Configuration;
using Stryker.Configuration.Options;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 59 (v2.45.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class FilePatternMutantFilterTests
{
    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance",
        Justification = "Test asserts behavior of IMutantFilter interface; perf-not-test-concern.")]
    public void ShouldHaveName()
    {
        var target = new FilePatternMutantFilter(new StrykerOptions()) as IMutantFilter;
        target!.DisplayName.Should().Be("mutate filter");
    }

    [Theory]
    [InlineData(new[] { "**/*" }, "myFolder/MyFile.cs", 5, 10, true)]
    [InlineData(new[] { "**/*File.cs" }, "myFolder/MyFile.cs", 5, 10, true)]
    [InlineData(new[] { "**/*File.cs" }, "myFolder/MyFileSomething.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs", "**/*" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!MyFile.cs" }, "myFolder/MyFile.cs", 5, 10, true)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs{3..13}" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs{1..7}{7..13}" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs{1..3}{5..10}" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs{1..7}" }, "myFolder/MyFile.cs", 5, 10, true)]
    [InlineData(new[] { "**/*", "!**/MyFile.cs{7..13}" }, "myFolder/MyFile.cs", 5, 10, true)]
    [InlineData(new[] { "**/*", "!C:/test/myFolder/MyFile.cs" }, "myFolder/MyFile.cs", 5, 10, false)]
    [InlineData(new[] { "**/*", "!C:\\test\\myFolder\\MyFile.cs" }, "myFolder/MyFile.cs", 5, 10, false)]
    public void FilterMutants_should_filter_included_and_excluded_files(
        string[] patterns,
        string filePath,
        int spanStart,
        int spanEnd,
        bool shouldKeepFile)
    {
        var options = new StrykerOptions { Mutate = patterns.Select(FilePattern.Parse) };
        var file = new CsharpFileLeaf { RelativePath = filePath, FullPath = Path.Combine("C:/test/", filePath) };

        var trivia = Enumerable.Range(0, spanStart).Select(x => SyntaxFactory.Space).ToArray();
        var triviaList = SyntaxFactory.TriviaList(trivia);
        var syntaxToken = SyntaxFactory.Identifier(
            triviaList,
            new string('a', spanEnd - spanStart),
            SyntaxTriviaList.Empty);

        var originalNode = SyntaxFactory.IdentifierName(syntaxToken);
        var mutant = new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = originalNode,
                ReplacementNode = originalNode,
                DisplayName = "test-mutation",
            },
        };

        var sut = new FilePatternMutantFilter(options);

        var result = sut.FilterMutants([mutant], file, null!);

        result.Contains(mutant).Should().Be(shouldKeepFile);
    }
}
