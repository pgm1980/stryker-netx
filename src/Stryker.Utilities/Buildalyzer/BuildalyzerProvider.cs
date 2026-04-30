using System.Diagnostics.CodeAnalysis;
using Buildalyzer;

namespace Stryker.Utilities.Buildalyzer;

[ExcludeFromCodeCoverage]
public class BuildalyzerProvider : IBuildalyzerProvider
{
    public IAnalyzerManager Provide(AnalyzerManagerOptions? options = null) => new AnalyzerManager(options);
}
