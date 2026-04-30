using System.Threading.Tasks;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;

namespace Stryker.Core.Clients;

public interface IDashboardClient
{
    Task<string?> PublishReport(IJsonReport report, string version, bool realTime = false);
    Task<JsonReport?> PullReport(string version);
    Task PublishMutantBatch(IJsonMutant mutant);
    Task PublishFinished();
}
