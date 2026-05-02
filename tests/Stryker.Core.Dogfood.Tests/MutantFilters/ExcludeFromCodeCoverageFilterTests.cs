using System;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Stryker.Abstractions;
using Stryker.Configuration.Options;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 77 (v2.63.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// CA1859 is suppressed file-level: upstream tests deliberately exercise the IMutantFilter
/// interface contract. Concrete type would defeat the purpose of these tests.</summary>
#pragma warning disable CA1859
public class ExcludeFromCodeCoverageFilterTests
{
    [Fact]
    public void ShouldHaveName()
    {
        IMutantFilter target = new ExcludeFromCodeCoverageFilter();
        target.DisplayName.Should().Be("exclude from code coverage filter");
    }

    [Fact]
    public void OnMethod()
    {
        var mutant = Create("""
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                [ExcludeFromCodeCoverage]
                private void TestMethod()
                {
                    var t = Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """, "<");

        IMutantFilter sut = new ExcludeFromCodeCoverageFilter();

        var results = sut.FilterMutants([mutant], null!, new StrykerOptions());

        results.Should().NotContain(mutant);
    }

    [Fact]
    public void OnProperty()
    {
        var mutant = Create("""
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                [ExcludeFromCodeCoverage]
                private string TestProperty => "something"
            }
            """, "something");

        IMutantFilter sut = new ExcludeFromCodeCoverageFilter();

        var results = sut.FilterMutants([mutant], null!, new StrykerOptions());

        results.Should().NotContain(mutant);
    }

    [Fact]
    public void OnClass()
    {
        var mutant = Create("""
            [ExcludeFromCodeCoverage("sowhat")]
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    var t = Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """, "<");

        var sut = new ExcludeFromCodeCoverageFilter();

        var results = sut.FilterMutants([mutant], null!, new StrykerOptions());

        results.Should().NotContain(mutant);
    }

    [Fact]
    public void Not()
    {
        var mutant = Create("""
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                private void TestMethod()
                {
                    var t = Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """, "<");

        var sut = new ExcludeFromCodeCoverageFilter();

        var results = sut.FilterMutants([mutant], null!, new StrykerOptions());

        results.Should().Contain(mutant);
    }

    [Theory]
    [InlineData("System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute")]
    [InlineData("ExcludeFromCodeCoverageAttribute")]
    [InlineData("ExcludeFromCodeCoverage")]
    public void Writings(string attr)
    {
        var mutant = Create($$"""
            public class IgnoredMethodMutantFilter_NestedMethodCalls
            {
                [{{attr}}]
                private void TestMethod()
                {
                    var t = Enumerable.Range(0, 9).Where(x => x < 5).ToList();
                }
            }
            """, "<");

        var sut = new ExcludeFromCodeCoverageFilter();

        var results = sut.FilterMutants([mutant], null!, new StrykerOptions());

        results.Should().NotContain(mutant);
    }

    private static Mutant Create(string source, string search)
    {
        var baseSyntaxTree = CSharpSyntaxTree.ParseText(source).GetRoot();
        var originalNode =
            baseSyntaxTree.FindNode(new TextSpan(source.IndexOf(search, StringComparison.OrdinalIgnoreCase), 5));

        return new Mutant
        {
            Mutation = new Mutation
            {
                OriginalNode = originalNode,
                ReplacementNode = originalNode,
                DisplayName = "test",
            },
        };
    }
}
#pragma warning restore CA1859
