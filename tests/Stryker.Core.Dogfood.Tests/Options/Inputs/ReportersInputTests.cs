using System.Linq;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 74 (v2.60.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ReportersInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ReportersInput();
        target.HelpText.Should().Be("Reporters inform about various stages in the mutation testrun. | default: ['Progress', 'Html'] | allowed: All, Progress, Dots, ClearText, ClearTextTree, Json, Html, Dashboard, RealTimeDashboard, Markdown, Baseline");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new ReportersInput { SuppliedInput = null!};

        var result = target.Validate(false).ToList();

        result.Should().HaveCount(2);
        result.Should().Contain(Reporter.Progress);
        result.Should().Contain(Reporter.Html);
    }

    [Fact]
    public void ShouldReturnReporter()
    {
        var target = new ReportersInput { SuppliedInput = ["Html"] };

        var result = target.Validate(false);

        result.Should().ContainSingle().Which.Should().Be(Reporter.Html);
    }

    [Fact]
    public void ShouldReturnReporters()
    {
        var target = new ReportersInput
        {
            SuppliedInput =
            [
                Reporter.Html.ToString(),
                Reporter.Json.ToString(),
                Reporter.Progress.ToString(),
                Reporter.Baseline.ToString(),
                Reporter.ClearText.ToString(),
                Reporter.ClearTextTree.ToString(),
                Reporter.Dashboard.ToString(),
                Reporter.Dots.ToString(),
            ],
        };

        var result = target.Validate(false).ToList();

        result.Should().HaveCount(8);
        result.Should().Contain(Reporter.Html);
        result.Should().Contain(Reporter.Json);
        result.Should().Contain(Reporter.Progress);
        result.Should().Contain(Reporter.Baseline);
        result.Should().Contain(Reporter.ClearText);
        result.Should().Contain(Reporter.ClearTextTree);
        result.Should().Contain(Reporter.Dashboard);
        result.Should().Contain(Reporter.Dots);
    }

    [Fact]
    public void ShouldValidateReporters()
    {
        var target = new ReportersInput { SuppliedInput = ["Gibberish", "Test"] };

        var act = () => target.Validate(false);

        act.Should().Throw<InputException>()
            .WithMessage("These reporter values are incorrect: Gibberish, Test.");
    }

    [Fact]
    public void ShouldEnableBaselineReporterWhenWithBaselineEnabled()
    {
        var target = new ReportersInput { SuppliedInput = null!};

        var validatedReporters = target.Validate(withBaseline: true);

        validatedReporters.Should().Contain(Reporter.Baseline);
    }
}
