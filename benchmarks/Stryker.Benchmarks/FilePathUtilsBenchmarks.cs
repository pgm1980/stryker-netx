using BenchmarkDotNet.Attributes;
using Stryker.Utilities;

namespace Stryker.Benchmarks;

/// <summary>
/// Hot-path 1: path-separator normalisation, called on every project/test file
/// during input-resolution and reporting.
/// </summary>
[MemoryDiagnoser]
public class FilePathUtilsBenchmarks
{
    [Params(
        "src/Stryker.Core/StrykerRunner.cs",
        @"src\Stryker.Core\StrykerRunner.cs",
        @"C:\Users\dev\repo\src\with\many\segments\and\back\slashes\file.cs")]
    public string Path { get; set; } = string.Empty;

    [Benchmark]
    public string? NormalizePathSeparators() => FilePathUtils.NormalizePathSeparators(Path);
}
