using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Utilities;
using Stryker.Core.Compiling;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.MutationTest;

public partial class CsharpMutationProcess : IMutationProcess
{
    private IStrykerOptions _options = null!; // initialized in Mutate() before any other call
    private IMutantFilter _mutantFilter = null!; // initialized in FilterMutants() before any other call
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public CsharpMutationProcess(
        IFileSystem fileSystem,
        ILogger<CsharpMutationProcess> logger)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _logger = logger;
    }

    public void Mutate(MutationTestInput input, IStrykerOptions options)
    {
        _options = options;
        var projectInfo = input.SourceProjectInfo.ProjectContents;
        var orchestrator = new CsharpMutantOrchestrator(new MutantPlacer(input.SourceProjectInfo.CodeInjector), options: _options);
        var compilingProcess = new CsharpCompilingProcess(input, options: _options);
        var semanticModels = compilingProcess.GetSemanticModels(projectInfo.GetAllFiles().Cast<CsharpFileLeaf>().Select(x => x.SyntaxTree));

        // Sprint 166 Phase A (ADR-046 §A, Aisess §8 + Wishlist #4): file-level
        // --mutate scope filter. Counter for the Wishlist #7 startup-summary log.
        var scannedFiles = 0;
        var skippedFiles = 0;

        // Mutate source files
        foreach (var file in projectInfo.GetAllFiles().Cast<CsharpFileLeaf>())
        {
            // Sprint 166 Phase A: skip files outside the --mutate scope entirely.
            // This avoids spurious ERR-logs for broken disable-directives in files
            // the user did NOT ask to mutate (Aisess Anomalies Report §8). For the
            // default --mutate=**/* pattern, every file is in scope (no regression).
            if (!IsFileInMutateScope(file, _options))
            {
                LogSkippedOutsideMutateScope(_logger, file.FullPath);
                file.Mutants = [];
                skippedFiles++;
                continue;
            }
            scannedFiles++;
            LogMutating(_logger, file.FullPath);
            // Mutate the syntax tree
            var mutatedSyntaxTree = orchestrator.Mutate(file.SyntaxTree, semanticModels.First(x => x.SyntaxTree == file.SyntaxTree));
            // Add the mutated syntax tree for compilation
            file.MutatedSyntaxTree = mutatedSyntaxTree;
            if (_options.DiagMode && _logger.IsEnabled(LogLevel.Trace))
            {
                var text = mutatedSyntaxTree.GetText();
                LogMutatedTree(_logger, file.FullPath, Environment.NewLine, text);
            }
            // Filter the mutants
            file.Mutants = orchestrator.GetLatestMutantBatch();
        }

        // Sprint 166 Phase A (ADR-046 §A, Wishlist #7): single startup-summary
        // INF-log after the per-file walk. Lets the user see scope + skip count
        // at-a-glance without grepping through per-file LogMutating debug-log.
        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogDisableDirectiveValidationSummary(_logger, scannedFiles, skippedFiles);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var count = projectInfo.Mutants.Count();
            LogMutantsCount(_logger, count);
        }

        CompileMutations(input, compilingProcess);
    }

    private void CompileMutations(MutationTestInput input, CsharpCompilingProcess compilingProcess)
    {
        var info = input.SourceProjectInfo;
        var projectInfo = (ProjectComponent<SyntaxTree>)info.ProjectContents;
        using var ms = new MemoryStream();
        using var msForSymbols = _options.DiagMode ? new MemoryStream() : null;
        // compile the mutated syntax trees
        var compileResult = compilingProcess.Compile(projectInfo.CompilationSyntaxTrees, ms, msForSymbols);

        foreach (var testProject in info.TestProjectsInfo.Analyses)
        {
            var injectionPath = TestProjectsInfo.GetInjectionFilePath(testProject, input.SourceProjectInfo.Analysis);
            if (!_fileSystem.Directory.Exists(testProject.GetAssemblyDirectoryPath()))
            {
                _fileSystem.Directory.CreateDirectory(testProject.GetAssemblyDirectoryPath());
            }

            // inject the mutated Assembly into the test project
            using var fs = _fileSystem.File.Create(injectionPath);
            ms.Position = 0;
            ms.CopyTo(fs);

            if (msForSymbols != null)
            {
                // inject the debug symbols into the test project
                using var symbolDestination = _fileSystem.File.Create(Path.Combine(testProject.GetAssemblyDirectoryPath(), input.SourceProjectInfo.Analysis.GetSymbolFileName()));
                msForSymbols.Position = 0;
                msForSymbols.CopyTo(symbolDestination);
            }

            LogInjectedAssembly(_logger, injectionPath);
        }

        // if a rollback took place, mark the rolled back mutants as status:BuildError
        if (compileResult.RollbackedIds.Any())
        {
            foreach (var mutant in projectInfo.Mutants
                .Where(x => compileResult.RollbackedIds.Contains(x.Id)))
            {
                // Ignore compilation errors if the mutation is skipped anyways.
                if (mutant.ResultStatus == MutantStatus.Ignored)
                {
                    continue;
                }

                mutant.ResultStatus = MutantStatus.CompileError;
                mutant.ResultStatusReason = "Mutant caused compile errors";
            }
        }
    }

    public void FilterMutants(MutationTestInput input)
    {
        _mutantFilter ??= MutantFilterFactory.Create(_options, input);
        foreach (var file in input.SourceProjectInfo.ProjectContents.GetAllFiles())
        {
            // CompileError is a final status and can not be changed during filtering.
            var mutantsToFilter = file.Mutants.Where(x => x.ResultStatus != MutantStatus.CompileError);
            _mutantFilter.FilterMutants(mutantsToFilter, file, _options);
        }
    }

    /// <summary>
    /// Sprint 166 Phase A (ADR-046 §A, Aisess §8 + Wishlist #4): per-file scope
    /// check before orchestration. Mirrors <see cref="FilePatternMutantFilter"/>'s
    /// pattern logic but at the FILE level — no <c>TextSpan</c> consideration.
    /// File-level scope is a superset of per-mutation scope, so spans like
    /// <c>MyService.cs{1..10}</c> still pre-pass at the file level here, then are
    /// narrowed by <see cref="FilePatternMutantFilter"/> in the downstream
    /// filter step. Default <c>**/*</c> pattern keeps every file in scope (no
    /// regression).
    /// </summary>
    private static bool IsFileInMutateScope(CsharpFileLeaf file, IStrykerOptions options)
    {
        var includes = options.Mutate.Where(p => !p.IsExclude).ToList();
        var excludes = options.Mutate.Where(p => p.IsExclude).ToList();

        // Defensive: no positive constraint = treat as include-all. In normal flow
        // MutateInput.Validate ensures at least one include pattern exists (the
        // default `**/*` is added when only exclude patterns are supplied), so this
        // branch is a safety-net.
        if (includes.Count == 0)
        {
            return true;
        }

        var fullPath = FilePathUtils.NormalizePathSeparators(file.FullPath);
        var relativePath = FilePathUtils.NormalizePathSeparators(file.RelativePath);

        // Check glob-only (skip the span check that FilePattern.IsMatch performs).
        // At file-level we cannot know mutation spans yet, and a file with ANY
        // in-scope mutation positions must be orchestrated to discover them.
        var matchesInclude = includes.Exists(p =>
            p.Glob.IsMatch(fullPath) || p.Glob.IsMatch(relativePath));
        if (!matchesInclude)
        {
            return false;
        }

        var matchesExclude = excludes.Exists(p =>
            p.Glob.IsMatch(fullPath) || p.Glob.IsMatch(relativePath));
        return !matchesExclude;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mutating {FilePath}")]
    private static partial void LogMutating(ILogger logger, string filePath);

    // Sprint 166 Phase A (ADR-046 §A, Aisess §8 + Wishlist #4): emitted when a file
    // is skipped because it does not match any positive --mutate include pattern
    // (or matches an exclude pattern). Debug-level by design — the user's --mutate
    // is explicit; per-file confirmation is only interesting under --diag.
    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping {FilePath} — outside --mutate scope; disable-directives in this file are not parsed.")]
    private static partial void LogSkippedOutsideMutateScope(ILogger logger, string filePath);

    // Sprint 166 Phase A (ADR-046 §A, Aisess Wishlist #7): single startup-summary
    // after the per-file orchestration walk. Replaces the "spammy per-file" UX of
    // pre-Sprint-166 with a single high-level "validated N files (M skipped)" line.
    [LoggerMessage(Level = LogLevel.Information, Message = "Disable-directive validation: scanned {ScannedFiles} files in --mutate scope ({SkippedFiles} skipped).")]
    private static partial void LogDisableDirectiveValidationSummary(ILogger logger, int scannedFiles, int skippedFiles);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Mutated {FullPath}:{NewLine}{MutatedSyntaxTree}")]
    private static partial void LogMutatedTree(ILogger logger, string fullPath, string newLine, Microsoft.CodeAnalysis.Text.SourceText mutatedSyntaxTree);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{MutantsCount} mutants created")]
    private static partial void LogMutantsCount(ILogger logger, int mutantsCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Injected the mutated assembly file into {InjectionPath}")]
    private static partial void LogInjectedAssembly(ILogger logger, string injectionPath);
}
