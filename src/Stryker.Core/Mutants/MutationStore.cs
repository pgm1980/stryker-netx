using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Core.Helpers;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Mutants;

/// <summary>
/// This class stores mutations generated during syntax tree traversal and injects them (with the appropriate control structure)
/// when requested
/// </summary>
internal sealed partial class MutationStore
{
    private static readonly ILogger Logger = ApplicationLogging.LoggerFactory.CreateLogger<MutationStore>();
    private readonly MutantPlacer _mutantPlacer;
    private readonly Stack<PendingMutations> _pendingMutations = new();
    private int _injectionBlockCounter;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mutantPlacer">Mutant placer that will be used for mutation injection</param>
    public MutationStore(MutantPlacer mutantPlacer) => _mutantPlacer = mutantPlacer;

    /// <summary>
    /// Checks if there are pending mutations for the current syntax level
    /// </summary>
    /// <returns></returns>
    public bool HasPendingMutations() => _pendingMutations.Count > 0 && _pendingMutations.Peek().Store.Count > 0;

    /// <summary>
    /// Returns the current mutation control
    /// </summary>
    public MutationControl CurrentControl => _pendingMutations.Count > 0 ? _pendingMutations.Peek().Control : MutationControl.Member;

    /// <summary>
    /// Enter a syntax level
    /// </summary>
    /// <param name="control">syntax level (should match current node level) <see cref="MutationControl"/></param>
    public void Enter(MutationControl control)
    {
        // we use a simple logic to keep the stack small: if the current level is the same as the previous one, we just increment the depth
        if (_pendingMutations.Count > 0 && _pendingMutations.Peek().Aggregate(control))
        {
            return;
        }
        _pendingMutations.Push(new PendingMutations(control));
    }

    /// <summary>
    /// Leave current syntax construct
    /// </summary>
    /// <remarks>Any non injected mutations will be forwarded to the enclosing syntax construct.
    /// If there is none (leaving a member), mutations are flagged as compile errors (and logged).</remarks>
    public void Leave()
    {
        if (!_pendingMutations.Peek().Leave())
        {
            return;
        }
        // we need to store pending mutations at the higher level
        var old = _pendingMutations.Pop();
        if (_pendingMutations.Count > 0)
        {
            _pendingMutations.Peek().StoreMutations(old.Store);
        }
        else if (old.Store.Count > 0)
        {
            LogMutationsDropped(Logger, old.Store.Count);
            foreach (var mutant in old.Store)
            {
                mutant.ResultStatus = MutantStatus.CompileError;
                mutant.ResultStatusReason = "Could not be injected in code.";
            }
        }
    }

    /// <summary>
    /// Prevent any mutation injection
    /// </summary>
    /// <remarks>This method is typically used for constant syntax node, where mutations need to be controlled at a higher syntax level.</remarks>
    public void BlockInjection() => _injectionBlockCounter++;

    /// <summary>
    /// Restore mutation injection
    /// </summary>
    public void EnableInjection() => _injectionBlockCounter--;

    private PendingMutations? FindControl(MutationControl control) => _pendingMutations.FirstOrDefault(item => item.Control >= control);

    /// <summary>
    /// Stores mutations at the given syntax level to be injected at a later time
    /// </summary>
    /// <param name="store">mutants to store</param>
    /// <param name="level">requested control level</param>
    /// <returns>true if insertion is successful</returns>
    /// <remarks>Mutants may be stored in a higher level construct if the desired level is nonexistent. If insertion fails, mutants are flagged
    /// as compile error and logged.</remarks>
    public bool StoreMutationsAtDesiredLevel(IEnumerable<Mutant> store, MutationControl level)
    {
        if (!store.Any())
        {
            return true;
        }

        var controller = FindControl(level);

        if (controller != null)
        {
            controller.StoreMutations(store);
            return true;
        }
        if (Logger.IsEnabled(LogLevel.Debug))
        {
            var count = store.Count();
            LogMutationsNoStructure(Logger, count);
        }
        foreach (var mutant in store)
        {
            mutant.ResultStatus = MutantStatus.CompileError;
            mutant.ResultStatusReason = "Could not be injected in code.";
        }
        return false;
    }

    /// <summary>
    /// Store mutations at current syntax level to be injected at a later time
    /// </summary>
    /// <param name="store"></param>
    /// <returns></returns>
    public bool StoreMutations(IEnumerable<Mutant> store)
    {
        if (_pendingMutations.Count == 0)
        {
            return false;
        }
        _pendingMutations.Peek().StoreMutations(store);
        return true;
    }

    /// <summary>
    /// Inject (current level) mutations inside the provided expression (controlled with ternary operator)
    /// </summary>
    /// <param name="mutatedNode">mutated expression</param>
    /// <param name="sourceNode">original node</param>
    /// <returns>a syntax expression with the mutations included </returns>
    public ExpressionSyntax Inject(ExpressionSyntax mutatedNode, ExpressionSyntax sourceNode)
    {
        if (_injectionBlockCounter > 0 || _pendingMutations.Peek().Control == MutationControl.MemberAccess)
        {
            // do not inject if explicitly blocked
            // never inject at member access level, there is no known control structure
            return mutatedNode;
        }
        var store = _pendingMutations.Peek().Store;
        var result = _mutantPlacer.PlaceExpressionControlledMutations(mutatedNode,
            ApplyMutationsValidated(store, sourceNode));
        store.Clear();
        return result;
    }

