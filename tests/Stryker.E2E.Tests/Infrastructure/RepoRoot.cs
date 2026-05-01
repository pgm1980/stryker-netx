using System;
using System.IO;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Walks up the parent-directory chain from the test-assembly location until
/// the repository root (identified by <c>stryker-netx.slnx</c>) is found.
/// E2E tests cannot use relative paths because xUnit launches them from
/// <c>tests/Stryker.E2E.Tests/bin/Debug/net10.0/</c>, several levels deep.
/// </summary>
internal static class RepoRoot
{
    private const string SolutionFileMarker = "stryker-netx.slnx";

    private static readonly Lazy<string> Cached = new(Resolve);

    public static string Path => Cached.Value;

    public static string SamplesDir => System.IO.Path.Combine(Path, "samples");

    public static string SampleSlnx => System.IO.Path.Combine(SamplesDir, "Sample.slnx");

    public static string StrykerCliBuildOutput =>
        System.IO.Path.Combine(Path, "src", "Stryker.CLI", "bin", "Debug", "net10.0", "Stryker.CLI.dll");

    public static string StrykerCliProject =>
        System.IO.Path.Combine(Path, "src", "Stryker.CLI", "Stryker.CLI.csproj");

    private static string Resolve()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(System.IO.Path.Combine(dir.FullName, SolutionFileMarker)))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"Could not locate '{SolutionFileMarker}' in any parent of '{AppContext.BaseDirectory}'.");
    }
}
