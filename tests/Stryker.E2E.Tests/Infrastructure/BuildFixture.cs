using System;
using System.Diagnostics;
using System.IO;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>
/// xUnit collection fixture that ensures Stryker.CLI.dll is built (Debug,
/// net10.0) before any E2E test runs. The test project carries a
/// <c>ProjectReference</c> with <c>ReferenceOutputAssembly=false</c> so build
/// ordering already guarantees the DLL exists; this fixture is a defensive
/// fallback for `dotnet test` invocations that bypass the standard build
/// (e.g. <c>--no-build</c>).
/// </summary>
public sealed class BuildFixture
{
    public BuildFixture()
    {
        if (File.Exists(RepoRoot.StrykerCliBuildOutput))
        {
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = RepoRoot.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(RepoRoot.StrykerCliProject);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("--nologo");

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to spawn `dotnet build` for Stryker.CLI fallback build");
        process.WaitForExit();
        if (process.ExitCode != 0 || !File.Exists(RepoRoot.StrykerCliBuildOutput))
        {
            throw new InvalidOperationException(
                $"Fallback build of Stryker.CLI failed (exit {process.ExitCode}). "
                + $"StdOut:\n{process.StandardOutput.ReadToEnd()}\n\nStdErr:\n{process.StandardError.ReadToEnd()}");
        }
    }
}
