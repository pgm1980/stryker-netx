using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.MutationTest;
using Stryker.Utilities.EmbeddedResources;
using Stryker.Utilities.Logging;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.Compiling;

/// <summary>
/// This process is in control of compiling the assembly and rolling back mutations that cannot compile
/// Compiles the given input onto the memory stream
/// </summary>
public partial class CsharpCompilingProcess : ICSharpCompilingProcess
{
    private const int MaxAttempt = 50;
    private readonly MutationTestInput _input;
    private readonly IStrykerOptions _options;
    private readonly ICSharpRollbackProcess _rollbackProcess;
    private readonly ILogger _logger;

    public CsharpCompilingProcess(MutationTestInput input,
        ICSharpRollbackProcess? rollbackProcess = null,
        IStrykerOptions? options = null)
    {
        _input = input;
        _options = options ?? new StrykerOptions();
        _rollbackProcess = rollbackProcess ?? new CSharpRollbackProcess();
        _logger = ApplicationLogging.LoggerFactory.CreateLogger<CsharpCompilingProcess>();
    }

    private string AssemblyName =>
        _input.SourceProjectInfo.Analysis.GetAssemblyName();

    /// <summary>
    /// Compiles the given input onto the memory stream
    /// The compiling process is closely related to the rollback process. When the initial compilation fails, the rollback process will be executed.
    /// <param name="syntaxTrees">The syntax trees to compile</param>
    /// <param name="ilStream">The memory stream to store the compilation result onto</param>
    /// <param name="symbolStream">The memory stream to store the debug symbol</param>
    /// </summary>
    public CompilingProcessResult Compile(IEnumerable<SyntaxTree> syntaxTrees, Stream ilStream, Stream? symbolStream)
    {
        var compilation = GetCSharpCompilation(syntaxTrees);

        // first try compiling
        var retryCount = 1;
        (var rollbackProcessResult, var emitResult, retryCount) = TryCompilation(ilStream, symbolStream, ref compilation, previousEmitResult: null, lastAttempt: false, retryCount);

        // If compiling failed and the error has no location, log and throw exception.
        if (!emitResult.Success && emitResult.Diagnostics.Any(diagnostic => diagnostic.Location == Location.None && diagnostic.Severity == DiagnosticSeverity.Error))
        {
            LogUnrecoverableError(_logger,
                emitResult.Diagnostics.First(diagnostic => diagnostic.Location == Location.None && diagnostic.Severity == DiagnosticSeverity.Error));
            DumpErrorDetails(emitResult.Diagnostics);
            throw new CompilationException("General Build Failure detected.");
        }

        for (var count = 1; !emitResult.Success && count < MaxAttempt; count++)
        {
            // compilation did not succeed. let's compile a couple of times more for good measure
            (rollbackProcessResult, emitResult, retryCount) = TryCompilation(ilStream, symbolStream, ref compilation, emitResult, retryCount == MaxAttempt - 1, retryCount);
        }

        if (emitResult.Success)
        {
            return new(
                true,
                rollbackProcessResult?.RollbackedIds ?? Enumerable.Empty<int>());
        }
        // compiling failed
        LogFailedToRestore(_logger);
        DumpErrorDetails(emitResult.Diagnostics);
        throw new CompilationException("Failed to restore build able state.");
    }

    /// <summary>
    /// Analyzes the syntax trees and returns the semantic models
    /// </summary>
    /// <param name="syntaxTrees">The syntax trees to analyze</param>
    /// <returns>Semantic models</returns>
    public IEnumerable<SemanticModel> GetSemanticModels(IEnumerable<SyntaxTree> syntaxTrees)
    {
        var compilation = GetCSharpCompilation(syntaxTrees);

        // extract semantic models from compilation
        var semanticModels = new List<SemanticModel>();
        foreach (var tree in syntaxTrees)
        {
            semanticModels.Add(compilation.GetSemanticModel(tree));
        }
        return semanticModels;
    }

    private static readonly string[] IgnoredErrors = ["RZ3600", "CS8784"];

