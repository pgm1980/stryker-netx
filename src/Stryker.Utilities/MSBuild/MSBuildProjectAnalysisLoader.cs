using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Analysis;
using EvaluationProject = Microsoft.Build.Evaluation.Project;

namespace Stryker.Utilities.MSBuild;

/// <summary>
/// Loads a project file via <see cref="IMSBuildWorkspaceProvider"/> + an MSBuild
/// <see cref="EvaluationProject"/> for raw property access, and wraps the result
/// in an <see cref="IProjectAnalysis"/>. Encapsulates the Roslyn workspace types
/// so that <c>Stryker.Core</c> can stay decoupled from
/// <c>Microsoft.CodeAnalysis.Workspaces</c> (referenced as <c>PrivateAssets="all"</c>).
/// </summary>
public static class MSBuildProjectAnalysisLoader
{
    /// <summary>
    /// Loads a single project asynchronously and returns an <see cref="IProjectAnalysis"/>
    /// adapter. The optional <paramref name="globalProperties"/> are applied to the parallel
    /// <see cref="EvaluationProject"/> instance used for raw MSBuild property lookup.
    /// </summary>
    public static async Task<IProjectAnalysis> LoadAsync(
        IMSBuildWorkspaceProvider workspaceProvider,
        string projectFilePath,
        IReadOnlyDictionary<string, string>? globalProperties,
        ILogger? logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workspaceProvider);
        ArgumentException.ThrowIfNullOrEmpty(projectFilePath);

        var roslynProject = await workspaceProvider.OpenProjectAsync(projectFilePath, cancellationToken).ConfigureAwait(false);

        var properties = globalProperties is null
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : new Dictionary<string, string>(globalProperties, StringComparer.Ordinal);

        using var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection(properties);
        EvaluationProject? evaluationProject = null;
        try
        {
            evaluationProject = projectCollection.LoadProject(projectFilePath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger?.LogWarning(ex, "Failed to load MSBuild evaluation for {Project}", projectFilePath);
        }

        return new RoslynProjectAnalysis(roslynProject, evaluationProject);
    }
}
