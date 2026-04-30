namespace Stryker.Core.Reporters.Progress;

public interface IStopWatchProvider
{
    void Start();
    void Stop();
    long GetElapsedMillisecond();
}
