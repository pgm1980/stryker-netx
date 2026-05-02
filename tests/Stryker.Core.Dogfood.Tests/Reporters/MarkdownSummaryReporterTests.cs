using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Spectre.Console.Testing;
using Stryker.Configuration.Options;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.Reporters;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 64 (v2.50.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class MarkdownSummaryReporterTests
{
    private static StrykerOptions CreateOptions() => new()
    {
        Thresholds = new Thresholds { High = 75, Low = 50, Break = 10 },
        OutputPath = Directory.GetCurrentDirectory(),
        ReportFileName = "mutation-summary",
    };

    [Fact]
    public void MarkdownSummaryReporter_ShouldGenerateReportOnReportDone()
    {
        var options = CreateOptions();
        var mockFileSystem = new MockFileSystem();
        var console = new TestConsole().EmitAnsiSequences().Width(160);

        var reportGenerator = new MarkdownSummaryReporter(options, mockFileSystem, console);

        reportGenerator.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), null!);

        var reportPath = Path.Combine(options.ReportPath, "mutation-summary.md");
        mockFileSystem.FileExists(reportPath).Should().BeTrue($"Path {reportPath} should exist but it does not.");
    }

    [Fact]
    public void MarkdownSummaryReporter_ShouldReportCorrectThresholds()
    {
        var options = CreateOptions();
        var mockFileSystem = new MockFileSystem();
        var console = new TestConsole().EmitAnsiSequences().Width(160);

        var reportGenerator = new MarkdownSummaryReporter(options, mockFileSystem, console);

        reportGenerator.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), null!);

        var reportPath = Path.Combine(options.ReportPath, "mutation-summary.md");
        var fileContents = mockFileSystem.File.ReadAllText(reportPath);

        fileContents.Should().Contain("high:75");
        fileContents.Should().Contain("low:50");
        fileContents.Should().Contain("break:10");
    }

    [Fact]
    public void MarkdownSummaryReporter_ShouldReportCorrectMutationCoverageValues()
    {
        var options = CreateOptions();
        var mockFileSystem = new MockFileSystem();
        var console = new TestConsole().EmitAnsiSequences().Width(160);

        var reportGenerator = new MarkdownSummaryReporter(options, mockFileSystem, console);
        var mockReport = ReportTestHelper.CreateProjectWith();

        reportGenerator.OnAllMutantsTested(mockReport, null!);

        var files = mockReport.GetAllFiles();
        var reportPath = Path.Combine(options.ReportPath, "mutation-summary.md");
        var fileContents = mockFileSystem.File.ReadAllText(reportPath);
        var stippedFileContents = fileContents.Replace(" ", string.Empty, StringComparison.Ordinal);

        foreach (var file in files)
        {
            var escapedFilename = file.RelativePath.Replace("/", "\\/", StringComparison.Ordinal);
            stippedFileContents.Should().Contain(
                System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(string.Empty) +
                $"|{escapedFilename}|{file.GetMutationScore() * 100:N2}%|");
        }
    }

    [Fact]
    public void MarkdownSummaryReporter_ShouldOutputSummaryLocationToTheConsole()
    {
        var options = CreateOptions();
        var mockFileSystem = new MockFileSystem();
        var console = new TestConsole().EmitAnsiSequences().Width(160);

        var reportGenerator = new MarkdownSummaryReporter(options, mockFileSystem, console);
        var mockReport = ReportTestHelper.CreateProjectWith();

        reportGenerator.OnAllMutantsTested(mockReport, null!);

        var expectedSummaryReportPath = $"{Path.Join(options.ReportPath, options.ReportFileName)}.md".Replace("\\", "/", StringComparison.Ordinal);
        console.Output.Should().Contain(expectedSummaryReportPath);
    }

    [Fact]
    public void MarkdownSummaryReporter_ShouldNotOutputForEmptyProject()
    {
        var options = CreateOptions();
        var mockFileSystem = new MockFileSystem();
        var console = new TestConsole().EmitAnsiSequences().Width(160);

        var reportGenerator = new MarkdownSummaryReporter(options, mockFileSystem, console);
        var emptyReport = new CsharpFolderComposite { FullPath = "/home/user/src/project/", RelativePath = "" };

        reportGenerator.OnAllMutantsTested(emptyReport, null!);

        mockFileSystem.AllFiles.Should().BeEmpty();
    }
}
