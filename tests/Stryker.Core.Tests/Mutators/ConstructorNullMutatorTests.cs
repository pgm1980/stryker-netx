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
}
