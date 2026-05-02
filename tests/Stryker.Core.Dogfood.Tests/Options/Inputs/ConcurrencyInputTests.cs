using System;
using System.Globalization;
using System.Linq;
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

    [Theory]
    [InlineData(2, LogLevel.Warning)]
    [InlineData(8, LogLevel.Warning)]
    [InlineData(16, LogLevel.Warning)]
    [InlineData(128, LogLevel.Warning)]
    public void WhenGivenValueIsPassedAsMaxConcurrentTestRunnersParam_ExpectedValueShouldBeSet_ExpectedMessageShouldBeLogged(int concurrentTestRunners, LogLevel expectedLoglevel)
    {
        _loggerMock.EnableAllLogLevels();

        var validatedInput = new ConcurrencyInput { SuppliedInput = concurrentTestRunners }.Validate(_loggerMock.Object);

        validatedInput.Should().Be(concurrentTestRunners);

        var safeProcessorCount = Math.Max(Environment.ProcessorCount / 2, 1);

        var formattedMessage = string.Format(CultureInfo.InvariantCulture, "Stryker will use a max of {0} parallel testsessions.", concurrentTestRunners);
        _loggerMock.Verify(LogLevel.Information, formattedMessage, Times.Once);

        if (concurrentTestRunners > safeProcessorCount)
        {
            formattedMessage = string.Format(CultureInfo.InvariantCulture, "Using a concurrency of {0} which is more than recommended {1} for normal system operation. This might have an impact on performance.", concurrentTestRunners, safeProcessorCount);
            _loggerMock.Verify(expectedLoglevel, formattedMessage, Times.Once);
        }
    }

    [Fact]
    public void WhenGiven1ShouldPrintWarning()
    {
        _loggerMock.EnableAllLogLevels();

        var validatedInput = new ConcurrencyInput { SuppliedInput = 1 }.Validate(_loggerMock.Object);

        validatedInput.Should().Be(1);

        _loggerMock.Verify(LogLevel.Information, "Stryker will use a max of 1 parallel testsessions.", Times.Once);
        _loggerMock.Verify(LogLevel.Warning, "Stryker is running in single threaded mode due to concurrency being set to 1.", Times.Once);
    }

#pragma warning disable CA1873
    [Fact]
    public void Debug_CaptureLoggerCalls()
    {
        // FIX: ConcurrencyInput.Validate guards Log with `if (logger.IsEnabled(LogLevel.X))` —
        // Mock<ILogger<T>> returns false by default. Setup IsEnabled to return true.
        _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        var captured = new System.Collections.Generic.List<(LogLevel level, string rendered)>();
        _loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var level = (LogLevel)invocation.Arguments[0];
                var state = invocation.Arguments[2];
                captured.Add((level, state?.ToString() ?? "<null>"));
            }));

        var validatedInput = new ConcurrencyInput { SuppliedInput = 1 }.Validate(_loggerMock.Object);

        validatedInput.Should().Be(1);
        var debug = string.Join("|", captured.Select(c => $"{c.level}={c.rendered}"));
        captured.Should().Contain(c => c.level == LogLevel.Information && c.rendered.Contains("Stryker will use a max of 1 parallel testsessions", StringComparison.Ordinal), debug);
        captured.Should().Contain(c => c.level == LogLevel.Warning && c.rendered.Contains("single threaded mode", StringComparison.Ordinal), debug);
    }
#pragma warning restore CA1873

    [Fact]
    public void WhenGivenNullShouldGetDefault()
    {
        var validatedInput = new ConcurrencyInput().Validate(_loggerMock.Object);

        var safeProcessorCount = Math.Max(Environment.ProcessorCount / 2, 1);

        validatedInput.Should().Be(safeProcessorCount);
    }
}
