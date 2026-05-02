using System.IO;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 68 (v2.54.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ReportFileNameInputTests
{
    private const string DefaultName = "mutation-report";

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ReportFileNameInput();
        target.HelpText.Should().Be($" | default: '{target.Default}'");
    }

    [Fact]
    public void ShouldDefaultToMutationReportFilenameIfEmptyString()
    {
        var target = new ReportFileNameInput { SuppliedInput = string.Empty };
        var result = target.Validate();

        result.Should().Be(DefaultName);
    }

    [Fact]
    public void ShouldUseDefaultFilenameIfNull()
    {
        var target = new ReportFileNameInput { SuppliedInput = null! };
        var result = target.Validate();

        result.Should().Be(DefaultName);
    }

    [Fact]
    public void ShouldDefaultToMutationReportFilenameIfWhitespace()
    {
        var target = new ReportFileNameInput { SuppliedInput = "  " };
        var result = target.Validate();

        result.Should().Be(DefaultName);
    }

    [Fact]
    public void ShouldNotAllowInvalidFilenames()
    {
        var target = new ReportFileNameInput { SuppliedInput = new string(Path.GetInvalidFileNameChars()) };
        var act = () => target.Validate();
        act.Should().Throw<InputException>();
    }

    [Fact]
    public void ShouldStripHtmlAndJsonFileExtensions()
    {
        var target = new ReportFileNameInput { SuppliedInput = $"{DefaultName}.html" };
        target.Validate().Should().Be(DefaultName);

        target = new ReportFileNameInput { SuppliedInput = $"{DefaultName}.json" };
        target.Validate().Should().Be(DefaultName);
    }

    [Fact]
    public void ShouldNotStripNoneHtmlAndJsonFileExtensions()
    {
        var input = $"{DefaultName}.project";
        var target = new ReportFileNameInput { SuppliedInput = input };
        target.Validate().Should().Be(input);
    }
}
