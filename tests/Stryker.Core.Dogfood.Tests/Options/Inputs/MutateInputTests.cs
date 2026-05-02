using System.IO;
using System.Linq;
using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 63 (v2.49.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MutateInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new MutateInput();
        target.HelpText.Should().Be(
            "Allows to specify file that should in- or excluded for the mutations.\n    Use glob syntax for wildcards: https://en.wikipedia.org/wiki/Glob_(programming)\n    Use '!' at the start of a pattern to exclude all matched files.\n    Use '{<start>..<end>}' at the end of a pattern to specify spans of text in files to in- or exclude.\n    Example: ['**/*Service.cs','!**/MySpecialService.cs', '**/MyOtherService.cs{1..10}{32..45}'] | default: ['**/*']"
                .Replace("\n", System.Environment.NewLine, System.StringComparison.Ordinal));
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new MutateInput { SuppliedInput = [] };

        var result = target.Validate();

        var item = result.Should().ContainSingle().Subject;
        item.Glob.ToString().Should().Be(Path.Combine("**", "*"));
        item.IsExclude.Should().BeFalse();
    }

    [Fact]
    public void ShouldReturnFiles()
    {
        var target = new MutateInput { SuppliedInput = [Path.Combine("**", "*.cs")] };

        var result = target.Validate();

        var item = result.Should().ContainSingle().Subject;
        item.Glob.ToString().Should().Be(Path.Combine("**", "*.cs"));
        item.IsExclude.Should().BeFalse();
    }

    [Fact]
    public void ShouldExcludeAll()
    {
        var target = new MutateInput { SuppliedInput = ["!" + Path.Combine("**", "Test.cs")] };

        var result = target.Validate();

        var resultList = result.ToList();
        resultList.Should().HaveCount(2);
        resultList[0].Glob.ToString().Should().Be(Path.Combine("**", "Test.cs"));
        resultList[0].IsExclude.Should().BeTrue();
        resultList[1].Glob.ToString().Should().Be(Path.Combine("**", "*"));
        resultList[1].IsExclude.Should().BeFalse();
    }
}
