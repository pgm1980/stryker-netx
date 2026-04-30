using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Spectre.Console;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Core.ProjectComponents;
using Stryker.Core.ProjectComponents.TestProjects;

namespace Stryker.Core.Reporters;

/// <summary>
/// The clear text tree reporter, prints a tree structure with results.
/// </summary>
public class ClearTextTreeReporter : IReporter
{
    private readonly IStrykerOptions _options;
    private readonly IAnsiConsole _console;

    public ClearTextTreeReporter(IStrykerOptions strykerOptions, IAnsiConsole? console = null)
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
        var state = new TreeRenderState();

        // setup display handlers
        reportComponent.DisplayFolder = (current) => HandleFolderDisplay(current, state);
        reportComponent.DisplayFile = (current) => HandleFileDisplay(current, state.Stack);

        // print empty line for readability
        _console.WriteLine();
        _console.WriteLine();
        _console.WriteLine("All mutants have been tested, and your mutation score has been calculated");

        // start recursive invocation of handlers
        reportComponent.Display();

        if (state.Root is not null)
        {
            _console.Write(state.Root);
        }
    }

    private sealed class TreeRenderState
    {
        public Tree? Root { get; set; }
        public Stack<IHasTreeNodes> Stack { get; } = new Stack<IHasTreeNodes>();
    }

    private void HandleFolderDisplay(IReadOnlyProjectComponent current, TreeRenderState state)
    {
        var name = Path.GetFileName(current.RelativePath);

        if (state.Root is null)
        {
            state.Root = new Tree("All files" + DisplayComponent(current));
            state.Stack.Push(state.Root);
        }
        else if (!string.IsNullOrWhiteSpace(name))
        {
            state.Stack.Push(state.Stack.Peek().AddNode(name + DisplayComponent(current)));
        }
    }

    private void HandleFileDisplay(IReadOnlyProjectComponent current, Stack<IHasTreeNodes> stack)
    {
        var name = Path.GetFileName(current.RelativePath);

        var fileNode = stack.Peek().AddNode(name + DisplayComponent(current));

        if (current.Parent is not null && string.Equals(current.FullPath, current.Parent.Children.Last().FullPath, StringComparison.Ordinal))
        {
            stack.Pop();
        }

        var totalMutants = current.TotalMutants();
        foreach (var mutant in totalMutants)
        {
            var status = mutant.ResultStatus switch
            {
                MutantStatus.Killed or MutantStatus.Timeout => $"[Green][[{mutant.ResultStatus}]][/]",
                MutantStatus.NoCoverage => $"[Yellow][[{mutant.ResultStatus}]][/]",
                _ => $"[Red][[{mutant.ResultStatus}]][/]",
            };

            var mutantLine = mutant.Mutation.OriginalNode.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
            var mutantNode = fileNode.AddNode(status + $" {mutant.Mutation.DisplayName} on line {mutantLine}");
            mutantNode.AddNode(Markup.Escape($"[-] {mutant.Mutation.OriginalNode}"));
            mutantNode.AddNode(Markup.Escape($"[+] {mutant.Mutation.ReplacementNode}"));
        }
    }

    private string DisplayComponent(IReadOnlyProjectComponent inputComponent)
    {
        var mutationScore = inputComponent.GetMutationScore();

        var stringBuilder = new StringBuilder();

        // Convert the threshold integer values to decimal values
        stringBuilder.Append(CultureInfo.InvariantCulture, $" [[{inputComponent.DetectedMutants().Count()}/{inputComponent.TotalMutants().Count()} ");

        if (inputComponent.IsComponentExcluded(_options.Mutate))
        {
            stringBuilder.Append("[Gray](Excluded)[/]");
        }
        else if (double.IsNaN(mutationScore))
        {
            stringBuilder.Append("[Gray](N/A)[/]");
        }
        else
        {
            // print the score as a percentage
            var scoreText = string.Format(CultureInfo.InvariantCulture, "({0:P2})", mutationScore);
            if (inputComponent.CheckHealth(_options.Thresholds) is Health.Good)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"[Green]{scoreText}[/]");
            }
            else if (inputComponent.CheckHealth(_options.Thresholds) is Health.Warning)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"[Yellow]{scoreText}[/]");
            }
            else if (inputComponent.CheckHealth(_options.Thresholds) is Health.Danger)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"[Red]{scoreText}[/]");
            }
        }

        stringBuilder.Append("]]");

        return stringBuilder.ToString();
    }
}
