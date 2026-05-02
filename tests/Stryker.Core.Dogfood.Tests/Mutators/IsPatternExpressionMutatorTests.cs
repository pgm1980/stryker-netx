using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Core.Mutators;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Mutators;

/// <summary>
/// Sprint 51 (v2.38.0) port of upstream stryker-net 4.14.1.
/// Framework conversion: MSTest [DynamicData] → xUnit [MemberData], Shouldly → FluentAssertions.
/// </summary>
public class IsPatternExpressionMutatorTests
{
    [Fact]
    public void ShouldMutateIsToIsNot()
    {
        var target = new IsPatternExpressionMutator();
        var expression = GenerateSimpleConstantPattern(false);

        var mutation = target.ApplyMutations(expression, null!).First();

        mutation.OriginalNode.ToString().Should().Be("1 is 1");
        mutation.ReplacementNode.ToString().Should().Be("1 is not 1");
        mutation.DisplayName.Should().Be("Equality mutation");
    }

    [Fact]
    public void ShouldMutateIsNotToIs()
    {
        var target = new IsPatternExpressionMutator();
        var expression = GenerateSimpleConstantPattern(true);

        var mutation = target.ApplyMutations(expression, null!).First();

        mutation.OriginalNode.ToString().Should().Be("1 is not 1");
        mutation.ReplacementNode.ToString().Should().Be("1 is 1");
        mutation.DisplayName.Should().Be("Equality mutation");
    }

    [Theory]
    [MemberData(nameof(GenerateNotSupportedPatterns))]
    public void ShouldNotMutateNotSupportedPatterns(IsPatternExpressionSyntax expression)
    {
        var target = new IsPatternExpressionMutator();

        var result = target.ApplyMutations(expression, null!).Skip(1).ToList();

        result.Should().BeEmpty();
    }

    private static IsPatternExpressionSyntax GenerateSimpleConstantPattern(bool isNotPattern)
    {
        var tree = CSharpSyntaxTree.ParseText($@"
using System;

namespace TestApplication
{{
    class Program
    {{
        static void Main(string[] args)
        {{
            var a = 1 is{(isNotPattern ? " not" : string.Empty)} 1;
        }}
    }}
}}");
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<IsPatternExpressionSyntax>()
            .Single();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "MA0051:Method is too long",
        Justification = "Test data factory yields 5 test cases via yield-return; splitting would obscure the upstream test surface.")]
    public static IEnumerable<object[]> GenerateNotSupportedPatterns
    {
        get
        {
            static IsPatternExpressionSyntax GetExpressionFromTree(SyntaxTree tree) =>
                tree.GetRoot().DescendantNodes().OfType<IsPatternExpressionSyntax>().Single();

            yield return [GetExpressionFromTree(CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestApplication
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var a = 1 is (1);
                        }
                    }
                }"))];

            yield return [GetExpressionFromTree(CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestApplication
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var a = 1 is (int b);
                        }
                    }
                }"))];

            yield return [GetExpressionFromTree(CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestApplication
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var a = 1 is (int);
                        }
                    }
                }"))];

            yield return [GetExpressionFromTree(CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestApplication
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var a = ""test"" is ({ Length: 1 });
                        }
                    }
                }"))];

            yield return [GetExpressionFromTree(CSharpSyntaxTree.ParseText(@"
                using System;
                namespace TestApplication
                {
                    class Program
                    {
                        static void Main(string[] args)
                        {
                            var a = new[] { 1, 2 } is ([1, _]);
                        }
                    }
                }"))];
        }
    }
}
