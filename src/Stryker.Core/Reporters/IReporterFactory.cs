using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Baseline.Providers;

namespace Stryker.Core.Reporters;

public interface IReporterFactory
{
    IReporter Create(IStrykerOptions options, IGitInfoProvider? branchProvider = null);
}