    // Can't test or mock code generators, so we exclude them from coverage
    [ExcludeFromCodeCoverage]
    private CSharpCompilation RunSourceGenerators(IProjectAnalysis analysis, Compilation compilation)
    {
        var generators = analysis.GetSourceGenerators(_logger);
        _ = CSharpGeneratorDriver
            .Create(generators, parseOptions: analysis.GetParseOptions(_options), optionsProvider: new SimpleAnalyserConfigOptionsProvider(analysis))
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var errors = diagnostics.Where(diagnostic => IgnoredErrors.Contains(diagnostic.Id) || (diagnostic.Severity == DiagnosticSeverity.Error && diagnostic.Location == Location.None)).ToList();
        if (errors.Count == 0)
        {
            // outputCompilation is non-null after RunGeneratorsAndUpdateCompilation; cast to CSharpCompilation is safe for our pipeline.
            return (outputCompilation as CSharpCompilation)!;
        }
        var fail = false;
        foreach (var diagnostic in errors)
        {
            if (IgnoredErrors.Contains(diagnostic.Id))
            {
                LogKnownGeneratorError(_logger, diagnostic);
            }
            else
            {
                LogGeneratorFailure(_logger, diagnostic);
                fail = true;
            }
        }
        if (fail)
        {
            throw new CompilationException("Source Generator Failure");
        }
        return (outputCompilation as CSharpCompilation)!;
    }

    private CSharpCompilation GetCSharpCompilation(IEnumerable<SyntaxTree> syntaxTrees)
    {
        var analysis = _input.SourceProjectInfo.Analysis;

        var compilation = CSharpCompilation.Create(AssemblyName,
            syntaxTrees.ToList(),
            _input.SourceProjectInfo.Analysis.LoadReferences(),
            analysis.GetCompilationOptions());

        // C# source generators must be executed before compilation
        return RunSourceGenerators(analysis, compilation);
    }

    private (CSharpRollbackProcessResult?, EmitResult, int) TryCompilation(
        Stream ms,
        Stream? symbolStream,
        ref CSharpCompilation compilation,
        EmitResult? previousEmitResult,
        bool lastAttempt,
        int retryCount)
    {
        CSharpRollbackProcessResult? rollbackProcessResult = null;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var readable = ReadableNumber(retryCount);
            LogTryingCompilation(_logger, readable);
        }

        var emitOptions = symbolStream == null ? null : new EmitOptions(false, DebugInformationFormat.PortablePdb,
            _input.SourceProjectInfo.Analysis.GetSymbolFileName());
        EmitResult? emitResult = null;
        var resourceDescriptions = _input.SourceProjectInfo.Analysis.GetResources(_logger);
        while (emitResult == null)
        {
            if (previousEmitResult != null)
            {
                // remove broken mutations
                rollbackProcessResult = _rollbackProcess.Start(compilation, previousEmitResult.Diagnostics, lastAttempt, _options.DiagMode);
                compilation = rollbackProcessResult.Compilation;
            }

            // reset the memoryStreams
            ms.SetLength(0);
            symbolStream?.SetLength(0);
            try
            {
                emitResult = compilation.Emit(
                    ms,
                    symbolStream,
                    manifestResources: resourceDescriptions,
                    win32Resources: compilation.CreateDefaultWin32Resources(
                        true, // Important!
                        false,
                        null!,
                        null!),
                    options: emitOptions);
            }
#pragma warning disable S1696 // this catches an exception raised by the C# compiler
            catch (NullReferenceException e)
            {
                LogRoslynNullReference(_logger);
                LogException(_logger, e);
                LogSkippingProblematicFiles(_logger);
                compilation = ScanForCauseOfException(compilation);
                EmbeddedResourcesGenerator.ResetCache();
            }
        }

        LogEmitResult(emitResult);

