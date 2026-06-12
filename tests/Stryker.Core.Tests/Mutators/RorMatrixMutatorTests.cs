using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class RorMatrixMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<RorMatrixMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void MutationLevel_IsComplete()
        => AssertMutationLevel<RorMatrixMutator>(MutationLevel.Complete);

    [Theory]
    [InlineData("a < b")]
    [InlineData("a <= b")]
    [InlineData("a > b")]
    [InlineData("a >= b")]
    [InlineData("a == b")]
    [InlineData("a != b")]
    public void ApplyMutations_OnRelationalOperator_EmitsExactly5Replacements(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 5);
        mutations.Should().AllSatisfy(m => m.Type.Should().Be(Mutator.Equality));
    }

    [Fact]
    public void ApplyMutations_OnLessThan_EmitsAllExpectedReplacements()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a < b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        var replacementOps = mutations.Select(m => m.ReplacementNode.ToString().Replace("a", "").Replace("b", "").Trim()).ToList();
        replacementOps.Should().BeEquivalentTo("<=", ">", ">=", "==", "!=");
    }

    [Fact]
    public void ApplyMutations_OnEquals_DoesNotEmitItselfAsReplacement()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a == b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 5);
        mutations.Should().NotContain(m => m.ReplacementNode.ToString().Contains("=="));
    }

    [Fact]
    public void ApplyMutations_DisplayName_FollowsExpectedFormat()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a < b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        mutations.Should().AllSatisfy(m => m.DisplayName.Should().StartWith("ROR matrix: '<' -> "));
    }

    [Theory]
    [InlineData("a + b")]
    [InlineData("a && b")]
    [InlineData("a || b")]
    [InlineData("a & b")]
    [InlineData("a | b")]
    [InlineData("a ^ b")]
    [InlineData("a * b")]
    public void ApplyMutations_OnNonRelationalOperator_ReturnsNoMutation(string source)
    {
        var node = ParseExpression<BinaryExpressionSyntax>(source);
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertNoMutations(mutations);
    }

    // Sprint 184 zu Issue 279, Befund F-06: die volle Ordnungs-Matrix lief auch auf
    // Referenztyp-Gleichheit — jeder Null-Check bekam vier garantierte CompileError
    // der Form name kleiner null. Ordnungs-Ersetzungen verlangen jetzt aufloesbar
    // ordnungsfaehige Operanden; Gleichheits-Tausch bleibt fuer alle Typen, und ohne
    // Semantik bleibt das bisherige Verhalten erhalten.
    [Fact]
    public void ApplyMutations_OnReferenceEquality_EmitsOnlyTheEqualitySwap()
    {
        var (model, node) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { bool Probe(string name) => name == null; }");

        var mutations = ApplyMutations(new RorMatrixMutator(), node, model);

        mutations.Should().ContainSingle("ordering operators do not exist on reference types")
            .Which.ReplacementNode.ToString().Should().Contain("!=");
    }

    [Fact]
    public void ApplyMutations_OnNumericEquality_KeepsTheFullMatrix()
    {
        var (model, node) = BuildSemanticContext<BinaryExpressionSyntax>(
            "class C { bool Probe(int x) => x == 1; }");

        var mutations = ApplyMutations(new RorMatrixMutator(), node, model);

        mutations.Should().HaveCount(5, "numeric operands support the whole ROR matrix");
    }

    [Fact]
    public void ApplyMutations_WithoutSemanticModel_KeepsTheFullMatrix()
    {
        var node = ParseExpression<BinaryExpressionSyntax>("a == b");
        var mutations = ApplyMutations<RorMatrixMutator, BinaryExpressionSyntax>(new(), node);
        AssertMutationCount(mutations, 5);
    }
}
