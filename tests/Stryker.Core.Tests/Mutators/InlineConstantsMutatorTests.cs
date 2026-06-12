using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class InlineConstantsMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<InlineConstantsMutator>(MutationProfile.Stronger | MutationProfile.All);

    // Sprint 184 (issue #280, F-01): die Konstanten-Mutatoren meldeten Mutator.Linq —
    // ignore-mutations ['linq'] deaktivierte sie still und Reports kategorisierten sie
    // als Linq-Methoden. Kein Test prüfte das Type-Feld; dieser pinnt es.
    [Fact]
    public void ApplyMutations_ReportsNumberCategory()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("42");
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().OnlyContain(m => m.Type == Mutator.Number);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("100L")]
    [InlineData("3.14")]
    [InlineData("2.5f")]
    public void ApplyMutations_OnNumericLiteral_EmitsTwoMutations(string source)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 2);
    }

    [Fact]
    public void ApplyMutations_OnNonNumericLiteral_ReturnsNoMutation()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("\"hello\"");
        AssertNoMutations(ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node));
    }

    // Sprint 168 (v3.3.0) regression: Bug Report #9 Anomaly #4 — InlineConstantsMutator
    // emitted typed-as-double literals for all numeric inputs (Convert.ToDouble in MakeNumericMutation),
    // causing CS0266 / CS1503 / CS0029 cascades when the receiver slot was int/long/uint/ulong/decimal.
    // Real-world impact: 70+ methods × ~10 mutants each = ~700 false-CompileError mutants on the
    // reporter's codebase (~39% CompileError rate). The new tests assert Token.Value type matches
    // the input type AND that the emitted source compiles in a typed slot.

    [Theory]
    [InlineData("42", typeof(int))]
    [InlineData("5U", typeof(uint))]
    [InlineData("100L", typeof(long))]
    [InlineData("42UL", typeof(ulong))]
    [InlineData("2.5f", typeof(float))]
    [InlineData("3.14", typeof(double))]
    [InlineData("1.5m", typeof(decimal))]
    public void ApplyMutations_OnNumericLiteral_EmittedTokenValueMatchesInputType(string source, System.Type expectedClrType)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().HaveCount(2, $"two mutations (+1/-1) are expected for any numeric literal");
        foreach (var mut in mutations)
        {
            var replacement = (LiteralExpressionSyntax)mut.ReplacementNode;
            replacement.Token.Value.Should().NotBeNull();
            replacement.Token.Value!.GetType().Should().Be(expectedClrType,
                $"the emitted Token.Value for source '{source}' must be {expectedClrType.Name} to compile in the original typed slot");
        }
    }

    [Theory]
    [InlineData("int x = 42;", "int x = ", ";", "42", typeof(int))]
    [InlineData("uint x = 5U;", "uint x = ", ";", "5U", typeof(uint))]
    [InlineData("long x = 100L;", "long x = ", ";", "100L", typeof(long))]
    [InlineData("ulong x = 42UL;", "ulong x = ", ";", "42UL", typeof(ulong))]
    [InlineData("float x = 2.5f;", "float x = ", ";", "2.5f", typeof(float))]
    [InlineData("double x = 3.14;", "double x = ", ";", "3.14", typeof(double))]
    [InlineData("decimal x = 1.5m;", "decimal x = ", ";", "1.5m", typeof(decimal))]
    public void ApplyMutations_EmittedReplacement_CompilesInTypedSlot(
        string fullSource, string prefix, string suffix, string literalSource, System.Type expectedClrType)
    {
        // Parse the literal in isolation, mutate it, then splice the mutated literal-text back
        // into the typed slot context and verify the surrounding compilation has no errors.
        var literalNode = ParseExpression<LiteralExpressionSyntax>(literalSource);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), literalNode);

        mutations.Should().HaveCount(2, $"two mutations are expected for {fullSource}");

        foreach (var mut in mutations)
        {
            var mutatedLiteralText = mut.ReplacementNode.ToFullString();
            var mutatedSource = prefix + mutatedLiteralText + suffix;
            var tree = CSharpSyntaxTree.ParseText("class C { void M() { " + mutatedSource + " } }");
            var compilation = CSharpCompilation.Create(
                "CompileRoundtripAssembly",
                [tree],
                [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            errors.Should().BeEmpty(
                $"the mutated source '{mutatedSource}' (expected {expectedClrType.Name}) must compile cleanly. " +
                $"Found errors: {string.Join("; ", errors.Select(e => e.Id + " " + e.GetMessage(System.Globalization.CultureInfo.InvariantCulture)))}");
        }
    }

    [Fact]
    public void ApplyMutations_OnDecimalMaxValue_PlusOneOverflow_EmitsOnlyMinusOneMutation()
    {
        // decimal.MaxValue + 1m throws OverflowException at AST-walk time; the +1 mutation
        // must be silently skipped while the -1 mutation still ships.
        var maxValueSource = decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        var node = ParseExpression<LiteralExpressionSyntax>(maxValueSource);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().HaveCount(1, "the +1 mutation overflows decimal.MaxValue and must be skipped; only -1 remains");
        var mut = mutations[0];
        mut.DisplayName.Should().Contain("-1", "the surviving mutation must be the -1 variant");
        var replacement = (LiteralExpressionSyntax)mut.ReplacementNode;
        replacement.Token.Value.Should().BeOfType<decimal>();
    }

    [Fact]
    public void ApplyMutations_OnDecimalMaxMinusOne_BothMutationsEmitted_NoOverflow()
    {
        // Boundary just below MaxValue: both +1 (lands on MaxValue) and -1 (lands on MaxValue-2)
        // must succeed without overflow. Guards against an overly-aggressive try-catch.
        var nearMaxSource = (decimal.MaxValue - 1m).ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        var node = ParseExpression<LiteralExpressionSyntax>(nearMaxSource);
        var mutations = ApplyMutations<InlineConstantsMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().HaveCount(2, "decimal.MaxValue - 1m is one step below overflow on both sides");
    }
}
