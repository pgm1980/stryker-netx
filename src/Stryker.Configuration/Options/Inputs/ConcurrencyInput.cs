using System;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Exceptions;
using Stryker.Utilities.Logging;

namespace Stryker.Configuration.Options.Inputs;

public partial class ConcurrencyInput : Input<int?>
{
    protected override string Description => @"By default Stryker tries to make the most of your CPU, by spawning as many parallel processes as you have CPU cores.
This setting allows you to override this default behavior.
Reasons you might want to lower this setting:

    - Your test runner starts a browser (another CPU-intensive process)
    - You're running on a shared server
    - You are running stryker in the background while doing other work";

    public override int? Default => Math.Max(Environment.ProcessorCount / 2, 1);

    public int Validate(ILogger<ConcurrencyInput>? logger = null)
    {
        logger ??= ApplicationLogging.LoggerFactory.CreateLogger<ConcurrencyInput>();

        if (SuppliedInput is null)
        {
            if (Environment.ProcessorCount < 1)
            {
                LogProcessorCountUnknown(logger);
            }

            return Default ?? 1;
        }

        if (SuppliedInput < 1)
        {
            throw new InputException("Concurrency must be at least 1.");
        }

        if (SuppliedInput > Default && logger.IsEnabled(LogLevel.Warning))
        {
            LogConcurrencyAboveRecommended(logger, SuppliedInput.Value, Default ?? 1);
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            LogParallelTestSessions(logger, SuppliedInput.Value);
        }

        if (SuppliedInput is 1)
        {
            LogSingleThreadedMode(logger);
        }

        return SuppliedInput.Value;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Processor count is not reported by the system, using concurrency of 1. Set a concurrency to remove this warning.")]
    private static partial void LogProcessorCountUnknown(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Using a concurrency of {Concurrency} which is more than recommended {SafeConcurrencyCount} for normal system operation. This might have an impact on performance.")]
    private static partial void LogConcurrencyAboveRecommended(ILogger logger, int concurrency, int safeConcurrencyCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stryker will use a max of {Concurrency} parallel testsessions.")]
    private static partial void LogParallelTestSessions(ILogger logger, int concurrency);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stryker is running in single threaded mode due to concurrency being set to 1.")]
    private static partial void LogSingleThreadedMode(ILogger logger);
}
