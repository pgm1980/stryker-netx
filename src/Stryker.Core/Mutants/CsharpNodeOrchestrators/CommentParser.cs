using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Mutants.CsharpNodeOrchestrators;

internal static partial class CommentParser
{
    [GeneratedRegex(@"^\s*(?<single>\/\/\s*Stryker(?<singleCmd>.*))|(?<multi>\/\*\s*Stryker(?<multiCmd>.*[^\*][^\\])\*\/\s*)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 200)]
    private static partial Regex Pattern();

    // Sprint 160 (ADR-040 Issue β): regex extends `(?<once>once|)` to `(?<scope>next-line|once|)`
    // to accept the Stryker.JS-style `next-line` scope qualifier. `next-line` is treated as a
    // pragmatic alias for `once` (single-mutation scope) — see _docs/disable-comment-syntax.md
    // for the documented semantic-difference vs. Stryker.JS-line-scope.
    [GeneratedRegex(@"^\s*(?<mode>disable|restore)\s*(?<scope>next-line|once|)\s*(?<mutators>[^:]*)\s*:?(?<comment>.*)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 200)]
    private static partial Regex Parser();
    private static readonly ILogger Logger = ApplicationLogging.LoggerFactory.CreateLogger("CommentParser");

    private static MutationContext ParseStrykerComment(MutationContext context, Match match, SyntaxNode node)
    {
        // get the ignore comment
        var comment = match.Groups["comment"].Value.Trim();
        if (string.IsNullOrEmpty(comment))
        {
            comment = "Ignored via code comment.";
        }

        var disable = match.Groups["mode"].Value.ToLowerInvariant() switch
        {
            "disable" => true,
            _ => false,
        };

        // Sprint 160 (ADR-040 Issue γ) + Sprint 162 (ADR-042 §6): parse the mutator-list
        // into a List<Mutator>, skip-on-failure for unrecognised labels, and short-circuit
        // to all-enum-values when "all" appears anywhere in the comma-separated list.
        var rawMutators = match.Groups["mutators"].Value.Trim();
        var filteredMutators = ParseMutatorList(rawMutators, node);

        // Sprint 160 (ADR-040 Issue β): scope group accepts `next-line` (alias for `once`,
        // single-mutation scope), `once` (existing single-mutation scope), or empty
        // (block-disable until matching restore — existing behavior).
        var scope = match.Groups["scope"].Value.ToLowerInvariant();
        var isOnceOrNextLine = string.Equals(scope, "once", StringComparison.Ordinal)
                            || string.Equals(scope, "next-line", StringComparison.Ordinal);

        return context.FilterMutators(disable, [.. filteredMutators], isOnceOrNextLine, comment);
    }

    /// <summary>
    /// Sprint 162 (ADR-042 §6): centralised mutator-list parsing. Extracted out of
    /// <see cref="ParseStrykerComment"/> to keep that method under the MA0051 60-line cap.
    /// Behavior:
    /// <list type="bullet">
    ///   <item>If <c>"all"</c> appears as ANY comma-separated token (case-insensitive),
    ///   return every <see cref="Mutator"/> enum value (union-semantik: all ∪ X = all).</item>
    ///   <item>Otherwise, parse each non-empty trimmed token via <c>Enum.TryParse&lt;Mutator&gt;</c>.
    ///   Failed labels emit <see cref="LogLabelNotRecognized"/> (with a class-name-hint
    ///   for PascalCase labels) and are skipped — no silent default-Statement fallback.</item>
    /// </list>
    /// </summary>
    private static List<Mutator> ParseMutatorList(string rawMutators, SyntaxNode node)
    {
        var labels = rawMutators.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (labels.Any(l => string.Equals(l, "all", StringComparison.OrdinalIgnoreCase)))
        {
            return [.. Enum.GetValues<Mutator>()];
        }
        var result = new List<Mutator>(labels.Length);
        foreach (var label in labels)
        {
            // Sprint 166 (ADR-046 §B, Aisess Wishlist #6 + §7): try the class-name
            // alias table first (ConfigureAwait / AsyncAwait / AsyncAwaitResult →
            // Boolean). Only on alias-miss + Enum.TryParse-miss do we fall through
            // to the Sprint-161 PascalCase-hint mechanism. The hint stays valuable
            // for OTHER unrecognised class names not in the alias table.
            if (MutatorClassNameAliases.TryResolve(label, out var aliasedMutator))
            {
                result.Add(aliasedMutator);
                continue;
            }
            if (Enum.TryParse<Mutator>(label, true, out var value))
            {
                result.Add(value);
            }
            else
            {
                var hint = LooksLikeMutatorClassName(label)
                    ? "Hint: mutator class names are not accepted here — use the Mutator-Kind name. " +
                      "Common: ConfigureAwait → Boolean, AsyncAwait → Boolean. Full table: " +
                      "https://github.com/pgm1980/stryker-netx/blob/main/_docs/disable-comment-syntax.md"
                    : string.Empty;
                LogLabelNotRecognized(Logger, label, node.GetLocation().GetMappedLineSpan().StartLinePosition, node.SyntaxTree.FilePath, string.Join(',', Enum.GetValues<Mutator>()), hint);
            }
        }
        return result;
    }

    /// <summary>
    /// Sprint 160 (ADR-040 Issue α): heuristic check whether a parse-failed mutator label
    /// looks like a Mutator class name (PascalCase, alphanumeric). Used to enrich the
    /// "label not recognized"-error with a hint pointing the user at the Mutator-Kind
    /// name they should use instead.
    /// </summary>
    private static bool LooksLikeMutatorClassName(string label) =>
        !string.IsNullOrEmpty(label)
        && char.IsUpper(label[0])
        && label.All(c => char.IsLetterOrDigit(c));

    public static MutationContext ParseNodeLeadingComments(SyntaxNode node, MutationContext context)
    {
        // Sprint 165 (ADR-045, §5 from Aisess STRYKER_NETX_ANOMALIES_AND_BUGS report):
        // also scan operator-token leading-trivia for chain-link nodes. Without this,
        // // Stryker disable next-line directives placed between continuation lines of
        // a multi-line method-chain (e.g., before `.ConfigureAwait(false)`) are silently
        // ignored — Roslyn attaches mid-chain comments to the OperatorToken `.` of the
        // chain-link MAE, which is not on the existing scan-path (GetFirstToken-LeadingTrivia
        // gives leading-trivia of the LEFTMOST deep token).
        var comments = node.GetFirstToken(true).GetPreviousToken(true)
            .TrailingTrivia.Union(node.GetLeadingTrivia())
            .Union(GetIntraChainOperatorTrivia(node))
            .Where(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToList();
        var result = comments.Aggregate(context, (current, t) => ProcessComment(node, current, t.ToString()));

        return result;
    }

    /// <summary>
    /// Sprint 165 (ADR-045): returns the leading-trivia of operator tokens that sit at
    /// the boundary between chain-link continuations of a multi-line expression. Roslyn
    /// attaches mid-chain comments to these operator tokens, not to the node's deepest
    /// first-token. Without this scan, <c>// Stryker disable next-line</c> directives
    /// placed between continuation lines (e.g.,
    /// <code>
    /// await _repo.GetAsync(slug)
    ///     // Stryker disable next-line all : reason
    ///     .ConfigureAwait(false);
    /// </code>
    /// ) are silently ignored. Reported in
    /// <c>_bug_reporting/stryker-netx-3.2.12-disable-directive-multiline-statement.md</c>.
    /// </summary>
    /// <remarks>
    /// For <see cref="InvocationExpressionSyntax"/> with an MAE/MBE Expression child,
    /// we return the child's operator-token leading-trivia. Lifting it to the Invocation's
    /// <see cref="NodeSpecificOrchestrator{T,T2}.PrepareContext"/> is essential — only at
    /// that level does the filter propagate to the <c>ArgList</c> sibling containing the
    /// mutation target. For direct chain-link nodes (MAE/CAE/Binary/Assignment/MemberBinding),
    /// returning their own operator-token leading-trivia covers cases where the chain-link
    /// is the immediate Expression child of a non-Invocation parent.
    /// </remarks>
    private static SyntaxTriviaList GetIntraChainOperatorTrivia(SyntaxNode node)
    {
        // Lift mid-chain trivia to the InvocationExpression level when the call target
        // is a chain-link (the common `.ConfigureAwait(false)`, `.Then(...)`, `.Where(...)` shape).
        if (node is InvocationExpressionSyntax inv)
        {
            if (inv.Expression is MemberAccessExpressionSyntax invMae)
            {
                return invMae.OperatorToken.LeadingTrivia;
            }
            if (inv.Expression is MemberBindingExpressionSyntax invMbe)
            {
                return invMbe.OperatorToken.LeadingTrivia;
            }
        }
        // Direct chain-link node: scan its own operator-token leading-trivia. Provides
        // redundancy when the same node is also wrapped in an InvocationExpression
        // (idempotent under SyntaxTrivia deduplication + FilterMutators), and covers
        // cases where the chain-link is not wrapped in an Invocation.
        return node switch
        {
            MemberAccessExpressionSyntax mae => mae.OperatorToken.LeadingTrivia,
            ConditionalAccessExpressionSyntax cae => cae.OperatorToken.LeadingTrivia,
            BinaryExpressionSyntax bex => bex.OperatorToken.LeadingTrivia,
            AssignmentExpressionSyntax aex => aex.OperatorToken.LeadingTrivia,
            MemberBindingExpressionSyntax mbe => mbe.OperatorToken.LeadingTrivia,
            _ => default(SyntaxTriviaList),
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Difficult to test timeouts")]
    private static MutationContext ProcessComment(SyntaxNode node, MutationContext context, string commentTrivia)
    {
        try
        {
            return InterpretStrykerComment(node, context, commentTrivia);
        }
        catch (RegexMatchTimeoutException exception)
        {
            LogParseTimeout(Logger, exception, node.GetLocation().GetMappedLineSpan().StartLinePosition, node.SyntaxTree.FilePath, commentTrivia);
            return context;
        }
    }

    private static MutationContext InterpretStrykerComment(SyntaxNode node, MutationContext context, string commentTrivia)
    {
        // perform a quick pattern check to see if it is a 'Stryker comment'
        var strykerCommentMatch = Pattern().Match(commentTrivia);
        if (!strykerCommentMatch.Success)
        {
            return context;
        }

        // now we can extract actual command
        var isSingleLine = strykerCommentMatch.Groups["single"].Success;
        var command = isSingleLine ? strykerCommentMatch.Groups["singleCmd"].Value : strykerCommentMatch.Groups["multiCmd"].Value;

        var match = Parser().Match(command);
        if (match.Success)
        {
            // this is a Stryker comments, now we parse it
            return ParseStrykerComment(context, match, node);
        }

        LogInvalidStrykerComment(Logger, node.GetLocation().GetMappedLineSpan().StartLinePosition, node.SyntaxTree.FilePath);
        return context;
    }

    // Sprint 160 (ADR-040): added trailing {Hint} placeholder. Empty when the failed label
    // is not PascalCase; otherwise contains a class-name-vs-Kind-name guidance string.
    [LoggerMessage(Level = LogLevel.Error, Message = "{Label} not recognized as a mutator at {Location}, {FilePath}. Legal values are {LegalValues}. {Hint}")]
    private static partial void LogLabelNotRecognized(ILogger logger, string label, Microsoft.CodeAnalysis.Text.LinePosition location, string filePath, string legalValues, string hint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Parsing Stryker comments at {StartLinePosition}, {FilePath} took too long to parse and was ignored. Comment: {Comment}")]
    private static partial void LogParseTimeout(ILogger logger, Exception ex, Microsoft.CodeAnalysis.Text.LinePosition startLinePosition, string filePath, string comment);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid Stryker comments at {Position}, {FilePath}.")]
    private static partial void LogInvalidStrykerComment(ILogger logger, Microsoft.CodeAnalysis.Text.LinePosition position, string filePath);
}
