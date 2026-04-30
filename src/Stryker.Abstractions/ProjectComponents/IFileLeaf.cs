namespace Stryker.Abstractions.ProjectComponents;

public interface IFileLeaf<T> : IFileLeaf
{
    T SyntaxTree { get; set; }

    T MutatedSyntaxTree { get; set; }
}

public interface IFileLeaf : IReadOnlyFileLeaf
{
    new string SourceCode { get; set; }
}
