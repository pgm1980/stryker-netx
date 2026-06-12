using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// Sprint 181 (360°-Analyse G-17): generic no-op detector. Some mutators can emit a
/// replacement that is structurally identical to the original node (e.g. a type-driven
/// return-value mutation whose target value already equals the replacement, or a constant
/// replacement landing on the present value). Such mutants change nothing, survive every
/// test and silently lower the mutation score. The shape-specific filters below only cover
/// their own patterns; this filter catches the whole class with a single trivia-insensitive
/// tree comparison and therefore runs first in the pipeline.
/// </summary>
public sealed class NoOpMutationFilter : IEquivalentMutantFilter
{
    public string FilterId => "NoOpMutation";

    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel) =>
        mutation.OriginalNode is not null
        && mutation.ReplacementNode is not null
        && SyntaxFactory.AreEquivalent(mutation.OriginalNode, mutation.ReplacementNode, topLevel: false);
}
