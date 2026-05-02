using FluentAssertions;
using Serilog.Events;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 63 (v2.49.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class VerbosityInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new VerbosityInput();
        target.HelpText.Should().Be("The verbosity (loglevel) for output to the console. | default: 'info' | allowed: error, warning, info, debug, trace");
    }

    [Fact]
    public void ShouldBeInformationWhenNull()
    {
        var input = new VerbosityInput { SuppliedInput = null! };
        var validatedInput = input.Validate();

        validatedInput.Should().Be(LogEventLevel.Information);
    }

    [Theory]
    [InlineData("error", LogEventLevel.Error)]
    [InlineData("warning", LogEventLevel.Warning)]
    [InlineData("info", LogEventLevel.Information)]
    [InlineData("debug", LogEventLevel.Debug)]
    [InlineData("trace", LogEventLevel.Verbose)]
    public void ShouldTranslateLogLevelToLogEventLevel(string argValue, LogEventLevel expectedLogLevel)
    {
        var validatedInput = new VerbosityInput { SuppliedInput = argValue }.Validate();

        validatedInput.Should().Be(expectedLogLevel);
    }

    [Theory]
    [InlineData("incorrect")]
    [InlineData("")]
    public void ShouldThrowWhenInputCannotBeTranslated(string logLevel)
    {
        var act = () => new VerbosityInput { SuppliedInput = logLevel }.Validate();

        act.Should().Throw<InputException>()
            .WithMessage($"Incorrect verbosity ({logLevel}). The verbosity options are [Trace, Debug, Info, Warning, Error]");
    }
}