    /// <summary>
    /// Inject (current level) mutations in the provided statement (controlled with if statement)
    /// </summary>
    /// <param name="mutatedNode">mutated statement</param>
    /// <param name="sourceNode">original node</param>
    /// <returns>a statement with the mutations included. It should be an 'if' statement when at least one mutation was provided </returns>
    public StatementSyntax Inject(StatementSyntax mutatedNode, StatementSyntax sourceNode)
    {
        var store = _pendingMutations.Peek().Store;
        var result = _mutantPlacer.PlaceStatementControlledMutations(mutatedNode,
            ApplyMutationsValidated(store, sourceNode));
        store.Clear();
        return result;
    }

    /// <summary>
    /// Inject (current level) mutations in the provided block (controlled with if statement)
    /// </summary>
    /// <param name="mutatedNode">mutated block</param>
    /// <param name="sourceNode">original node</param>
    /// <returns>a block with the mutations included.</returns>
    public BlockSyntax Inject(BlockSyntax mutatedNode, BlockSyntax sourceNode)
    {
        var store = _pendingMutations.Peek().Store;
        var result = _mutantPlacer.PlaceStatementControlledMutations(mutatedNode,
            ApplyMutationsValidated<StatementSyntax>(store, sourceNode));
        store.Clear();

        return result as BlockSyntax ?? SyntaxFactory.Block(result);
    }

    /// <summary>
    /// Inject mutations within the provided expression body and return them as a block syntax.
    /// </summary>
    /// <param name="mutatedNode">target node</param>
    /// <param name="originalNode">original expression body</param>
    /// <param name="needReturn">true if the expression body has a return value</param>
    /// <returns>a block syntax with the mutated expression body converted to statement(s).</returns>
    /// <remarks>This method should be used when mutating expression body methods or functions</remarks>
    public BlockSyntax Inject(BlockSyntax mutatedNode, ExpressionSyntax originalNode, bool needReturn)
    {
        var wrapper = needReturn
            ? (Func<ExpressionSyntax, StatementSyntax>)SyntaxFactory.ReturnStatement
            : SyntaxFactory.ExpressionStatement;

        var blockStore = _pendingMutations.Peek();
        if (blockStore.Store.Count == 0)
        {
            return mutatedNode;
        }

        var validated = ApplyMutationsValidated(blockStore.Store, originalNode)
            .Select(pair => (pair.mutant, wrapper(pair.mutated)));
        var result = _mutantPlacer.PlaceStatementControlledMutations(mutatedNode, validated);
        blockStore.Store.Clear();
        return result as BlockSyntax ?? SyntaxFactory.Block(result);
    }

    /// <summary>
    /// Sprint 147 (ADR-028, Bug #9 architectural fix from Calculator-Tester Bug-Report 4):
    /// applies each mutation through <see cref="RoslynHelper.TryInjectMutation"/> so that
    /// slot-incompatible replacements are rejected before they reach the placer's
    /// conditional/if envelope. Rejected mutations are marked
    /// <see cref="MutantStatus.CompileError"/> so reporters classify them correctly, and
    /// they are excluded from the resulting tree (no behavioural effect, no late visitor
    /// crash). This is the centre of the validation layer the user requested in Bug-Report 4.
    /// </summary>
    /// <typeparam name="T">Type of the host node — typically <see cref="ExpressionSyntax"/> or <see cref="StatementSyntax"/>.</typeparam>
    private static IEnumerable<(Mutant mutant, T mutated)> ApplyMutationsValidated<T>(
        IEnumerable<Mutant> mutants,
        T sourceNode)
        where T : SyntaxNode
    {
        foreach (var mutant in mutants)
        {
            if (sourceNode.TryInjectMutation(mutant.Mutation, out var mutated, out var validationError))
            {
                yield return (mutant, mutated);
                continue;
            }

            // Validator rejected the mutation. Mark it so reporters and the runner
            // classify it correctly, and skip it from the conditional/if envelope.
            mutant.ResultStatus = MutantStatus.CompileError;
            mutant.ResultStatusReason = validationError
                ?? "Slot-incompatible mutation rejected by SyntaxSlotValidator (ADR-028).";
            LogValidatorRejected(Logger, mutant.Id, validationError ?? "(no diagnostic)");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "{MutationsCount} mutation(s) could not be injected, they are dropped.")]
    private static partial void LogMutationsDropped(ILogger logger, int mutationsCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "There is no structure to control {MutationsCount} mutations. They are dropped.")]
    private static partial void LogMutationsNoStructure(ILogger logger, int mutationsCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "SyntaxSlotValidator rejected mutation {MutantId} (ADR-028): {ValidationError}")]
    private static partial void LogValidatorRejected(ILogger logger, int mutantId, string validationError);

    private sealed class PendingMutations
    {
        public readonly MutationControl Control;
        private int _depth;
        public readonly List<Mutant> Store = [];

        public PendingMutations(MutationControl control) => Control = control;

        public bool Aggregate(MutationControl control)
        {
            if (Store.Count != 0 || Control != control)
            {
                return false;
            }
            _depth++;
            return true;
        }

        public void StoreMutations(IEnumerable<Mutant> store) =>
            Store.AddRange(store.Where(m => m.ResultStatus == MutantStatus.Pending));

        public bool Leave()
        {
            if (_depth <= 0)
            {
                return true;
            }

            _depth--;
            return false;
        }
    }
}
