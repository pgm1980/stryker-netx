using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Solutions;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.Initialisation;

/// <summary>
///  - Reads .csproj to find project under test
///  - Scans project under test and store files to mutate
///  - Build composite for files
/// </summary>
public partial class InputFileResolver : IInputFileResolver
{
    private readonly string[] _foldersToExclude = ["obj", "bin", "node_modules", "StrykerOutput"];
    private readonly ILogger _logger;
    private readonly IMSBuildWorkspaceProvider _workspaceProvider;
    private readonly ISolutionProvider _solutionProvider;
    // Phase 10.4: FrozenSet for O(1) lookup of MSBuild diagnostic-property names.
    // Read-only after construction; never mutated.
    private static readonly FrozenSet<string> ImportantProperties =
        FrozenSet.ToFrozenSet(["Configuration", "Platform", "AssemblyName", "Configurations"], StringComparer.Ordinal);

    public InputFileResolver(IFileSystem fileSystem,
        IMSBuildWorkspaceProvider workspaceProvider,
        INugetRestoreProcess nugetRestoreProcess,
        ISolutionProvider solutionProvider,
        ILogger<InputFileResolver> logger)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentNullException.ThrowIfNull(workspaceProvider);
        // INugetRestoreProcess is retained on the constructor signature for backwards-compatibility
        // with external callers that still pass it via DI; full-framework restore retry was removed
        // in Phase 9b because MSBuildWorkspace handles package restore internally.
        ArgumentNullException.ThrowIfNull(nugetRestoreProcess);
        ArgumentNullException.ThrowIfNull(solutionProvider);
        ArgumentNullException.ThrowIfNull(logger);

