using Stryker.Regex.Parser.Nodes;

namespace Stryker.RegexMutators;

public class RegexMutation
{
    public required RegexNode OriginalNode { get; set; }
    public RegexNode? ReplacementNode { get; set; }
    public required string ReplacementPattern { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
}
