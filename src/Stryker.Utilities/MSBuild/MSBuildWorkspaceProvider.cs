using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace Stryker.Utilities.MSBuild;

/// <summary>
/// Default <see cref="IMSBuildWorkspaceProvider"/> backed by the Roslyn
/// <see cref="MSBuildWorkspace"/>. Disposes the workspace on disposal.
/// </summary>
/// <remarks>
/// <see cref="MSBuildWorkspace"/> needs the .NET SDK MSBuild to be visible to the
/// process before <see cref="MSBuildWorkspace.Create()"/> is invoked. Callers (typically
/// <c>Stryker.CLI.Program</c>) must call <c>Microsoft.Build.Locator.MSBuildLocator.RegisterDefaults()</c>
/// once at startup. This class deliberately does not auto-register so that test hosts
/// can supply their own locator.
/// </remarks>
public sealed class MSBuildWorkspaceProvider : IMSBuildWorkspaceProvider
{
    private readonly MSBuildWorkspace _workspace;

    /// <summary>
    /// Initializes a workspace with default host-services and an empty MSBuild property set.
    /// </summary>
    public MSBuildWorkspaceProvider()
        : this(properties: null)
    {
    }

    /// <summary>
    /// Initializes a workspace with the given MSBuild global-property overrides
    /// (forwarded to every project the workspace loads).
    /// </summary>
    public MSBuildWorkspaceProvider(IReadOnlyDictionary<string, string>? properties)
    {
        _workspace = properties is null
            ? MSBuildWorkspace.Create()
            : MSBuildWorkspace.Create(properties.ToDictionary(p => p.Key, p => p.Value, System.StringComparer.Ordinal));
    }

    /// <inheritdoc />
    public async Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = default) =>
        await _workspace.OpenSolutionAsync(solutionFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken = default) =>
        await _workspace.OpenProjectAsync(projectFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public ImmutableList<WorkspaceDiagnostic> Diagnostics => _workspace.Diagnostics;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Properties => _workspace.Properties;

    /// <inheritdoc />
    public void Dispose() => _workspace.Dispose();
}
