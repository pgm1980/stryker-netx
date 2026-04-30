namespace Stryker.Core.ProjectComponents.SourceProjects;

public class SolutionInfo(string file, string configuration, string platform)
{
    public string SolutionFilePath { get; init; } = file;
    public string Configuration { get; init; } = configuration;
    public string Platform { get; init; } = platform;
}
