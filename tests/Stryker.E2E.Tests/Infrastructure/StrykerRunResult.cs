namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Captured outcome of a single subprocess invocation of <c>Stryker.CLI.dll</c>.
/// Includes the exit code, captured streams, the StrykerOutput run-directory
/// the CLI produced (if any), and the parsed mutation report (if a JSON
/// report was emitted).
/// </summary>
public sealed record StrykerRunResult(
    int ExitCode,
    string StdOut,
    string StdErr,
    string? StrykerOutputRunDir,
    string? JsonReportPath,
    MutationReport? Report);
