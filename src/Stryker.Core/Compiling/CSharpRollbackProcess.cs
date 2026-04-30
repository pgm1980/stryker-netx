using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Core.Mutants;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Compiling;

/// <summary>
/// Responsible for rolling back all mutations that prevent compiling the mutated assembly
/// </summary>
public partial class CSharpRollbackProcess : ICSharpRollbackProcess
{
    private List<int> RollBackedIds { get; }
    private ILogger Logger { get; }

    public CSharpRollbackProcess()
    {
        Logger = ApplicationLogging.LoggerFactory.CreateLogger<CSharpRollbackProcess>();
        RollBackedIds = new List<int>();
    }

    public CSharpRollbackProcessResult Start(CSharpCompilation compiler, ImmutableArray<Diagnostic> diagnostics,
        bool lastAttempt, bool devMode)
    {
        // match the diagnostics with their syntax trees
        var syntaxTreeMapping =
            compiler.SyntaxTrees.ToDictionary<SyntaxTree, SyntaxTree, ICollection<Diagnostic>>(
                syntaxTree => syntaxTree, _ => new Collection<Diagnostic>());

        foreach (var diagnostic in diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error))
        {
            if (diagnostic.Location.SourceTree == null)
            {
                LogGeneralCompilationError(Logger, diagnostic.GetMessage(CultureInfo.InvariantCulture));
                continue;
            }

            syntaxTreeMapping[diagnostic.Location.SourceTree].Add(diagnostic);
        }

        // remove the broken mutations from the syntax trees
        foreach (var syntaxTreeMap in syntaxTreeMapping.Where(x => x.Value.Count > 0))
        {
            var originalTree = syntaxTreeMap.Key;
            LogRollbackingMutations(Logger, originalTree.FilePath);
            if (devMode)
            {
                DumpBuildErrors(syntaxTreeMap);
                LogOriginalSource(Logger, originalTree);
            }

            var updatedSyntaxTree = RemoveCompileErrorMutations(originalTree, syntaxTreeMap.Value);

            if (updatedSyntaxTree == originalTree || lastAttempt)
            {
                LogCannotCompileAfterMutation(Logger);
                throw new CompilationException("Internal error due to compile error.");
            }

            LogRolledBackTo(Logger, updatedSyntaxTree);

            // update the compiler object with the new syntax tree
            compiler = compiler.ReplaceSyntaxTree(originalTree, updatedSyntaxTree);
        }

