using System;
using System.Linq;
using DotNet.Globbing;
using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Stryker.Configuration;
using Stryker.Utilities;
using Xunit;

namespace Stryker.Core.Dogfood.Tests;

/// <summary>Sprint 58 (v2.44.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class FilePatternTests
{
    [Theory]
    [InlineData("file.cs", "file.cs", true)]
    [InlineData("TestFolder/file.cs", "**/file.cs", true)]
    [InlineData("C:\\TestFolder\\file.cs", "**/file.cs", true)]
    [InlineData("C:\\TestFolder\\file.cs", "file.cs", false)]
    [InlineData("C:\\TestFolder\\file.cs", "./file.cs", false)]
    [InlineData("file.cs", "File.cs", false)]
    [InlineData("differentFile.cs", "file.cs", false)]
    [InlineData("File.cs", "*File.cs", true)]
    [InlineData("FileFile.cs", "*File.cs", true)]
    [InlineData("FileDifferent.cs", "*File.cs", false)]
    public void IsMatch_should_match_glob_pattern(string file, string glob, bool isMatch)
    {
        var textSpan = new TextSpan(0, 1);
        var sut = new FilePattern(Glob.Parse(glob), false, [textSpan]);

        var result = sut.IsMatch(file, textSpan);

        result.Should().Be(isMatch);
    }

    [Theory]
    [InlineData("{10..20}", 14, 16, true)]
    [InlineData("{10..20}", 04, 06, false)]
    [InlineData("{10..20}", 24, 26, false)]
    [InlineData("{10..20}", 14, 26, false)]
    [InlineData("{10..20}", 04, 16, false)]
    [InlineData("{10..20}{20..30}", 15, 25, true)]
    [InlineData("{10..23}{17..30}", 15, 25, true)]
    [InlineData("{10..19}{20..30}", 15, 25, false)]
    public void IsMatch_should_match_textSpans(string spanPattern, int spanStart, int spanEnd, bool isMatch)
    {
        var sut = FilePattern.Parse("*.*" + spanPattern);

        var result = sut.IsMatch("test.cs", TextSpan.FromBounds(spanStart, spanEnd));

        result.Should().Be(isMatch);
    }

    [Theory]
    [InlineData("**/*.cs{10..20}", "**/*.cs", false, new[] { 10, 20 })]
    [InlineData("**/*.cs{10..20}{20..30}", "**/*.cs", false, new[] { 10, 30 })]
    [InlineData("**/*.cs{10..20}{30..40}", "**/*.cs", false, new[] { 10, 20, 30, 40 })]
    [InlineData("!**/*.cs", "**/*.cs", true, new[] { 0, int.MaxValue })]
    public void Parse_should_parse_correctly(string spanPattern, string glob, bool isExclude, int[] spans)
    {
        var textSpans = Enumerable.Range(0, spans.Length)
            .GroupBy(i => Math.Floor(i / 2d))
            .Select(x => TextSpan.FromBounds(spans[x.First()], spans[x.Skip(1).First()]));

        var result = FilePattern.Parse(spanPattern);

        result.Glob.ToString().Should().Be(FilePathUtils.NormalizePathSeparators(glob));
        result.IsExclude.Should().Be(isExclude);
        result.TextSpans.SequenceEqual(textSpans).Should().BeTrue();
    }
}
