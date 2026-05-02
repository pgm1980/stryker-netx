using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 70 (v2.56.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class OpenReportInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new OpenReportInput();
        target.HelpText.Should().Be("When this option is passed, generated reports should open in the browser automatically once Stryker starts testing mutants, and will update the report till Stryker is done. Both html and dashboard reports can be opened automatically. | default: 'Html' | allowed: Html, Dashboard");
    }

    [Fact]
    public void ShouldHaveDefaultHtml()
    {
        var target = new OpenReportInput { SuppliedInput = null! };
        var result = target.Validate(true);

        result.Should().Be(ReportType.Html);
    }

    [Fact]
    public void ShouldReturnReportType()
    {
        var target = new OpenReportInput { SuppliedInput = "Html" };
        var result = target.Validate(true);

        result.Should().Be(ReportType.Html);
    }

    [Fact]
    public void ShouldValidateReportType()
    {
        var target = new OpenReportInput { SuppliedInput = "gibberish" };
        var act = () => target.Validate(true);

        act.Should().Throw<InputException>()
            .WithMessage("The given report type (gibberish) is invalid. Valid options are: [Html, Dashboard]");
    }
}
