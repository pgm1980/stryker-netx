using System;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 53 (v2.39.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class LogToFileInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new LogToFileInput();
        target.HelpText.Should().Be(@"Makes the logger write to a file. Logging to file always uses loglevel trace. | default: 'False'");
    }

    [Fact]
    public void ShouldThrowIfTrueAndNoOutputPath()
    {
        var target = new LogToFileInput { SuppliedInput = true };

        Action act = () => target.Validate(null!);
        act.Should().Throw<InputException>()
            .Which.Message.Should().Be("Output path must be set if log to file is enabled");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(null, false)]
    public void ShouldValidate(bool? input, bool expected)
    {
        var target = new LogToFileInput { SuppliedInput = input };

        var result = target.Validate("TestPath");

        result.Should().Be(expected);
    }
}
