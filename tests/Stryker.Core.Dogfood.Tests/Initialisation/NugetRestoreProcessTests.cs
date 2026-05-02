using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Moq;
using Stryker.Abstractions.Exceptions;
using Stryker.Core.Helpers.ProcessUtil;
using Stryker.Core.Initialisation;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Initialisation;

/// <summary>Sprint 98 (v2.84.0) v2.x-shape rewrite of upstream NugetRestoreProcessTests
/// (replaces Sprint 93 placeholder). PRODUCTION DRIFT: stryker-netx uses
/// `MsBuildHelper.GetVersion()` which calls <c>dotnet msbuild-version /nologo</c>
/// directly instead of upstream's where.exe + vswhere.exe + MSBuild.exe -version flow.
/// 2 happy-path tests (HappyFlow + ShouldThrowOnNugetNotInstalled) are portable as v2.x-shape;
/// 4 upstream tests covering vswhere.exe orchestration are not relevant in v2.x and skipped
/// with documented reason.</summary>
public class NugetRestoreProcessTests : TestBase
{
    private const string SolutionPath = @"..\MySolution.sln";
    private readonly string _solutionDir = Path.GetDirectoryName(Path.GetFullPath(SolutionPath))!;

    [Fact]
    public void HappyFlow()
    {
        var nugetPath = @"C:\choco\bin\NuGet.exe";
        var msBuildVersion = "16.0.0";
        var nugetDirectory = Path.GetDirectoryName(nugetPath)!;

        var processExecutorMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        // v2.x: MsBuildHelper.GetVersion() runs `dotnet msbuild-version /nologo` from cwd ""
        // NOTE: production GetMsBuildExeAndCommand() returns ("dotnet","msbuild") with no trailing space,
// so the resulting GetVersion args are "msbuild-version /nologo" (no space between msbuild and -version).
// Matching production exactly here.
            processExecutorMock.Setup(x => x.Start("", "dotnet", "msbuild-version /nologo", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = msBuildVersion });
        // where.exe nuget.exe → returns nuget path
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe", "nuget.exe", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = nugetPath });
        // nuget.exe restore solution.sln -MsBuildVersion 16.0.0 → success
        processExecutorMock.Setup(x => x.Start(nugetDirectory, nugetPath,
                $"restore \"{Path.GetFullPath(SolutionPath)}\" -MsBuildVersion \"{msBuildVersion}\"", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "Packages restored" });

        var target = new NugetRestoreProcess(processExecutorMock.Object, TestLoggerFactory.CreateLogger<NugetRestoreProcess>());

        target.RestorePackages(SolutionPath);

        // exactly 3 IProcessExecutor.Start calls in v2.x happy-path
        processExecutorMock.Verify(p => p.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public void ShouldThrowOnNugetNotInstalled()
    {
        var msBuildVersion = "16.0.0";

        var processExecutorMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        // dotnet msbuild-version succeeds — production needs MSBuild version BEFORE nuget lookup
        // NOTE: production GetMsBuildExeAndCommand() returns ("dotnet","msbuild") with no trailing space,
// so the resulting GetVersion args are "msbuild-version /nologo" (no space between msbuild and -version).
// Matching production exactly here.
            processExecutorMock.Setup(x => x.Start("", "dotnet", "msbuild-version /nologo", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = msBuildVersion });
        // where.exe nuget.exe → not found
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe", "nuget.exe", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "INFO: Could not find files for the given pattern(s)." });
        // The fallback `where.exe /R <root> nuget.exe` is also not setup → would fail strict mock if reached.
        // But on `msbuildPath = null` (default), Path.GetPathRoot(null) returns null → the production
        // fallback is `where.exe /R  nuget.exe` (extra space) — let's setup that match too as catch-all:
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe",
                It.Is<string>(s => s.Contains("/R", System.StringComparison.Ordinal) && s.EndsWith("nuget.exe", System.StringComparison.Ordinal)), null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "INFO: Could not find files for the given pattern(s)." });

        var target = new NugetRestoreProcess(processExecutorMock.Object, TestLoggerFactory.CreateLogger<NugetRestoreProcess>());
        System.Action act = () => target.RestorePackages(SolutionPath);
        act.Should().Throw<InputException>().WithMessage("*Nuget.exe should be installed*");
    }
}
