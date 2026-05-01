using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// Spawns <c>Stryker.CLI.dll</c> as a subprocess via <c>dotnet exec</c>, captures
/// stdout/stderr, locates the produced StrykerOutput run-directory, and parses
/// the JSON mutation report. Designed for sequential E2E test execution
/// (DisableTestParallelization is enforced assembly-wide).
/// </summary>
public static class ProcessSpawnHelper
{
    private const int DefaultTimeoutSeconds = 240;

    private static readonly JsonSerializerOptions ReportJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Runs Stryker against the Sample.slnx with the supplied arguments.
    /// Working directory is <c>samples/</c> so StrykerOutput lands there.
    /// </summary>
    public static StrykerRunResult RunStrykerAgainstSample(string[] extraArgs, int timeoutSeconds = DefaultTimeoutSeconds)
    {
        var args = new[] { "--solution", RepoRoot.SampleSlnx }.Concat(extraArgs).ToArray();
        return RunCli(args, RepoRoot.SamplesDir, timeoutSeconds);
    }

    /// <summary>
    /// Runs the Stryker CLI with arbitrary arguments and a chosen working directory.
    /// Captures exit code, stdout, stderr; if a StrykerOutput run-dir was produced,
    /// finds it, locates the JSON mutation report inside, and deserialises it.
    /// </summary>
    public static StrykerRunResult RunCli(string[] args, string workingDirectory, int timeoutSeconds = DefaultTimeoutSeconds)
    {
        var preExistingRunDirs = SnapshotExistingRunDirs(workingDirectory);
        var (exitCode, stdout, stderr) = RunProcess(args, workingDirectory, timeoutSeconds);
        var newRunDir = FindNewRunDir(workingDirectory, preExistingRunDirs);
        var (jsonReportPath, report) = LoadJsonReport(newRunDir);
        return new StrykerRunResult(exitCode, stdout, stderr, newRunDir, jsonReportPath, report);
    }

    private static HashSet<string> SnapshotExistingRunDirs(string workingDirectory)
    {
        var outputRoot = Path.Combine(workingDirectory, "StrykerOutput");
        return Directory.Exists(outputRoot)
            ? Directory.GetDirectories(outputRoot).ToHashSet(StringComparer.Ordinal)
            : [];
    }

    private static (int ExitCode, string StdOut, string StdErr) RunProcess(string[] args, string workingDirectory, int timeoutSeconds)
    {
        var psi = BuildStartInfo(args, workingDirectory);
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start dotnet exec for {RepoRoot.StrykerCliBuildOutput}");

        var stdoutBuilder = new StringBuilder();
        var stderrBuilder = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (stdoutBuilder) { stdoutBuilder.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (stderrBuilder) { stderrBuilder.AppendLine(e.Data); } } };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            try { process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { /* process already exited between WaitForExit and Kill */ }
            throw new TimeoutException(
                $"Stryker CLI did not exit within {timeoutSeconds}s. StdOut so far:\n{stdoutBuilder}\n\nStdErr:\n{stderrBuilder}");
        }
        process.WaitForExit();

        return (process.ExitCode, stdoutBuilder.ToString(), stderrBuilder.ToString());
    }

    private static ProcessStartInfo BuildStartInfo(string[] args, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("exec");
        psi.ArgumentList.Add(RepoRoot.StrykerCliBuildOutput);
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }
        return psi;
    }

    private static string? FindNewRunDir(string workingDirectory, HashSet<string> preExistingRunDirs)
    {
        var outputRoot = Path.Combine(workingDirectory, "StrykerOutput");
        if (!Directory.Exists(outputRoot))
        {
            return null;
        }
        return Directory.GetDirectories(outputRoot)
            .Where(d => !preExistingRunDirs.Contains(d))
            .OrderByDescending(Directory.GetCreationTimeUtc)
            .FirstOrDefault();
    }

    private static (string? JsonReportPath, MutationReport? Report) LoadJsonReport(string? newRunDir)
    {
        if (newRunDir is null)
        {
            return (null, null);
        }
        var reportsDir = Path.Combine(newRunDir, "reports");
        if (!Directory.Exists(reportsDir))
        {
            return (null, null);
        }
        var jsonReportPath = Directory.GetFiles(reportsDir, "mutation-report.json").FirstOrDefault();
        if (jsonReportPath is null)
        {
            return (null, null);
        }
        try
        {
            var json = File.ReadAllText(jsonReportPath);
            var report = JsonSerializer.Deserialize<MutationReport>(json, ReportJsonOptions);
            return (jsonReportPath, report);
        }
        catch (JsonException)
        {
            return (jsonReportPath, null);
        }
    }
}
