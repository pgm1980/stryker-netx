using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Analysis;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Utilities.Logging;
using Stryker.Utilities.MSBuild;

namespace Stryker.Core.ProjectComponents.TestProjects;

public partial class TestProjectsInfo : ITestProjectsInfo
{
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<TestProjectsInfo> _logger;

    public IEnumerable<ITestProject> TestProjects { get; set; }

    public IEnumerable<ITestFile> TestFiles => TestProjects.SelectMany(testProject => testProject.TestFiles).Distinct();

    public IEnumerable<IProjectAnalysis> Analyses => TestProjects.Select(testProject => testProject.Analysis);

    public IReadOnlyList<string> GetTestAssemblies() =>
        [.. Analyses.Select(a => a.GetAssemblyPath())];

    public TestProjectsInfo(IFileSystem fileSystem, ILogger<TestProjectsInfo>? logger = null)
    {
        _fileSystem = fileSystem ?? new FileSystem();
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<TestProjectsInfo>();
        TestProjects = [];
    }

    public static TestProjectsInfo operator +(TestProjectsInfo a, TestProjectsInfo b) =>
        new(a._fileSystem, a._logger)
        {
            TestProjects = a.TestProjects.Union(b.TestProjects)
        };

    public static bool operator ==(TestProjectsInfo? left, TestProjectsInfo? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(TestProjectsInfo? left, TestProjectsInfo? right) => !(left == right);

    public override bool Equals(object? obj) => ReferenceEquals(this, obj);

    public override int GetHashCode() => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);

    public void RestoreOriginalAssembly(IProjectAnalysis sourceProject)
    {
        foreach (var testProject in Analyses)
        {
            var injectionPath = GetInjectionFilePath(testProject, sourceProject);
            var backupFilePath = GetBackupName(injectionPath);

            if (!_fileSystem.File.Exists(backupFilePath))
            {
                continue;
            }
            try
            {
                _fileSystem.File.Copy(backupFilePath, injectionPath, true);
            }
            catch (IOException ex)
            {
                LogFailedToRestore(_logger, ex, injectionPath);
            }
        }
    }

    public void BackupOriginalAssembly(IProjectAnalysis sourceProject)
    {
        foreach (var testProject in Analyses)
        {
            var injectionPath = GetInjectionFilePath(testProject, sourceProject);
            var backupFilePath = GetBackupName(injectionPath);
            if (!_fileSystem.Directory.Exists(sourceProject.GetAssemblyDirectoryPath()))
            {
                _fileSystem.Directory.CreateDirectory(sourceProject.GetAssemblyDirectoryPath());
            }
            if (_fileSystem.File.Exists(injectionPath))
            {
                // Only create backup if there isn't already a backup
                if (!_fileSystem.File.Exists(backupFilePath))
                {
                    _fileSystem.File.Move(injectionPath, backupFilePath, false);
                }
            }
            else
            {
                LogCouldNotLocateAssembly(_logger, injectionPath);
            }
        }
    }

    public static string GetInjectionFilePath(IProjectAnalysis testProject, IProjectAnalysis sourceProject) => Path.Combine(testProject.GetAssemblyDirectoryPath(), sourceProject.GetAssemblyFileName());

    private static string GetBackupName(string injectionPath) => injectionPath + ".stryker-unchanged";

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to restore output assembly {Path}. Mutated assembly is still in place.")]
    private static partial void LogFailedToRestore(ILogger logger, System.Exception ex, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not locate source assembly {InjectionPath}")]
    private static partial void LogCouldNotLocateAssembly(ILogger logger, string injectionPath);
}
