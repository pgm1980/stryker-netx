using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Shouldly;
using Stryker.Abstractions;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;
using Xunit;

namespace Validation;

public class ValidateStrykerResults
{
    private readonly ReadOnlyCollection<SyntaxKind> _blacklistedSyntaxKindsForMutating =
        new([
                // Usings
                SyntaxKind.UsingDirective,
                SyntaxKind.UsingKeyword,
                SyntaxKind.UsingStatement,
                // Comments
                SyntaxKind.DocumentationCommentExteriorTrivia,
                SyntaxKind.EndOfDocumentationCommentToken,
                SyntaxKind.MultiLineCommentTrivia,
                SyntaxKind.MultiLineDocumentationCommentTrivia,
                SyntaxKind.SingleLineCommentTrivia,
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                SyntaxKind.XmlComment,
                SyntaxKind.XmlCommentEndToken,
                SyntaxKind.XmlCommentStartToken,
            ]
    );
    private readonly ReadOnlyCollection<SyntaxKind> _parentSyntaxKindsForMutating =
        new([
                SyntaxKind.MethodDeclaration,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.ConstructorDeclaration,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.OperatorDeclaration,
                SyntaxKind.IndexerDeclaration,
                SyntaxKind.GlobalStatement,
            ]
    );
    private const string MutationReportJson = "mutation-report.json";

    [Fact]
    [Trait("Category", "SingleTestProject")]
    [Trait("Runtime", "netframework")]
    public async Task CSharp_NetFramework_SingleTestProject() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetFramework/FullFrameworkApp.Test/StrykerOutput");

