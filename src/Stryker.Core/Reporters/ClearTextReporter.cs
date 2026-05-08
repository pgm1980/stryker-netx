using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Spectre.Console;
using Spectre.Console.Rendering;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Core.ProjectComponents;

namespace Stryker.Core.Reporters;

/// <summary>
/// The clear text reporter, prints a table with results.
/// </summary>
public class ClearTextReporter : IReporter
{
    private readonly IStrykerOptions _options;
    private readonly IAnsiConsole _console;

    public ClearTextReporter(IStrykerOptions strykerOptions, IAnsiConsole? console = null)
    {
        _options = strykerOptions;
        _console = console ?? AnsiConsole.Console;
    }

    public void OnMutantsCreated(IReadOnlyProjectComponent reportComponent, ITestProjectsInfo testProjectsInfo)
    {
        // This reporter does not report during the testrun
    }

    public void OnStartMutantTestRun(IEnumerable<IReadOnlyMutant> mutantsToBeTested)
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
            // print empty line for readability
            _console.WriteLine();
            _console.WriteLine();
            _console.WriteLine("All mutants have been tested, and your mutation score has been calculated");

            // Sprint 161 (ADR-041 Issue 1, Aisess Anomaly C): compact one-letter
            // column labels avoid the vertical-wrap on narrow terminals that the
            // Aisess team reported on v3.2.11/v3.2.12. The 1-line legend is printed
            // once below the table for first-time readers.
            var table = new Table()
                .RoundedBorder()
                .AddColumn("File", c => c.NoWrap())
                .AddColumn("%", c => c.Alignment(Justify.Right).NoWrap())
                .AddColumn("K", c => c.Alignment(Justify.Right).NoWrap())
                .AddColumn("T", c => c.Alignment(Justify.Right).NoWrap())
                .AddColumn("S", c => c.Alignment(Justify.Right).NoWrap())
                .AddColumn("NoCov", c => c.Alignment(Justify.Right).NoWrap())
                .AddColumn("Err", c => c.Alignment(Justify.Right).NoWrap());

            DisplayComponent(reportComponent, table);

            foreach (var file in files)
            {
                DisplayComponent(file, table);
            }

            _console.Write(table);
            _console.WriteLine("Legend: % = mutation score | K = Killed | T = Timeout | S = Survived | NoCov = NoCoverage | Err = Compile/Runtime Error");
        }
    }

    private void DisplayComponent(IReadOnlyProjectComponent inputComponent, Table table)
    {
        var columns = new List<IRenderable>
        {
            new Text(inputComponent.RelativePath ?? "All files")
        };

        var mutationScore = inputComponent.GetMutationScore();

        if (inputComponent.IsComponentExcluded(_options.Mutate))
        {
            columns.Add(new Markup("[Gray]Excluded[/]"));
        }
        else if (double.IsNaN(mutationScore))
        {
            columns.Add(new Markup("[Gray]N/A[/]"));
        }
        else
        {
            var scoreText = $"{mutationScore * 100:N2}";

            var checkHealth = inputComponent.CheckHealth(_options.Thresholds);
            if (checkHealth is Health.Good)
            {
                columns.Add(new Markup($"[Green]{scoreText}[/]"));
            }
            else if (checkHealth is Health.Warning)
            {
                columns.Add(new Markup($"[Yellow]{scoreText}[/]"));
            }
            else if (checkHealth is Health.Danger)
            {
                columns.Add(new Markup($"[Red]{scoreText}[/]"));
            }
        }

        var mutants = inputComponent.Mutants.ToList();

        columns.Add(new Text(mutants.Count(m => m.ResultStatus == MutantStatus.Killed).ToString(CultureInfo.InvariantCulture)));
        columns.Add(new Text(mutants.Count(m => m.ResultStatus == MutantStatus.Timeout).ToString(CultureInfo.InvariantCulture)));
        columns.Add(new Text((inputComponent.TotalMutants().Count() - inputComponent.DetectedMutants().Count()).ToString(CultureInfo.InvariantCulture)));
        columns.Add(new Text(mutants.Count(m => m.ResultStatus == MutantStatus.NoCoverage).ToString(CultureInfo.InvariantCulture)));
        columns.Add(new Text(mutants.Count(m => m.ResultStatus == MutantStatus.CompileError).ToString(CultureInfo.InvariantCulture)));

        table.AddRow(columns);
    }
}
