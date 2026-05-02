using System.IO;
using FluentAssertions;
using Stryker.Core.Baseline.Utils;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Json.SourceFiles;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Baseline.Utils;

/// <summary>Sprint 83 (v2.69.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BaselineMutantHelperTests
{
    private static string LoadExampleSourceFile()
    {
        var path = Path.Combine("TestResources", "ExampleSourceFile.cs");
        return File.ReadAllText(path);
    }

    [Fact]
    public void GetMutantSourceShouldReturnMutantSource()
    {
        var source = LoadExampleSourceFile();
        var jsonMutant = new JsonMutant
        {
            Location = new Location
            {
                Start = new Position { Column = 17, Line = 16 },
                End = new Position { Column = 62, Line = 16 },
            },
        };

        var target = new BaselineMutantHelper();

        var result = target.GetMutantSourceCode(source, jsonMutant);

        result.Should().Be("return Fibonacci(b, a + b, counter + 1, len);");
    }

    [Fact]
    public void GetMutantSourceShouldReturnMutantSource_When_Multiple_Lines()
    {
        var source = LoadExampleSourceFile();
        var jsonMutant = new JsonMutant
        {
            Location = new Location
            {
                Start = new Position { Column = 13, Line = 23 },
                End = new Position { Column = 38, Line = 25 },
            },
        };

        var target = new BaselineMutantHelper();

        var result = target.GetMutantSourceCode(source, jsonMutant);

        // Use the source file's own line-ending convention (test resource is copied with PreserveNewest
        // so it carries CRLF on Windows checkout, LF on Linux checkout — match what's actually on disk).
        var nl = source.Contains("\r\n", System.StringComparison.Ordinal) ? "\r\n" : "\n";
        result.Should().Be($"return @\"Lorem Ipsum{nl}                    Dolor Sit Amet{nl}                    Lorem Dolor Sit\";");
    }

    [Fact]
    public void GetMutantSource_Gets_Partial_Line()
    {
        var source = LoadExampleSourceFile();
        var jsonMutant = new JsonMutant
        {
            Location = new Location
            {
                Start = new Position { Column = 30, Line = 32 },
                End = new Position { Column = 34, Line = 32 },
            },
        };

        var target = new BaselineMutantHelper();

        var result = target.GetMutantSourceCode(source, jsonMutant);

        result.Should().Be("\"\\n\"");
    }
}
