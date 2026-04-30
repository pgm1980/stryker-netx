using Buildalyzer;

namespace Stryker.Utilities.Buildalyzer;

/// <summary>
/// This is an interface to mock buildalyzer classes
/// </summary>
public interface IBuildalyzerProvider
{
    IAnalyzerManager Provide(AnalyzerManagerOptions? options = null);
}
