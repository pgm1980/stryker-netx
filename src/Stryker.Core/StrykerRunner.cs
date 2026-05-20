using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Initialisation;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters;

namespace Stryker.Core;

public partial class StrykerRunner(
    IReporterFactory reporterFactory,
    IProjectOrchestrator projectOrchestrator,
    ILogger<StrykerRunner> logger) : IStrykerRunner
{
    private IEnumerable<IMutationTestProcess> _mutationTestProcesses = [];
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IReporterFactory _reporterFactory = reporterFactory ?? throw new ArgumentNullException(nameof(reporterFactory));
    private readonly IProjectOrchestrator _projectOrchestrator = projectOrchestrator ?? throw new ArgumentNullException(nameof(projectOrchestrator));

    /// <summary>
    /// Starts a mutation test run
    /// </summary>
    /// <param name="inputs">user options</param>
    /// <exception cref="InputException">For managed exceptions</exception>
    public async Task<StrykerRunResult> RunMutationTestAsync(IStrykerInputs inputs)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var options = inputs.ValidateAll();
        LogStrykerStartedWithOptions(_logger, options);

        var reporters = _reporterFactory.Create(options);

        try
        {
            return await ExecutePipelineAsync(options, reporters).ConfigureAwait(false);
        }
#if !DEBUG
        // S2139: log-and-rethrow is intentional here — in non-DEBUG builds the user sees a
        // contextual log entry on the console while still letting the exception propagate to
        // produce a non-zero exit code. In DEBUG builds the catch is omitted so the debugger
        // breaks at the throw site. Matches upstream Stryker.NET 4.14.1 behaviour exactly.
#pragma warning disable S2139
        catch (Exception ex) when (ex is not InputException)
        {
            LogMutationTestError(_logger, ex);
            throw;
        }
#pragma warning restore S2139

#endif
        finally
        {
            // log duration
            stopwatch.Stop();
            LogTimeElapsed(_logger, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Sprint 166 (ADR-046 §C): extracted from <see cref="RunMutationTestAsync"/> to
    /// keep that method under MA0051's 60-line cap after the diagnostic short-circuit
    /// addition. Same refactor pattern as Sprint 148 BuildCommandLineApplication.
    /// </summary>
    private async Task<StrykerRunResult> ExecutePipelineAsync(IStrykerOptions options, IReporter reporters)
    {
        // Mutate
        _mutationTestProcesses = [.. await _projectOrchestrator.MutateProjectsAsync(options, reporters).ConfigureAwait(false)];

        // Sprint 166 (ADR-046 §C, Aisess Wishlist #9): diagnostic short-circuit.
        // The non-None sentinel is the authoritative "stopped early" signal —
        // _mutationTestProcesses may be empty (analysis/build/initial-test-run
        // break-points) OR populated (mutation-generation), so we cannot rely on
        // the count alone.
        if (options.BreakAfter != BreakAfterPhase.None)
        {
            return CompleteEarlyForDiagnostic(options, reporters);
        }

        var rootComponent = AddRootFolderIfMultiProject([.. _mutationTestProcesses.Select(x => x.Input.SourceProjectInfo.ProjectContents)], options);
        var combinedTestProjectsInfo = _mutationTestProcesses.Select(mtp => mtp.Input.TestProjectsInfo).Aggregate((a, b) => (TestProjectsInfo)a + (TestProjectsInfo)b);

        LogMutantsCountIfEnabled(rootComponent);
        AnalyzeCoverage(options);

        // Filter
        foreach (var project in _mutationTestProcesses)
        {
            project.FilterMutants();
        }

        // Report
        reporters.OnMutantsCreated(rootComponent, combinedTestProjectsInfo);

        var mutantsNotRun = rootComponent.NotRunMutants().ToList();
        if (mutantsNotRun.Count == 0)
        {
            return CompleteWithoutRun(options, rootComponent, combinedTestProjectsInfo, reporters);
        }

        return await ExecuteMutationTestAsync(options, rootComponent, combinedTestProjectsInfo, reporters, mutantsNotRun).ConfigureAwait(false);
    }

    /// <summary>
    /// Sprint 166 (ADR-046 §C, Aisess Wishlist #9): clean diagnostic exit path for
    /// the <c>--break-after</c> flag. Disposes the test runner and emits a partial
    /// "what would have been tested" HTML/JSON report when the break-point was
    /// <see cref="BreakAfterPhase.MutationGeneration"/> (i.e. mutants were generated
    /// but never tested). For earlier break-points (analysis/build/initial-test-run),
    /// no mutants exist yet, so reporter callbacks are skipped.
    /// </summary>
    private StrykerRunResult CompleteEarlyForDiagnostic(IStrykerOptions options, IReporter reporters)
    {
        // CA1873 false-positive: source-gen [LoggerMessage] partials check IsEnabled
        // internally, but the analyzer cannot see through the .ToString() call. Guard
        // it manually. Same pattern as Sprint 163 (ADR-043) HeartbeatLogger.
#pragma warning disable CA1873 // Avoid potentially expensive logging argument evaluation
        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogDiagnosticEarlyExit(_logger, options.BreakAfter.ToString());
        }
#pragma warning restore CA1873

        // For mutation-generation break-point, flush a partial OnMutantsCreated report
        // so the user sees the would-be mutants list. For earlier break-points there
        // are no mutants to report — just dispose and return.
        if (options.BreakAfter == BreakAfterPhase.MutationGeneration && _mutationTestProcesses.Any())
        {
            var rootComponent = AddRootFolderIfMultiProject([.. _mutationTestProcesses.Select(x => x.Input.SourceProjectInfo.ProjectContents)], options);
            var combinedTestProjectsInfo = _mutationTestProcesses.Select(mtp => mtp.Input.TestProjectsInfo).Aggregate((a, b) => (TestProjectsInfo)a + (TestProjectsInfo)b);
            LogMutantsCountIfEnabled(rootComponent);
            reporters.OnMutantsCreated(rootComponent, combinedTestProjectsInfo);
            _projectOrchestrator.Dispose();
            return new StrykerRunResult(options, rootComponent.GetMutationScore());
        }

        _projectOrchestrator.Dispose();
        return new StrykerRunResult(options, double.NaN);
    }

    private StrykerRunResult CompleteWithoutRun(IStrykerOptions options, IReadOnlyProjectComponent rootComponent, ITestProjectsInfo combinedTestProjectsInfo, IReporter reporters)
    {
        var allMutants = rootComponent.Mutants.ToList();
        if (allMutants.Any(x => x.ResultStatus == MutantStatus.Ignored))
        {
            LogAllIgnored(_logger);
        }
        if (allMutants.Any(x => x.ResultStatus == MutantStatus.NoCoverage))
        {
            LogAllNoCoverage(_logger);
        }
        if (allMutants.Any(x => x.ResultStatus == MutantStatus.CompileError))
        {
            LogAllCompileErrors(_logger);
        }
        if (allMutants.Count == 0)
        {
            LogNothingToTest(_logger);
        }

        reporters.OnAllMutantsTested(rootComponent, combinedTestProjectsInfo);
        _projectOrchestrator.Dispose();
        return new StrykerRunResult(options, rootComponent.GetMutationScore());
    }

    private async Task<StrykerRunResult> ExecuteMutationTestAsync(IStrykerOptions options, IReadOnlyProjectComponent rootComponent, ITestProjectsInfo combinedTestProjectsInfo, IReporter reporters, IReadOnlyList<IReadOnlyMutant> mutantsNotRun)
    {
        // Report
        reporters.OnStartMutantTestRun(mutantsNotRun);

        // Test
        foreach (var project in _mutationTestProcesses)
        {
            await project.TestAsync([.. project.Input.SourceProjectInfo.ProjectContents.Mutants.Where(x => x.ResultStatus == MutantStatus.Pending)]).ConfigureAwait(false);
        }
        // dispose and stop runners
        _projectOrchestrator.Dispose();

        // Restore assemblies
        foreach (var project in _mutationTestProcesses)
        {
            project.Restore();
        }

        reporters.OnAllMutantsTested(rootComponent, combinedTestProjectsInfo);

        return new StrykerRunResult(options, rootComponent.GetMutationScore());
    }

    private void AnalyzeCoverage(IStrykerOptions options)
    {
        if (options.OptimizationMode.HasFlag(OptimizationModes.SkipUncoveredMutants) || options.OptimizationMode.HasFlag(OptimizationModes.CoverageBasedTest))
        {
            LogCaptureCoverage(_logger, options.OptimizationMode);

            foreach (var project in _mutationTestProcesses)
            {
                project.GetCoverage();
            }
        }
    }

    /// <summary>
    /// In the case of multiple projects we wrap them inside a wrapper root component. Otherwise the only project root will be the root component.
    /// </summary>
    /// <param name="projectComponents">A list of all project root components</param>
    /// <param name="options">The current stryker options</param>
    /// <returns>The root folder component</returns>
    private static IReadOnlyProjectComponent AddRootFolderIfMultiProject(IEnumerable<IReadOnlyProjectComponent> projectComponents, IStrykerOptions options)
    {
        if (!projectComponents.Any())
        {
            throw new NoTestProjectsException();
        }

        if (projectComponents.Count() > 1)
        {
            var rootComponent = new Solution
            {
                FullPath = options.ProjectPath ?? string.Empty // in case of a solution run the basePath will be where the solution file is
            };
            rootComponent.AddRange(projectComponents.Cast<IProjectComponent>());
            return rootComponent;
        }

        return projectComponents.FirstOrDefault() ?? throw new InvalidOperationException("No project component found.");
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stryker started with options: {Options}")]
    private static partial void LogStrykerStartedWithOptions(ILogger logger, IStrykerOptions options);

    [LoggerMessage(Level = LogLevel.Information, Message = "{MutantsCount} mutants created")]
    private static partial void LogMutantsCount(ILogger logger, int mutantsCount);

    private void LogMutantsCountIfEnabled(IReadOnlyProjectComponent rootComponent)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }
        var mutantsCount = rootComponent.Mutants.Count();
        LogMutantsCount(_logger, mutantsCount);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred during the mutation test run ")]
    private static partial void LogMutationTestError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Time Elapsed {Duration}")]
    private static partial void LogTimeElapsed(ILogger logger, TimeSpan duration);

    [LoggerMessage(Level = LogLevel.Warning, Message = "It looks like all mutants with tests were ignored. Try a re-run with less ignoring!")]
    private static partial void LogAllIgnored(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "It looks like all non-ignored mutants are not covered by a test. Go add some tests!")]
    private static partial void LogAllNoCoverage(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "It looks like all mutants resulted in compile errors. Mutants sure are strange!")]
    private static partial void LogAllCompileErrors(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "It's a mutant-free world, nothing to test.")]
    private static partial void LogNothingToTest(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Capture mutant coverage using '{OptimizationMode}' mode.")]
    private static partial void LogCaptureCoverage(ILogger logger, OptimizationModes optimizationMode);

    // Sprint 166 (ADR-046 §C, Aisess Wishlist #9): emitted when --break-after has
    // triggered a clean diagnostic exit at the named phase. Different from the
    // per-phase log emitted by ProjectOrchestrator — this one runs at the runner
    // level after the orchestrator has returned.
    [LoggerMessage(Level = LogLevel.Information, Message = "Stryker exits cleanly after the --break-after '{Phase}' diagnostic break-point. The per-mutant test loop did not execute.")]
    private static partial void LogDiagnosticEarlyExit(ILogger logger, string phase);
}
