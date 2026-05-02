using System.Linq;
using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class IgnoreMethodsInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new IgnoreMethodsInput();
        target.HelpText.Should().Be(@"Ignore mutations on method parameters. | default: []");
    }

    [Fact]
    public void ShouldReturnRegex()
    {
        var target = new IgnoreMethodsInput { SuppliedInput = ["Dispose"] };

        var result = target.Validate().ToList();

        result.Should().ContainSingle();
        result[0].ToString().Should().Be(@"^(?:[^.]*\.)*Dispose(<[^>]*>)?$");
    }

    [Fact]
    public void ShouldReturnMultipleItems()
    {
        var target = new IgnoreMethodsInput { SuppliedInput = ["Dispose", "Test"] };

        var result = target.Validate().ToList();

        result.Count.Should().Be(2);
        result[0].ToString().Should().Be(@"^(?:[^.]*\.)*Dispose(<[^>]*>)?$");
        result[1].ToString().Should().Be(@"^(?:[^.]*\.)*Test(<[^>]*>)?$");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new IgnoreMethodsInput { SuppliedInput = null! };
        target.Validate().Should().BeEmpty();
    }
}
