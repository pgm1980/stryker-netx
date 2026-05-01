using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

    /// <summary>
    /// v2.4.0 (Sprint 17): hardcoded BCL-interface-pair table for the
    /// <see cref="TypeConstraintSyntax"/> case. When a constraint targets one
    /// of these interfaces, we additionally emit the paired-interface
    /// alternative (e.g. <c>where T : ICloneable → where T : IDisposable</c>)
    /// alongside the v2.1.0 class-constraint replacement. Catches "is the
    /// constraint actually exercising the chosen interface's API, or could
    /// the body have used a different but related interface?" tests.
    /// Conservative: only well-known BCL pairs to keep noise low; user-
    /// defined interfaces fall back to the class-constraint replacement.
    /// </summary>
    private static readonly ImmutableDictionary<string, ImmutableArray<string>> InterfacePairs =
        new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal)
        {
            ["ICloneable"] = ["IDisposable"],
            ["IDisposable"] = ["ICloneable"],
            ["IComparable"] = ["IEquatable"],
            ["IEquatable"] = ["IComparable"],
            ["IEnumerable"] = ["ICollection"],
            ["ICollection"] = ["IEnumerable", "IList"],
            ["IList"] = ["ICollection"],
        }.ToImmutableDictionary(StringComparer.Ordinal);

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
                // Class-constraint replacement: weakest reference-type-only restriction.
                // Drop-all is already covered by GenericConstraintMutator, so this is the
                // milder per-clause loosening.
                yield return SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint);

                // v2.4.0 Sprint 17: BCL-interface-pair extension. If the constraint
                // targets a well-known BCL interface, additionally emit the paired-
                // interface alternative (e.g. ICloneable → IDisposable). Catches "is
                // the chosen interface's API actually exercised, or could a different
                // interface have served?" tests.
                var typeName = ExtractInterfaceName(tc.Type);
                if (typeName is not null && InterfacePairs.TryGetValue(typeName, out var pairs))
                {
                    foreach (var paired in pairs)
                    {
                        yield return SyntaxFactory.TypeConstraint(
                            ReplaceInterfaceName(tc.Type, paired));
                    }
                }
                yield break;

            default:
                yield break;
        }
    }

    /// <summary>
    /// Extracts the unqualified type-name from a TypeSyntax. Handles
    /// IdentifierNameSyntax (<c>ICloneable</c>) and GenericNameSyntax
    /// (<c>IEnumerable&lt;T&gt;</c>) — generic-arity is dropped since the
    /// pair-table indexes on simple names.
    /// </summary>
    private static string? ExtractInterfaceName(TypeSyntax type) => type switch
    {
        IdentifierNameSyntax id => id.Identifier.Text,
        GenericNameSyntax g => g.Identifier.Text,
        _ => null,
    };

    /// <summary>
    /// Returns a TypeSyntax with the interface name swapped, preserving
    /// the original arity / generic-argument shape.
    /// </summary>
    private static TypeSyntax ReplaceInterfaceName(TypeSyntax original, string replacement) => original switch
    {
        IdentifierNameSyntax => SyntaxFactory.IdentifierName(replacement),
        GenericNameSyntax g => g.WithIdentifier(SyntaxFactory.Identifier(replacement)),
        _ => SyntaxFactory.IdentifierName(replacement),
    };
}
