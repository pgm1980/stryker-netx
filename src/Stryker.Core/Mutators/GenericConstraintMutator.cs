using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.0.0 (Sprint 12, comparison.md §4.4 — greenfield .NET-specific): drops
/// the entire <c>where T : ...</c> constraint clause from a generic method.
/// Catches "is the constraint actually exploited (or just decorative)?"
/// tests — if every call-site happens to pass a type that satisfies the
/// dropped constraint, the mutant still compiles and the test suite must
/// detect a behavioral difference (often via a body that uses the
/// constraint, e.g. <c>new T()</c> or a member only on the constraint).
///
/// Conservative scope: only mutates method declarations that have at least
/// one constraint clause. Drops them all in a single mutation rather than
/// per-clause — keeps mutation volume manageable on heavily-generic APIs.
/// May produce a non-compiling mutant if the body relies on the constraint
/// (e.g. <c>new T()</c> needs <c>where T : new()</c>) — in that case the
/// mutant is automatically classified as "Compile error" (killed) by the
/// runner. That's correct behaviour: the constraint is load-bearing.
///
/// Profile membership: All only — most aggressive (compile-time mutation).
/// </summary>
[MutationProfileMembership(MutationProfile.All)]
public sealed class GenericConstraintMutator : MutatorBase<MethodDeclarationSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Complete;

    public override IEnumerable<Mutation> ApplyMutations(MethodDeclarationSyntax node, SemanticModel semanticModel)
    {
        if (node.ConstraintClauses.Count == 0)
        {
            yield break;
        }

        var stripped = node.WithConstraintClauses(
            SyntaxFactory.List<TypeParameterConstraintClauseSyntax>());

        yield return new Mutation
        {
            OriginalNode = node,
            ReplacementNode = stripped.WithCleanTriviaFrom(node),
            DisplayName = $"Generic constraints dropped from '{node.Identifier.Text}'",
            Type = Mutator.Statement,
        };
    }
}
