using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration;
using Stryker.Core.Helpers;
using Stryker.Core.Helpers.ProcessUtil;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Initialisation;

/// <summary>
/// Restores nuget packages for a given solution file
/// </summary>
public partial class NugetRestoreProcess : INugetRestoreProcess
{
    private IProcessExecutor ProcessExecutor { get; set; }
    private readonly ILogger _logger;

    public NugetRestoreProcess(IProcessExecutor processExecutor, ILogger<NugetRestoreProcess> logger)
    {
        ProcessExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RestorePackages(string solutionPath, string? msbuildPath = null)
    {
        LogRestoringNugetPackages(_logger);
        if (string.IsNullOrWhiteSpace(solutionPath))
        {
            throw new InputException(
                "Solution path is required on .net framework projects. Please supply the solution path.");
        }

        solutionPath = Path.GetFullPath(solutionPath);
        var solutionDir = Path.GetDirectoryName(solutionPath) ?? string.Empty;

        var helper = new MsBuildHelper(null, ProcessExecutor, msbuildPath, _logger);
        var msBuildVersion = FindMsBuildShortVersion(helper);

        // Validate nuget.exe is installed and included in path
        var nugetWhereExeResult = ProcessExecutor.Start(solutionDir, "where.exe", "nuget.exe");
        if (!nugetWhereExeResult.Output.Contains("nuget.exe", StringComparison.InvariantCultureIgnoreCase))
        {
            // try to extend the search
            nugetWhereExeResult = ProcessExecutor.Start(solutionDir, "where.exe",
                $"/R {Path.GetPathRoot(msbuildPath)} nuget.exe");

            if (!nugetWhereExeResult.Output.Contains("nuget.exe", StringComparison.InvariantCultureIgnoreCase))
                throw new InputException(
                    "Nuget.exe should be installed to restore .net framework nuget packages. Install nuget.exe and make sure it's included in your path.");
        }

        // Get the first nuget.exe path from the where.exe output
        var nugetPath = nugetWhereExeResult.Output
            .Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries)[0].Trim();

        if (!InternalRestore(solutionPath, msBuildVersion, nugetPath) && !string.IsNullOrEmpty(msBuildVersion))
        {
            InternalRestore(solutionPath, string.Empty, nugetPath);
        }
    }

    private bool InternalRestore(string solutionPath, string msBuildVersion, string nugetPath)
    {
        // Restore packages using nuget.exe
        var nugetRestoreCommand = $"restore \"{solutionPath}\"";
        if (!string.IsNullOrEmpty(msBuildVersion))
        {
            nugetRestoreCommand += $" -MsBuildVersion \"{msBuildVersion}\"";
        }

        LogRestoringWithCommand(_logger, nugetPath, nugetRestoreCommand);

        const int NugetRestoreTimeoutMs = 120000;
        try
        {
            var nugetRestoreResult = ProcessExecutor.Start(Path.GetDirectoryName(nugetPath) ?? string.Empty, nugetPath,
                nugetRestoreCommand, timeoutMs: NugetRestoreTimeoutMs);
            if (nugetRestoreResult.ExitCode == ExitCodes.Success)
            {
                LogRestoredPackages(_logger, nugetRestoreResult.Output);
                return true;
            }

            LogFailedNugetRestore(_logger, nugetRestoreResult.Error);
        }
        catch (OperationCanceledException e)
        {
            LogNugetRestoreTimeout(_logger, e, NugetRestoreTimeoutMs / 1000);
        }
        return false;
    }

    private string FindMsBuildShortVersion(MsBuildHelper helper)
    {
        var msBuildVersionOutput = helper.GetVersion();
        string msBuildVersion;
        if (string.IsNullOrWhiteSpace(msBuildVersionOutput))
        {
            msBuildVersion = string.Empty;
            if (_logger.IsEnabled(LogLevel.Information))
            {
                var msBuildPath = helper.GetMsBuildPath() ?? string.Empty;
                LogMsBuildNoVersion(_logger, msBuildPath);
            }
        }
        else
        {
            msBuildVersion = msBuildVersionOutput.Trim();
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var msBuildPath = helper.GetMsBuildPath() ?? string.Empty;
                LogMsBuildVersion(_logger, msBuildVersion, msBuildPath);
            }
        }

        return msBuildVersion;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Restoring nuget packages using nuget.exe")]
    private static partial void LogRestoringNugetPackages(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Restoring packages using command: {NugetPath} {NugetRestoreCommand}")]
    private static partial void LogRestoringWithCommand(ILogger logger, string nugetPath, string nugetRestoreCommand);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Restored packages using nuget.exe, output: {Error}")]
    private static partial void LogRestoredPackages(ILogger logger, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to restore nuget packages. Nuget error: {Error}")]
    private static partial void LogFailedNugetRestore(ILogger logger, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to restore nuget packages in less than {Time} seconds.")]
    private static partial void LogNugetRestoreTimeout(ILogger logger, Exception ex, int time);

    [LoggerMessage(Level = LogLevel.Information, Message = "Auto detected msbuild at: {MsBuildPath}, but failed to get version.")]
    private static partial void LogMsBuildNoVersion(ILogger logger, string msBuildPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Auto detected msbuild version {MsBuildVersion} at: {MsBuildPath}")]
    private static partial void LogMsBuildVersion(ILogger logger, string msBuildVersion, string msBuildPath);
}
