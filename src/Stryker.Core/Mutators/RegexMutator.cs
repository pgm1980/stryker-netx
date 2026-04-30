using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Core.Helpers;
using Stryker.RegexMutators;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Mutators;

[MutationProfileMembership(MutationProfile.Defaults | MutationProfile.Stronger | MutationProfile.All)]

public partial class RegexMutator : MutatorBase<ObjectCreationExpressionSyntax>
{
    private const string PatternArgumentName = "pattern";
    private ILogger Logger { get; }

    public override MutationLevel MutationLevel => MutationLevel.Advanced;

    public RegexMutator()
    {
        Logger = ApplicationLogging.LoggerFactory.CreateLogger<RegexMutator>();
    }

    public override IEnumerable<Mutation> ApplyMutations(ObjectCreationExpressionSyntax node,
        SemanticModel semanticModel)
    {
        var name = node.Type.ToString();
        if (string.Equals(name, nameof(Regex), StringComparison.Ordinal) || string.Equals(name, typeof(Regex).FullName, StringComparison.Ordinal))
        {
            var arguments = node.ArgumentList!.Arguments;
            var namedArgument = arguments.FirstOrDefault(argument =>
                string.Equals(argument.NameColon?.Name.Identifier.ValueText, PatternArgumentName, StringComparison.Ordinal));
            var patternArgument = namedArgument ?? node.ArgumentList.Arguments.FirstOrDefault();
            var patternExpression = patternArgument?.Expression;

            if (patternExpression!= null && patternExpression.IsAStringExpression())
            {
                var currentValue = ((LiteralExpressionSyntax)patternExpression).Token.ValueText;
                var regexMutantOrchestrator = new RegexMutantOrchestrator(currentValue);
                var replacementValues = regexMutantOrchestrator.Mutate();
                foreach (var regexMutation in replacementValues)
                {
                    try
                    {
                        _ = new Regex(regexMutation.ReplacementPattern, RegexOptions.ExplicitCapture, TimeSpan.FromMilliseconds(200));
                    }
                    catch (ArgumentException exception)
                    {
                        LogInvalidRegex(Logger, exception, currentValue, regexMutation.ReplacementPattern);
                        continue;
                    }

                    yield return new Mutation()
                    {
                        OriginalNode = node,
                        ReplacementNode =  node.ReplaceNode(patternExpression, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression,
                            SyntaxFactory.Literal(regexMutation.ReplacementPattern))),
                        DisplayName = regexMutation.DisplayName,
                        Type = Mutator.Regex,
                        Description = regexMutation.Description
                    };
                }
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "RegexMutator created mutation {CurrentValue} -> {ReplacementPattern} which is an invalid regular expression")]
    private static partial void LogInvalidRegex(ILogger logger, Exception ex, string currentValue, string replacementPattern);
}
