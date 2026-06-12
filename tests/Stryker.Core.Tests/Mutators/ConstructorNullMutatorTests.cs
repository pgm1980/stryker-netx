using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ConstructorNullMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsStrongerOrAll()
        => AssertProfileMembership<ConstructorNullMutator>(MutationProfile.Stronger | MutationProfile.All);

    [Fact]
    public void ApplyMutations_OnObjectCreation_EmitsNullReplacement()
    {
        var node = ParseExpression<ObjectCreationExpressionSyntax>("new Foo(1, 2)");
        var m = AssertSingleMutation(ApplyMutations<ConstructorNullMutator, ObjectCreationExpressionSyntax>(new(), node));
        m.ReplacementNode.ToString().Should().Be("null");
    }

    [Fact]
    public void ApplyMutations_InsideThrowStatement_SkipsMutation()
    {
        var stmt = ParseStatement<ThrowStatementSyntax>("throw new Exception(\"x\");");
        var creation = stmt.Expression.Should().BeOfType<ObjectCreationExpressionSyntax>().Subject;
        AssertNoMutations(ApplyMutations<ConstructorNullMutator, ObjectCreationExpressionSyntax>(new(), creation));
    }

    // Sprint 184 zu Issue 279, Befund F-35: die Doku versprach Typ-Bewusstsein, der Code
    // prüfte nichts — jede Struct-Konstruktion wurde zu null und damit zu CS0037. Bei
    // aufloesbarem Werttyp wird nicht mutiert; Referenztypen, Nullable-Konstruktionen und
    // unbekannte Typen mutieren weiter.
    [Fact]
    public void ApplyMutations_OnStructConstruction_SkipsMutation()
    {
        var (model, node) = BuildSemanticContext<ObjectCreationExpressionSyntax>(
            "class C { System.TimeSpan Probe() => new System.TimeSpan(1); }");

        var mutations = ApplyMutations(new ConstructorNullMutator(), node, model);

        mutations.Should().BeEmpty("a struct can never be null");
    }

    [Fact]
    public void ApplyMutations_OnClassConstruction_StillMutates()
    {
        var (model, node) = BuildSemanticContext<ObjectCreationExpressionSyntax>(
            "class C { object Probe() => new object(); }");

        var mutations = ApplyMutations(new ConstructorNullMutator(), node, model);

        mutations.Should().ContainSingle();
    }

    [Fact]
    public void ApplyMutations_OnNullableConstruction_StillMutates()
    {
        var (model, node) = BuildSemanticContext<ObjectCreationExpressionSyntax>(
            "class C { int? Probe() => new int?(); }");

        var mutations = ApplyMutations(new ConstructorNullMutator(), node, model);

        mutations.Should().ContainSingle("null is assignable to a nullable value type");
    }
}
