using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stryker.Abstractions;
using Stryker.Core.Mutators;

namespace Stryker.Core.Tests;

/// <summary>
/// v2.5.0 (Sprint 18 Hardening): shared base class for all
/// per-mutator test classes. Centralises Roslyn syntax-tree construction,
/// mutation invocation, and FluentAssertions-driven assertion helpers
/// so per-mutator test classes stay focused on the mutation contract
/// itself.
///
/// Architecture decision (Sprint 18 Maxential, S1 chosen): one project
/// for all Stryker.Core unit tests, mirroring upstream Stryker.NET
/// pattern. SemanticModel strategy (Sprint 18 ToT, Hybrid chosen): real
/// CSharpCompilation for happy-path semantic-correct tests; null cast
/// for the explicit `if (semanticModel is null) return [];` branch in
/// <see cref="TypeAwareMutatorBase{T}"/>.
/// </summary>
public abstract class MutatorTestBase
{
    /// <summary>Parses an expression string into the requested ExpressionSyntax-derived type.</summary>
    protected static T ParseExpression<T>(string source) where T : ExpressionSyntax
    {
        var node = SyntaxFactory.ParseExpression(source);
        node.Should().BeAssignableTo<T>($"the parsed expression '{source}' should be a {typeof(T).Name}");
        return (T)node;
    }

    /// <summary>Parses a statement string into the requested StatementSyntax-derived type.</summary>
    protected static T ParseStatement<T>(string source) where T : StatementSyntax
    {
        var node = SyntaxFactory.ParseStatement(source);
        node.Should().BeAssignableTo<T>($"the parsed statement '{source}' should be a {typeof(T).Name}");
        return (T)node;
    }

    /// <summary>
    /// Parses arbitrary class-member source by wrapping into a synthetic class shell,
    /// then locates and returns the first node of the requested type. Useful for
    /// MethodDeclarationSyntax / TypeParameterConstraintClauseSyntax / etc.
    /// </summary>
    protected static T ParseMember<T>(string memberSource) where T : SyntaxNode
    {
        const string usingDirectives = "using System; using System.Collections.Generic; using System.Linq; using System.Threading.Tasks;";
        var source = $"{usingDirectives}\nclass C {{ {memberSource} }}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var found = tree.GetRoot().DescendantNodes().OfType<T>().FirstOrDefault();
        found.Should().NotBeNull($"the parsed source should contain a {typeof(T).Name}");
        return found!;
    }

    /// <summary>
    /// Builds a real <see cref="SemanticModel"/> for the given source, returning
    /// the SemanticModel and the first descendant of type <typeparamref name="TNode"/>
    /// found in the parsed tree. Used by type-aware mutator tests for happy-path
    /// semantic-correct scenarios.
    /// </summary>
    protected static (SemanticModel SemanticModel, TNode Node) BuildSemanticContext<TNode>(string source)
        where TNode : SyntaxNode
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: [tree],
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var node = tree.GetRoot().DescendantNodes().OfType<TNode>().FirstOrDefault();
        node.Should().NotBeNull($"the parsed source should contain a {typeof(TNode).Name}");
        return (semanticModel, node!);
    }

    /// <summary>
    /// Invokes the mutator's <c>ApplyMutations</c> method directly, avoiding the
    /// MutationLevel / Profile dispatch in <c>Mutate(...)</c>. Returns the produced
    /// mutations as a list for assertion convenience.
    /// </summary>
    protected static IReadOnlyList<Mutation> ApplyMutations<TMutator, TNode>(
        TMutator mutator,
        TNode node,
        SemanticModel? semanticModel = null)
        where TMutator : MutatorBase<TNode>
        where TNode : SyntaxNode
    {
        // MutatorBase.ApplyMutations takes a non-null SemanticModel parameter declaration,
        // but most simple mutators ignore it. The null-default lets callers omit when
        // the mutator doesn't read it. We use null-forgiving for those cases.
        return [.. mutator.ApplyMutations(node, semanticModel!)];
    }

    /// <summary>
    /// Invokes a type-aware mutator's <c>ApplyMutations</c> method. Uses the
    /// public ApplyMutations entry point on the typed-base which expects a
    /// non-null SemanticModel.
    /// </summary>
    protected static IReadOnlyList<Mutation> ApplyTypeAwareMutations<TMutator, TNode>(
        TMutator mutator,
        TNode node,
        SemanticModel semanticModel)
        where TMutator : TypeAwareMutatorBase<TNode>
        where TNode : SyntaxNode
    {
        return [.. mutator.ApplyMutations(node, semanticModel)];
    }

    /// <summary>
    /// Asserts the <see cref="MutationProfile"/> declared via the
    /// <see cref="MutationProfileMembershipAttribute"/> on the mutator class.
    /// </summary>
    protected static void AssertProfileMembership<TMutator>(MutationProfile expected)
    {
        var attr = typeof(TMutator).GetCustomAttribute<MutationProfileMembershipAttribute>();
        attr.Should().NotBeNull(
            $"{typeof(TMutator).Name} must carry a [MutationProfileMembership] attribute (CLAUDE.md profile-discipline)");
        attr!.Profiles.Should().Be(expected,
            $"{typeof(TMutator).Name}'s declared profile membership must match the documented design");
    }

    /// <summary>
    /// Asserts the <see cref="MutationLevel"/> declared by the mutator.
    /// Requires the mutator to expose a parameterless ctor.
    /// </summary>
    protected static void AssertMutationLevel<TMutator>(MutationLevel expected)
        where TMutator : new()
    {
        // MutationLevel is an instance property on MutatorBase / TypeAwareMutatorBase.
        // Use reflection to avoid having to switch the generic constraint.
        var instance = new TMutator()!;
        var levelProp = typeof(TMutator).GetProperty("MutationLevel", BindingFlags.Public | BindingFlags.Instance);
        levelProp.Should().NotBeNull($"{typeof(TMutator).Name} must expose a MutationLevel property");
        var level = (MutationLevel)levelProp!.GetValue(instance)!;
        level.Should().Be(expected, $"{typeof(TMutator).Name}'s declared MutationLevel must match the documented design");
    }

    /// <summary>Convenience: asserts the mutation list is empty.</summary>
    protected static void AssertNoMutations(IReadOnlyList<Mutation> mutations) =>
        mutations.Should().BeEmpty();

    /// <summary>Convenience: asserts a specific number of mutations.</summary>
    protected static void AssertMutationCount(IReadOnlyList<Mutation> mutations, int expected) =>
        mutations.Should().HaveCount(expected);

    /// <summary>Convenience: asserts a single mutation and returns it for further inspection.</summary>
    protected static Mutation AssertSingleMutation(IReadOnlyList<Mutation> mutations)
    {
        mutations.Should().ContainSingle();
        return mutations[0];
    }

    /// <summary>
    /// v2.6.0 (Sprint 19): builds a <see cref="Mutation"/> from a pair of
    /// SyntaxNodes for filter-input scenarios. Filters consume Mutation
    /// objects rather than SyntaxNodes directly, so per-filter tests need
    /// this constructor convenience.
    /// </summary>
    protected static Mutation BuildMutation(SyntaxNode original, SyntaxNode replacement, Mutator type = Mutator.Statement, string displayName = "test")
        => new()
        {
            OriginalNode = original,
            ReplacementNode = replacement,
            Type = type,
            DisplayName = displayName,
        };
}
