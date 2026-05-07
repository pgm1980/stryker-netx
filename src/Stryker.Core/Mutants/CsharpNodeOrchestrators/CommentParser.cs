using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        // Sprint 160 (ADR-040 Issue γ): array-with-default-fallback (Mutator[] = Statement on
        // unrecognised label) replaced by List<Mutator> with skip-on-failure. Closes silent
        // semantic corruption where a typo in a mutator label silently disabled Statement
        // mutations on the targeted code position.
        List<Mutator> filteredMutators;
        var rawMutators = match.Groups["mutators"].Value.Trim();
        if (string.Equals(rawMutators, "all", StringComparison.OrdinalIgnoreCase))
        {
            filteredMutators = [.. Enum.GetValues<Mutator>()];
        }
        else
        {
            var labels = rawMutators.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            filteredMutators = new List<Mutator>(labels.Length);
            foreach (var label in labels)
            {
                if (Enum.TryParse<Mutator>(label, true, out var value))
                {
                    filteredMutators.Add(value);
                }
                else
                {
                    // Sprint 160 (ADR-040 Issue α): if the unrecognised label looks like a Mutator
                    // class name (PascalCase, alphanumeric), append a hint pointing the user at
                    // the Kind-name they should use instead. Empty hint otherwise.
                    var hint = LooksLikeMutatorClassName(label)
                        ? "Hint: mutator class names are not accepted here — use the Mutator-Kind name (see _docs/disable-comment-syntax.md for the Class-to-Kind mapping)."
                        : string.Empty;
                    LogLabelNotRecognized(Logger, label, node.GetLocation().GetMappedLineSpan().StartLinePosition, node.SyntaxTree.FilePath, string.Join(',', Enum.GetValues<Mutator>()), hint);
                }
            }
        }

        // Sprint 160 (ADR-040 Issue β): scope group accepts `next-line` (alias for `once`,
        // single-mutation scope), `once` (existing single-mutation scope), or empty
        // (block-disable until matching restore — existing behavior).
        var scope = match.Groups["scope"].Value.ToLowerInvariant();
        var isOnceOrNextLine = string.Equals(scope, "once", StringComparison.Ordinal)
                            || string.Equals(scope, "next-line", StringComparison.Ordinal);

        return context.FilterMutators(disable, [.. filteredMutators], isOnceOrNextLine, comment);
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
        var comments = node.GetFirstToken(true).GetPreviousToken(true)
            .TrailingTrivia.Union(node.GetLeadingTrivia())
            .Where(t => t.IsKind(SyntaxKind.MultiLineCommentTrivia) || t.IsKind(SyntaxKind.SingleLineCommentTrivia)).ToList();
        var result = comments.Aggregate(context, (current, t) => ProcessComment(node, current, t.ToString()));

        return result;
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
