using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ConstantReplacementMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<ConstantReplacementMutator>(MutationProfile.Stronger | MutationProfile.All);

    // Sprint 184 (issue #280, F-01): Pin gegen die Linq-Fehlkategorisierung — siehe
    // InlineConstantsMutatorTests fuer die volle Begruendung.
    [Fact]
    public void ApplyMutations_ReportsNumberCategory()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("42");
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().OnlyContain(m => m.Type == Mutator.Number);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("100L")]
    [InlineData("3.14")]
    [InlineData("2.5f")]
    public void ApplyMutations_OnNumericLiteral_EmitsMultipleMutations(string source)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public void ApplyMutations_OnZeroLiteral_SkipsZeroAxis()
    {
        var node = ParseExpression<LiteralExpressionSyntax>("0");
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);
        mutations.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // Sprint 169 (v3.3.1) regression: BUG_REPORT_9_FOLLOWUP_2 (filesystem-mcp-server, 2026-05-25).
    // Reporter v3.3.0 validation: 184/196 residual Safe Mode! warnings traced to
    // ConstantReplacementMutator still emitting via Convert.ToDouble + Literal(string, double) —
    // the same anti-pattern ADR-047 fixed in InlineConstantsMutator. When this mutator's
    // double-typed leaf joins typed-int leaves from InlineConstantsMutator in the
    // ConditionalInstrumentationEngine ternary wrap, the common-type inference picks
    // double → CS0029 cascade on int/long/uint/ulong/decimal slots.
    // Fix: mirror ADR-047 Branch B — switch-expression dispatch to typed Literal(T) +
    // catalogue extension {int, long, float, double} → 7 numeric types.

    [Theory]
    [InlineData("42", typeof(int))]
    [InlineData("5U", typeof(uint))]
    [InlineData("100L", typeof(long))]
    [InlineData("42UL", typeof(ulong))]
    [InlineData("2.5f", typeof(float))]
    [InlineData("3.14", typeof(double))]
    [InlineData("1.5m", typeof(decimal))]
    public void ApplyMutations_EmittedTokenValueMatchesInputType(string source, System.Type expectedClrType)
    {
        var node = ParseExpression<LiteralExpressionSyntax>(source);
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().NotBeEmpty($"CRCR must emit ≥1 mutation for any numeric literal, got 0 for '{source}'");
        foreach (var mut in mutations)
        {
            var replacement = (LiteralExpressionSyntax)mut.ReplacementNode;
            replacement.Token.Value.Should().NotBeNull();
            replacement.Token.Value!.GetType().Should().Be(expectedClrType,
                $"CRCR's emitted Token.Value for source '{source}' must be {expectedClrType.Name} to compile in the original typed slot " +
                $"(reporter's 184 A.1 warnings all root-cause here)");
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
        // This is the test that would have caught Sprint-168's overclaim: it splices each
        // emitted mutation back into a typed C# slot and verifies the surrounding compilation
        // is clean. Pre-fix the int/long/uint/ulong/decimal cases fail with CS0029/CS1503/CS0266.
        var literalNode = ParseExpression<LiteralExpressionSyntax>(literalSource);
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), literalNode);

        mutations.Should().NotBeEmpty($"CRCR must emit ≥1 mutation for {fullSource}");

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
    public void ApplyMutations_OnUnsignedLiteral_SkipsNegativeAxes()
    {
        // For unsigned types, the →-1 and →-c axes have no representation. Emit only →0 and →1
        // (and either may be skipped via the "skip-if-equal" rule). Total mutations: 0 / 1 / 2 depending on value.
        var node = ParseExpression<LiteralExpressionSyntax>("5U");
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().HaveCountLessThanOrEqualTo(2, "unsigned types only support the →0 and →1 axes; →-1 and →-c are unrepresentable");
        foreach (var mut in mutations)
        {
            mut.DisplayName.Should().NotContain("→-", "no negative-axis mutations are emitted for unsigned types");
        }
    }

    [Fact]
    public void ApplyMutations_OnDecimalMaxValue_NegateAxis_LandsOnMinValue()
    {
        // Decimal is symmetric: -decimal.MaxValue = decimal.MinValue (no overflow).
        // All four axes (→0, →1, →-1, →-c) emit cleanly; the defensive try/catch in
        // TryMakeDecimalNegate is unreachable for any legal decimal literal value
        // but kept as a safety net for any future type-extension that isn't symmetric.
        var maxValueSource = decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
        var node = ParseExpression<LiteralExpressionSyntax>(maxValueSource);
        var mutations = ApplyMutations<ConstantReplacementMutator, LiteralExpressionSyntax>(new(), node);

        mutations.Should().HaveCount(4, "all four axes (→0, →1, →-1, →-c) emit on decimal.MaxValue (decimal is symmetric — no overflow)");
        var negateMutation = mutations.Single(m => m.DisplayName.Contains("→-c"));
        var replacement = (LiteralExpressionSyntax)negateMutation.ReplacementNode;
        replacement.Token.Value.Should().Be(decimal.MinValue,
            "negating decimal.MaxValue must land on decimal.MinValue exactly");
    }
}
