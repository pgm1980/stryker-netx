using System;
using System.Linq;
using LibGit2Sharp;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Baseline.Providers;

public sealed partial class GitInfoProvider : IGitInfoProvider
{
    private readonly IStrykerOptions _options;
    private readonly string? _repositoryPath;
    private readonly ILogger<GitInfoProvider> _logger;

    public IRepository? Repository { get; }

    public string? RepositoryPath => _repositoryPath ?? LibGit2Sharp.Repository.Discover(_options.ProjectPath)?.Split(".git")[0];

    public GitInfoProvider(IStrykerOptions options, IRepository? repository = null, string? repositoryPath = null, ILogger<GitInfoProvider>? logger = null)
    {
        _repositoryPath = repositoryPath;
        _options = options;
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<GitInfoProvider>();

        if (!options.Since)
        {
            return;
        }

        Repository = repository ?? CreateRepository();
    }

    public string GetCurrentBranchName()
    {
        string? branchName = null;
        if (Repository?.Branches?.FirstOrDefault(b => b.IsCurrentRepositoryHead) is var identifiedBranch && identifiedBranch is { })
        {
            LogBranchIdentified(_logger, identifiedBranch.FriendlyName);
            branchName = identifiedBranch.FriendlyName;
        }

        if (string.IsNullOrWhiteSpace(branchName))
        {
            LogUsingProjectVersion(_logger, _options.ProjectVersion ?? string.Empty);
            branchName = _options.ProjectVersion;
        }

        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new InputException("Unfortunately we could not determine the branch name automatically. Please set the dashboard project version option to your current branch.");
        }
        return branchName;
    }

    public Commit DetermineCommit()
    {
        var commit = GetTargetCommit();

        if (commit == null)
        {
            throw new InputException($"No branch, tag or commit found for the configured --since-target '{_options.SinceTarget}'. Please pass a different --since-target (branch name, tag or commit SHA).");
        }

        return commit;
    }

    private Repository CreateRepository()
    {
        if (string.IsNullOrEmpty(RepositoryPath))
        {
            throw new InputException("Could not locate git repository. Unable to determine git diff to filter mutants. Did you run inside a git repo? If not please disable the 'since' feature.");
        }

        return new Repository(RepositoryPath);
    }

    private Commit? GetTargetCommit()
    {
        var sinceTarget = _options.SinceTarget ?? string.Empty;
        LogLookingForBranch(_logger, sinceTarget);
        if (Repository is null)
        {
            return null;
        }

        var commit = FindBranchTip(sinceTarget) ?? FindTagCommit(sinceTarget) ?? LookupCommitBySha(sinceTarget);

        // Sprint 183 (issue #299, H-27): the historical default 'master' strands on modern
        // main-repositories — fall back once, loudly.
        if (commit is null && string.Equals(sinceTarget, "master", StringComparison.Ordinal))
        {
            LogMasterFallsBackToMain(_logger);
            commit = FindBranchTip("main");
        }

        return commit;
    }

    /// <summary>
    /// Matches a git reference name against the configured target — exact name or final
    /// path segment(s), never a substring.
    /// </summary>
    /// <param name="name">reference name (friendly or canonical)</param>
    /// <param name="target">configured since-target</param>
    /// <returns>true when the reference designates the target</returns>
    /// <remarks>
    /// Sprint 183 (issue #299, J-04): the previous Contains-matching let
    /// <c>--since-target main</c> silently match 'maintenance' (first enumeration hit
    /// wins) — a wrong diff base without any warning.
    /// </remarks>
    private static bool MatchesTarget(string? name, string target) =>
        !string.IsNullOrEmpty(name)
        && !string.IsNullOrEmpty(target)
        && (string.Equals(name, target, StringComparison.Ordinal)
            || name.EndsWith("/" + target, StringComparison.Ordinal));

    private Commit? FindBranchTip(string target)
    {
        foreach (var branch in Repository!.Branches)
        {
            try
            {
                if (MatchesTarget(branch.UpstreamBranchCanonicalName, target))
                {
                    LogMatchedUpstreamCanonical(_logger, branch.UpstreamBranchCanonicalName!);
                    return branch.Tip;
                }
                if (MatchesTarget(branch.CanonicalName, target))
                {
                    LogMatchedCanonical(_logger, branch.CanonicalName!);
                    return branch.Tip;
                }
                if (MatchesTarget(branch.FriendlyName, target))
                {
                    LogMatchedFriendly(_logger, branch.FriendlyName!);
                    return branch.Tip;
                }
            }
            catch (ArgumentNullException)
            {
                // Internal error thrown by libgit2sharp which happens when there is no upstream on a branch.
            }
        }

        return null;
    }

    private Commit? FindTagCommit(string target)
    {
        LogLookingForTag(_logger, target);
        var tag = Repository!.Tags.FirstOrDefault(t => t.Target is Commit && MatchesTarget(t.CanonicalName, target));
        if (tag?.Target is Commit tagCommit)
        {
            LogFoundTag(_logger, tag.CanonicalName, target);
            return tagCommit;
        }

        return null;
    }

    private Commit? LookupCommitBySha(string target)
    {
        // Sprint 183 (issue #299, J-05): abbreviated SHAs (7..40 hex digits) are an everyday
        // form — the previous gate only accepted exactly 40 characters.
        if (target.Length is < 7 or > 40 || !target.All(Uri.IsHexDigit))
        {
            return null;
        }

        var commit = target.Length == 40
            ? Repository!.Lookup(new ObjectId(target)) as Commit
            : Repository!.Lookup(target) as Commit;
        if (commit != null)
        {
            LogFoundCommit(_logger, commit.Sha, target);
        }

        return commit;
    }

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "The since target 'master' was not found — falling back to 'main'. Pass --since-target explicitly to silence this.")]
    private static partial void LogMasterFallsBackToMain(ILogger logger);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "{BranchName} identified as current branch")]
    private static partial void LogBranchIdentified(ILogger logger, string branchName);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Could not locate the current branch name, using project version instead: {ProjectVersion}")]
    private static partial void LogUsingProjectVersion(ILogger logger, string projectVersion);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Looking for branch matching {GitDiffTarget}")]
    private static partial void LogLookingForBranch(ILogger logger, string gitDiffTarget);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Matched with upstream canonical name {UpstreamCanonicalName}")]
    private static partial void LogMatchedUpstreamCanonical(ILogger logger, string upstreamCanonicalName);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Matched with canonical name {CanonicalName}")]
    private static partial void LogMatchedCanonical(ILogger logger, string canonicalName);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Matched with friendly name {FriendlyName}")]
    private static partial void LogMatchedFriendly(ILogger logger, string friendlyName);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Looking for tag matching {GitDiffTarget}")]
    private static partial void LogLookingForTag(ILogger logger, string gitDiffTarget);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Found tag {Tag} for diff target {GitDiffTarget}")]
    private static partial void LogFoundTag(ILogger logger, string tag, string gitDiffTarget);

    [LoggerMessage(Level = Microsoft.Extensions.Logging.LogLevel.Debug, Message = "Found commit {Commit} for diff target {GitDiffTarget}")]
    private static partial void LogFoundCommit(ILogger logger, string commit, string gitDiffTarget);
}