        FileSystem = fileSystem;
        _workspaceProvider = workspaceProvider;
        _solutionProvider = solutionProvider;
        _logger = logger;
    }

    public IFileSystem FileSystem { get; }

    public IReadOnlyCollection<SourceProjectInfo> ResolveSourceProjectInfos(IStrykerOptions options)
    {
        var normalizedProjectUnderTestNameFilter = NormalizePath(options.SourceProjectName ?? string.Empty);

        SolutionFile? solution;
        if (string.IsNullOrEmpty(options.SolutionPath))
        {
            solution = null;
        }
        else
        {
            // load the solution file when provided
            try
            {
                LogLoadingSolution(_logger, options.SolutionPath);
                solution = _solutionProvider.GetSolution(options.SolutionPath);
            }
            catch (IOException e)
            {
                LogFailedLoadSolution(_logger, e, options.SolutionPath);
                return [];
            }
            catch (UnauthorizedAccessException e)
            {
                LogFailedAccessSolution(_logger, e, options.SolutionPath);
                return [];
            }
            catch (AggregateException e) // Handles exceptions from .Result on Task
            {
                LogFailedLoadSolution(_logger, e, options.SolutionPath);
                return [];
            }
        }

        if (options.IsSolutionContext)
        {
            return ScanInSolutionMode(options, solution, normalizedProjectUnderTestNameFilter);
        }

        var result = SourceProjectInfos(options, solution, normalizedProjectUnderTestNameFilter);
        return result.Count <= 1 ? result : ResolveMultiReferenceCase(options, result);
    }

    /// <summary>
    /// Sprint 141 + 150 (Bug #8 from Calculator-Tester Bug-Report 4) + Sprint 162
    /// (ADR-042 §3): when the test project references multiple source projects,
    /// decide between four outcomes — accept all references when <c>--all-projects</c>
    /// opted-in (Sprint 150 / ADR-031), narrow via short-name <c>--project</c>
    /// filter when exactly one match results (Sprint 162 / ADR-042 §3), throw the
    /// disambiguation exception with an improved error message, or fall through
    /// to solution-mode (handled earlier in <see cref="ResolveSourceProjectInfos"/>).
    /// </summary>
    private List<SourceProjectInfo> ResolveMultiReferenceCase(
        IStrykerOptions options, List<SourceProjectInfo> result)
    {
        // Sprint 150 (ADR-031): explicit opt-in to multi-project mutation.
        if (options.IsAllProjectsMode)
        {
            LogAllProjectsMode(_logger, result.Count);
            return result;
        }

        // Sprint 162 (ADR-042 §3): if the user supplied --project <short-name>
        // (e.g. "Aisess.Application"), try to filter the references down to a
        // single match using MatchesFilter (filename-with-or-without-csproj-ext,
        // case-insensitive). This fixes the misleading "more than one project
        // reference" error when the user HAS supplied --project but with a
        // short-name that the upstream resolver didn't recognise.
        if (!string.IsNullOrEmpty(options.ProjectName))
        {
            var byShortName = result
                .Where(p => MatchesFilter(p.Analysis.ProjectFilePath, options.ProjectName))
                .ToList();
            if (byShortName.Count == 1)
            {
                LogSelectedProject(_logger, byShortName[0].Analysis.ProjectFilePath);
                return byShortName;
            }
            if (byShortName.Count > 1)
            {
                throw new InputException(
                    string.Create(CultureInfo.InvariantCulture,
                        $"Project filter '{options.ProjectName}' is ambiguous — multiple test-project references match. ") +
                    "Please supply a more specific value or an absolute .csproj path." + Environment.NewLine +
                    BuildReferenceChoice(byShortName.Select(p => p.Analysis.ProjectFilePath)));
            }
            // byShortName.Count == 0 → fall through to the existing multi-reference error
            // (the user-supplied name does not match any reference, so we list the choices).
        }

        var stringBuilder = new StringBuilder().AppendLine(
                "Test project contains more than one project reference. Please set the project option (https://stryker-mutator.io/docs/stryker-net/configuration#project-file-name) to specify which project to mutate.")
            .AppendLine("Alternatively, run stryker-netx with --all-projects to mutate ALL referenced source projects sequentially, or with --solution <path>.slnx for whole-solution mode.")
            .Append(BuildReferenceChoice(result.Select(p => p.Analysis.ProjectFilePath)));
        throw new InputException(stringBuilder.ToString());
    }

    private List<SourceProjectInfo> SourceProjectInfos(IStrykerOptions options, SolutionFile? solution,
        string? normalizedProjectUnderTestNameFilter)
    {
        var (solutionInfo, configuration, platform) = ResolveSolutionContext(options, solution);

        // we analyze the test project(s) and identify the project to be mutated
        var testProjectsSpecified = options.TestProjects.Any();
        var testProjectFileNames = testProjectsSpecified ? options.TestProjects.Select(FindTestProject).ToList()
            : [FindTestProject(options.ProjectPath ?? string.Empty)];

        LogAnalyzingTestProjects(_logger, testProjectFileNames.Count);
        List<(string projectFile, string framework, string configuration, string platform)> projectList =
            [..testProjectFileNames.Select(p => (p, options.TargetFramework ?? string.Empty, configuration, platform))];
        // if test project is provided but no source project
        var targetProjectMode = testProjectsSpecified && string.IsNullOrEmpty(options.SourceProjectName);
        if (targetProjectMode)
        {
            LogAssumeWorkingDirContainsTarget(_logger);
            normalizedProjectUnderTestNameFilter = NormalizePath(FindProjectFile(options.WorkingDirectory ?? string.Empty));
            targetProjectMode =
                options.TestProjects.All(tp => !string.Equals(NormalizePath(tp), normalizedProjectUnderTestNameFilter, StringComparison.Ordinal));
            if (!targetProjectMode)
            {
                // we detected a test project, discard it
                LogWorkingDirIsTest(_logger);
                normalizedProjectUnderTestNameFilter = null;
            }
        }

        // we match test projects to mutable projects
        var analyzeAllNeededProjects = AnalyzeAllNeededProjects(projectList, normalizedProjectUnderTestNameFilter ?? string.Empty, options, ScanMode.ScanTestProjectReferences);
        var (findMutableAnalyses, orphans) = FindMutableAnalyses(analyzeAllNeededProjects);

        var result = AnalyzeAndIdentifyProjects(options, solutionInfo!, findMutableAnalyses, orphans);
        return SelectMutableProject(result, testProjectFileNames, normalizedProjectUnderTestNameFilter, targetProjectMode);
    }

    private (Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo? solutionInfo, string configuration, string platform) ResolveSolutionContext(IStrykerOptions options, SolutionFile? solution)
    {
        Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo? solutionInfo = null;
        var configuration = options.Configuration ?? string.Empty;
        // "Any CPU" is the solution-level name; MSBuild requires "AnyCPU"
        var platform = NormalizePlatform(options.Platform ?? string.Empty);

        // identify the target configuration and platform
        if (solution != null)
        {
            var (actualBuildType, actualPlatform) = solution.GetMatching(options.Configuration ?? string.Empty, options.Platform ?? string.Empty);
            LogUsingSolutionConfigPlatform(_logger, actualBuildType, actualPlatform);
            solutionInfo = new Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo(solution.FileName, actualBuildType, actualPlatform);
            configuration = actualBuildType;
            platform = NormalizePlatform(actualPlatform);
        }

        return (solutionInfo, configuration, platform);
    }

    private List<SourceProjectInfo> SelectMutableProject(List<SourceProjectInfo> result, List<string> testProjectFileNames, string? normalizedProjectUnderTestNameFilter, bool targetProjectMode)
    {
        var mutableProjectsFound = result.Count;
        if (mutableProjectsFound == 1)
        {
            return result;
        }

        if (mutableProjectsFound == 0)
        {
            if (targetProjectMode)
            {
                LogProjectNotReferencedByTests(_logger, normalizedProjectUnderTestNameFilter ?? string.Empty);
            }
            else
            {
                LogNoProjectReferencedByTests(_logger);
            }

            return result;
        }

        // Too many references found
        // look for one project that references all provided test projects
        result = [.. result.Where(p => testProjectFileNames.TrueForAll(n => p.TestProjectsInfo.TestProjects.Any(t => string.Equals(t.ProjectFilePath, n, StringComparison.Ordinal))))];
        if (result.Count == 1)
        {
            LogSelectedProject(_logger, result[0].Analysis.ProjectFilePath);
        }

        return result;
    }

    private List<SourceProjectInfo> ScanInSolutionMode(IStrykerOptions options, SolutionFile? solution,
        string? normalizedProjectUnderTestNameFilter)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            var solutionName = FileSystem.Path.GetFileNameWithoutExtension(options.SolutionPath) ?? string.Empty;
            LogMutatingSolution(_logger, solutionName);
        }
        // identify actual configuration/platform to use
        var (actualBuildType, actualPlatform) = solution!.GetMatching(options.Configuration ?? string.Empty, options.Platform ?? string.Empty);
        if ((!string.IsNullOrEmpty(options.Configuration) && !string.Equals(options.Configuration, actualBuildType, StringComparison.Ordinal)) ||
            (!string.IsNullOrEmpty(options.Platform) && !string.Equals(options.Platform, actualPlatform, StringComparison.Ordinal)))
        {
            LogConfigPlatformInsteadOf(_logger, actualBuildType, actualPlatform, options.Configuration ?? string.Empty, options.Platform ?? string.Empty);
        }
        else
        {
            LogUsingConfigPlatform(_logger, actualBuildType, actualPlatform);
        }

        LogIdentifyingProjects(_logger, options.SolutionPath ?? string.Empty);

        var solutionInfo = new Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo(solution.FileName, actualBuildType, actualPlatform);
        // analyze all projects
        var projectsWithDetails = solution.GetProjectsWithDetails(actualBuildType, actualPlatform)
            .Select(p => (p.file, framework: options.TargetFramework ?? string.Empty, p.buildType, NormalizePlatform(p.platform))).ToList();

        // Sprint 159 (ADR-039) Layer 1: fast-fail validation against solution paths
        // before any Roslyn workspace loading. Sub-100ms feedback for filter typos.
        ValidateFilterMatchesAnyProject(normalizedProjectUnderTestNameFilter, projectsWithDetails);

        LogAnalyzingProjects(_logger, projectsWithDetails.Count);
        // we match test projects to mutable projects
        var mutableProjectsAnalyses = AnalyzeAllNeededProjects(projectsWithDetails,
            normalizedProjectUnderTestNameFilter ?? string.Empty,
            options,
            ScanMode.NoScan);
        var (findMutableAnalyses, orphanedProjects) = FindMutableAnalyses(mutableProjectsAnalyses);

        return AnalyzeAndIdentifyProjects(options, solutionInfo, findMutableAnalyses, orphanedProjects);
    }

    public string FindTestProject(string path)
    {
        var projectFile = FindProjectFile(path);
        LogUsingTestProject(_logger, projectFile);
        return projectFile;
    }

    private enum ScanMode
    {
        NoScan = 0, // no project added during analysis
        ScanTestProjectReferences = 1 // add test project references during scan
    }

    // analyze projects, do same for their upstream dependencies if activated, and identify which one(s)
    // to proceed with
    private List<SourceProjectInfo> AnalyzeAndIdentifyProjects(IStrykerOptions options,
        Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo solutionInfo,
        Dictionary<IProjectAnalysis, List<IProjectAnalysis>> findMutableAnalyses,
        List<IProjectAnalysis> unusedTestProjects)
    {
        // build all projects
        LogAnalyzingProjectCount(_logger, findMutableAnalyses.Count);

        // we match test projects to mutable projects
        if (findMutableAnalyses.All(r =>
                !r.Key.IsValid() || r.Value.All(r2 => !r2.IsValid())))
        {
            // no mutable project found
            LogAnalysis(findMutableAnalyses, unusedTestProjects, options.DiagMode);
            throw new InputException("Failed to analyze project builds. Stryker cannot continue.");
        }

        // keep only projects with one or more test projects
        var analyses = findMutableAnalyses
            .Where(p => p.Value.Count > 0)
            .Select(p => p.Key).GroupBy(p => p.ProjectFilePath, StringComparer.Ordinal);
        // we must select projects according to framework settings if any
        var projectInfos = analyses
            .Select(g => SelectAnalysis(g, options.TargetFramework))
            .Select(analysis => BuildSourceProjectInfo(options, solutionInfo, analysis, findMutableAnalyses[analysis]))
            .ToList();

        if (projectInfos.Count != 0)
        {
            return projectInfos;
        }

        LogProjectAnalysisFailed(_logger);
        throw new InputException("No valid project analysis results could be found.");
    }

    // Log the analysis results
    private void LogAnalysis(Dictionary<IProjectAnalysis, List<IProjectAnalysis>> findMutableAnalyses,
        List<IProjectAnalysis> unusedTestProjects, bool optionsDiagMode)
    {
        if (findMutableAnalyses.Count == 0)
        {
            LogNoProjectFound(_logger);
            return;
        }
        foreach (var (mutableProject, testProjects) in findMutableAnalyses)
        {
            LogProjectAnalysisResult(_logger, mutableProject.ProjectFilePath, mutableProject.IsValid() ? "succeeded" : "failed hence can't be mutated");
            if (testProjects.Count == 0)
            {
                LogProjectNoTestRef(_logger);
                continue;
            }
            // dump associated test projects
            foreach (var testProject in testProjects)
            {
                LogReferencedByTestProject(_logger, testProject.ProjectFilePath, testProject.IsValid() ? "succeeded" : "failed");
            }
            // provide synthetic status
            if (testProjects.Any(r => r.IsValid()))
            {
                LogCanBeMutated(_logger);
            }
            else
            {
                LogCantBeMutated(_logger);
            }
        }
        // dump test projects that do not reference any mutable project
        foreach (var unusedTestProject in unusedTestProjects)
        {
            LogUnusedTestProject(_logger, unusedTestProject.ProjectFilePath, unusedTestProject.IsValid() ? "succeeded" : "failed");
        }

        if (!optionsDiagMode)
        {
            LogDiagOptionTip(_logger);
        }
    }

    private List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> AnalyzeAllNeededProjects(
        List<(string projectFile, string framework, string configuration, string platform)> projects, string normalizedProjectUnderTestNameFilter,
        IStrykerOptions options, ScanMode mode)
    {
        var mutableProjectsAnalyses = new List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)>();
        var list = new SequentialEnumerableQueue<(string projectFile, string framework, string configuration, string platform)>(projects);
        while (!list.Empty)
        {
            foreach (var entry in list.Consume())
            {
                var analyses = LoadProjectAnalyses(entry.projectFile, entry.framework, entry.configuration, entry.platform, options);
                var buildResult = AnalyzeThisProject(analyses,
                    entry.framework,
                    options,
                    mutableProjectsAnalyses);
                // scan references if recursive scan is enabled
                ScanReferences(mode, buildResult).ForEach(p => list.Add((p, entry.framework, options.Configuration ?? string.Empty, entry.platform)));
            }
        }

        // Sprint 159 (ADR-039): apply project filter centrally with full analysis
        // collection available, instead of inside AnalyzeThisProject. Implements
        // Layer 2 (proactive validation: rejects test-project-as-filter mis-config)
        // and Layer 3 (zero-match safety-net: warns + returns unfiltered set).
        return ApplyProjectFilter(mutableProjectsAnalyses, normalizedProjectUnderTestNameFilter);
    }

    /// <summary>
    /// Sprint 159 (ADR-039): exact-filename match (with or without <c>.csproj</c>
    /// extension), case-insensitive. Replaces the legacy substring match that caused
    /// the Aisess <c>.slnx</c> bug — substring matches were ambiguous (filter
    /// <c>"Domain"</c> matched both <c>Aisess.Domain.csproj</c> and a hypothetical
    /// <c>Aisess.Domain.Tests.csproj</c>) and silently excluded all source projects
    /// when the user passed a test-project name as filter.
    /// </summary>
    private static bool MatchesFilter(string projectFilePath, string filter)
    {
        if (string.IsNullOrEmpty(projectFilePath) || string.IsNullOrEmpty(filter))
        {
            return false;
        }
        return string.Equals(Path.GetFileName(projectFilePath), filter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                Path.GetFileNameWithoutExtension(projectFilePath),
                Path.GetFileNameWithoutExtension(filter),
                StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Sprint 159 (ADR-039) Layer 2 + Layer 3 of the 3-layer filter defense.
    /// Layer 2: throws InputException if the filter matches only test projects
    /// (the Aisess mis-configuration case — the user passed a test-project name
    /// where a source-project name is expected).
    /// Layer 3: if the filter matches no source projects (e.g. matched source
    /// project failed to build), logs a warning and returns the unfiltered set
    /// so the pipeline continues with all source projects instead of failing
    /// opaquely.
    /// </summary>
    private List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> ApplyProjectFilter(
        List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> all,
        string? filter)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return all;
        }

        // Layer 2 (C-check): does the filter match only test projects?
        var matchingProjects = all
            .SelectMany(a => a.result)
            .Where(p => MatchesFilter(p.ProjectFilePath, filter))
            .ToList();

        if (matchingProjects.Count > 0 && matchingProjects.TrueForAll(p => p.IsTestProject))
        {
            throw new InputException(
                string.Create(CultureInfo.InvariantCulture,
                    $"Project filter '{filter}' matches only test project(s): '{matchingProjects[0].ProjectFilePath}'. ") +
                "Specify a source project (the project to be mutated, not the project that runs the tests).");
        }

        // Apply filter: test projects are always retained (they drive the matching
        // pipeline); source projects only if at least one analysis result matches.
        var filtered = all
            .Where(a => a.isTest || a.result.Any(p => MatchesFilter(p.ProjectFilePath, filter)))
            .ToList();

        // Layer 3 (B-fallback): if the post-filter source-project count is zero
        // (e.g. matched source project failed to build, or unforeseen edge case),
        // log warning + return unfiltered set so the pipeline does not fail opaquely.
        var sourceCount = filtered
            .Where(a => !a.isTest)
            .SelectMany(a => a.result)
            .Count(p => p.BuildsAnAssembly());

        if (sourceCount == 0)
        {
            LogFilterFallback(_logger, filter);
            return all;
        }

        return filtered;
    }

    /// <summary>
    /// Sprint 159 (ADR-039) Layer 1 of the 3-layer filter defense. Pre-validates
    /// that the project filter matches at least one csproj path in the solution
    /// before any Roslyn workspace loading happens. Provides a sub-100ms fast-fail
    /// for filter-typo cases (legacy behaviour was to silently load every project
    /// in the solution and only fail at the matching stage ~6 seconds later).
    /// </summary>
    private static void ValidateFilterMatchesAnyProject(
        string? filter,
        List<(string projectFile, string framework, string configuration, string platform)> projects)
    {
        if (string.IsNullOrEmpty(filter))
        {
            return;
        }
        if (projects.Exists(p => MatchesFilter(p.projectFile, filter)))
        {
            return;
        }

        var solutionRoot = projects.Count > 0
            ? Path.GetDirectoryName(projects[0].projectFile) ?? string.Empty
            : string.Empty;

        var available = string.Join(
            Environment.NewLine + "  - ",
            projects.Select(p => Path.GetRelativePath(solutionRoot, p.projectFile)));

        throw new InputException(
            string.Create(CultureInfo.InvariantCulture,
                $"Project filter '{filter}' matches no project in the solution. ") +
            "Available projects:" + Environment.NewLine + "  - " + available);
    }

    private IReadOnlyList<IProjectAnalysis> LoadProjectAnalyses(string projectFile, string framework, string configuration, string platform, IStrykerOptions options)
    {
        try
        {
            var globalProperties = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!string.IsNullOrEmpty(configuration))
            {
                globalProperties["Configuration"] = configuration;
            }

            if (!string.IsNullOrEmpty(platform))
            {
                globalProperties["Platform"] = platform;
            }

            if (!string.IsNullOrEmpty(framework))
            {
                globalProperties["TargetFramework"] = framework;
            }

            var projectLogName = FileSystem.Path.GetRelativePath(options.WorkingDirectory ?? string.Empty, projectFile);
            LogAnalyzingProjectFile(_logger, projectLogName);

            var analysis = MSBuildProjectAnalysisLoader.LoadAsync(
                _workspaceProvider,
                projectFile,
                globalProperties,
                _logger,
                CancellationToken.None).GetAwaiter().GetResult();

            return [analysis];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFailedToAnalyzeProject(_logger, ex, projectFile);
            return [];
        }
    }

    private IReadOnlyList<IProjectAnalysis> AnalyzeThisProject(
        IReadOnlyList<IProjectAnalysis> analyses,
        string framework,
        IStrykerOptions options,
        List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> mutableProjectsAnalyses)
    {
        IReadOnlyList<IProjectAnalysis> buildResult = analyses;
        if (buildResult.Count == 0)
        {
            mutableProjectsAnalyses.Add((buildResult, false));
            // analysis failed
            return buildResult;
        }

        LogAnalysisResult(buildResult, options);

        var isTestProject = buildResult.IsTestProject();
        if (isTestProject)
        {
            // filter frameworks for test projects (if one is selected)
            buildResult = [SelectAnalysis(buildResult, framework)];
        }

        // Sprint 159 (ADR-039): the project-name filter is no longer applied here.
        // AnalyzeThisProject is now strictly per-project analysis; filter logic is
        // centralised in ApplyProjectFilter, called once at the end of
        // AnalyzeAllNeededProjects with the full analysis collection available.
        mutableProjectsAnalyses.Add((buildResult, isTestProject));

        return buildResult;
    }

    /// <summary>
    /// Scan the references of a project and add them for analysis according to scan option
    /// </summary>
    /// <param name="mode">scan mode</param>
    /// <param name="buildResult">analyses to parse</param>
    /// <returns>A list of project to analyse</returns>
    private List<string> ScanReferences(ScanMode mode, IEnumerable<IProjectAnalysis> buildResult)
    {
        var referencesToAdd = new List<string>();

        if (mode == ScanMode.NoScan || (mode == ScanMode.ScanTestProjectReferences && !buildResult.IsTestProject()))
        {
            return referencesToAdd;
        }

        // Stryker will recursively scan projects
        // add any project reference for progressive discovery (when not using solution file)
        referencesToAdd.AddRange(buildResult.SelectMany(p => p.ProjectReferences).Where(projectReference => FileSystem.File.Exists(projectReference)));

        return referencesToAdd;
    }

    private void LogAnalysisResult(IReadOnlyList<IProjectAnalysis> analyses, IStrykerOptions options)
    {
        // do not log if trace is not enabled
        if (!_logger.IsEnabled(LogLevel.Trace) || !options.DiagMode)
        {
            return;
        }
        if (analyses.Count == 0)
        {
            LogNoAnalysesToLog(_logger);
            return;
        }
        var log = new StringBuilder();
        log.AppendLine("**** Project analysis result ****");

        log.AppendLine(CultureInfo.InvariantCulture, $"Project: {analyses[0].ProjectFilePath}");
        foreach (var analysis in analyses)
        {
            log.AppendLine(CultureInfo.InvariantCulture, $"TargetFramework: {analysis.TargetFramework}");
            log.AppendLine(CultureInfo.InvariantCulture, $"Succeeded: {analysis.Succeeded}");

            foreach (var property in ImportantProperties)
            {
                var value = analysis.GetPropertyOrDefault(property);
                log.AppendLine(CultureInfo.InvariantCulture, $"Property {property}={value ?? "\"'undefined'\""}");
            }
            foreach (var sourceFile in analysis.SourceFiles)
            {
                log.AppendLine(CultureInfo.InvariantCulture, $"SourceFile {sourceFile}");
            }

            foreach (var reference in analysis.References)
            {
                log.AppendLine(CultureInfo.InvariantCulture, $"References: {FileSystem.Path.GetFileName(reference)} (in {FileSystem.Path.GetDirectoryName(reference)})");
            }

            log.AppendLine();
        }
        log.AppendLine("**** End project analysis result ****");
        var logString = log.ToString();
        LogTraceMessage(_logger, logString);
    }

    private IProjectAnalysis SelectAnalysis(IEnumerable<IProjectAnalysis> analyses, string? targetFramework)
    {
        var validResults = analyses.ToList();
        var projectName = validResults.Count > 0 ? validResults[0].ProjectFilePath : string.Empty;
        if (validResults.Count == 0)
        {
            throw new InputException($"No valid project analysis results could be found for '{projectName}'.");
        }

        if (string.IsNullOrEmpty(targetFramework))
        {
            // Sprint 139 (Bug #5 from real-life Calculator-tester report): empty-string
            // targetFramework was previously slipping past the null-check and producing
            // log noise like "Could not find a valid analysis for target  for project ..."
            // (note the doubled space — empty {TargetFramework} placeholder). Treat empty
            // as the same "no target requested" case as null.
            return PickFrameworkVersion();
        }

        var resultForRequestedFramework = validResults.Find(a => string.Equals(a.TargetFramework, targetFramework, StringComparison.Ordinal));
        if (resultForRequestedFramework is not null)
        {
            return resultForRequestedFramework;
        }
        // if there is only one available framework version, we log an info
        if (validResults.Count == 1)
        {
            var single = validResults[0];
            LogSelectedVersionForFramework(_logger, targetFramework, projectName, single.TargetFramework);
            return single;
        }

        var first = PickFrameworkVersion();
        var availableFrameworks = validResults.Select(a => a.TargetFramework).Distinct(StringComparer.Ordinal);
        var firstFramework = first.TargetFramework;
        LogNoValidAnalysisForFramework(_logger, targetFramework, projectName, string.Join(',', availableFrameworks), firstFramework);

        return first;

        IProjectAnalysis PickFrameworkVersion()
        {
            return validResults.Find(a => a.Succeeded && !a.TargetsFullFramework()) ?? validResults[0];
        }
    }

    private (Dictionary<IProjectAnalysis, List<IProjectAnalysis>>, List<IProjectAnalysis>) FindMutableAnalyses(List<(IReadOnlyList<IProjectAnalysis> result, bool isTest)> mutableProjectsAnalyses)
    {
        var analyzerTestProjects = mutableProjectsAnalyses.Where(p => p.isTest).SelectMany(p => p.result).Where(p => p.BuildsAnAssembly());
        var mutableProjects = mutableProjectsAnalyses.Where(p => !p.isTest).SelectMany(p => p.result).Where(p => p.BuildsAnAssembly()).ToArray();

        var mutableToTestMap = mutableProjects.ToDictionary(p => p, _ => new List<IProjectAnalysis>());
        var unusedTestProjects = new List<IProjectAnalysis>();
        // for each test project
        foreach (var testProject in analyzerTestProjects)
        {
            if (ScanAssemblyReferences(mutableToTestMap, mutableProjects, testProject))
            {
                continue;
            }

            LogNoAssemblyRef(_logger, testProject.ProjectFilePath);
            // we try to find a project reference
            if (!ScanProjectReferences(mutableToTestMap, mutableProjects, testProject))
            {
                unusedTestProjects.Add(testProject);
            }
        }

        return (mutableToTestMap, unusedTestProjects);
    }

    private static bool ScanProjectReferences(Dictionary<IProjectAnalysis, List<IProjectAnalysis>> mutableToTestMap, IProjectAnalysis[] mutableProjects, IProjectAnalysis testProject)
    {
        // Sprint 159 (ADR-039 Fix-3): replaces StringComparer.Ordinal — that comparator
        // is case-sensitive on Windows, where paths are case-insensitive, and breaks on
        // any drive-letter/separator/normalisation difference between the two strings
        // even though both come from the same Roslyn solution snapshot. Path.GetFullPath
        // normalises separators + resolves relative segments; OrdinalIgnoreCase covers
        // the Windows case-insensitive filesystem semantics.
        var mutableProject = mutableProjects.FirstOrDefault(p => testProject.ProjectReferences.Any(pr =>
            string.Equals(Path.GetFullPath(pr), Path.GetFullPath(p.ProjectFilePath), StringComparison.OrdinalIgnoreCase)));
        if (mutableProject == null)
        {
            return false;
        }
        if (!mutableToTestMap.TryGetValue(mutableProject, out var dependencies))
        {
            mutableToTestMap[mutableProject] = dependencies = [];
        }

        dependencies.Add(testProject);
        return true;
    }

    private static bool ScanAssemblyReferences(Dictionary<IProjectAnalysis, List<IProjectAnalysis>> mutableToTestMap, IProjectAnalysis[] mutableProjects, IProjectAnalysis testProject)
    {
        var foundOneProject = false;
        // we identify which project are referenced by it
        foreach (var mutableProject in mutableProjects)
        {
            var assemblyPath = mutableProject.GetAssemblyPath();
            var refAssemblyPath = mutableProject.GetReferenceAssemblyPath();

            if (testProject.References.All(r => !r.Equals(assemblyPath, StringComparison.OrdinalIgnoreCase) &&
                                    !r.Equals(refAssemblyPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }
            if (!mutableToTestMap.TryGetValue(mutableProject, out var dependencies))
            {
                mutableToTestMap[mutableProject] = dependencies = [];
            }
            dependencies.Add(testProject);
            foundOneProject = true;
        }

        return foundOneProject;
    }

    /// <summary>
    /// Builds a <see cref="SourceProjectInfo"/> instance describing a project its associated test project(s)
    /// </summary>
    /// <param name="options">Stryker options</param>
    /// <param name="solutionInfo"></param>
    /// <param name="analysis">project analysis</param>
    /// <param name="testAnalyses">test project(s) analyses</param>
    /// <returns></returns>
    private SourceProjectInfo BuildSourceProjectInfo(IStrykerOptions options,
        Stryker.Core.ProjectComponents.SourceProjects.SolutionInfo solutionInfo,
        IProjectAnalysis analysis,
        IEnumerable<IProjectAnalysis> testAnalyses)
    {
        var targetProjectInfo = new SourceProjectInfo
        {
            Analysis = analysis
        };

        var language = targetProjectInfo.Analysis.GetLanguage();
        if (language == Language.Fsharp)
        {
            LogError(_logger, targetProjectInfo.LogError(
                    "Mutation testing of F# projects is not ready yet. No mutants will be generated."));
        }

        var builder = (ProjectComponentsBuilder)(language switch
        {
            Language.Csharp => new CsharpProjectComponentsBuilder(
                targetProjectInfo,
                options,
                _foldersToExclude,
                _logger,
                FileSystem),

            _ => throw new NotSupportedException($"Language not supported: {language}")
        });

        var inputFiles = builder.Build();
        builder.InjectHelpers(inputFiles);
        targetProjectInfo.OnProjectBuilt = builder.PostBuildAction();
        targetProjectInfo.ProjectContents = inputFiles;
        targetProjectInfo.SolutionInfo = solutionInfo;
        LogFoundProjectToMutate(_logger, analysis.ProjectFilePath);
        targetProjectInfo.TestProjectsInfo = new TestProjectsInfo(FileSystem)
        {
            TestProjects = testAnalyses.Select(testProjectAnalysis => new TestProject(FileSystem, testProjectAnalysis)).ToList()
        };
        return targetProjectInfo;
    }

    private string FindProjectFile(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            throw new ArgumentNullException(path, "Project path cannot be null or empty.");
        }

        if (FileSystem.File.Exists(path) && (FileSystem.Path.HasExtension(".csproj") || FileSystem.Path.HasExtension(".fsproj")))
        {
            return path;
        }

        string[] projectFiles;
        try
        {
            projectFiles = [.. FileSystem.Directory.GetFiles(path, "*.*?sproj").Where(file => file.EndsWith("csproj", StringComparison.OrdinalIgnoreCase) || file.EndsWith("fsproj", StringComparison.OrdinalIgnoreCase))];
        }
        catch (DirectoryNotFoundException)
        {
            throw new InputException($"No .csproj or .fsproj file found, please check your project directory at {path}");
        }

        LogScannedDirectory(_logger, path, projectFiles);

        switch (projectFiles.Length)
        {
            case > 1:
            {
                var sb = new StringBuilder();
                sb.AppendLine("Expected exactly one .csproj file, found more than one:");
                foreach (var file in projectFiles)
                {
                    sb.AppendLine(file);
                }
                sb.AppendLine().AppendLine("Please specify a test project name filter that results in one project.");
                throw new InputException(sb.ToString());
            }
            case 0:
                throw new InputException($"No .csproj or .fsproj file found, please check your project or solution directory at {path}");
            default:
                var single = projectFiles.Single();
                LogFoundProjectFile(_logger, single, path);
                return single;
        }
    }

    private static StringBuilder BuildReferenceChoice(IEnumerable<string> projectReferences)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Choose one of the following references:").AppendLine("");

        foreach (var projectReference in projectReferences)
        {
            builder.Append("  ").AppendLine(NormalizePath(projectReference));
        }
        return builder;
    }

    private static string NormalizePath(string path) => path?.Replace('\\', '/') ?? string.Empty;

    private static string NormalizePlatform(string platform) =>
        string.Equals(platform, "Any CPU", StringComparison.OrdinalIgnoreCase) ? "AnyCPU" : platform;

    // Sequential queue used for progressive discovery. MSBuildWorkspace is not safe for parallel
    // project loads (the underlying Microsoft.Build engine and the workspace's diagnostics list
    // are not thread-safe), so we replaced the previous Parallel.ForEach loop with a sequential
    // walk in Phase 9b.
    private sealed class SequentialEnumerableQueue<T> where T : notnull
    {
        private readonly Queue<T> _queue;
        private readonly HashSet<T> _seen;

        public SequentialEnumerableQueue(IEnumerable<T> init)
        {
            _seen = [.. init];
            _queue = new Queue<T>(_seen);
        }

        public bool Empty => _queue.Count == 0;

        public void Add(T entry)
        {
            if (!_seen.Add(entry))
            {
                return;
            }
            _queue.Enqueue(entry);
        }

        public IEnumerable<T> Consume()
        {
            while (_queue.Count > 0)
            {
                yield return _queue.Dequeue();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading solution file {SolutionFile}.")]
    private static partial void LogLoadingSolution(ILogger logger, string solutionFile);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Failed to load solution file {SolutionFile}.")]
    private static partial void LogFailedLoadSolution(ILogger logger, Exception ex, string solutionFile);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Failed to access solution file {SolutionFile}.")]
    private static partial void LogFailedAccessSolution(ILogger logger, Exception ex, string solutionFile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Analyzing {ProjectCount} test project(s).")]
    private static partial void LogAnalyzingTestProjects(ILogger logger, int projectCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "--all-projects mode: mutating {ProjectCount} referenced source projects sequentially.")]
    private static partial void LogAllProjectsMode(ILogger logger, int projectCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Assume working directory contains target project to be mutated.")]
    private static partial void LogAssumeWorkingDirContainsTarget(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Working directory contains a test project.")]
    private static partial void LogWorkingDirIsTest(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using solution configuration/platform '{Configuration}|{Platform}'.")]
    private static partial void LogUsingSolutionConfigPlatform(ILogger logger, string configuration, string platform);

    [LoggerMessage(Level = LogLevel.Error, Message = "Project {ProjectFile} could not be found as a project referenced by the provided test projects.")]
    private static partial void LogProjectNotReferencedByTests(ILogger logger, string projectFile);

    [LoggerMessage(Level = LogLevel.Error, Message = "No project could be found as a project referenced by the provided test projects.")]
    private static partial void LogNoProjectReferencedByTests(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Selected project {ProjectFile} as it is referenced by all provided test projects.")]
    private static partial void LogSelectedProject(ILogger logger, string projectFile);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stryker will mutate solution {Solution}.")]
    private static partial void LogMutatingSolution(ILogger logger, string solution);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Using configuration/platform '{ActualBuildType}|{ActualPlatform}' instead of requested '{Configuration}|{Platform}'.")]
    private static partial void LogConfigPlatformInsteadOf(ILogger logger, string actualBuildType, string actualPlatform, string configuration, string platform);

    [LoggerMessage(Level = LogLevel.Information, Message = "Using configuration/platform '{Configuration}|{Platform}'.")]
    private static partial void LogUsingConfigPlatform(ILogger logger, string configuration, string platform);

    [LoggerMessage(Level = LogLevel.Information, Message = "Identifying projects to mutate in {Solution}. This can take a while.")]
    private static partial void LogIdentifyingProjects(ILogger logger, string solution);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Analyzing {ProjectCount} projects.")]
    private static partial void LogAnalyzingProjects(ILogger logger, int projectCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Using {ProjectFile} as test project")]
    private static partial void LogUsingTestProject(ILogger logger, string projectFile);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Analyzing {Count} projects.")]
    private static partial void LogAnalyzingProjectCount(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Project analysis failed.")]
    private static partial void LogProjectAnalysisFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No project found, check settings and ensure project file is not corrupted.\nUse --diag option to have the analysis logs in the log file.")]
    private static partial void LogNoProjectFound(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Project {ProjectPath} analysis {Result}.")]
    private static partial void LogProjectAnalysisResult(ILogger logger, string projectPath, string result);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  can't be mutated because no test project references it. If this is a test project, ensure it has the property: <IsTestProject>true</IsTestProject> in its project file.")]
    private static partial void LogProjectNoTestRef(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "  referenced by test project {ProjectName}, analysis {Result}.")]
    private static partial void LogReferencedByTestProject(ILogger logger, string projectName, string result);

    [LoggerMessage(Level = LogLevel.Information, Message = "  can be mutated.")]
    private static partial void LogCanBeMutated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "  can't be mutated because all referencing test projects' analysis failed.")]
    private static partial void LogCantBeMutated(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Test project {ProjectName} does not appear to test any mutable project, analysis {Result}.")]
    private static partial void LogUnusedTestProject(ILogger logger, string projectName, string result);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Use --diag option to have the analysis logs in the log file.")]
    private static partial void LogDiagOptionTip(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Analyzing {ProjectFilePath}")]
    private static partial void LogAnalyzingProjectFile(ILogger logger, string projectFilePath);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to analyze project {Project}.")]
    private static partial void LogFailedToAnalyzeProject(ILogger logger, Exception ex, string project);

    [LoggerMessage(Level = LogLevel.Trace, Message = "No analyses to log. This indicates an early failure in analysis, check log file for details.")]
    private static partial void LogNoAnalysesToLog(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "{Log}")]
    private static partial void LogTraceMessage(ILogger logger, string log);

    [LoggerMessage(Level = LogLevel.Information, Message = "Could not find a valid analysis for target {TargetFramework} for project '{ProjectName}'. Selected version is {SelectedVersion}.")]
    private static partial void LogSelectedVersionForFramework(ILogger logger, string targetFramework, string projectName, string selectedVersion);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not find a valid analysis for target {TargetFramework} for project '{ProjectName}'.\nThe available target frameworks are: {AvailableFrameworks}.\n     selected version is {SelectedVersion}.")]
    private static partial void LogNoValidAnalysisForFramework(ILogger logger, string targetFramework, string projectName, string availableFrameworks, string selectedVersion);

    [LoggerMessage(Level = LogLevel.Information, Message = "Could not find an assembly reference to a mutable assembly for project {ProjectName}. Will look into project references.")]
    private static partial void LogNoAssemblyRef(ILogger logger, string projectName);

    // Sprint 159 (ADR-039 Layer 3): emitted when ApplyProjectFilter's zero-match
    // safety-net kicks in — filter resolved to no source projects, so we retry
    // without the filter rather than failing opaquely. Typically caused by a
    // filter that matches a source project whose build itself failed.
    [LoggerMessage(Level = LogLevel.Warning, Message = "Project filter '{Filter}' produced no source projects. Retrying with all source projects.")]
    private static partial void LogFilterFallback(ILogger logger, string filter);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Error}")]
    private static partial void LogError(ILogger logger, string error);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found project {ProjectFileName} to mutate.")]
    private static partial void LogFoundProjectToMutate(ILogger logger, string projectFileName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Scanned the directory {Path} for *.csproj files: found {ProjectFilesCount}")]
    private static partial void LogScannedDirectory(ILogger logger, string path, string[] projectFilesCount);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Found project file {File} in path {Path}")]
    private static partial void LogFoundProjectFile(ILogger logger, string file, string path);
}
