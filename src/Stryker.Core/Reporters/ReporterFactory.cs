using System.Collections.Generic;
using System.Linq;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Reporters.Html;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Progress;

namespace Stryker.Core.Reporters;

public class ReporterFactory : IReporterFactory
{
    public IReporter Create(IStrykerOptions options, IGitInfoProvider? branchProvider = null)
    {
        return new BroadcastReporter(DetermineEnabledReporters(options.Reporters.ToList(), CreateReporters(options)));
    }

    private static Dictionary<Reporter, IReporter> CreateReporters(IStrykerOptions options)
    {
        return new Dictionary<Reporter, IReporter>
        {
            { Reporter.Dots, new ConsoleDotProgressReporter() },
            { Reporter.Progress, CreateProgressReporter() },
            { Reporter.ClearText, new ClearTextReporter(options) },
            { Reporter.ClearTextTree, new ClearTextTreeReporter(options) },
            { Reporter.Json, new JsonReporter(options) },
            { Reporter.Html, new HtmlReporter(options) },
            { Reporter.Dashboard, new DashboardReporter(options) },
            { Reporter.RealTimeDashboard, new DashboardReporter(options) },
            { Reporter.Markdown, new MarkdownSummaryReporter(options) },
            { Reporter.Baseline, new BaselineReporter(options) }
        };
    }

    private static IEnumerable<IReporter> DetermineEnabledReporters(List<Reporter> enabledReporters, Dictionary<Reporter, IReporter> possibleReporters)
    {
        if (enabledReporters.Contains(Reporter.All))
        {
            return possibleReporters.Values;
        }

        return possibleReporters.Where(reporter => enabledReporters.Contains(reporter.Key))
            .Select(reporter => reporter.Value);
    }

    private static ProgressReporter CreateProgressReporter()
    {
        var progressBarReporter = new ProgressBarReporter(new ProgressBar(), new StopWatchProvider());

        return new ProgressReporter(progressBarReporter);
    }
}
