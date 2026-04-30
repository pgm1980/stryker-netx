using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Baseline.Utils;
using Stryker.Utilities;
using Stryker.Utilities.Logging;

namespace Stryker.Core.MutantFilters;

public partial class BaselineMutantFilter : IMutantFilter
{
    private readonly IBaselineProvider _baselineProvider;
    private readonly IGitInfoProvider _gitInfoProvider;
    private readonly ILogger<BaselineMutantFilter> _logger;
    private readonly IBaselineMutantHelper _baselineMutantHelper;

    private readonly IStrykerOptions _options;
    private readonly IJsonReport? _baseline;

    public MutantFilter Type => MutantFilter.Baseline;
    public string DisplayName => "baseline filter";

    public BaselineMutantFilter(IStrykerOptions options, IBaselineProvider? baselineProvider = null,
        IGitInfoProvider? gitInfoProvider = null, IBaselineMutantHelper? baselineMutantHelper = null)
    {
        _logger = ApplicationLogging.LoggerFactory.CreateLogger<BaselineMutantFilter>();
        _baselineProvider = baselineProvider ?? BaselineProviderFactory.Create(options);
        _gitInfoProvider = gitInfoProvider ?? new GitInfoProvider(options);
        _baselineMutantHelper = baselineMutantHelper ?? new BaselineMutantHelper();

        _options = options;

        if (options.WithBaseline)
        {
            _baseline = GetBaselineAsync().Result;
        }
    }


    public IEnumerable<IMutant> FilterMutants(IEnumerable<IMutant> mutants, IReadOnlyFileLeaf file,
        IStrykerOptions options)
    {
        if (options.WithBaseline)
        {
            if (_baseline == null)
            {
                LogReturningAllMutants(_logger, file.RelativePath);
            }
            else
            {
                UpdateMutantsWithBaselineStatus(mutants, file);
            }
        }

        return mutants;
    }

    private void UpdateMutantsWithBaselineStatus(IEnumerable<IMutant> mutants, IReadOnlyFileLeaf file)
    {
        if (_baseline is null)
        {
            return;
        }
        var normalizedRelativePath = FilePathUtils.NormalizePathSeparators(file.RelativePath);
        if (normalizedRelativePath is null || !_baseline.Files.TryGetValue(normalizedRelativePath, out var baselineFile))
        {
            return;
        }

        if (baselineFile is { })
        {
            foreach (var baselineMutant in baselineFile.Mutants)
            {
                var baselineMutantSourceCode =
                    _baselineMutantHelper.GetMutantSourceCode(baselineFile.Source, baselineMutant);

                if (string.IsNullOrEmpty(baselineMutantSourceCode))
                {
                    LogMutantSpanNotFound(_logger);
                    continue;
                }

                var matchingMutants =
                    _baselineMutantHelper.GetMutantMatchingSourceCode(mutants, baselineMutant,
                        baselineMutantSourceCode);

                SetMutantStatusToBaselineMutantStatus(baselineMutant, matchingMutants);
            }
        }
    }

    private static void SetMutantStatusToBaselineMutantStatus(IJsonMutant baselineMutant,
        IEnumerable<IMutant> matchingMutants)
    {
        if (matchingMutants.Count() == 1)
        {
            var matchingMutant = matchingMutants.First();
            matchingMutant.ResultStatus = Enum.Parse<MutantStatus>(baselineMutant.Status);
            matchingMutant.ResultStatusReason = "Result based on previous run";
        }
        else
        {
            foreach (var matchingMutant in matchingMutants)
            {
                matchingMutant.ResultStatus = MutantStatus.Pending;
                matchingMutant.ResultStatusReason = "Result based on previous run was inconclusive";
            }
        }
    }

    private async Task<IJsonReport?> GetBaselineAsync()
    {
        var branchName = _gitInfoProvider.GetCurrentBranchName();

        var baselineLocation = $"baseline/{branchName}";

        var report = await _baselineProvider.Load(baselineLocation).ConfigureAwait(false);

        if (report == null)
        {
            LogNoBaselineForBranch(_logger, branchName, _options.FallbackVersion ?? string.Empty);

            return await GetFallbackBaselineAsync().ConfigureAwait(false);
        }

        LogFoundBaselineForBranch(_logger, branchName);

        return report;
    }

    private async Task<IJsonReport?> GetFallbackBaselineAsync(bool baseline = true)
    {
        var report = await _baselineProvider.Load($"{(baseline ? "baseline/" : "")}{_options.FallbackVersion}").ConfigureAwait(false);

        if (report == null)
        {
            if (baseline)
            {
                LogNoBaselineFallback(_logger);
                return await GetFallbackBaselineAsync(false).ConfigureAwait(false);
            }

            LogNoBaselineFresh(_logger);
            return null;
        }

        LogFoundFallbackReport(_logger, _options.FallbackVersion ?? string.Empty);

        return report;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Returning all mutants on {RelativeFilePath} because there is no baseline available")]
    private static partial void LogReturningAllMutants(ILogger logger, string relativeFilePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unable to find mutant span in original baseline source code. This indicates a bug in stryker. Please report this on github.")]
    private static partial void LogMutantSpanNotFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "We could not locate a baseline for branch {BranchName}, now trying fallback version {FallbackVersion}")]
    private static partial void LogNoBaselineForBranch(ILogger logger, string branchName, string fallbackVersion);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found baseline report for current branch {BranchName}")]
    private static partial void LogFoundBaselineForBranch(ILogger logger, string branchName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "We could not locate a baseline report for the fallback version. Now trying regular fallback version.")]
    private static partial void LogNoBaselineFallback(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "We could not locate a baseline report for the current branch, version or fallback version. Now running a complete test to establish a fresh baseline.")]
    private static partial void LogNoBaselineFresh(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found fallback report using version {FallbackVersion}")]
    private static partial void LogFoundFallbackReport(ILogger logger, string fallbackVersion);
}
