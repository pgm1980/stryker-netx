using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutators;

/// <summary>
/// v2.1.0 (Sprint 14, comparison.md §4.4 — generic-constraint loosening,
/// the per-clause complement to v2.0.0's <see cref="GenericConstraintMutator"/>
/// which drops the entire constraint clause set). Walks each
/// <c>where T : ...</c> clause and emits one mutation per per-constraint
/// loosening:
/// <list type="bullet">
///   <item><c>where T : class</c> → <c>where T : new()</c></item>
///   <item><c>where T : class</c> → <c>where T : struct</c></item>
///   <item><c>where T : struct</c> → <c>where T : class</c></item>
///   <item><c>where T : new()</c> → <c>where T : class</c></item>
///   <item><c>where T : SomeInterface</c> → drop the constraint</item>
/// </list>
/// May produce non-compiling mutants (e.g. when the body relies on the
/// original constraint via <c>new T()</c> for <c>: new()</c>); the runner
/// classifies non-compiling mutants as killed (precedent:
/// <see cref="GenericConstraintMutator"/>).
///
/// Profile membership: Stronger | All — less aggressive than the drop-all
/// variant since it's per-clause-targeted.
/// </summary>
[MutationProfileMembership(MutationProfile.Stronger | MutationProfile.All)]
public sealed class GenericConstraintLoosenMutator : MutatorBase<TypeParameterConstraintClauseSyntax>
{
    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public override IEnumerable<Mutation> ApplyMutations(TypeParameterConstraintClauseSyntax node, SemanticModel semanticModel)
    {
        var constraints = node.Constraints;
        if (constraints.Count == 0)
        {
            yield break;
        }

        for (var i = 0; i < constraints.Count; i++)
        {
            var original = constraints[i];
            foreach (var alternative in BuildAlternatives(original))
            {
                var newConstraints = constraints.Replace(original, alternative);
                var newClause = node.WithConstraints(newConstraints);
                yield return new Mutation
                {
                    OriginalNode = node,
                    ReplacementNode = newClause.WithCleanTriviaFrom(node),
                    DisplayName = $"Generic constraint loosen: '{original.ToString().Trim()}' → '{alternative.ToString().Trim()}' (parameter '{node.Name}')",
                    Type = Mutator.Statement,
                };
            }
        }
    }

    private static IEnumerable<TypeParameterConstraintSyntax> BuildAlternatives(TypeParameterConstraintSyntax original)
    {
        switch (original)
        {
            case ClassOrStructConstraintSyntax cs:
                if (cs.IsKind(SyntaxKind.ClassConstraint))
                {
                    yield return SyntaxFactory.ConstructorConstraint();
                    yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.StructConstraint);
                }
                else if (cs.IsKind(SyntaxKind.StructConstraint))
                {
                    yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                }
                yield break;

            case ConstructorConstraintSyntax:
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                yield break;

            case TypeConstraintSyntax tc:
                // For "where T : SomeType / SomeInterface" the loosening is dropping
                // the constraint — represent that as a no-op-marker by yielding
                // a synthetic class-constraint placeholder which acts as the
                // weakest reference-type-only restriction. The drop-all is already
                // covered by GenericConstraintMutator, so here we emit the milder
                // weakening to "any class" instead of complete removal.
                _ = tc;
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);
                yield break;

            default:
                yield break;
        }
    }
}
