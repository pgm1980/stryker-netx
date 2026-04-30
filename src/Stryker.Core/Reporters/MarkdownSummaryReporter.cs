using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Grynwald.MarkdownGenerator;
using Spectre.Console;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters;

/// <summary>
/// Markdown result table reporter.
/// </summary>
public class MarkdownSummaryReporter : IReporter
{
    private static readonly string[] SummaryTableHeaders =
    [
        "File",
        "Score",
        "Killed",
        "Survived",
        "Timeout",
        "No Coverage",
        "Ignored",
        "Compile Errors",
        "Total Detected",
        "Total Undetected",
        "Total Mutants"
    ];

    private readonly IStrykerOptions _options;
    private readonly IAnsiConsole _console;
    private readonly IFileSystem _fileSystem;

    public MarkdownSummaryReporter(IStrykerOptions strykerOptions, IFileSystem? fileSystem = null, IAnsiConsole? console = null)
    {
        _options = strykerOptions;
        _console = console ?? AnsiConsole.Console;
        _fileSystem = fileSystem ?? new FileSystem();
    }

    public void OnStartMutantTestRun(IEnumerable<IReadOnlyMutant> mutantsToBeTested)
    {
        // This reporter does not report during the testrun
    }

    public void OnMutantsCreated(IReadOnlyProjectComponent reportComponent, ITestProjectsInfo testProjectsInfo)
    {
        // This reporter does not report during the testrun
    }

    public void OnMutantTested(IReadOnlyMutant result)
    {
        // This reporter does not report during the testrun
    }

    public void OnAllMutantsTested(IReadOnlyProjectComponent reportComponent, ITestProjectsInfo testProjectsInfo)
    {
        var files = reportComponent.GetAllFiles();
        if (files.Any())
        {
            var filename = _options.ReportFileName + ".md";
            var reportPath = Path.Combine(_options.ReportPath, filename);
            var reportUri = "file://" + reportPath.Replace("\\", "/");

            GenerateMarkdownReport(reportPath, files, reportComponent.GetMutationScore());

            _console.WriteLine();
            _console.MarkupLine("[Green]Your Markdown summary has been generated at:[/]");

            // We must print the report path as the link text because on some terminals links might be supported but not actually clickable: https://github.com/spectreconsole/spectre.console/issues/764
            _console.MarkupLineInterpolated(CultureInfo.InvariantCulture, _console.Profile.Capabilities.Links
                ? $"[Green][link={reportUri}]{reportPath}[/][/]"
                : (FormattableString)$"[Green]{reportUri}[/]");
        }
    }

    private void GenerateMarkdownReport(string reportPath, IEnumerable<IFileLeaf> files, double mutationScore)
    {
        if (!files.Any())
        {
            return;
        }

        var mdSummaryDocument = new MdDocument();
        mdSummaryDocument.Root.Add(new MdHeading(1, "Mutation Testing Summary"));

        var mdSummary = new MdTable(new MdTableRow(SummaryTableHeaders));

        foreach (var file in files)
        {
            mdSummary.Add(GenerateFileData(file));
        }

        mdSummaryDocument.Root.Add(mdSummary);
        mdSummaryDocument.Root.Add(new MdHeading(2, new MdTextSpan($"The final mutation score is {mutationScore * 100:N2}%")));
        mdSummaryDocument.Root.Add(new MdHeading(3, new MdEmphasisSpan($"Coverage Thresholds: high:{_options.Thresholds.High} low:{_options.Thresholds.Low} break:{_options.Thresholds.Break}")));

        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? string.Empty);
        _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(reportPath) ?? string.Empty);
        using var mdFile = _fileSystem.File.Create(reportPath);
        mdSummaryDocument.Save(mdFile);
    }

    private static MdTableRow GenerateFileData(IFileLeaf fileScores)
    {
        var mutationScore = fileScores.GetMutationScore();
        var values = new List<string>
        {
            // Files
            fileScores.RelativePath ?? "All files",

            // Score
            double.IsNaN(mutationScore) ? "N/A" : $"{mutationScore * 100:N2}%"
        };

        var mutants = fileScores.Mutants.ToList();

        // Killed
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.Killed).ToString(CultureInfo.InvariantCulture));

        // Survived
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.Survived).ToString(CultureInfo.InvariantCulture));

        // Timeout
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.Timeout).ToString(CultureInfo.InvariantCulture));

        // No Coverage
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.NoCoverage).ToString(CultureInfo.InvariantCulture));

        // Ignored
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.Ignored).ToString(CultureInfo.InvariantCulture));

        // Compile Errors
        values.Add(mutants.Count(m => m.ResultStatus == MutantStatus.CompileError).ToString(CultureInfo.InvariantCulture));

        // Total Detected
        values.Add(mutants
                .Count(m => m.ResultStatus is MutantStatus.Killed or MutantStatus.Timeout)
                .ToString(CultureInfo.InvariantCulture));

        // Total Undetected
        values.Add(
            mutants
                .Count(m => m.ResultStatus is MutantStatus.Survived or MutantStatus.NoCoverage)
                .ToString(CultureInfo.InvariantCulture));

        // Total
        values.Add(mutants.Count.ToString(CultureInfo.InvariantCulture));
        return new MdTableRow(values);
    }
}
