using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Tests.Mutators;

public class ExceptionSwapMutatorTests : MutatorTestBase
{
    [Fact]
    public void Profile_IsAllOnly()
        => AssertProfileMembership<ExceptionSwapMutator>(MutationProfile.All);

    [Theory]
    [InlineData("throw new ArgumentNullException(\"p\");")]
    [InlineData("throw new ArgumentException(\"x\");")]
    [InlineData("throw new InvalidOperationException(\"x\");")]
    [InlineData("throw new NotSupportedException(\"x\");")]
    public void ApplyMutations_OnKnownExceptionType_EmitsSwap(string source)
    {
        var stmt = ParseStatement<ThrowStatementSyntax>(source);
        var creation = stmt.Expression.Should().BeOfType<ObjectCreationExpressionSyntax>().Subject;
        AssertSingleMutation(ApplyMutations<ExceptionSwapMutator, ObjectCreationExpressionSyntax>(new(), creation));
    }

    [Fact]
    public void ApplyMutations_OnUnknownExceptionType_ReturnsNoMutation()
    {
        var stmt = ParseStatement<ThrowStatementSyntax>("throw new MyCustomException(\"x\");");
        var creation = stmt.Expression.Should().BeOfType<ObjectCreationExpressionSyntax>().Subject;
        AssertNoMutations(ApplyMutations<ExceptionSwapMutator, ObjectCreationExpressionSyntax>(new(), creation));
    }
}
