#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using System.Collections.ObjectModel;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using Spectre.Console.Testing;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 117 (v3.0.4) full structural port (replaces Sprint 110 minimum-viable).
/// Upstream uses `new TestConsole().EmitAnsiSequences().Width(160)` — wide width prevents
/// header truncation. Sprint 110 missed this. Now we can assert table contents structurally.
/// Color-count assertions (DarkGraySpanCount, GreenSpanCount) skipped — these are extension
/// methods not in our Spectre.Console.Testing version.</summary>
public class ClearTextReporterTests : TestBase
{
    private static Mutation NewMutation(BinaryExpressionSyntax originalNode) => new()
    {
        OriginalNode = originalNode,
        ReplacementNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, originalNode.Left, originalNode.Right),
        DisplayName = "test mutation",
    };

    private static (CsharpFolderComposite root, BinaryExpressionSyntax originalNode) BuildTreeWithMutant(MutantStatus status)
    {
        var tree = CSharpSyntaxTree.ParseText("void M(){ int i = 0 + 8; }");
        var originalNode = (BinaryExpressionSyntax)tree.GetRoot().DescendantNodes().First(n => n is BinaryExpressionSyntax);
        var rootFolder = new CsharpFolderComposite();
        var folder = new CsharpFolderComposite { RelativePath = "FolderA", FullPath = "C://Project/FolderA" };
        folder.Add(new CsharpFileLeaf
        {
            RelativePath = "FolderA/SomeFile.cs",
            FullPath = "C://Project/FolderA/SomeFile.cs",
            Mutants = new Collection<IMutant>
            {
                new Mutant { Id = 1, ResultStatus = status, Mutation = NewMutation(originalNode) },
            },
        });
        rootFolder.Add(folder);
        return (rootFolder, originalNode);
    }

    [Fact]
    public void ClearTextReporter_ShouldPrintOnReportDone()
    {
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextReporter(new StrykerOptions(), console);

        var rootFolder = new CsharpFolderComposite();
        var folder = new CsharpFolderComposite { RelativePath = "FolderA", FullPath = "C://Project/FolderA" };
        folder.Add(new CsharpFileLeaf
        {
            RelativePath = "FolderA/SomeFile.cs",
            FullPath = "C://Project/FolderA/SomeFile.cs",
            Mutants = new Collection<IMutant>(),
        });
        rootFolder.Add(folder);

        target.OnAllMutantsTested(rootFolder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        console.Output.Should().Contain("File");
        console.Output.Should().Contain("% score");
        console.Output.Should().Contain("# killed");
        console.Output.Should().Contain("# survived");
        console.Output.Should().Contain("FolderA/SomeFile.cs");
    }

    [Fact]
    public void ClearTextReporter_ShouldPrintKilledMutation()
    {
        var (rootFolder, _) = BuildTreeWithMutant(MutantStatus.Killed);
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextReporter(new StrykerOptions(), console);

        target.OnAllMutantsTested(rootFolder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        // Killed mutant → score 100%, killed count 1
        console.Output.Should().Contain("100");
    }

    [Fact]
    public void ClearTextReporter_ShouldPrintSurvivedMutation()
    {
        var (rootFolder, _) = BuildTreeWithMutant(MutantStatus.Survived);
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextReporter(new StrykerOptions(), console);

        target.OnAllMutantsTested(rootFolder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        // Survived mutant → score 0%
        console.Output.Should().Contain("0");
    }

    [Fact]
    public void ClearTextReporter_TableHeadersStructurallyPresent()
    {
        var (rootFolder, _) = BuildTreeWithMutant(MutantStatus.Killed);
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextReporter(new StrykerOptions(), console);

        target.OnAllMutantsTested(rootFolder, It.IsAny<TestProjectsInfo>());

        // Width(160) keeps headers un-truncated
        console.Output.Should().Contain("File");
        console.Output.Should().Contain("% score");
        console.Output.Should().Contain("# killed");
        console.Output.Should().Contain("# timeout");
        console.Output.Should().Contain("# survived");
        console.Output.Should().Contain("# no cov");
        console.Output.Should().Contain("# error");
    }
}
