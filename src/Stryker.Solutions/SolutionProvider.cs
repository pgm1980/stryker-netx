namespace Stryker.Solutions;

public class SolutionProvider : ISolutionProvider
{
    public SolutionFile GetSolution(string solutionPath) => SolutionFile.GetSolution(solutionPath);
}
