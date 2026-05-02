using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 67 (v2.53.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ProjectVersionInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ProjectVersionInput();
        target.HelpText.Should().Be("Project version used in dashboard reporter and baseline feature. | default: ''");
    }

    [Fact]
    public void ProjectVersion_UsesSuppliedInput_IfDashboardReporterEnabled()
    {
        var suppliedInput = "test";
        var input = new ProjectVersionInput { SuppliedInput = suppliedInput };

        var result = input.Validate(reporters: [Reporter.Dashboard], withBaseline: false);
        result.Should().Be(suppliedInput);
    }

    [Fact]
    public void ProjectVersion_UsesSuppliedInput_IfBaselineEnabled()
    {
        var suppliedInput = "test";
        var input = new ProjectVersionInput { SuppliedInput = suppliedInput };

        var result = input.Validate(reporters: [], withBaseline: true);
        result.Should().Be(suppliedInput);
    }

    [Fact]
    public void ProjectVersion_ShouldBeDefault_IfBaselineAndDashboardDisabled()
    {
        var suppliedInput = "test";
        var input = new ProjectVersionInput { SuppliedInput = suppliedInput };

        var result = input.Validate(reporters: [], withBaseline: false);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ProjectVersion_ShouldBeDefault_IfDashboardEnabledAndSuppliedInputNull()
    {
        var input = new ProjectVersionInput();

        var result = input.Validate(reporters: [Reporter.Dashboard], withBaseline: false);
        result.Should().BeEmpty();
    }

    [Fact]
    public void ProjectVersion_CannotBeEmpty_WhenBaselineEnabled()
    {
        var input = new ProjectVersionInput();

        var act = () => input.Validate(reporters: [], withBaseline: true);

        act.Should().Throw<InputException>().WithMessage("Project version cannot be empty when baseline is enabled");
    }
}
