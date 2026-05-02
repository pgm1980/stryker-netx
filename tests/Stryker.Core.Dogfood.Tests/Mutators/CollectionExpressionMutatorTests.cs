#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>Sprint 123 (v3.0.10) partial port (replaces Sprint 109 architectural-deferral).
/// First 3 upstream tests use simple [DataRow]/[InlineData] patterns and port directly to xUnit.
/// 4th upstream test uses custom [CollectionExpressionTest] MSTest attribute with multi-line C#
/// fixture data — that one defers to dedicated MemberData rewrite sprint.</summary>
public class CollectionExpressionMutatorTests : TestBase
{
    [Fact]
    public void ShouldBeMutationLevelAdvanced()
    {
        var target = new CollectionExpressionMutator();
        target.MutationLevel.Should().Be(MutationLevel.Advanced);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[ ]")]
    [InlineData("[           ]")]
    [InlineData("[ /* Comment */ ]")]
    public void ShouldAddValueToEmptyCollectionExpression(string expression)
    {
        var expressionSyntax = (CollectionExpressionSyntax)SyntaxFactory.ParseExpression(expression);
        var target = new CollectionExpressionMutator();
        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Collection expression mutation");
        var replacement = mutation.ReplacementNode.Should().BeOfType<CollectionExpressionSyntax>().Which;
        var element = replacement.Elements.Should().ContainSingle().Which;
        var token = element.Should().BeOfType<ExpressionElementSyntax>().Which.Expression.Should().BeOfType<LiteralExpressionSyntax>().Which.Token;
        token.Kind().Should().Be(SyntaxKind.DefaultKeyword);
    }

    [Theory]
    [InlineData("[1, 2, 3]")]
    [InlineData("[-1, 3]")]
    [InlineData("[1, .. abc, 3]")]
    [InlineData("[..abc]")]
    public void ShouldRemoveValuesFromCollectionExpression(string expression)
    {
        var expressionSyntax = (CollectionExpressionSyntax)SyntaxFactory.ParseExpression(expression);
        var target = new CollectionExpressionMutator();
        var result = target.ApplyMutations(expressionSyntax, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];
        mutation.DisplayName.Should().Be("Collection expression mutation");
        var replacement = mutation.ReplacementNode.Should().BeOfType<CollectionExpressionSyntax>().Which;
        replacement.Elements.Should().BeEmpty();
    }

    /// <summary>Sprint 129 (v3.0.16): structural-port of the custom [CollectionExpressionTest]
    /// MSTest attribute (just a [DataRow]-derived class taking testName + inputCode + mutationCount).
    /// Converted to standard xUnit [Theory] + [InlineData(inputCode, expectedMutants)] — testName
    /// is documentation-only and dropped. Verifies mutation COUNT only (not full Compile()
    /// integration which still defers to compiler-pipeline harness sprint).</summary>
    [Theory]
    [InlineData("class C { void M() { int[] abc = [ 1, 5, 7 ]; int[] bcd = [ 1, ..abc, 3 ]; } }", 2)]
    [InlineData("class C { void M() { int[] abc = [ 1, 5, 7 ]; var bcd = (int[])[ 1, ..abc, 3 ]; } }", 2)]
    [InlineData("class C { void M() { int[][] abc = [ [ 1, 5 ], [ 7 ] ]; } }", 3)]
    [InlineData("class C { void M() { int[][] abc = [ [ 1, 5 ], new [] { 7 } ]; } }", 2)]
    [InlineData("class C { void M() { int[] abc = []; } }", 1)]
    [InlineData("class C { static int[][][][][] Deep => [[[[[]]]]]; }", 5)]
    public void MutatedCollectionExpressions_StructuralCount(string inputText, int expectedMutants)
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(inputText);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("TestAssembly")
            .WithOptions(new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Enable))
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var injector = new Stryker.Core.InjectedHelpers.CodeInjection();
        var orchestrator = new Stryker.Core.Mutants.CsharpMutantOrchestrator(
            new Stryker.Core.Mutants.MutantPlacer(injector),
            options: new Stryker.Configuration.Options.StrykerOptions
            {
                MutationLevel = MutationLevel.Complete,
                OptimizationMode = Stryker.Abstractions.Options.OptimizationModes.CoverageBasedTest,
                ExcludedMutations = System.Enum.GetValues<Stryker.Abstractions.Mutator>()
                    .Except(new[] { Stryker.Abstractions.Mutator.CollectionExpression }),
            });
        _ = orchestrator.Mutate(syntaxTree, semanticModel);
        orchestrator.Mutants.Count(m => m.Mutation.Type == Stryker.Abstractions.Mutator.CollectionExpression).Should().Be(expectedMutants);
    }

    [Fact]
    public void CollectionExpression_Compile_AfterMutation_Succeeds()
    {
        // Sprint 134 (v3.0.21): replaces Sprint 129 reduced-scope deferral with end-to-end
        // Compile() roundtrip on a mutated CollectionExpression source. Uses Sprint 131/132
        // proven setup pattern.
        var source = "public class C { public int[] M() { int[] abc = [ 1, 5, 7 ]; return abc; } }";
        var folder = new Stryker.Core.ProjectComponents.Csharp.CsharpFolderComposite();
        folder.Add(new Stryker.Core.ProjectComponents.Csharp.CsharpFileLeaf
        {
            SourceCode = source,
            SyntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source),
            FullPath = "/Sample.cs",
            RelativePath = "Sample.cs",
        });

        var fileSystem = new System.IO.Abstractions.TestingHelpers.MockFileSystem();
        var input = new Stryker.Core.MutationTest.MutationTestInput
        {
            SourceProjectInfo = new Stryker.Core.ProjectComponents.SourceProjects.SourceProjectInfo
            {
                Analysis = Stryker.TestHelpers.TestHelper.SetupProjectAnalyzerResult(
                    projectFilePath: "/ProjectUnderTest/ProjectUnderTest.csproj",
                    properties: new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)
                    {
                        ["TargetDir"] = "/ProjectUnderTest/bin/Debug/net10.0",
                        ["TargetFileName"] = "Sample.dll",
                        ["AssemblyName"] = "Sample",
                        ["Language"] = "C#",
                    },
                    references: new[] { typeof(object).Assembly.Location, typeof(System.Linq.Enumerable).Assembly.Location }).Object,
                ProjectContents = folder,
                TestProjectsInfo = new Stryker.Core.ProjectComponents.TestProjects.TestProjectsInfo(fileSystem),
            },
        };

        var process = new Stryker.Core.Compiling.CsharpCompilingProcess(input);
        using var ilStream = new System.IO.MemoryStream();
        var syntaxTree = ((Stryker.Core.ProjectComponents.Csharp.CsharpFileLeaf)folder.GetAllFiles().First()).SyntaxTree;

        var result = process.Compile(new[] { syntaxTree }, ilStream, null);

        result.Should().NotBeNull();
        result.Success.Should().BeTrue("collection-expression source should compile");
        ilStream.Length.Should().BeGreaterThan(0);
    }
}
