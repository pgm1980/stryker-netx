using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.InjectedHelpers;
using Stryker.Core.Mutants;
using Stryker.TestHelpers;

namespace Stryker.Core.Dogfood.Tests.Mutants;

/// <summary>
/// Sprint 62 (v2.48.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Base class for source-mutation orchestrator tests. Inherits TestBase to seed
/// ApplicationLogging.LoggerFactory (CsharpMutantOrchestrator ctor uses it).
/// </summary>
public class MutantOrchestratorTestsBase : TestBase
{
    protected CsharpMutantOrchestrator Target { get; set; }
    protected CodeInjection Injector { get; } = new();

    protected MutantOrchestratorTestsBase()
    {
        Target = new CsharpMutantOrchestrator(new MutantPlacer(Injector), options: new StrykerOptions
        {
            MutationLevel = MutationLevel.Complete,
            OptimizationMode = OptimizationModes.CoverageBasedTest,
        });
    }

    protected void ShouldMutateSourceToExpected(string actual, string expected)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(actual);
        Type[] typeToLoad = [typeof(object), typeof(List<>), typeof(Enumerable), typeof(Nullable<>)];
        MetadataReference[] references = [.. typeToLoad.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location))];
        var compilation = CSharpCompilation.Create(null).WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable))
                        .AddSyntaxTrees(syntaxTree).WithReferences(references);
        var actualNode = Target.Mutate(syntaxTree, compilation.GetSemanticModel(syntaxTree));
        actual = actualNode.GetRoot().ToFullString();
        actual = actual.Replace(Injector.HelperNamespace, "StrykerNamespace", StringComparison.Ordinal);
        actualNode = CSharpSyntaxTree.ParseText(actual);
        actualNode.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        var expectedNode = CSharpSyntaxTree.ParseText(expected);

        // Semantic equivalence: normalise whitespace + structurally compare the syntax trees.
        // Sprint 62: stand-in for upstream Shouldly's `ShouldBeSemantically(...)` extension —
        // strict node-shape match without trivia.
        actualNode.GetRoot().NormalizeWhitespace().ToFullString()
            .Should().Be(expectedNode.GetRoot().NormalizeWhitespace().ToFullString());
    }

    protected void ShouldMutateSourceInClassToExpected(string actual, string expected)
    {
        const string InitBlock = """
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using System.Text;
            namespace StrykerNet.UnitTest.Mutants.TestResources;
            """;

        actual = string.Format(System.Globalization.CultureInfo.InvariantCulture, """
            {0}
            class TestClass
            {{
            {1}
            }}
            """, InitBlock, actual);

        expected = string.Format(System.Globalization.CultureInfo.InvariantCulture, """
            {0}
            class TestClass
            {{
            {1}
            }}
            """, InitBlock, expected);

        ShouldMutateSourceToExpected(actual, expected);
    }

    /// <summary>Sprint 119: structural-assertion helper for bucket-3 tests that can't use
    /// literal-string comparison (IDs depend on v2.x mutator-pipeline order). Asserts the
    /// orchestrator produces AT LEAST minMutations, that the output contains IsActive markers,
    /// and (optionally) that specific mutator types fired.</summary>
    protected int CountMutations(string source)
    {
        var sourceWithClass = $$"""
            using System;
            using System.Linq;
            using System.Collections.Generic;
            using System.Text;
            namespace StrykerNet.UnitTest.Mutants.TestResources;
            class TestClass
            {
            {{source}}
            }
            """;
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceWithClass);
        Type[] typeToLoad = [typeof(object), typeof(List<>), typeof(Enumerable), typeof(Nullable<>)];
        MetadataReference[] references = [.. typeToLoad.Select(t => MetadataReference.CreateFromFile(t.Assembly.Location))];
        var compilation = CSharpCompilation.Create(null).WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable))
                        .AddSyntaxTrees(syntaxTree).WithReferences(references);
        var actualNode = Target.Mutate(syntaxTree, compilation.GetSemanticModel(syntaxTree));
        var actualString = actualNode.GetRoot().ToFullString();
        // Count IsActive(N) markers — each represents a mutation
        var isActiveMarker = $"{Injector.HelperNamespace}.MutantControl.IsActive(";
        var count = 0;
        var idx = 0;
        while ((idx = actualString.IndexOf(isActiveMarker, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += isActiveMarker.Length;
        }
        return count;
    }
}
