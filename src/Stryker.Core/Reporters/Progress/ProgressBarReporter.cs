using System;
using System.Text;
using Spectre.Console;
using Stryker.Abstractions;

namespace Stryker.Core.Reporters.Progress;

public class ProgressBarReporter : IProgressBarReporter, IDisposable
{
    private const string LoggingFormat = "│ Testing mutant {0} / {1} │ K {2} │ S {3} │ T {4} │ {5} │";
    private static readonly CompositeFormat LoggingCompositeFormat = CompositeFormat.Parse(LoggingFormat);

    private readonly IProgressBar _progressBar;
    private readonly IStopWatchProvider _stopWatch;
    private readonly IAnsiConsole _console;

    private int _mutantsToBeTested;
    private int _numberOfMutantsRan;
    private bool _disposedValue;

    private int _mutantsKilledCount;
    private int _mutantsSurvivedCount;
    private int _mutantsTimeoutCount;

    public ProgressBarReporter(IProgressBar progressBar, IStopWatchProvider stopWatch, IAnsiConsole? console = null)
    {
        _progressBar = progressBar;
        _stopWatch = stopWatch;
        _console = console ?? AnsiConsole.Console;
    }

    public void ReportInitialState(int mutantsToBeTested)
    {
        _stopWatch.Start();
        _mutantsToBeTested = mutantsToBeTested;

        _progressBar.Start(_mutantsToBeTested, string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggingCompositeFormat, 0, _mutantsToBeTested, _mutantsKilledCount, _mutantsSurvivedCount, _mutantsTimeoutCount, RemainingTime()));
    }

    public void ReportRunTest(IReadOnlyMutant mutantTestResult)
    {
        _numberOfMutantsRan++;

        switch (mutantTestResult.ResultStatus)
        {
            case MutantStatus.Killed:
                _mutantsKilledCount++;
                break;
            case MutantStatus.Survived:
                _mutantsSurvivedCount++;
                break;
            case MutantStatus.Timeout:
                _mutantsTimeoutCount++;
                break;
        }

        _progressBar.Tick(string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggingCompositeFormat, _numberOfMutantsRan, _mutantsToBeTested, _mutantsKilledCount, _mutantsSurvivedCount, _mutantsTimeoutCount, RemainingTime()));
    }

    public void ReportFinalState()
    {
        _progressBar.Tick(string.Format(System.Globalization.CultureInfo.InvariantCulture, LoggingCompositeFormat, _numberOfMutantsRan, _mutantsToBeTested, _mutantsKilledCount, _mutantsSurvivedCount, _mutantsTimeoutCount, RemainingTime()));
        Dispose();

        var length = _mutantsToBeTested.ToString(System.Globalization.CultureInfo.InvariantCulture).Length;

        _console.WriteLine();
        _console.MarkupLine($"Killed:   [Magenta]{_mutantsKilledCount.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(length)}[/]");
        _console.MarkupLine($"Survived: [Magenta]{_mutantsSurvivedCount.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(length)}[/]");
        _console.MarkupLine($"Timeout:  [Magenta]{_mutantsTimeoutCount.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(length)}[/]");
    }

    private string RemainingTime()
    {
        if (_mutantsToBeTested == 0 || _numberOfMutantsRan == 0)
        {
            return "NA";
        }

        var elapsed = _stopWatch.GetElapsedMillisecond();
        var remaining = (_mutantsToBeTested - _numberOfMutantsRan) * elapsed / _numberOfMutantsRan;

        return MillisecondsToText(remaining);
    }

    private static string MillisecondsToText(double remaining)
    {
        var span = TimeSpan.FromMilliseconds(remaining);
        if (span.TotalDays >= 1)
        {
            return span.ToString(@"\~d\d\ h\h", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (span.TotalHours >= 1)
        {
            return span.ToString(@"\~h\h\ mm\m", System.Globalization.CultureInfo.InvariantCulture);
        }

        return span.ToString(@"\~m\m\ ss\s", System.Globalization.CultureInfo.InvariantCulture);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stopWatch?.Stop();
                _progressBar?.Stop();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
