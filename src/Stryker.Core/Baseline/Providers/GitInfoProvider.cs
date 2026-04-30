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
            throw new InputException($"No branch or tag or commit found with given target {_options.SinceTarget}. Please provide a different GitDiffTarget.");
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
        foreach (var branch in Repository.Branches)
        {
            try
            {
                if (branch.UpstreamBranchCanonicalName?.Contains(sinceTarget) ?? false)
                {
                    LogMatchedUpstreamCanonical(_logger, branch.UpstreamBranchCanonicalName);
                    return branch.Tip;
                }
                if (branch.CanonicalName?.Contains(sinceTarget) ?? false)
                {
                    LogMatchedCanonical(_logger, branch.CanonicalName);
                    return branch.Tip;
                }
                if (branch.FriendlyName?.Contains(sinceTarget) ?? false)
                {
                    LogMatchedFriendly(_logger, branch.FriendlyName);
                    return branch.Tip;
                }
            }
            catch (ArgumentNullException)
            {
                // Internal error thrown by libgit2sharp which happens when there is no upstream on a branch.
            }
        }

        LogLookingForTag(_logger, sinceTarget);
        var tag = Repository.Tags.FirstOrDefault(t => t.Target is Commit && (t.CanonicalName?.Contains(sinceTarget) ?? false));
        var tagCommit = tag?.Target as Commit;
        if (tagCommit != null)
        {
            LogFoundTag(_logger, tag!.CanonicalName, sinceTarget);
            return tagCommit;
        }

        // It's a commit!
        if (sinceTarget.Length == 40)
        {
            var commit = Repository.Lookup(new ObjectId(sinceTarget)) as Commit;

            if (commit != null)
            {
                LogFoundCommit(_logger, commit.Sha, sinceTarget);
                return commit;
            }
        }

        return null;
    }

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