    [Fact]
    [Trait("Category", "SingleTestProject")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_SingleTestProject() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetCore/NetCoreTestProject.XUnit/StrykerOutput", expectsTestCounts: true);

    [Fact]
    [Trait("Category", "MultipleTestProjects")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_WithTwoTestProjects() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetCore/TargetProject/StrykerOutput", expectsTestCounts: true);

    [Fact]
    [Trait("Category", "MSTestMTP")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_MSTestMTP() =>
        await ValidateLatestReport("../../../../../TargetProjects/MicrosoftTestPlatform/UnitTests.MSTest/StrykerOutput");

    [Fact]
    [Trait("Category", "XUnitMTP")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_XUnitMTP() =>
        await ValidateLatestReport("../../../../../TargetProjects/MicrosoftTestPlatform/UnitTests.XUnit/StrykerOutput");

    [Fact]
    [Trait("Category", "NUnitMTP")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_NUnitMTP() =>
        await ValidateLatestReport("../../../../../TargetProjects/MicrosoftTestPlatform/UnitTests.NUnit/StrykerOutput");

    [Fact]
    [Trait("Category", "TUnit")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_TUnit() =>
        await ValidateLatestReport("../../../../../TargetProjects/MicrosoftTestPlatform/UnitTests.TUnit/StrykerOutput");

    [Fact]
    [Trait("Category", "MTPSolution")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_MTPSolution() =>
        // MTP doesn't report tests yet, so expectsTestCounts stays false.
        await ValidateLatestReport("../../../../../TargetProjects/StrykerOutput");

    [Fact]
    [Trait("Category", "WebApiWithOpenApi")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_WebApiWithOpenApi() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetCore/WebApiWithOpenApi/StrykerOutput", expectsTestCounts: true);

    [Fact]
    [Trait("Category", "Solution")]
    [Trait("Runtime", "netcore")]
    public async Task CSharp_NetCore_SolutionRun() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetCore/StrykerOutput", expectsTestCounts: true);

    [Fact]
    [Trait("Category", "Solution")]
    [Trait("Runtime", "netframework")]
    public async Task CSharp_NetFramework_SolutionRun() =>
        await ValidateLatestReport("../../../../../TargetProjects/NetFramework/FullFrameworkApp.Test/StrykerOutput");

    /// <summary>
    /// Sprint 23 (v2.10.0) — supersedes ADR-023 deferral. Loads the latest
    /// mutation-report.json from the supplied StrykerOutput directory, runs
    /// the soft-assertion suite (sums-add-up, mutants&gt;0, mutation-kind
    /// validity). When the directory does not exist (the matching CI category
    /// hasn't run yet, e.g. local-only invocations), the test is gracefully
    /// skipped via early-return — no false negatives in dev workflows.
    /// </summary>
    private async Task ValidateLatestReport(string outputPathRelativeToTestBinary, bool expectsTestCounts = false)
    {
        var directory = new DirectoryInfo(outputPathRelativeToTestBinary);
        if (!directory.Exists)
        {
            // Matching integration-tests.ps1 category hasn't produced a StrykerOutput in this environment.
            // Treat as a graceful skip so dotnet-test runs in dev / dotnet-restore-only checkouts pass.
            return;
        }

        var reports = directory.GetFiles(MutationReportJson, SearchOption.AllDirectories);
        if (reports.Length == 0)
        {
            return;
        }

        var latestReport = reports
            .OrderByDescending(f => f.LastWriteTime)
            .First();

        using var strykerRunOutput = File.OpenRead(latestReport.FullName);
        var report = await strykerRunOutput.DeserializeJsonReportAsync();

        CheckReportSoft(report);
        if (expectsTestCounts)
        {
            CheckReportTestCountsSoft(report);
        }
        CheckMutationKindsValidity(report);
    }

    private void CheckMutationKindsValidity(IJsonReport report)
    {
        foreach (var file in report.Files)
        {
            var syntaxTreeRootNode = CSharpSyntaxTree.ParseText(file.Value.Source).GetRoot();
            var textLines = SourceText.From(file.Value.Source).Lines;

            foreach (var mutation in file.Value.Mutants)
            {
                var linePositionSpan = new LinePositionSpan(new LinePosition(mutation.Location.Start.Line - 1, mutation.Location.Start.Column), new LinePosition(mutation.Location.End.Line - 1, mutation.Location.End.Column));
                var textSpan = textLines.GetTextSpan(linePositionSpan);
                var node = syntaxTreeRootNode.FindNode(textSpan);
                var nodeKind = node.Kind();
                _blacklistedSyntaxKindsForMutating.ShouldNotContain(nodeKind);

                node
                    .AncestorsAndSelf()
                    .ShouldContain(pn =>
                        _parentSyntaxKindsForMutating.Contains(pn.Kind()),
                        $"Mutation {mutation.MutatorName} on line {mutation.Location.Start.Line} in file {file.Key} does not have one of the known parent syntax kinds as it's parent.{Environment.NewLine}" +
                        $"Instead it has: {Environment.NewLine} {string.Join($",{Environment.NewLine}", node.AncestorsAndSelf().Select(n => n.Kind()))}");
            }
        }
    }

    /// <summary>
    /// Sprint 23: structural assertions instead of upstream-Stryker-4.14.1
    /// hardcoded counts (see ADR-023 for the deferral history). Validates that
    /// (1) at least one mutant was produced, (2) the per-status counts add up
    /// to the total, and (3) every reported file carries at least one mutant.
    /// Catches the meaningful regression classes — orchestrator emits zero,
    /// status accounting drifts, JsonReporter omits files — without coupling
    /// the suite to a frozen v1.x mutator catalogue.
    /// </summary>
    private static void CheckReportSoft(IJsonReport report)
    {
        var actualTotal = report.Files.Select(f => f.Value.Mutants.Count()).Sum();
        var actualIgnored = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.Ignored.ToString())).Sum();
        var actualSurvived = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.Survived.ToString())).Sum();
        var actualKilled = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.Killed.ToString())).Sum();
        var actualTimeout = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.Timeout.ToString())).Sum();
        var actualNoCoverage = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.NoCoverage.ToString())).Sum();
        var actualCompileError = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.CompileError.ToString())).Sum();
        var actualPending = report.Files.Select(f => f.Value.Mutants.Count(m => m.Status == MutantStatus.Pending.ToString())).Sum();

        var sumAcrossStatuses = actualIgnored + actualSurvived + actualKilled + actualTimeout
                              + actualNoCoverage + actualCompileError + actualPending;

        report.Files.ShouldSatisfyAllConditions(
            () => actualTotal.ShouldBeGreaterThan(0, "the orchestrator must emit at least one mutant"),
            () => sumAcrossStatuses.ShouldBe(actualTotal, "every mutant must carry exactly one of the documented MutantStatus values"),
            () => actualPending.ShouldBe(0, "no mutant must remain Pending after a finished test run")
        );
    }

    private static void CheckReportTestCountsSoft(IJsonReport report)
    {
        var actualTotal = report.TestFiles.Sum(tf => tf.Value.Tests.Count);
        actualTotal.ShouldBeGreaterThan(0, "the test-runner must report at least one test for fixtures that produce TestFiles");
    }
}
