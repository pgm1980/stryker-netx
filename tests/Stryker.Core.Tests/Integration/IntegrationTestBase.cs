using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.Mutants;
using Stryker.Core.Mutators;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Tests.Integration;

/// <summary>
/// v2.7.0 (Sprint 20): shared base class for integration tests covering the
/// 6 integration layers (Orchestrator, Profile-Filter, Filter-Pipeline,
/// MutantPlacer, Reporter, Configuration).
/// Extends <see cref="MutatorTestBase"/> with orchestrator-level helpers:
/// build a real <see cref="CsharpMutantOrchestrator"/>, run it on a source
/// snippet, build a real <see cref="StrykerOptions"/> without the brittle
/// stub from Sprint 18.
/// </summary>
public abstract class IntegrationTestBase : MutatorTestBase
{
    // Stryker.Core mutators (e.g. RegexMutator) instantiate loggers eagerly via
    // Stryker.Utilities.Logging.ApplicationLogging.LoggerFactory. The CLI bootstrap
    // sets it; tests don't run that bootstrap, so we seed a NullLoggerFactory here.
    static IntegrationTestBase()
    {
        ApplicationLogging.LoggerFactory ??= NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Builds a real <see cref="StrykerOptions"/> with sensible defaults.
    /// Replaces the brittle stub from Sprint 18 — direct concrete-class
    /// init avoids the IStrykerOptions reflection surface entirely.
    /// </summary>
    protected static StrykerOptions BuildStrykerOptions(
        MutationProfile profile = MutationProfile.Defaults,
        MutationLevel level = MutationLevel.Standard)
    {
#pragma warning disable CS0618 // MutationEngine is the deprecated shim from ADR-021; integration tests use the default Recompile.
        return new StrykerOptions
        {
            MutationLevel = level,
            MutationProfile = profile,
            MutationEngine = MutationEngine.Recompile,
            LanguageVersion = LanguageVersion.Latest,
            Concurrency = 1,
        };
#pragma warning restore CS0618
    }

    /// <summary>
    /// Builds a <see cref="CsharpMutantOrchestrator"/> with the given profile.
    /// The active mutator-set is filtered by the profile membership attribute
    /// per the Sprint-6 ADR-018 contract.
    /// </summary>
    protected static CsharpMutantOrchestrator BuildOrchestrator(
        MutationProfile profile = MutationProfile.Defaults,
        IEnumerable<IMutator>? customMutators = null)
    {
        var options = BuildStrykerOptions(profile);
        var placer = new MutantPlacer(new global::Stryker.Core.InjectedHelpers.CodeInjection());
        return new CsharpMutantOrchestrator(placer, customMutators, options);
    }

    /// <summary>
    /// End-to-end pipeline run on a source snippet: parses, builds Compilation +
    /// SemanticModel, runs the Orchestrator, returns the produced Mutants and
    /// the mutated SyntaxTree. The Orchestrator's internal filter pipeline runs
    /// before mutant emission (per Sprint 7 ADR-017).
    /// </summary>
    protected static (IReadOnlyList<Mutant> Mutants, SyntaxTree MutatedTree) RunOrchestratorOnSource(
        string source,
        MutationProfile profile = MutationProfile.Defaults)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "IntegrationTestAssembly",
            syntaxTrees: [tree],
            references: [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location),
            ],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(tree);
        var orchestrator = BuildOrchestrator(profile);
        var mutatedTree = orchestrator.Mutate(tree, semanticModel);
        return ([.. orchestrator.Mutants.Cast<Mutant>()], mutatedTree);
    }

    /// <summary>
    /// Convenience: extract the active-mutator-set from an orchestrator instance
    /// via reflection on the private Mutators field. Used by the L2 Profile-Filter
    /// integration tests.
    /// </summary>
    protected static IReadOnlyList<IMutator> GetActiveMutators(CsharpMutantOrchestrator orchestrator)
    {
        var prop = typeof(CsharpMutantOrchestrator).GetProperty("Mutators",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var mutators = (IEnumerable<IMutator>)prop!.GetValue(orchestrator)!;
        return [.. mutators];
    }
}
