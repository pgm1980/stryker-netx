using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Testing;
using Stryker.TestRunner.Results;
using static Stryker.Abstractions.Testing.ITestRunner;

namespace Stryker.Core.MutationTest;

public partial class MutationTestExecutor : IMutationTestExecutor
{
    // Test runner can't be set in the constructor because it is determined at runtime.
    public ITestRunner TestRunner { get; set; } = null!; // initialized after construction
    private ILogger Logger { get; }

    public MutationTestExecutor(ILogger<MutationTestExecutor> logger)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task TestAsync(IProjectAndTests project, IList<IMutant> mutantsToTest, ITimeoutValueCalculator timeoutMs,
        TestUpdateHandler updateHandler)
    {
        var forceSingle = false;
        while (mutantsToTest.Any())
        {
            var result = await RunTestSessionAsync(project, mutantsToTest, timeoutMs, updateHandler, forceSingle).ConfigureAwait(false);

            if (Logger.IsEnabled(LogLevel.Debug))
            {
                var displayNames = string.Join(", ", mutantsToTest.Select(x => x.DisplayName));
                var status = result.FailingTests.Count == 0 ? "success" : "failed";
                LogTestRun(Logger, displayNames, status);
            }

            if (result.Messages is not null && result.Messages.Any() && Logger.IsEnabled(LogLevel.Trace))
            {
                var displayNames = string.Join(", ", mutantsToTest.Select(x => x.DisplayName));
                var messages = string.Join("", result.Messages);
                LogMessages(Logger, displayNames, Environment.NewLine, messages);
            }

            var remainingMutants = mutantsToTest.Where((m) => m.ResultStatus == MutantStatus.Pending).ToList();
            if (remainingMutants.Count == mutantsToTest.Count)
            {
                // the test failed to get any conclusive results
                if (!result.SessionTimedOut)
                {
                    // something bad happened.
                    LogStrykerFailedToTest(Logger, remainingMutants.Count);
                    return;
                }

                // test session's results have been corrupted by the time out
                // we retry and run tests one by one, if necessary
                if (remainingMutants.Count == 1)
                {
                    // only one mutant was tested, we mark it as timeout.
                    remainingMutants[0].ResultStatus = MutantStatus.Timeout;
                    remainingMutants.Clear();
                }
                else
                {
                    // we don't know which tests timed out, we rerun all tests in dedicated sessions
                    forceSingle = true;
                }
            }

            if (remainingMutants.Count > 0)
            {
                LogNotAllMutantsTested(Logger);
            }

            mutantsToTest = remainingMutants;
        }
    }

    private async Task<ITestRunResult> RunTestSessionAsync(IProjectAndTests projectAndTests, ICollection<IMutant> mutantsToTest,
        ITimeoutValueCalculator timeoutMs,
        TestUpdateHandler updateHandler, bool forceSingle)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            var displayNames = string.Join(" ,", mutantsToTest.Select(x => x.DisplayName));
            LogTesting(Logger, displayNames);
        }
        if (forceSingle)
        {
            foreach (var mutant in mutantsToTest)
            {
                var localResult =
                    await TestRunner.TestMultipleMutantsAsync(projectAndTests, timeoutMs, new[] { mutant }, updateHandler).ConfigureAwait(false);
                if (updateHandler == null || localResult.SessionTimedOut)
                {
                    mutant.AnalyzeTestRun(localResult.FailingTests,
                        localResult.ExecutedTests,
                        localResult.TimedOutTests,
                        localResult.SessionTimedOut);
                }
            }

            return new TestRunResult(true);
        }

        var result = await TestRunner.TestMultipleMutantsAsync(projectAndTests, timeoutMs, mutantsToTest.ToList(), updateHandler).ConfigureAwait(false);
        if (updateHandler != null && !result.SessionTimedOut)
        {
            return result;
        }

        foreach (var mutant in mutantsToTest)
        {
            mutant.AnalyzeTestRun(result.FailingTests,
                result.ExecutedTests,
                result.TimedOutTests,
                mutantsToTest.Count == 1 && result.SessionTimedOut);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Test run for {Mutants} is {Result} ")]
    private static partial void LogTestRun(ILogger logger, string mutants, string result);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Messages for {Mutants}: {NewLine}{Messages}")]
    private static partial void LogMessages(ILogger logger, string mutants, string newLine, string messages);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stryker failed to test {RemainingMutantsCount} mutant(s).")]
    private static partial void LogStrykerFailedToTest(ILogger logger, int remainingMutantsCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Not all mutants were tested.")]
    private static partial void LogNotAllMutantsTested(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Testing {MutantsToTest}.")]
    private static partial void LogTesting(ILogger logger, string mutantsToTest);
}
