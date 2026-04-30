using System.Threading.Tasks;
using NuGet.Versioning;

namespace Stryker.CLI.Clients;

public interface IStrykerNugetFeedClient
{
    Task<SemanticVersion> GetLatestVersionAsync();
    Task<SemanticVersion> GetPreviewVersionAsync();
}
