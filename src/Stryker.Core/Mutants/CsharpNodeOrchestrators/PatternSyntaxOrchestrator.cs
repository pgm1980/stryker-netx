using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Helpers;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

/// <summary>
/// Handles pattern nodes (which are neither expressions nor statements and would not be
/// mutated by default). Sprint 180 (issue #284a): when the pattern belongs to an
/// is-expression that binds a variable, pattern mutations are stored at block level.
/// The default expression-level control would wrap the enclosing is-expression in a
/// conditional expression, duplicating the designation in the same statement scope
/// (CS0128). Block-level if/else copies each get their own scope, so the designation
/// stays legal there.
/// </summary>
internal sealed class PatternSyntaxOrchestrator : NodeSpecificOrchestrator<PatternSyntax, PatternSyntax>
{
    protected override MutationContext StoreMutations(PatternSyntax node,
        IEnumerable<Mutant> mutations,
        MutationContext context) =>
        node.IsInsideVariableBindingIsPattern()
            ? context.AddMutations(mutations, MutationControl.Block)
            : context.AddMutations(mutations);
}
