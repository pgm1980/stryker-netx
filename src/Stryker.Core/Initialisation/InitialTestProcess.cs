using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;

namespace Stryker.Core.Initialisation;

public partial class InitialTestProcess(ILogger<InitialTestProcess> logger) : IInitialTestProcess
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public ITimeoutValueCalculator TimeoutValueCalculator { get; private set; } = null!; // initialized via InitialTest() before any other call

    /// <summary>
    /// Executes the initial test run using the given testrunner
    /// </summary>
    /// <param name="project"></param>
    /// <param name="testRunner"></param>
    /// <param name="options">Stryker options</param>
    /// <returns>The duration of the initial test run</returns>
    public async Task<InitialTestRun> InitialTestAsync(IStrykerOptions options, IProjectAndTests project, ITestRunner testRunner)
    {
        // Setup a stopwatch to record the initial test duration
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var initTestRunResult = await testRunner.InitialTestAsync(project).ConfigureAwait(false);
        // Stop stopwatch immediately after test run
        stopwatch.Stop();

        // timings
        LogInitialTestRunOutput(_logger, initTestRunResult.ResultMessage);

        TimeoutValueCalculator = new TimeoutValueCalculator(options.AdditionalTimeout,
            (int)stopwatch.ElapsedMilliseconds,
            (int)initTestRunResult.Duration.TotalMilliseconds);

        return new InitialTestRun(initTestRunResult, TimeoutValueCalculator);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initial test run output: {ResultMessage}.")]
    private static partial void LogInitialTestRunOutput(ILogger logger, string resultMessage);
}