        return (rollbackProcessResult, emitResult, retryCount + 1);
    }

    private CSharpCompilation ScanForCauseOfException(CSharpCompilation compilation)
    {
        var syntaxTrees = compilation.SyntaxTrees;
        // we add each file incrementally until it fails
        foreach (var st in syntaxTrees)
        {
            var local = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(st);
            try
            {
                using var ms = new MemoryStream();
                local.Emit(
                    ms,
                    manifestResources: _input.SourceProjectInfo.Analysis.GetResources(_logger),
                    options: null);
            }
            catch (Exception e)
            {
                LogFailedToCompile(_logger, e, st.FilePath);
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    var text = st.GetText();
                    LogSourceCode(_logger, text);
                }
                syntaxTrees = syntaxTrees.Where(x => x != st).Append(_rollbackProcess.CleanUpFile(st)).ToImmutableArray();
            }
        }
        LogReportIssue(_logger);
        return compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);
    }

    private void LogEmitResult(EmitResult result)
    {
        if (!result.Success)
        {
            LogCompilationFailed(_logger);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                foreach (var err in result.Diagnostics.Where(x => x.Severity is DiagnosticSeverity.Error))
                {
                    var msg = err?.GetMessage(CultureInfo.InvariantCulture) ?? "No message";
                    var loc = err?.Location.ToString() ?? "Unknown filepath";
                    LogCompilationDiagnostic(_logger, msg, loc);
                }
            }
        }
        else
        {
            LogCompilationSuccess(_logger);
        }
    }

    private void DumpErrorDetails(IEnumerable<Diagnostic> diagnostics)
    {
        var messageBuilder = new StringBuilder();
        var materializedDiagnostics = diagnostics.ToArray();

        foreach (var diagnostic in materializedDiagnostics)
        {
            messageBuilder
                .Append(Environment.NewLine)
                .Append(diagnostic.Id).Append(": ").AppendLine(diagnostic.ToString());
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            var diagsString = messageBuilder.ToString();
            LogCompilationErrors(_logger, diagsString);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to build the mutated assembly due to unrecoverable error: {Error}")]
    private static partial void LogUnrecoverableError(ILogger logger, Diagnostic error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to restore the project to a buildable state. Please report the issue. Stryker can not proceed further")]
    private static partial void LogFailedToRestore(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stryker encountered a known error from a coe generator but it will keep on. Compilation may still fail later on: {Diagnostic}")]
    private static partial void LogKnownGeneratorError(ILogger logger, Diagnostic diagnostic);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to generate source code for mutated assembly: {Diagnostics}")]
    private static partial void LogGeneratorFailure(ILogger logger, Diagnostic diagnostics);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Trying compilation for the {RetryCount} time.")]
    private static partial void LogTryingCompilation(ILogger logger, string retryCount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Roslyn C# compiler raised an NullReferenceException. This is a known Roslyn's issue that may be triggered by invalid usage of conditional access expression.")]
    private static partial void LogRoslynNullReference(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Exception")]
    private static partial void LogException(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Stryker will attempt to skip problematic files.")]
    private static partial void LogSkippingProblematicFiles(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to compile {FilePath}")]
    private static partial void LogFailedToCompile(ILogger logger, Exception ex, string filePath);

    [LoggerMessage(Level = LogLevel.Trace, Message = "source code:\n {Source}")]
    private static partial void LogSourceCode(ILogger logger, Microsoft.CodeAnalysis.Text.SourceText source);

    [LoggerMessage(Level = LogLevel.Error, Message = "Please report an issue and provide the source code of the file that caused the exception for analysis.")]
    private static partial void LogReportIssue(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Compilation failed")]
    private static partial void LogCompilationFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{ErrorMessage}, {ErrorLocation}")]
    private static partial void LogCompilationDiagnostic(ILogger logger, string errorMessage, string errorLocation);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Compilation successful")]
    private static partial void LogCompilationSuccess(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Compilation errors: {Diagnostics}")]
    private static partial void LogCompilationErrors(ILogger logger, string diagnostics);

    private static string ReadableNumber(int number) => number switch
    {
        1 => "first",
        2 => "second",
        3 => "third",
        _ => number + "th"
    };

    // This class is used to provide the options to the source generators
    [ExcludeFromCodeCoverage]
    internal sealed class SimpleAnalyserConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly NullAnalyzerConfigOptions _nullProvider = new();

        public SimpleAnalyserConfigOptionsProvider(IProjectAnalysis analysis) => GlobalOptions = new SimpleAnalyzerConfigOptions(analysis);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _nullProvider;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _nullProvider;

        public override AnalyzerConfigOptions GlobalOptions { get; }

        private sealed class SimpleAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            private const string Prefix = "build_property.";
            private readonly IProjectAnalysis _analysis;

            public SimpleAnalyzerConfigOptions(IProjectAnalysis analysis) => _analysis = analysis;

            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
            {
                if (key.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    value = _analysis.GetPropertyOrDefault(key[Prefix.Length..]);
                    return !string.IsNullOrEmpty(value);
                }

                value = null;
                return false;
            }

            public override IEnumerable<string> Keys => [];
        }

        private sealed class NullAnalyzerConfigOptions : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
            {
                value = null;
                return false;
            }

            public override IEnumerable<string> Keys => [];
        }

    }
}
