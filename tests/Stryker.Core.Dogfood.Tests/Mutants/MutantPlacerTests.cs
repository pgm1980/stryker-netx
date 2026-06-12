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

    // Sprint 179 (issue #282, 360°-Analyse G-25): C# 14 simple lambda parameters with
    // modifiers may omit the parameter type, so the Type property of such a parameter is
    // null. The engine used to dereference that null and crash the whole run with an NRE.
    // A typeless default literal assigns correctly even without a type syntax.
    [Fact]
    public void ShouldInjectInitializersForTypelessOutParameter()
    {
        var source = "class Test {bool Method(out int x) {x=0;}}";
        var expected = "class Test {bool Method(out int x) {{x = default;}x=0;}}";
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<BlockSyntax>(source, expected,
            n => MutantPlacer.InjectOutParametersInitialization(n,
                [SyntaxFactory.Parameter(SyntaxFactory.Identifier("x")).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.OutKeyword)))]));
    }

    // Sprint 180 (issue #285a): the initializer carries a mutation, so the tracking
    // marker is required and must keep its place-and-revert roundtrip.
    [Fact]
    public void ShouldStaticMarkerInStaticFieldInitializers()
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var source = "class Test {static int x = 2;}";
        var expected = $"class Test {{static int x = {codeInjection.HelperNamespace}.MutantContext.TrackValue(()=>2);}}";
        CheckMutantPlacerProperlyPlaceAndRemoveHelpers<ExpressionSyntax>(source, expected,
            syntax => placer.PlaceStaticContextMarker(
                syntax.WithAdditionalAnnotations(new SyntaxAnnotation("MutationId", "42"))),
            syntax => syntax.Kind() == SyntaxKind.NumericLiteralExpression);
    }

    // Sprint 180 (issue #285a, 360-Grad-Analyse G-32): ein TrackValue-Wrap um einen
    // mutationsfreien konstanten Initializer trackt nichts, loescht aber Konstanten-
    // Konvertierungen (byte x = 5 -> CS0266) — der Marker entfaellt dort ersatzlos.
    [Theory]
    [InlineData("static byte x = 5;")]
    [InlineData("static int x = -(2 + 3);")]
    [InlineData("static string x = null!;")]
    public void ShouldNotPlaceStaticMarkerOnUnmutatedConstantLikeInitializer(string declaration)
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var initializer = ParseInitializerValue($"class Test {{{declaration}}}");

        var marked = placer.PlaceStaticContextMarker(initializer);

        marked.Should().BeSameAs(initializer);
    }

    // Sprint 180 (issue #285a): target-typed Ausdruecke verlieren im Tracking-Lambda
    // ihren Zieltyp (new() -> CS8754, Collection-Expression -> CS9176), der Marker wurde
    // bislang erst per Rollback-Heilrunde (mutant -1) wieder entfernt. Er darf gar nicht
    // erst gesetzt werden — auch wenn der Ausdruck Mutationen enthaelt.
    [Theory]
    [InlineData("static object x = new();")]
    [InlineData("static int[] x = [1, 2];")]
    [InlineData("static int x = default;")]
    public void ShouldNotPlaceStaticMarkerOnTargetTypedInitializer(string declaration)
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var initializer = ParseInitializerValue($"class Test {{{declaration}}}")
            .WithAdditionalAnnotations(new SyntaxAnnotation("MutationId", "42"));

        var marked = placer.PlaceStaticContextMarker(initializer);

        marked.Should().BeSameAs(initializer);
    }

    // Sprint 180 (issue #285a): ein Initializer, der Benutzer-Code ausfuehrt, braucht den
    // Marker auch OHNE lokale Mutationen — TrackValue setzt waehrend der Auswertung den
    // Static-Kontext fuer alle transitiv aufgerufenen Mutanten (MutantContext.InStatic).
    [Theory]
    [InlineData("static int x = Compute();")]
    [InlineData("static object x = new object();")]
    [InlineData("static int x = Other.Value;")]
    public void ShouldPlaceStaticMarkerOnInitializerExecutingUserCode(string declaration)
    {
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var initializer = ParseInitializerValue($"class Test {{{declaration}}}");

        var marked = placer.PlaceStaticContextMarker(initializer);

        marked.ToString().Should().Contain("TrackValue");
    }

    private static ExpressionSyntax ParseInitializerValue(string source) =>
        CSharpSyntaxTree.ParseText(source).GetRoot()
            .DescendantNodes().OfType<EqualsValueClauseSyntax>().Single().Value;

    [Fact]
    public void ShouldRollBackFailedConstructor()
    {
        // Sprint 113 (v2.99.0): un-skipped. Original Sprint 62 bucket-3 concern was ID-drift, but
        // this test removes mutants by node-type (BlockSyntax/IfStatement/ConstructorDeclaration),
        // not by IsActive(N) ID — so it's bucket-2 (single-mutation, type-driven removal) and works
        // identically on the v2.x 52-mutator pipeline.
        var codeInjection = new CodeInjection();
        var placer = new MutantPlacer(codeInjection);
        var source = "class Test {\nstatic TestClass()=> Value-='a';}";

        var orchestrator = new CsharpMutantOrchestrator(placer, options: new StrykerOptions
        {
            OptimizationMode = OptimizationModes.CoverageBasedTest,
            MutationLevel = MutationLevel.Complete,
        });
        var actualNode = orchestrator.Mutate(CSharpSyntaxTree.ParseText(source), null!).GetRoot();

        // Remove marker
        var node = actualNode.DescendantNodes().First(t => t is BlockSyntax);
        var restored = MutantPlacer.RemoveMutant(node);
        actualNode = actualNode.ReplaceNode(node, restored);

        // remove mutation
        node = actualNode.DescendantNodes().First(t => t.IsKind(SyntaxKind.IfStatement));
        restored = MutantPlacer.RemoveMutant(node);
        actualNode = actualNode.ReplaceNode(node, restored);

        // remove expression to body conversion
        node = actualNode.DescendantNodes().First(t => t is ConstructorDeclarationSyntax);
        restored = MutantPlacer.RemoveMutant(node);
        actualNode = actualNode.ReplaceNode(node, restored);

        var expectedNode = CSharpSyntaxTree.ParseText(source.Replace("StrykerNamespace", codeInjection.HelperNamespace, StringComparison.Ordinal));

        var actualNormalized = CSharpSyntaxTree.ParseText(actualNode.ToFullString()).GetRoot().NormalizeWhitespace().ToFullString();
        var expectedNormalized = expectedNode.GetRoot().NormalizeWhitespace().ToFullString();
        actualNormalized.Should().Be(expectedNormalized);

        // No syntax errors after rollback
        actualNode.SyntaxTree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
    }
}
