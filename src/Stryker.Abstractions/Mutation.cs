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

    /// <summary>
    /// Optional human-readable description of the mutation.
    /// Set by regex-mutators (which provide pattern-specific text);
    /// the C# mutators leave this null and rely on <see cref="DisplayName"/>.
    /// </summary>
    public string? Description { get; set; }
}
