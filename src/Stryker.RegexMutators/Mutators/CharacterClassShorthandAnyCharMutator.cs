using System.Collections.Generic;
using System.Globalization;
using Stryker.Regex.Parser.Nodes;
using Stryker.Regex.Parser.Nodes.CharacterClass;

namespace Stryker.RegexMutators.Mutators;

public sealed class CharacterClassShorthandAnyCharMutator : RegexMutatorBase<CharacterClassShorthandNode>, IRegexMutator
{
    /// <inheritdoc />
    public override IEnumerable<RegexMutation> ApplyMutations(CharacterClassShorthandNode node, RegexNode root)
    {
        var replacementNode = new CharacterClassNode(new CharacterClassCharacterSetNode([
            node,
            new CharacterClassShorthandNode(char.IsLower(node.Shorthand)
                                                ? char.ToUpper(node.Shorthand, CultureInfo.InvariantCulture)
                                                : char.ToLower(node.Shorthand, CultureInfo.InvariantCulture))
        ]), false);

        yield return new RegexMutation
        {
            OriginalNode    = node,
            ReplacementNode = replacementNode,
            DisplayName     = "Regex predefined character class to character class with its negation change",
            Description =
                $"""Character class shorthand "{node}" was replaced with "{replacementNode}" at offset {node.GetSpan().Start}.""",
            ReplacementPattern = root.ReplaceNode(node, replacementNode).ToString()
        };
    }
}
