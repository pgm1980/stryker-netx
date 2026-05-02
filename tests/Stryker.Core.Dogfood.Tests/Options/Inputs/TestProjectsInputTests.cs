using System.IO;
using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Stryker.Utilities;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class TestProjectsInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new TestProjectsInput();
        target.HelpText.Should().Be(@"Specify the test projects. | default: []");
    }

    [Fact]
    public void ShouldUseDefaultWhenNull()
    {
        var input = new TestProjectsInput { SuppliedInput = null! };
        input.Validate().Should().BeEmpty();
    }

    [Fact]
    public void ShouldIgnoreEmptyString()
    {
        var input = new TestProjectsInput { SuppliedInput = ["", "", ""] };
        input.Validate().Should().BeEmpty();
    }

    [Fact]
    public void ShouldNormalizePaths()
    {
        var paths = new[] { "/c/root/bla/test.csproj" };
        var expected = new[] { Path.GetFullPath(FilePathUtils.NormalizePathSeparators(paths[0])!) };
        var input = new TestProjectsInput { SuppliedInput = paths };

        input.Validate().Should().BeEquivalentTo(expected);
    }
}
