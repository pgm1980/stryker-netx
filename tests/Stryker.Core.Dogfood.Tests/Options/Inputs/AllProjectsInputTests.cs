using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>
/// Sprint 150 (ADR-031, Bug #8 from Calculator-Tester Bug-Report 4): unit tests for
/// the new <see cref="AllProjectsInput"/> flag (mutate ALL referenced source projects
/// in one run instead of throwing the multi-reference disambiguation exception).
/// </summary>
public class AllProjectsInputTests
{
    [Fact]
    public void ShouldHaveDefaultFalse()
    {
        var target = new AllProjectsInput();
        target.Default.Should().Be(false);
    }

    [Theory]
    [InlineData(null, false)]    // not supplied → default false
    [InlineData(false, false)]   // explicitly disabled
    [InlineData(true, true)]     // explicitly enabled
    public void ShouldValidate(bool? supplied, bool expected)
    {
        var target = new AllProjectsInput { SuppliedInput = supplied };

        var result = target.Validate();

        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new AllProjectsInput();
        target.HelpText.Should().Contain("Mutate ALL source projects",
            "the help text must surface the user-visible behaviour");
        target.HelpText.Should().Contain("Clean-Architecture",
            "the help text should mention the canonical use case");
    }
}
