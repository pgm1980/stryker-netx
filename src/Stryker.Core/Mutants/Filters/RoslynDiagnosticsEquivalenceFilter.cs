using System.Linq;
using Microsoft.CodeAnalysis;
using Stryker.Abstractions;

namespace Stryker.Core.Mutants.Filters;

/// <summary>
/// v2.1.0 (Sprint 14, comparison.md §4.3 — mutmut Type-Checker-Integration):
/// inspects the replacement <see cref="SyntaxNode"/> of a candidate mutation
/// for parser-level diagnostics carried on the node itself, and short-circuits
/// as "equivalent" (in the pipeline sense of "do not schedule a test run for
/// this") when the replacement is structurally invalid. Catches mutator bugs
/// that would otherwise cost a full compile-and-test cycle per mutant before
/// the runner classifies them as compile-fail.
///
/// Conservative scope: only acts when the replacement node carries
/// diagnostics of severity <c>Error</c>. The replacement is constructed by
/// the mutator from existing tokens / SyntaxFactory calls, so any parse
/// error already lives on the node — no need to re-parse, which would
/// misclassify standalone expression / statement snippets that don't
/// independently form a compilation unit.
///
/// Does NOT classify on semantic errors (would need a Compilation context
/// the <see cref="IEquivalentMutantFilter"/> contract doesn't currently
/// expose — a future v2.2 ADR may extend it). The runner still classifies
/// semantic-fail mutants as killed at the build step; this filter just
/// fast-paths the obvious syntax-fail cases.
///
/// Always-on: mirrors mutmut's mypy/pyrefly pre-filter — no profile gate.
/// </summary>
public sealed class RoslynDiagnosticsEquivalenceFilter : IEquivalentMutantFilter
{
    /// <inheritdoc />
    public string FilterId => "RoslynDiagnostics";

    /// <inheritdoc />
    public bool IsEquivalent(Mutation mutation, SemanticModel? semanticModel)
    {
        if (mutation.ReplacementNode is null)
        {
            return false;
        }

        // Use the diagnostics already attached to the replacement node by
        // Roslyn during construction. Re-parsing standalone expressions /
        // statements via CSharpSyntaxTree.ParseText would misclassify them
        // because they're not compilation units — the parser would flag
        // every bare expression as invalid top-level code.
        var diagnostics = mutation.ReplacementNode.GetDiagnostics();
        return diagnostics.Any(static d => d.Severity == DiagnosticSeverity.Error);
    }
}
