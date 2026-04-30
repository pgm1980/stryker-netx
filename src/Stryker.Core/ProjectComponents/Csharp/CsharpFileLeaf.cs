using Microsoft.CodeAnalysis;
using Stryker.Abstractions;
using Stryker.Abstractions.ProjectComponents;
using System.Collections.Generic;

namespace Stryker.Core.ProjectComponents.Csharp;

public class CsharpFileLeaf : ProjectComponent<SyntaxTree>, IFileLeaf<SyntaxTree>
{
    public string SourceCode { get; set; } = string.Empty;

    /// <summary>
    /// The original unmutated syntax tree
    /// </summary>
    public SyntaxTree SyntaxTree { get; set; } = null!; // initialized via object initializer in CsharpProjectComponentsBuilder

    /// <summary>
    /// The mutated syntax tree
    /// </summary>
    public SyntaxTree MutatedSyntaxTree { get; set; } = null!; // initialized after mutation in CsharpMutationProcess

    public override IEnumerable<IMutant> Mutants { get; set; } = [];

    public override IEnumerable<SyntaxTree> CompilationSyntaxTrees => MutatedSyntaxTrees;

    public override IEnumerable<SyntaxTree> MutatedSyntaxTrees => new List<SyntaxTree> { MutatedSyntaxTree };

    public override IEnumerable<IFileLeaf<SyntaxTree>> GetAllFiles()
    {
        yield return this;
    }

    public override void Display()
    {
        DisplayFile(this);
    }
}
