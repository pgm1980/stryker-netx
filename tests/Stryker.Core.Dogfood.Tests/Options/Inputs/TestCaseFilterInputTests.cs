using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class TestCaseFilterInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var input = new TestCaseFilterInput();
        // Cross-platform line-ending tolerance (Sprint 53 lesson)
        input.HelpText.Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("Filters out tests in the project using the given expression.\nUses the syntax for dotnet test --filter option and vstest.console.exe --testcasefilter option.\nFor more information on running selective tests, see https://docs.microsoft.com/en-us/dotnet/core/testing/selective-unit-tests. | default: ''");
    }

    [Fact]
    public void DefaultShouldBeEmpty()
    {
        var input = new TestCaseFilterInput();
        input.Default.Should().BeEmpty();
    }

    [Fact]
    public void ShouldReturnSuppliedInputWhenNotNullOrWhiteSpace()
    {
        var input = new TestCaseFilterInput { SuppliedInput = "Category=Unit" };
        input.Validate().Should().Be("Category=Unit");
    }

    [Fact]
    public void ShouldReturnDefaultWhenSuppliedInputNull()
    {
        var input = new TestCaseFilterInput { SuppliedInput = null! };
        input.Validate().Should().Be("");
    }

    [Fact]
    public void ShouldReturnDefaultWhenSuppliedInputWhiteSpace()
    {
        var input = new TestCaseFilterInput { SuppliedInput = "    " };
        input.Validate().Should().Be("");
    }
}
