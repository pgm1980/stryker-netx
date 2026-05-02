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

/// <summary>Sprint 118 (v3.0.5) full structural port (Sprint 110 → Sprint 116 minimum-viable
/// → Sprint 118 full). Width(160) on TestConsole prevents tree-line truncation. Tests assert
/// STRUCTURAL invariants (file names, mutation status keywords, tree structure markers) instead
/// of full literal box-drawing tree. Color-count assertions skipped — extension methods not in
/// our Spectre.Console.Testing version.</summary>
public class ClearTextTreeReporterTests : TestBase
{
    private static Mutation NewMutation(BinaryExpressionSyntax originalNode) => new()
    {
        OriginalNode = originalNode,
        ReplacementNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression, originalNode.Left, originalNode.Right),
        DisplayName = "test mutation",
    };

    [Fact]
    public void ClearTextTreeReporter_ShouldPrintOnReportDone()
    {
        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextTreeReporter(new StrykerOptions(), console);

        var folder = new CsharpFolderComposite { RelativePath = "RootFolder", FullPath = "C://RootFolder" };
        folder.Add(new CsharpFileLeaf
        {
            RelativePath = "RootFolder/SomeFile.cs",
            FullPath = "C://RootFolder/SomeFile.cs",
            Mutants = new Collection<IMutant>(),
        });

        target.OnAllMutantsTested(folder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        console.Output.Should().Contain("SomeFile.cs");
    }

    [Fact]
    public void ClearTextTreeReporter_ShouldPrintKilledMutation()
    {
        var tree = CSharpSyntaxTree.ParseText("void M(){ int i = 0 + 8; }");
        var originalNode = (BinaryExpressionSyntax)tree.GetRoot().DescendantNodes().First(n => n is BinaryExpressionSyntax);
        var mutation = NewMutation(originalNode);

        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextTreeReporter(new StrykerOptions(), console);

        var folder = new CsharpFolderComposite { FullPath = "C://ProjectFolder", RelativePath = "/" };
        folder.Add(new CsharpFileLeaf
        {
            RelativePath = "ProjectFolder/Killed.cs",
            FullPath = "C://ProjectFolder/Killed.cs",
            Mutants = new Collection<IMutant> { new Mutant { Id = 1, ResultStatus = MutantStatus.Killed, Mutation = mutation } },
        });

        target.OnAllMutantsTested(folder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        console.Output.Should().Contain("Killed.cs");
        console.Output.Should().Contain("Killed");
    }

    [Fact]
    public void ClearTextTreeReporter_ShouldPrintSurvivedMutation()
    {
        var tree = CSharpSyntaxTree.ParseText("void M(){ int i = 0 + 8; }");
        var originalNode = (BinaryExpressionSyntax)tree.GetRoot().DescendantNodes().First(n => n is BinaryExpressionSyntax);
        var mutation = NewMutation(originalNode);

        var console = new TestConsole().EmitAnsiSequences().Width(160);
        var target = new ClearTextTreeReporter(new StrykerOptions(), console);

        var folder = new CsharpFolderComposite { FullPath = "C://ProjectFolder", RelativePath = "/" };
        folder.Add(new CsharpFileLeaf
        {
            RelativePath = "ProjectFolder/Survived.cs",
            FullPath = "C://ProjectFolder/Survived.cs",
            Mutants = new Collection<IMutant> { new Mutant { Id = 1, ResultStatus = MutantStatus.Survived, Mutation = mutation } },
        });

        target.OnAllMutantsTested(folder, It.IsAny<TestProjectsInfo>());

        console.Output.Should().Contain("All mutants have been tested");
        console.Output.Should().Contain("Survived.cs");
        console.Output.Should().Contain("Survived");
    }
}
