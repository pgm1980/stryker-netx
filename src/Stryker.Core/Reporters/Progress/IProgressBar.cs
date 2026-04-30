namespace Stryker.Core.Reporters.Progress;

public interface IProgressBar
{
    void Start(int maxTicks, string message);

    void Stop();

    void Tick(string message);
}
