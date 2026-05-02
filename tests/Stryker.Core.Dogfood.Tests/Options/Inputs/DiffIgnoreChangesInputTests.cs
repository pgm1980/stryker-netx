using System.Linq;
using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 71 (v2.57.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class DiffIgnoreChangesInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new DiffIgnoreChangesInput();
        target.HelpText.Should().Be(
            "Allows to specify an array of C# files which should be ignored if present in the diff.\nAny non-excluded files will trigger all mutants to be tested because we cannot determine what mutants are affected by these files.\nThis feature is only recommended when you are sure these files will not affect results, or when you are prepared to sacrifice accuracy for performance.\n\nUse glob syntax for wildcards: https://en.wikipedia.org/wiki/Glob_(programming)\nExample: ['**/*Assets.json','**/favicon.ico'] | default: []"
                .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldAcceptGlob()
    {
        var target = new DiffIgnoreChangesInput { SuppliedInput = ["*"] };

        var result = target.Validate();

        result.Should().ContainSingle().Which.Glob.ToString().Should().Be("*");
    }

    [Fact]
    public void ShouldParseAll()
    {
        var target = new DiffIgnoreChangesInput { SuppliedInput = ["*", "MyFile.cs"] };

        var result = target.Validate().ToList();

        result.Should().HaveCount(2);
        result[0].Glob.ToString().Should().Be("*");
        result[1].Glob.ToString().Should().Be("MyFile.cs");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new DiffIgnoreChangesInput { SuppliedInput = null! };

        var result = target.Validate();

        result.Should().BeEmpty();
    }
}
