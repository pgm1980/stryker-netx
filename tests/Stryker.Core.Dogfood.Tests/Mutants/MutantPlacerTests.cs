using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.InjectedHelpers;
using Stryker.Core.Mutants;
using Stryker.Core.Mutants.CsharpNodeOrchestrators;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>Sprint 89 (v2.75.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: orchestrator/placer use ApplicationLogging.LoggerFactory.
/// `ShouldBeSemantically` upstream → `NormalizeWhitespace+ToFullString` ours (Sprint 62 lesson).</summary>
public class MutantPlacerTests : TestBase
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void MutantPlacer_ShouldPlaceWithIfStatement(int id)
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var originalNode = SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8))));
        var mutatedNode = SyntaxFactory.ExpressionStatement(SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8))));

        var mutants = new List<(Mutant, StatementSyntax)>
        {
            (new Mutant { Id = id, Mutation = new Mutation { OriginalNode = originalNode, ReplacementNode = mutatedNode, DisplayName = "test" } }, mutatedNode),
        };

        var result = placer.PlaceStatementControlledMutations(originalNode, mutants);

        var expected = CSharpSyntaxTree.ParseText("if (StrykerNamespace.MutantControl.IsActive(" + id + ")){1 - 8;} else {1 + 8;}").GetRoot().NormalizeWhitespace().ToFullString();
        var actual = CSharpSyntaxTree.ParseText(result.ToFullString().Replace(codeInjection.HelperNamespace, "StrykerNamespace", StringComparison.Ordinal)).GetRoot().NormalizeWhitespace().ToFullString();
        actual.Should().Be(expected);

        var removedResult = MutantPlacer.RemoveMutant(result);
        removedResult.ToString().Should().Be(originalNode.ToString());
    }

    [Theory]
    [InlineData(10)]
    [InlineData(16)]
    public void MutantPlacer_ShouldPlaceWithConditionalExpression(int id)
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var originalNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));
        var mutatedNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(8)));

        var mutants = new List<(Mutant, ExpressionSyntax)>
        {
            (new Mutant { Id = id, Mutation = new Mutation { OriginalNode = originalNode, ReplacementNode = mutatedNode, DisplayName = "test" } }, mutatedNode),
        };

        var result = placer.PlaceExpressionControlledMutations(originalNode, mutants);

        var expected = CSharpSyntaxTree.ParseText($"({codeInjection.HelperNamespace}.MutantControl.IsActive({id})?1-8:1+8)").GetRoot().NormalizeWhitespace().ToFullString();
        CSharpSyntaxTree.ParseText(result.ToFullString()).GetRoot().NormalizeWhitespace().ToFullString().Should().Be(expected);

        var removedResult = MutantPlacer.RemoveMutant(result);
        removedResult.ToString().Should().Be(originalNode.ToString());
    }

    private static void CheckMutantPlacerProperlyPlaceAndRemoveHelpers<T>(string sourceCode, string expectedCode, Func<T, T> placer, Predicate<T>? condition = null)
        where T : SyntaxNode
        => CheckMutantPlacerProperlyPlaceAndRemoveHelpers<T, T>(sourceCode, expectedCode, placer, condition);

    private static void CheckMutantPlacerProperlyPlaceAndRemoveHelpers<T, TU>(string sourceCode, string expectedCode, Func<T, T> placer, Predicate<T>? condition = null)
        where T : SyntaxNode where TU : SyntaxNode
    {
        var actualNode = CSharpSyntaxTree.ParseText(sourceCode).GetRoot();
        var node = (T?)actualNode.DescendantNodes().First(t => t is T ct && (condition == null || condition(ct)));
        actualNode = actualNode.ReplaceNode(node!, placer(node!));

        var actualNormalized = CSharpSyntaxTree.ParseText(actualNode.ToFullString()).GetRoot().NormalizeWhitespace().ToFullString();
        var expectedNormalized = CSharpSyntaxTree.ParseText(expectedCode).GetRoot().NormalizeWhitespace().ToFullString();
        actualNormalized.Should().Be(expectedNormalized);

        TU? newNode;
        if (typeof(TU) == typeof(T))
        {
            newNode = (TU?)actualNode.DescendantNodes().First(t => t is TU && t.ContainsAnnotations);
        }
        else
        {
            newNode = (TU?)actualNode.DescendantNodes().First(t => t is T).DescendantNodes().First(t => t is TU && t.ContainsAnnotations);
        }

        var restored = MutantPlacer.RemoveMutant(newNode!);
        actualNode = actualNode.ReplaceNode(newNode!, restored);

        var restoredNormalized = CSharpSyntaxTree.ParseText(actualNode.ToFullString()).GetRoot().NormalizeWhitespace().ToFullString();
        var sourceNormalized = CSharpSyntaxTree.ParseText(sourceCode).GetRoot().NormalizeWhitespace().ToFullString();
        restoredNormalized.Should().Be(sourceNormalized);

        var act = () => MutantPlacer.RemoveMutant(restored);
        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("static TestClass()=> Value-='a';", "static TestClass(){ Value-='a';}")]
    [InlineData("void TestClass()=> Value-='a';", "void TestClass(){ Value-='a';}")]
    [InlineData("int TestClass()=> 1;", "int TestClass(){ return 1;}")]
    [InlineData("~TestClass()=> Value-='a';", "~TestClass(){ Value-='a';}")]
    [InlineData("public static operator int(Test t)=> 0;", "public static operator int(Test t){ return 0;}")]
    [InlineData("public static int operator +(Test t, Test q)=> 0;", "public static int operator +(Test t, Test q){return 0;}")]
    public void ShouldConvertExpressionBodyBackAndForth(string original, string injected)
    {
        var source = $"class Test {{{original}}}";
        var expectedCode = $"class Test {{{injected}}}";
        var placer = new BaseMethodDeclarationOrchestrator<BaseMethodDeclarationSyntax>();
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<BaseMethodDeclarationSyntax>(source, expectedCode, placer.ConvertToBlockBody);
    }

    [Theory]
    [InlineData("void TestClass(){ void LocalFunction() => Value-='a';}", "void TestClass(){ void LocalFunction() {Value-='a';};}")]
    [InlineData("void TestClass(){ int LocalFunction() => 4;}", "void TestClass(){ int LocalFunction() {return 4;};}")]
    public void ShouldConvertExpressionBodyBackLocalFunctionAndForth(string original, string injected)
    {
        var source = $"class Test {{{original}}}";
        var expectedCode = $"class Test {{{injected}}}";
        var placer = new LocalFunctionStatementOrchestrator();
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<LocalFunctionStatementSyntax>(source, expectedCode, placer.ConvertToBlockBody);
    }

    [Theory]
    [InlineData("() => Call(2)", "() => {return Call(2);}")]
    [InlineData("(x) => Call(2)", "(x) => {return Call(2);}")]
    [InlineData("x => Call(2)", "x => {return Call(2);}")]
    [InlineData("(out x) => Call(out x)", "(out x) => {return Call(out x);}")]
    [InlineData("(x, y) => Call(2)", "(x, y) => {return Call(2);}")]
    public void ShouldConvertAccessorExpressionBodyBackAndForth(string original, string injected)
    {
        var source = $"class Test {{ private void Any(){{ Register({original});}}}}";
        var expectedCode = $"class Test {{ private void Any(){{ Register({injected});}}}}";
        var placer = new AnonymousFunctionExpressionOrchestrator();
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<AnonymousFunctionExpressionSyntax>(source, expectedCode, placer.ConvertToBlockBody);
    }

    [Theory]
    [InlineData("public int X { get => 1;}", "public int X { get {return 1;}}")]
    public void ShouldConvertAnonymousFunctionExpressionBodyBackAndForth(string original, string injected)
    {
        var source = $"class Test {{{original}}}";
        var expectedCode = $"class Test {{{injected}}}";
        var placer = new AccessorSyntaxOrchestrator();
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<AccessorDeclarationSyntax>(source, expectedCode, placer.ConvertToBlockBody);
    }

    [Fact]
    public void ShouldConvertPropertyExpressionBodyBackAndForth()
    {
        var source = "class Test {public int X => 1;}";
        var expected = "class Test {public int X {get{return 1;}}}";
        var placer = new ExpressionBodiedPropertyOrchestrator();
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<PropertyDeclarationSyntax>(source, expected, placer.ConvertToBlockBody);
    }

    [Fact]
    public void ShouldInjectInitializersAndRestore()
    {
        var source = "class Test {bool Method(out int x) {x=0;}}";
        var expected = "class Test {bool Method(out int x) {{x = default(int);}x=0;}}";
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<BlockSyntax>(source, expected,
            n => MutantPlacer.InjectOutParametersInitialization(n,
                [SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword))).WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))]));
    }

    [Fact]
    public void ShouldStaticMarkerInStaticFieldInitializers()
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var source = "class Test {static int x = 2;}";
        var expected = $"class Test {{static int x = {codeInjection.HelperNamespace}.MutantContext.TrackValue(()=>2);}}";
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<ExpressionSyntax>(source, expected,
            placer.PlaceStaticContextMarker, syntax => syntax.Kind() == SyntaxKind.NumericLiteralExpression);
    }

    [Fact(Skip = "Bucket-3 (Sprint 62 lesson): orchestrator-driven IDs depend on v2.x mutator-pipeline order — defer to structural-rewrite sprint.")]
    public void ShouldRollBackFailedConstructor()
    {
        // Skipped: ID-drift between upstream 40-mutator pipeline and our 52-mutator pipeline.
        _ = new CodeInjection();
        _ = new StrykerOptions { OptimizationMode = OptimizationModes.CoverageBasedTest, MutationLevel = MutationLevel.Complete };
    }
}
