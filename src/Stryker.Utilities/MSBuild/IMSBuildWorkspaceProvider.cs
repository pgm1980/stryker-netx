using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Stryker.Utilities.MSBuild;

/// <summary>
/// Abstraction over <c>Microsoft.CodeAnalysis.MSBuild.MSBuildWorkspace</c> that lets
/// stryker-netx open solutions and projects through the .NET SDK MSBuild without
/// taking a hard dependency on the concrete <c>MSBuildWorkspace</c> type at the
/// call-site (which simplifies testing / mocking).
/// </summary>
public interface IMSBuildWorkspaceProvider : IDisposable
{
    /// <summary>
    /// Asynchronously opens a solution (.sln / .slnx) and returns its <see cref="Solution"/> view.
    /// </summary>
    Task<Solution> OpenSolutionAsync(string solutionFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously opens a single project (.csproj / .vbproj) and returns its <see cref="Project"/> view.
    /// </summary>
    Task<Project> OpenProjectAsync(string projectFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Workspace diagnostics produced during loading (warnings / failures that did not throw).
    /// </summary>
    ImmutableList<WorkspaceDiagnostic> Diagnostics { get; }

    /// <summary>
    /// MSBuild global-properties applied to every project the workspace loads.
    /// </summary>
    IReadOnlyDictionary<string, string> Properties { get; }
}
