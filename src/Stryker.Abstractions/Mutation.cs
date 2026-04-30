using Microsoft.CodeAnalysis;

namespace Stryker.Abstractions;

/// <summary>
/// Represents a single mutation on code level
/// </summary>
public class Mutation
{
    public required SyntaxNode OriginalNode { get; set; }
    public required SyntaxNode ReplacementNode { get; set; }
    public required string DisplayName { get; set; }
    public Mutator Type { get; set; }
    public required string Description { get; set; }
}
