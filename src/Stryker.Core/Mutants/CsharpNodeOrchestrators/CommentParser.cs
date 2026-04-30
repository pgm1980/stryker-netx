using System;
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

internal static class CommentParser
{
    private static readonly Regex Pattern = new("^\\s*(?<single>\\/\\/\\s*Stryker(?<singleCmd>.*))|(?<multi>\\/\\*\\s*Stryker(?<multiCmd>.*[^\\*][^\\\\])\\*\\/\\s*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200));
    private static readonly Regex Parser = new("^\\s*(?<mode>disable|restore)\\s*(?<once>once|)\\s*(?<mutators>[^:]*)\\s*:?(?<comment>.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200));
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

        Mutator[] filteredMutators;
        if (string.Equals(match.Groups["mutators"].Value.ToLowerInvariant().Trim(), "all", StringComparison.Ordinal))
        {
            filteredMutators = Enum.GetValues<Mutator>();
        }
        else
        {
            var labels = match.Groups["mutators"].Value.ToLowerInvariant().Split(',');
            filteredMutators = new Mutator[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                if (Enum.TryParse<Mutator>(labels[i], true, out var value))
                {
                    filteredMutators[i] = value;
                }
                else
                {
                    Logger.LogError(
                        "{Label} not recognized as a mutator at {Location}, {FilePath}. Legal values are {LegalValues}.",
                        labels[i],
                        node.GetLocation().GetMappedLineSpan().StartLinePosition,
                        node.SyntaxTree.FilePath,
                        string.Join(',', Enum.GetValues<Mutator>()));
                }
            }
        }

        return context.FilterMutators(disable, filteredMutators, string.Equals(match.Groups["once"].Value.ToLowerInvariant(), "once", StringComparison.Ordinal), comment);
    }

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
            Logger.LogWarning(exception,
                "Parsing Stryker comments at {StartLinePosition}, {FilePath} took too long to parse and was ignored. Comment: {Comment}",
                node.GetLocation().GetMappedLineSpan().StartLinePosition,
                node.SyntaxTree.FilePath, commentTrivia);
            return context;
        }
    }

    private static MutationContext InterpretStrykerComment(SyntaxNode node, MutationContext context, string commentTrivia)
    {
        // perform a quick pattern check to see if it is a 'Stryker comment'
        var strykerCommentMatch = Pattern.Match(commentTrivia);
        if (!strykerCommentMatch.Success)
        {
            return context;
        }

        // now we can extract actual command
        var isSingleLine = strykerCommentMatch.Groups["single"].Success;
        var command = isSingleLine ? strykerCommentMatch.Groups["singleCmd"].Value : strykerCommentMatch.Groups["multiCmd"].Value;

        var match = Parser.Match(command);
        if (match.Success)
        {
            // this is a Stryker comments, now we parse it
            return ParseStrykerComment(context, match, node);
        }

        Logger.LogWarning(
            "Invalid Stryker comments at {Position}, {FilePath}.",
            node.GetLocation().GetMappedLineSpan().StartLinePosition,
            node.SyntaxTree.FilePath);
        return context;
    }
}
