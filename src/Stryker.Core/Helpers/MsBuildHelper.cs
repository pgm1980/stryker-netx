using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Stryker.Configuration;
using Stryker.Core.Helpers.ProcessUtil;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Helpers;

/// <summary>
/// Resolves and invokes MSBuild for project builds.
/// </summary>
/// <remarks>
/// stryker-netx (ADR-010) drops the upstream Stryker.NET 4.14.1 vswhere-fallback chain
/// (Visual Studio 2017 + legacy .NET Framework MSBuild paths). On .NET 10 the SDK ships
/// a bundled MSBuild that is invoked via <c>dotnet msbuild</c>, which works on Windows,
/// Linux, and macOS without external probing. An explicit <c>msBuildPath</c> can still
/// be passed via the constructor (DI override), and the legacy MSBuild.exe code path is
/// kept for that case so users on locked-down build agents can still inject a path.
/// </remarks>
public sealed partial class MsBuildHelper
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger _logger;
    private readonly IProcessExecutor _executor;

    private string? _msBuildPath;

    /// <summary>
    /// Initializes a new <see cref="MsBuildHelper"/>.
    /// </summary>
    /// <param name="fileSystem">File-system abstraction (defaults to <see cref="FileSystem"/>).</param>
    /// <param name="executor">Process-executor abstraction (defaults to <see cref="ProcessExecutor"/>).</param>
    /// <param name="msBuildPath">Optional explicit MSBuild path; when null, <c>dotnet msbuild</c> is used.</param>
    /// <param name="logger">Optional logger (defaults to <see cref="ApplicationLogging"/>).</param>
    public MsBuildHelper(IFileSystem? fileSystem = null, IProcessExecutor? executor = null, string? msBuildPath = null, ILogger? logger = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<MsBuildHelper>();
        _executor = executor ?? new ProcessExecutor();
        _msBuildPath = msBuildPath;
    }

    /// <summary>
    /// Returns the MSBuild version string, or <see cref="string.Empty"/> when invocation failed.
    /// </summary>
    public string GetVersion()
    {
        var (exe, command) = GetMsBuildExeAndCommand();
        var msBuildVersionOutput = _executor.Start("", exe, $"{command}-version /nologo");
        return msBuildVersionOutput.ExitCode != ExitCodes.Success ? string.Empty : msBuildVersionOutput.Output.Trim();
    }

    /// <summary>
    /// Returns the configured MSBuild path. When no explicit path was injected via the
    /// constructor, returns <see langword="null"/> — callers should then fall back to
    /// <c>dotnet msbuild</c>.
    /// </summary>
    public string? GetMsBuildPath()
    {
        if (!string.IsNullOrWhiteSpace(_msBuildPath) && _fileSystem.File.Exists(_msBuildPath))
        {
            return _msBuildPath;
        }

        if (!string.IsNullOrWhiteSpace(_msBuildPath))
        {
            LogMsBuildPathNotFound(_logger, _msBuildPath);
            _msBuildPath = null;
        }

        return _msBuildPath;
    }

    /// <summary>
    /// Builds a project using either MSBuild (when <paramref name="usingMsBuild"/> is true)
    /// or <c>dotnet build</c>.
    /// </summary>
    public (ProcessResult result, string exe, string command) BuildProject(string path, string projectFile, bool usingMsBuild,
        string? configuration = null, string? platform = null, string? options = null, string? forcedFramework = null)
    {
        var (exe, command) = usingMsBuild ? GetMsBuildExeAndCommand() : ("dotnet", "build");

        List<string> fullOptions = string.IsNullOrEmpty(command) ? [QuotesIfNeeded(projectFile)] : [command, QuotesIfNeeded(projectFile)];
        if (!string.IsNullOrEmpty(configuration))
        {
            fullOptions.Add($"{(usingMsBuild ? "/property:Configuration=" : "-c ") + QuotesIfNeeded(configuration)}");
        }

        if (!string.IsNullOrEmpty(platform))
        {
            fullOptions.Add($"{(usingMsBuild ? "/" : "--")}property:Platform={QuotesIfNeeded(platform)}");
        }

        if (options is not null)
        {
            fullOptions.Add(options);
        }

        var arguments = string.Join(' ', fullOptions);
        LogBuildingProject(_logger, projectFile, exe, arguments, path);
        return (_executor.Start(path, exe, arguments), exe, arguments);
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Configured MSBuild path '{MsBuildPath}' was not found on disk. Falling back to 'dotnet msbuild'.")]
    private static partial void LogMsBuildPathNotFound(ILogger logger, string msBuildPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Building project {Project} using {MsBuildPath} {Options} (directory {Path}.)")]
    private static partial void LogBuildingProject(ILogger logger, string project, string msBuildPath, string options, string path);

    private static string QuotesIfNeeded(string parameter)
    {
        if (!parameter.Contains(' ', StringComparison.Ordinal) || parameter.Length < 3 || (parameter[0] == '"' && parameter[^1] == '"'))
        {
            return parameter;
        }
        return $"\"{parameter}\"";
    }

    private (string executable, string command) GetMsBuildExeAndCommand()
    {
        var configuredPath = GetMsBuildPath();
        if (configuredPath is null)
        {
            return ("dotnet", "msbuild");
        }

        return configuredPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? (configuredPath, string.Empty)
            : ("dotnet", QuotesIfNeeded(configuredPath) + ' ');
    }
}
