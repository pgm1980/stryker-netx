namespace Stryker.Abstractions.ProjectComponents;

public interface IReadOnlyFileLeaf : IReadOnlyProjectComponent
{
    string SourceCode { get; }
}
