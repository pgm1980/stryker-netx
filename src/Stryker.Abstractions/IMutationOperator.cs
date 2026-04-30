using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions.Options;

namespace Stryker.Abstractions;

/// <summary>
/// v2.0.0 (ADR-014): the leaf node of the new operator hierarchy
/// <c>IMutatorGroup → IMutator → IMutationOperator</c>. A
/// <see cref="IMutationOperator"/> represents a single, atomic substitution
/// (e.g. "<c>+</c> → <c>-</c>") that an outer <see cref="IMutator"/> can
/// enumerate over its body.
///
/// In v1.x the equivalent of this interface was implicit — every
/// <see cref="IMutator"/> implementation hard-coded its substitutions
/// inside its `Mutate` method body. The v2.0.0 hierarchy makes them
/// addressable as first-class objects so that a CLI/config can disable
/// individual sub-operators (e.g. <c>--disable-suboperator MATH_ADD_TO_SUB</c>)
/// without touching the parent mutator.
///
/// Implementation begins in Sprint 6 (operator-hierarchy refactor); for
/// Sprint 5 this is a stub interface to lock the contract.
/// </summary>
public interface IMutationOperator
{
    /// <summary>
    /// Stable identifier of this sub-operator (e.g. "MATH_ADD_TO_SUB"). Used
    /// for CLI selection, config-file references, and report grouping.
    /// </summary>
    string OperatorId { get; }

    /// <summary>
    /// Human-readable description of the substitution this sub-operator
    /// performs (e.g. "Replace + with -").
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Apply this sub-operator to the given syntax node, yielding zero or more
    /// concrete mutations. Most sub-operators emit either zero (the node
    /// pattern doesn't match) or one (the substitution applies once); a few
    /// (e.g. ROR-Vollmatrix) emit multiple variants per call.
    /// </summary>
    IEnumerable<Mutation> Apply(SyntaxNode node, SemanticModel semanticModel, IStrykerOptions options);
}
