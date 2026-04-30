using ShellProgressBar;
using System;

namespace Stryker.Core.Reporters.Progress;

public class ProgressBar : IProgressBar, IDisposable
{
    private readonly ProgressBarOptions _options = new()
    {
        ForegroundColor = ConsoleColor.Yellow,
        ForegroundColorDone = ConsoleColor.Green,
        BackgroundColor = ConsoleColor.DarkGray,
        ProgressCharacter = '█',
        BackgroundCharacter = '▓'
    };

    private bool _disposedValue;
    private ShellProgressBar.ProgressBar? _progressBar;

    public void Start(int maxTicks, string message) => _progressBar = new ShellProgressBar.ProgressBar(Math.Max(maxTicks, 1), message, _options);

    public void Tick(string message) => _progressBar?.Tick(message);

    public void Stop() => Dispose();

    public int Ticks() => _progressBar?.CurrentTick ?? -1;

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }
        if (disposing)
        {
            _progressBar?.Dispose();
            _progressBar = null;
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
