using Stryker.Abstractions;

namespace Stryker.Core.Reporters.Progress;

public interface IProgressBarReporter
{
    void ReportInitialState(int mutantsToBeTested);
    void ReportRunTest(IReadOnlyMutant mutantTestResult);
    void ReportFinalState();
}
