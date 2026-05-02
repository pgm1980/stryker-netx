#pragma warning disable IDE0028, IDE0300, CA1859, CA1861, MA0051, MA0011 // collection-expression on cast; CA1859/MA0051 perf-not-test-concern; CA1861 inline arrays for readability; MA0011 IFormatProvider
using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Stryker.Configuration.Options;
using Stryker.Configuration.Options.Inputs;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Xunit;
using Mutation = Stryker.Abstractions.Mutation;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 124 (v3.0.11) substantial port (replaces Sprint 109 architectural-deferral).
/// Upstream had 130 [DataRow] occurrences across many tests. This port covers the most-impactful
/// subset: ShouldHaveName + ShouldFilterDocumentedCases (9 InlineData) + MutantFilter_ChainedMethodsCalls
/// (16 InlineData) + MutantFilter_ChainedMethodsCallStatement (4 InlineData) +
/// MutantFilters_DoNotIgnoreOtherMutantsInFile = 31 effective tests covering the main filter behavior.
/// Sprint 2 Mutation required-init drift handled via SyntaxFactory placeholders.</summary>
public class IgnoredMethodMutantFilterTests
{
    private static Mutation MutationFor(SyntaxNode original) => new()
    {
        OriginalNode = original,
        ReplacementNode = SyntaxFactory.IdentifierName("_replacement"),
        DisplayName = "test",
    };

    private static T? FindEnclosingNode<T>(SyntaxNode? start) where T : SyntaxNode =>
        start switch
        {
            null => null,
            T t => t,
            _ => FindEnclosingNode<T>(start.Parent),
        };

    private static T? FindEnclosingNode<T>(SyntaxNode start, string anchor) where T : SyntaxNode =>
        FindEnclosingNode<T>(start.FindNode(new TextSpan(start.ToFullString().IndexOf(anchor, StringComparison.Ordinal), anchor.Length)));

    private static IEnumerable<(Mutant, string)> BuildMutantsToFilter(string csharp, string anchor)
    {
        var baseSyntaxTree = CSharpSyntaxTree.ParseText(csharp).GetRoot();

        var originalExpr = FindEnclosingNode<ExpressionSyntax>(baseSyntaxTree, anchor);
        if (originalExpr is not null)
        {
            yield return (new Mutant { Mutation = MutationFor(originalExpr) }, "Expression mutant");
        }

        var originalStmt = FindEnclosingNode<StatementSyntax>(baseSyntaxTree, anchor);
        if (originalStmt is not null)
        {
            yield return (new Mutant { Mutation = MutationFor(originalStmt) }, "Statement mutant");
        }

        var originalBlock = FindEnclosingNode<BlockSyntax>(baseSyntaxTree, anchor);
        if (originalBlock is not null)
        {
            yield return (new Mutant { Mutation = MutationFor(originalBlock) }, "Block mutant");
        }
    }

    private static (Mutant, string) BuildExpressionMutant(string sourceCode, string anchor)
    {
        var node = FindEnclosingNode<ExpressionSyntax>(CSharpSyntaxTree.ParseText(sourceCode).GetRoot(), anchor);
        var mutant = new Mutant { Mutation = MutationFor(node!) };
        return (mutant, "Expression");
    }

    [Fact]
    public void ShouldHaveName()
    {
        var target = new IgnoredMethodMutantFilter() as IMutantFilter;
        target.DisplayName.Should().Be("method filter");
    }

