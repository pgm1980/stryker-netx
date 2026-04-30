namespace Stryker.Solutions;

public interface ISolutionProvider
{
    SolutionFile GetSolution(string solutionPath);
}