        // by returning the same compiler object (with different syntax trees) the next compilation will use Roslyn's incremental compilation
        return new(
            compiler,
            RollBackedIds);
    }

    // search is this node contains or is within a mutation
    private (SyntaxNode?, int) FindMutationIfAndId(SyntaxNode startNode)
    {
        var info = ExtractMutationInfo(startNode);
        if (info.Id != null)
        {
            return (startNode, info.Id.Value);
        }

        for (var node = startNode; node != null; node = node.Parent)
        {
            info = ExtractMutationInfo(node);
            if (info.Id != null)
            {
                return (node, info.Id.Value);
            }
        }

        // scan within the expression
        return startNode is ExpressionSyntax ? FindMutationInChildren(startNode) : (null, -1);
    }

    // search the first mutation within the node
    private (SyntaxNode?, int) FindMutationInChildren(SyntaxNode startNode)
    {
        foreach (var node in startNode.ChildNodes())
        {
            var info = ExtractMutationInfo(node);
            if (info.Id != null)
            {
                return (node, info.Id.Value);
            }
        }

        foreach (var node in startNode.ChildNodes())
        {
            var (subNode, mutantId) = FindMutationInChildren(node);
            if (subNode != null)
            {
                return (subNode, mutantId);
            }
        }

        return (null, -1);
    }

    private MutantInfo ExtractMutationInfo(SyntaxNode node)
    {
        var info = MutantPlacer.FindAnnotations(node);

        if (info.Engine == null)
        {
            return new MutantInfo();
        }

        LogFoundMutant(Logger, info.Id ?? -1, info.Type ?? string.Empty, info.Engine!);

        return info;
    }

    private static SyntaxNode FindEnclosingMember(SyntaxNode node)
    {
        for (var currentNode = node; currentNode != null; currentNode = currentNode.Parent)
        {
            if (currentNode.IsKind(SyntaxKind.MethodDeclaration) ||
                currentNode.IsKind(SyntaxKind.GetAccessorDeclaration) ||
                currentNode.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                currentNode.IsKind(SyntaxKind.ConstructorDeclaration))
            {
                return currentNode;
            }
        }

        // return the whole file if not found
        return node.SyntaxTree.GetRoot();
    }

    private List<MutantInfo> ScanAllMutationsIfsAndIds(SyntaxNode node)
    {
        var scan = new List<MutantInfo>();
        foreach (var childNode in node.ChildNodes())
        {
            scan.AddRange(ScanAllMutationsIfsAndIds(childNode));
        }

        var info = ExtractMutationInfo(node);
        if (info.Id != null)
        {
            scan.Add(info);
        }

        return scan;
    }

    private void DumpBuildErrors(KeyValuePair<SyntaxTree, ICollection<Diagnostic>> syntaxTreeMap)
    {
        if (!Logger.IsEnabled(LogLevel.Debug))
        {
            return;
        }
        LogDumpingBuildError(Logger);
        var sourceLines = syntaxTreeMap.Key.ToString().Split("\n");
        foreach (var diagnostic in syntaxTreeMap.Value)
        {
            var fileLinePositionSpan = diagnostic.Location.GetMappedLineSpan();
            var diagnosticMessage = diagnostic.GetMessage(CultureInfo.InvariantCulture);
            LogErrorWithSpan(Logger, diagnosticMessage, fileLinePositionSpan);
            for (var i = Math.Max(0, fileLinePositionSpan.StartLinePosition.Line - 1);
                 i <= Math.Min(fileLinePositionSpan.EndLinePosition.Line + 1, sourceLines.Length - 1);
                 i++)
            {
                LogSourceLineIndex(Logger, i + 1, sourceLines[i]);
            }
        }

        LogNewLine(Logger, Environment.NewLine);
    }

    private SyntaxTree RemoveCompileErrorMutations(SyntaxTree originalTree, IEnumerable<Diagnostic> diagnosticInfo)
    {
        var rollbackRoot = originalTree.GetRoot();
        // find all if statements to remove
        var brokenMutations =
            IdentifyMutationsAndFlagForRollback(diagnosticInfo, rollbackRoot, out var diagnostics);

        if (brokenMutations.Count == 0)
        {
            // we were unable to identify any mutation that could have caused the build issue(s)
            brokenMutations = ScanForSuspiciousMutations(diagnostics, rollbackRoot);
        }

        // mark the broken mutation nodes to track
        var trackedTree = rollbackRoot.TrackNodes(brokenMutations);
        foreach (var brokenMutation in brokenMutations)
        {
            // find the mutated node in the new tree
            var nodeToRemove = trackedTree.GetCurrentNode(brokenMutation);
            if (nodeToRemove is null)
            {
                continue;
            }
            // remove the mutated node using its MutantPlacer remove method and update the tree
            trackedTree = trackedTree.ReplaceNode(nodeToRemove, MutantPlacer.RemoveMutant(nodeToRemove));
        }

        return trackedTree.SyntaxTree;
    }

    private Collection<SyntaxNode> ScanForSuspiciousMutations(Diagnostic[] diagnostics, SyntaxNode rollbackRoot)
    {
        var suspiciousMutations = new Collection<SyntaxNode>();
        foreach (var diagnostic in diagnostics)
        {
            var brokenMutation = rollbackRoot.FindNode(diagnostic.Location.SourceSpan);
            var initNode = FindEnclosingMember(brokenMutation);
            var scan = ScanAllMutationsIfsAndIds(initNode);

            if (scan.Any(x => string.Equals(x.Type, Mutator.Block.ToString(), StringComparison.Ordinal)))
            {
                // we remove all block mutation first
                foreach (var mutant in scan.Where(x =>
                             string.Equals(x.Type, Mutator.Block.ToString(), StringComparison.Ordinal) && x.Node is not null && !suspiciousMutations.Contains(x.Node)))
                {
                    suspiciousMutations.Add(mutant.Node!);
                    if (mutant.Id is not null)
                    {
                        RollBackedIds.Add(mutant.Id.Value);
                    }
                }
            }
            else
            {
                // we have to remove every mutation
                var errorLocation = diagnostic.Location.GetMappedLineSpan();
                if (Logger.IsEnabled(LogLevel.Warning))
                {
                    var msg = diagnostic.GetMessage(CultureInfo.InvariantCulture);
                    LogUnidentifiedMutation(Logger,
                        errorLocation.Path, errorLocation.StartLinePosition.Line,
                        errorLocation.StartLinePosition.Character, diagnostic.Id,
                        msg, brokenMutation);
                }

                if (Logger.IsEnabled(LogLevel.Information))
                {
                    var dispName = DisplayName(initNode);
                    LogSafeMode(Logger, dispName);
                }
                // backup, remove all mutations in the node
                foreach (var mutant in scan.Where(mutant => mutant.Node is not null && !suspiciousMutations.Contains(mutant.Node)))
                {
                    suspiciousMutations.Add(mutant.Node!);
                    if (mutant.Id != -1)
                    {
                        RollBackedIds.Add(mutant.Id!.Value);
                    }
                }
            }
        }

        return suspiciousMutations;
    }

    // removes all mutation from a file
    public SyntaxTree CleanUpFile(SyntaxTree file)
    {
        var rollbackRoot = file.GetRoot();
        var scan = ScanAllMutationsIfsAndIds(rollbackRoot);
        var suspiciousMutations = new Collection<SyntaxNode>();
        foreach (var mutant in scan.Where(mutant => mutant.Node is not null && !suspiciousMutations.Contains(mutant.Node)))
        {
            suspiciousMutations.Add(mutant.Node!);
            if (mutant.Id != -1)
            {
                RollBackedIds.Add(mutant.Id!.Value);
            }
        }

        // mark the broken mutation nodes to track
        var trackedTree = rollbackRoot.TrackNodes(suspiciousMutations);
        foreach (var brokenMutation in suspiciousMutations)
        {
            // find the mutated node in the new tree
            var nodeToRemove = trackedTree.GetCurrentNode(brokenMutation);
            if (nodeToRemove is null)
            {
                continue;
            }
            // remove the mutated node using its MutantPlacer remove method and update the tree
            trackedTree = trackedTree.ReplaceNode(nodeToRemove, MutantPlacer.RemoveMutant(nodeToRemove));
        }

        return file.WithRootAndOptions(trackedTree, file.Options);
    }

    private static string DisplayName(SyntaxNode initNode) =>
        initNode switch
        {
            MethodDeclarationSyntax method => $"{method.Identifier}",
            ConstructorDeclarationSyntax constructor => $"{constructor.Identifier}",
            AccessorDeclarationSyntax accessor => $"{accessor.Keyword} {accessor.Keyword}",
            not null => initNode.Parent == null ? "whole file" : "the current node",
            null => "the current node",
        };

    private Collection<SyntaxNode> IdentifyMutationsAndFlagForRollback(IEnumerable<Diagnostic> diagnosticInfo,
        SyntaxNode rollbackRoot, out Diagnostic[] diagnostics)
    {
        var brokenMutations = new Collection<SyntaxNode>();
        diagnostics = diagnosticInfo as Diagnostic[] ?? diagnosticInfo.ToArray();
        foreach (var diagnostic in diagnostics)
        {
            var brokenMutation = rollbackRoot.FindNode(diagnostic.Location.SourceSpan);
            var (mutationIf, mutantId) = FindMutationIfAndId(brokenMutation);
            if (mutationIf == null || brokenMutations.Contains(mutationIf))
            {
                continue;
            }

            if (MutantPlacer.RequiresRemovingChildMutations(mutationIf))
            {
                FlagChildrenMutationsForRollback(mutationIf, brokenMutations);
            }
            else
            {
                brokenMutations.Add(mutationIf);
                if (mutantId >= 0)
                {
                    RollBackedIds.Add(mutantId);
                }
            }
        }

        return brokenMutations;
    }

    private void FlagChildrenMutationsForRollback(SyntaxNode mutationIf, Collection<SyntaxNode> brokenMutations)
    {
        var scan = ScanAllMutationsIfsAndIds(mutationIf);

        foreach (var mutant in scan.Where(mutant => mutant.Node is not null && !brokenMutations.Contains(mutant.Node)))
        {
            brokenMutations.Add(mutant.Node!);
            if (mutant.Id != -1)
            {
                RollBackedIds.Add(mutant.Id!.Value);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "General compilation error: {Message}")]
    private static partial void LogGeneralCompilationError(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Debug, Message = "RollBacking mutations from {FilePath}.")]
    private static partial void LogRollbackingMutations(ILogger logger, string filePath);

    [LoggerMessage(Level = LogLevel.Trace, Message = "source {OriginalTree}")]
    private static partial void LogOriginalSource(ILogger logger, SyntaxTree originalTree);

    [LoggerMessage(Level = LogLevel.Critical, Message = "Stryker.NET could not compile the project after mutation. This is probably an error for Stryker.NET and not your project. Please report this issue on github with the previous error message.")]
    private static partial void LogCannotCompileAfterMutation(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "RolledBack to {UpdatedSyntaxTree}")]
    private static partial void LogRolledBackTo(ILogger logger, SyntaxTree updatedSyntaxTree);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found mutant {Id} of type '{Type}' controlled by '{Engine}'.")]
    private static partial void LogFoundMutant(ILogger logger, int id, string type, string engine);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Dumping build error in file")]
    private static partial void LogDumpingBuildError(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Error :{Message}, {FileLinePositionSpan}")]
    private static partial void LogErrorWithSpan(ILogger logger, string message, FileLinePositionSpan fileLinePositionSpan);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Index}: {SourceLine}")]
    private static partial void LogSourceLineIndex(ILogger logger, int index, string sourceLine);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{NewLine}")]
    private static partial void LogNewLine(ILogger logger, string newLine);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An unidentified mutation in {Path} resulted in a compile error (at {Line}:{StartCharacter}) with id: {DiagnosticId}, message: {Message} (Source code: {BrokenMutation})")]
    private static partial void LogUnidentifiedMutation(ILogger logger, string path, int line, int startCharacter, string diagnosticId, string message, SyntaxNode brokenMutation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Safe Mode! Stryker will remove all mutations in {DisplayName} and mark them as 'compile error'.")]
    private static partial void LogSafeMode(ILogger logger, string displayName);
}