    [Theory]
    [InlineData("IgnoredMethod(true);", "true", true)]
    [InlineData("x = IgnoredMethod(true);", null, true)]
    [InlineData("var x = IgnoredMethod(true);", null, true)]
    [InlineData("while (x == IgnoredMethod(true));", "==", false)]
    [InlineData("IgnoredMethod()++;", "IgnoredMethod()++", false)]
    [InlineData("x==1 ? IgnoredMethod(true): IgnoredMethod(false);", "==", true)]
    [InlineData("SomeMethod(true).IgnoredMethod(false);", "true", true)]
    [InlineData("IgnoredMethod(x==> SomeCall(param));", "param", true)]
    [InlineData("IgnoredMethod(x==1 ? true : false);", "false", true)]
    public void ShouldFilterDocumentedCases(string methodCall, string? anchor, bool shouldSkipMutant)
    {
        var source = $$"""
            class Test{public void StubMethod() =>

                {{methodCall}}
            }
            """;
        anchor ??= methodCall;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { "IgnoredMethod" } }.Validate(),
        };

        var sut = new IgnoredMethodMutantFilter();
        foreach (var (mutant, label) in BuildMutantsToFilter(source, anchor))
        {
            var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
            if (shouldSkipMutant)
            {
                filteredMutants.Should().NotContain(mutant, $"{label} should have been filtered out.");
            }
            else
            {
                filteredMutants.Should().Contain(mutant, $"{label} should have been kept.");
            }
        }
    }

    [Theory]
    [InlineData("Where", true)]
    [InlineData("Where*", true)]
    [InlineData("*Where", true)]
    [InlineData("*Where*", true)]
    [InlineData("*ere", true)]
    [InlineData("Wh*", true)]
    [InlineData("W*e", true)]
    [InlineData("*", true)]
    [InlineData("ToList", true)]
    [InlineData("*List", true)]
    [InlineData("To*", true)]
    [InlineData("T*ist", true)]
    [InlineData("Range", false)]
    [InlineData("*Range", false)]
    [InlineData("Ra*", false)]
    [InlineData("R*nge", false)]
    public void MutantFilter_ChainedMethodsCalls(string ignoredMethodName, bool shouldSkipMutant)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    var t = Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };

        var sut = new IgnoredMethodMutantFilter();
        var (mutant, label) = BuildExpressionMutant(source, "<");
        var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);

        if (shouldSkipMutant)
        {
            filteredMutants.Should().NotContain(mutant, $"{label} should have been filtered out.");
        }
        else
        {
            filteredMutants.Should().Contain(mutant, $"{label} should have been kept.");
        }
    }

    [Theory]
    [InlineData("Range", false)]
    [InlineData("Where", false)]
    [InlineData("ToList", true)]
    [InlineData("", false)]
    public void MutantFilter_ChainedMethodsCallStatement(string ignoredMethodName, bool shouldSkipMutant)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };

        var sut = new IgnoredMethodMutantFilter();
        foreach (var (mutant, label) in BuildMutantsToFilter(source, "ToList"))
        {
            var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
            if (shouldSkipMutant)
            {
                filteredMutants.Should().NotContain(mutant, $"{label} should have been filtered out.");
            }
            else
            {
                filteredMutants.Should().Contain(mutant, $"{label} should have been kept.");
            }
        }
    }

    [Fact]
    public void MutantFilters_DoNotIgnoreOtherMutantsInFile()
    {
        var source = """
            public class MutantFilters_DoNotIgnoreOtherMutantsInFile
            {
                private void TestMethod()
                {
                    Foo(true);
                    Bar("A Mutation");
                    Quux(42);
                }
            }
            """;
        var baseSyntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        SyntaxNode GetOriginalNode(string node) =>
            baseSyntaxTree.FindNode(new TextSpan(source.IndexOf(node, StringComparison.OrdinalIgnoreCase), node.Length));

        var mutants = new[] { "true", "\"A Mutation\"", "42" }
            .Select(GetOriginalNode)
            .Select(node => new Mutant { Mutation = MutationFor(node) })
            .ToArray();
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { "TestMethod" } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();

        var filteredMutants = sut.FilterMutants(mutants, null!, options).ToList();

        filteredMutants.Should().Contain(mutants[0]);
        filteredMutants.Should().Contain(mutants[1]);
        filteredMutants.Should().Contain(mutants[2]);
    }

    [Theory]
    [InlineData("Where", true)]
    [InlineData("ToList", true)]
    [InlineData("Range", false)]
    public void MutantFilter_WorksWithConditionalInvocation(string ignoredMethodName, bool shouldSkipMutant)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    Enumerable.Range(0, 9)?.Where(x => x < 5).ToList();
                }
            }
            """;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();
        var mutant = BuildExpressionMutant(source, "<").Item1;
        var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);

        if (shouldSkipMutant)
        {
            filteredMutants.Should().NotContain(mutant);
        }
        else
        {
            filteredMutants.Should().Contain(mutant);
        }
    }

    [Fact]
    public void MutantFilter_WorksWithGenericMethodCalls()
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    Enumerable.Range(0, 9)?.Where(x => x < 5)?.ToList<int>();
                }
            }
            """;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { "ToList" } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();

        foreach (var (mutant, label) in BuildMutantsToFilter(source, "ToList"))
        {
            var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
            filteredMutants.Should().NotContain(mutant, $"{label} should have been filtered out.");
        }
    }

    [Theory]
    [InlineData("Dispose")]
    [InlineData("Dispose*")]
    [InlineData("*Dispose")]
    [InlineData("*Dispose*")]
    [InlineData("*ispose")]
    [InlineData("Dis*")]
    [InlineData("*")]
    public void ShouldFilterStandaloneInvocation(string ignoredMethodName)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    Dispose();
                }
            }
            """;
        var mutant = BuildExpressionMutant(source, "Dispose").Item1;
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();

        var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
        filteredMutants.Should().NotContain(mutant);
    }

    [Theory]
    [InlineData("Bar.Foo.Dispose", true)]
    [InlineData("Bar.*.Dispose", true)]
    [InlineData("Foo.Dispose*", true)]
    [InlineData("Foo.Dispos*", true)]
    [InlineData("*Foo.Dispose", true)]
    [InlineData("F*.Dispose", true)]
    [InlineData("*o.Dispose", true)]
    [InlineData("*o.D*se", true)]
    [InlineData("*.*", true)]
    [InlineData("Foo.*", true)]
    [InlineData("Foo*Dispose", false)]
    [InlineData("Bar.Foo", false)]
    [InlineData("Bar*", false)]
    [InlineData("Bar.", false)]
    [InlineData("Bar.*", false)]
    [InlineData("Foo", false)]
    [InlineData("Foo.", false)]
    [InlineData("*.*.*.*", false)]
    public void ShouldFilterInvocationWithQualifiedMemberName(string ignoredMethodName, bool shouldSkipMutant)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    Bar // comment
                        .Foo.Dispose();
                }
            }
            """;
        var baseSyntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var originalNode = FindEnclosingNode<StatementSyntax>(baseSyntaxTree, "Dispose");
        var mutant = new Mutant { Mutation = MutationFor(originalNode!) };
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();

        var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
        if (shouldSkipMutant)
        {
            filteredMutants.Should().NotContain(mutant);
        }
        else
        {
            filteredMutants.Should().Contain(mutant);
        }
    }

    [Theory]
    [InlineData("Foo.MyType.ctor", true)]
    [InlineData("MyType.ctor", true)]
    [InlineData("Foo.MyType*.ctor", true)]
    [InlineData("Foo*.MyType*.ctor", true)]
    [InlineData("*.MyType*.ctor", true)]
    [InlineData("F*.My*ype*.ctor", true)]
    [InlineData("MyType*.ctor", true)]
    [InlineData("*MyType.ctor", true)]
    [InlineData("*MyType*.ctor", true)]
    [InlineData("*Type.ctor", true)]
    [InlineData("My*.ctor", true)]
    [InlineData("*.ctor", true)]
    [InlineData("*.*.ctor", true)]
    [InlineData("MyType.constructor", false)]
    [InlineData("Type.ctor", false)]
    [InlineData("Foo.ctor", false)]
    public void MutantFilter_ShouldIgnoreConstructor(string ignoredMethodName, bool shouldSkipMutant)
    {
        var source = """
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    var t = new Foo
                                .MyType("Param");
                }
            }
            """;
        var baseSyntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var originalNode = FindEnclosingNode<SyntaxNode>(baseSyntaxTree, "Param");
        var mutant = new Mutant { Mutation = MutationFor(originalNode!) };
        var options = new StrykerOptions
        {
            IgnoredMethods = new IgnoreMethodsInput { SuppliedInput = new[] { ignoredMethodName } }.Validate(),
        };
        var sut = new IgnoredMethodMutantFilter();

        var filteredMutants = sut.FilterMutants(new[] { mutant }, null!, options);
        if (shouldSkipMutant)
        {
            filteredMutants.Should().NotContain(mutant);
        }
        else
        {
            filteredMutants.Should().Contain(mutant);
        }
    }

    [Fact(Skip = "ARCHITECTURAL DEFERRAL: ~50 remaining [DataRow] tests cover advanced edge cases (constructor patterns, async/await constructs, switch expressions, statement+block-only ignored methods). Sprint 124+126 ported 70 most-impactful tests; remaining ~50 defer to dedicated edge-case sprint.")]
    public void IgnoredMethodMutantFilter_RemainingDataRowTests_Deferral() { /* defer */ }
}
