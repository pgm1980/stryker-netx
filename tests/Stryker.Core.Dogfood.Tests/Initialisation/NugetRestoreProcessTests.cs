using System;
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

/// <summary>Sprint 98 (v2.84.0) v2.x-shape rewrite of upstream NugetRestoreProcessTests.
/// Sprint 99 corrects the production-mock contract: <c>MsBuildHelper.GetVersion()</c> now
/// invokes <c>dotnet msbuild -version /nologo</c> (with a space) and
/// <c>NugetRestoreProcess.FindMsBuildShortVersion</c> extracts the last non-empty line of the
/// output so the multi-line .NET-SDK-MSBuild banner does not leak into the
/// <c>nuget.exe -MsBuildVersion</c> argument. Upstream tests covering the dropped
/// vswhere.exe + MSBuild.exe orchestration remain skipped per ADR-010.</summary>
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
        processExecutorMock.Setup(x => x.Start("", "dotnet", "msbuild -version /nologo", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = msBuildVersion });
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe", "nuget.exe", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = nugetPath });
        processExecutorMock.Setup(x => x.Start(nugetDirectory, nugetPath,
                $"restore \"{Path.GetFullPath(SolutionPath)}\" -MsBuildVersion \"{msBuildVersion}\"", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "Packages restored" });

        var target = new NugetRestoreProcess(processExecutorMock.Object, TestLoggerFactory.CreateLogger<NugetRestoreProcess>());

        target.RestorePackages(SolutionPath);

        processExecutorMock.Verify(p => p.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public void HappyFlow_WithMultiLineMsBuildVersionOutput_ExtractsNumericVersionForNugetRestore()
    {
        var nugetPath = @"C:\choco\bin\NuGet.exe";
        var numericVersion = "18.0.11.21808";
        // Real .NET SDK output from `dotnet msbuild -version /nologo`:
        // line 1 = locale-dependent banner, line 2 = numeric version. Strict mock below
        // asserts that nuget.exe receives only the numeric version — never the full blob.
        var multiLineOutput = "MSBuild-Version 18.0.11+b16286c22 für .NET" + Environment.NewLine + numericVersion;
        var nugetDirectory = Path.GetDirectoryName(nugetPath)!;

        var processExecutorMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processExecutorMock.Setup(x => x.Start("", "dotnet", "msbuild -version /nologo", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = multiLineOutput });
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe", "nuget.exe", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = nugetPath });
        processExecutorMock.Setup(x => x.Start(nugetDirectory, nugetPath,
                $"restore \"{Path.GetFullPath(SolutionPath)}\" -MsBuildVersion \"{numericVersion}\"", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "Packages restored" });

        var target = new NugetRestoreProcess(processExecutorMock.Object, TestLoggerFactory.CreateLogger<NugetRestoreProcess>());

        target.RestorePackages(SolutionPath);

        processExecutorMock.Verify(p => p.Start(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<KeyValuePair<string, string>>>(), It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public void ShouldThrowOnNugetNotInstalled()
    {
        var msBuildVersion = "16.0.0";

        var processExecutorMock = new Mock<IProcessExecutor>(MockBehavior.Strict);
        processExecutorMock.Setup(x => x.Start("", "dotnet", "msbuild -version /nologo", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = msBuildVersion });
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe", "nuget.exe", null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "INFO: Could not find files for the given pattern(s)." });
        // Default ctor → msbuildPath = null → Path.GetPathRoot(null) → null, so the production
        // fallback emits `where.exe /R  nuget.exe` (extra space). Match any /R-prefixed lookup.
        processExecutorMock.Setup(x => x.Start(_solutionDir, "where.exe",
                It.Is<string>(s => s.Contains("/R", StringComparison.Ordinal) && s.EndsWith("nuget.exe", StringComparison.Ordinal)), null, It.IsAny<int>()))
            .Returns(new ProcessResult { ExitCode = 0, Output = "INFO: Could not find files for the given pattern(s)." });

        var target = new NugetRestoreProcess(processExecutorMock.Object, TestLoggerFactory.CreateLogger<NugetRestoreProcess>());
        Action act = () => target.RestorePackages(SolutionPath);
        act.Should().Throw<InputException>().WithMessage("*Nuget.exe should be installed*");
    }
}
