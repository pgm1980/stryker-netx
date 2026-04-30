using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
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

        // Mutate source files
        foreach (var file in projectInfo.GetAllFiles().Cast<CsharpFileLeaf>())
        {
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Mutating {FilePath}")]
    private static partial void LogMutating(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Mutated {FullPath}:{NewLine}{MutatedSyntaxTree}")]
    private static partial void LogMutatedTree(ILogger logger, string fullPath, string newLine, Microsoft.CodeAnalysis.Text.SourceText mutatedSyntaxTree);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{MutantsCount} mutants created")]
    private static partial void LogMutantsCount(ILogger logger, int mutantsCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Injected the mutated assembly file into {InjectionPath}")]
    private static partial void LogInjectedAssembly(ILogger logger, string injectionPath);
}
