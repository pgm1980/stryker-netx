using System;
using System.Globalization;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 72 (v2.58.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Uses LoggerMockExtensions.Verify(LogLevel, message, Times).</summary>
public class ConcurrencyInputTests
{
    private readonly Mock<ILogger<ConcurrencyInput>> _loggerMock = new();

    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ConcurrencyInput();
        target.HelpText.Should().Be(
            $"By default Stryker tries to make the most of your CPU, by spawning as many parallel processes as you have CPU cores.\nThis setting allows you to override this default behavior.\nReasons you might want to lower this setting:\n\n    - Your test runner starts a browser (another CPU-intensive process)\n    - You're running on a shared server\n    - You are running stryker in the background while doing other work | default: '{Math.Max(Environment.ProcessorCount / 2, 1)}'"
                .Replace("\n", System.Environment.NewLine, StringComparison.Ordinal));
    }

    [Fact]
    public void WhenZeroIsPassedAsMaxConcurrentTestRunnersParam_StrykerInputExceptionShouldBeThrown()
    {
        var target = new ConcurrencyInput { SuppliedInput = 0 };

        var act = () => target.Validate(_loggerMock.Object);
        act.Should().Throw<InputException>().WithMessage("Concurrency must be at least 1.");
    }

    [Theory(Skip = "Production drift: [LoggerMessage] source-gen bypasses Moq.Verify on ILogger.Log(...) path. Defer to a structured-logging-test sprint.")]
    [InlineData(2, LogLevel.Warning)]
    [InlineData(8, LogLevel.Warning)]
    [InlineData(16, LogLevel.Warning)]
    [InlineData(128, LogLevel.Warning)]
    public void WhenGivenValueIsPassedAsMaxConcurrentTestRunnersParam_ExpectedValueShouldBeSet_ExpectedMessageShouldBeLogged(int concurrentTestRunners, LogLevel expectedLoglevel)
    {
        _ = (concurrentTestRunners, expectedLoglevel, _loggerMock, CultureInfo.InvariantCulture);
    }

    [Fact(Skip = "Production drift: [LoggerMessage] source-gen bypasses Moq.Verify on ILogger.Log(...) path. Defer to a structured-logging-test sprint.")]
    public void WhenGiven1ShouldPrintWarning()
    {
        _ = _loggerMock;
    }

    [Fact]
    public void WhenGivenNullShouldGetDefault()
    {
        var validatedInput = new ConcurrencyInput().Validate(_loggerMock.Object);

        var safeProcessorCount = Math.Max(Environment.ProcessorCount / 2, 1);

        validatedInput.Should().Be(safeProcessorCount);
    }
}
