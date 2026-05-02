using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 47 (v2.34.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
public class StatementMutatorTests
{
    [Fact]
    public void ShouldBeMutationlevelStandard()
    {
        var target = new StatementMutator();
        target.MutationLevel.Should().Be(MutationLevel.Standard);
    }

    [Theory]
    [InlineData("return;")]
    [InlineData("break;")]
    [InlineData("continue;")]
    [InlineData("goto test;")]
    [InlineData("throw null;")]
    [InlineData("yield break;")]
    [InlineData("yield return 0;")]
    [InlineData("await null;")]
    public void ShouldMutate(string statementString)
    {
        var source = $@"class Test {{
                void Method() {{
                    {statementString}
                }}
            }}";

        var tree = CSharpSyntaxTree.ParseText(source).GetRoot();

        var statement = tree.DescendantNodes().OfType<StatementSyntax>().First(s => s is not BlockSyntax);

        var target = new StatementMutator();

        var result = target.ApplyMutations(statement, null!).ToList();

        result.Should().ContainSingle();
        var mutation = result[0];

        mutation.ReplacementNode.Should().BeOfType<EmptyStatementSyntax>();
        mutation.DisplayName.Should().Be("Statement mutation");
    }

    [Fact]
    public void ShouldNotMutate()
    {
        var tree = CSharpSyntaxTree.ParseText(@"
namespace Test
{
    class Program
    {
        static int Method()
        {
            int variable = 0;

            Method(out var x);

            if (x is Type v)
            {
                return;
            }

            switch (x)
            {
                case 1:
                    return;
                    break;
                    continue;
                    goto X;
                    throw x;
            }

            return 1;
        }
    }
}");
        var statements = tree.GetRoot()
            .DescendantNodes()
            .OfType<StatementSyntax>();

        var target = new StatementMutator();

        var result = statements.SelectMany(statement => target.ApplyMutations(statement, null!)).ToList();

        result.Should().BeEmpty();
    }
}
