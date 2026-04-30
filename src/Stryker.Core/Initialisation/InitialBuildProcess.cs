using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration;
using Stryker.Core.Helpers;
using Stryker.Core.Helpers.ProcessUtil;

namespace Stryker.Core.Initialisation;

public partial class InitialBuildProcess(
    IProcessExecutor processExecutor,
    IFileSystem fileSystem,
    ILogger<InitialBuildProcess> logger) : IInitialBuildProcess
{
    private readonly IProcessExecutor _processExecutor = processExecutor ?? throw new ArgumentNullException(nameof(processExecutor));
    private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void InitialBuild(bool fullFramework, string projectPath, string solutionPath, string? configuration = null,
        string? platform = null, string? targetFramework = null,
        string? msbuildPath = null)
    {
        if (fullFramework)
        {
            // ensure prerequisites for building .NETFramework projects are met
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                throw new InputException("Stryker cannot build .NET Framework projects on non-Windows platforms.");
            }
            if (string.IsNullOrEmpty(solutionPath))
            {
                throw new InputException("Stryker could not build your project as no solution file was presented. Please pass the solution path to stryker.");
            }
        }

        var msBuildHelper = new MsBuildHelper(fileSystem: _fileSystem, executor: _processExecutor, msBuildPath: msbuildPath);

        LogStartedInitialBuild(_logger);

        var target = !string.IsNullOrEmpty(solutionPath) ? solutionPath : projectPath;
        var buildPath = _fileSystem.Path.GetFileName(target);
        var directoryName = _fileSystem.Path.GetDirectoryName(target) ?? string.Empty;
        var (result, exe, args) = msBuildHelper.BuildProject(directoryName,
            buildPath,
            fullFramework,
            configuration: configuration,
            platform: platform,
            forcedFramework: targetFramework);

        if (OperatingSystem.IsWindows() && result.ExitCode != ExitCodes.Success && !string.IsNullOrEmpty(solutionPath))
        {
            // dump previous build result
            LogInitialBuildOutput(_logger, result.Output);
            LogDotnetBuildFailed(_logger);
            (result, _, _) = msBuildHelper.BuildProject(directoryName,
                buildPath,
                true,
                configuration,
                options: "-t:restore -p:RestorePackagesConfig=true", forcedFramework: targetFramework);

            if (result.ExitCode != ExitCodes.Success)
            {
                LogPackageRestoreFailed(_logger, result.Output);
            }
            LogLastAttempt(_logger);
            (result, exe, args) = msBuildHelper.BuildProject(directoryName,
                buildPath,
                true,
                configuration,
                forcedFramework: targetFramework);
        }

        CheckBuildResult(result, target, exe, args);
    }

    private void CheckBuildResult(ProcessResult result, string path, string buildCommand, string options)
    {
        if (result.ExitCode != ExitCodes.Success)
        {
            LogInitialBuildFailed(_logger, buildCommand, options, path, result.Output);
            // Initial build failed
            throw new InputException(result.Output, FormatBuildResultErrorString(buildCommand, options));
        }
        LogBuildOutput(_logger, result.Output);
        LogBuildSuccessful(_logger);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Started initial build using dotnet build")]
    private static partial void LogStartedInitialBuild(ILogger logger);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Initial build output: {Output}")]
    private static partial void LogInitialBuildOutput(ILogger logger, string output);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Dotnet build failed, trying with MsBuild and forcing package restore.")]
    private static partial void LogDotnetBuildFailed(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Package restore failed: {Result}")]
    private static partial void LogPackageRestoreFailed(ILogger logger, string result);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Last attempt to build.")]
    private static partial void LogLastAttempt(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Initial build failed. Command was [{Exe} {Args}] (in folder '{Folder}'). Result: {Result}")]
    private static partial void LogInitialBuildFailed(ILogger logger, string exe, string args, string folder, string result);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Initial build output {Result}")]
    private static partial void LogBuildOutput(ILogger logger, string result);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Initial build successful")]
    private static partial void LogBuildSuccessful(ILogger logger);

    private static string FormatBuildResultErrorString(string buildCommand, string options) =>
        "Initial build of targeted project failed. Please make sure the targeted project is buildable." +
        $" You can reproduce this error yourself using: \"{QuotesIfNeeded(buildCommand)} {options}\"";

    private static string QuotesIfNeeded(string parameter)
    {
        if (!parameter.Contains(' ') || parameter.Length < 3 || parameter[0] == '"' && parameter[^1] == '"')
        {
            return parameter;
        }
        return $"\"{parameter}\"";
    }
}
