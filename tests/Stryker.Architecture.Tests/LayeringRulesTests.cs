using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Stryker.Architecture.Tests;

/// <summary>
/// Enforces the inter-module dependency layering documented in ADR-011.
/// Loaded once per test class; ArchUnit-Architecture is expensive to construct.
/// </summary>
public sealed class LayeringRulesTests
{
    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(Stryker.Abstractions.IMutant).Assembly,
            typeof(Stryker.Utilities.FilePathUtils).Assembly,
            typeof(Stryker.Configuration.ExitCodes).Assembly,
            typeof(Stryker.RegexMutators.Mutators.IRegexMutator).Assembly,
            typeof(Stryker.Solutions.SolutionProvider).Assembly,
            typeof(Stryker.TestRunner.Results.TestRunResult).Assembly,
            typeof(Stryker.TestRunner.MicrosoftTestPlatform.MicrosoftTestPlatformRunnerPool).Assembly,
            typeof(Stryker.TestRunner.VsTest.VsTestRunner).Assembly,
            typeof(Stryker.Core.StrykerRunner).Assembly,
            typeof(Stryker.CLI.Program).Assembly)
        .Build();

    [Fact]
    public void Abstractions_Should_Not_Depend_On_Higher_Layers()
    {
        Types()
            .That().ResideInNamespace("Stryker.Abstractions", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.Configuration", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.CLI", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.TestRunner", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.Solutions", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.RegexMutators", useRegularExpressions: true))
            .Because("Stryker.Abstractions is Layer-0; depending on higher layers would create a cycle.")
            .Check(Architecture);
    }

    [Fact]
    public void Utilities_Should_Not_Depend_On_Higher_Layers()
    {
        Types()
            .That().ResideInNamespace("Stryker.Utilities", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.Configuration", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.CLI", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.TestRunner", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.Solutions", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.RegexMutators", useRegularExpressions: true))
            .Because("Stryker.Utilities is Layer-0; depending on higher layers would create a cycle.")
            .Check(Architecture);
    }

    [Fact]
    public void Configuration_Should_Not_Depend_On_Core_Or_CLI()
    {
        Types()
            .That().ResideInNamespace("Stryker.Configuration", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.CLI", useRegularExpressions: true))
            .Because("Stryker.Configuration is Layer-1; Stryker.Core (Layer-3) and Stryker.CLI (Layer-4) sit above it.")
            .Check(Architecture);
    }

    [Fact]
    public void RegexMutators_Should_Not_Depend_On_Core_Or_CLI()
    {
        Types()
            .That().ResideInNamespace("Stryker.RegexMutators", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.CLI", useRegularExpressions: true))
            .Because("Stryker.RegexMutators is Layer-1; Stryker.Core (Layer-3) and Stryker.CLI (Layer-4) sit above it.")
            .Check(Architecture);
    }

    [Fact]
    public void TestRunner_Modules_Should_Not_Depend_On_Core_Or_CLI()
    {
        Types()
            .That().ResideInNamespace("Stryker.TestRunner", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
                    .Or().ResideInNamespace("Stryker.CLI", useRegularExpressions: true))
            .Because("TestRunner adapters are Layer-1/2; Stryker.Core (Layer-3) and Stryker.CLI (Layer-4) sit above them.")
            .Check(Architecture);
    }

    [Fact]
    public void Core_Should_Not_Depend_On_CLI()
    {
        Types()
            .That().ResideInNamespace("Stryker.Core", useRegularExpressions: true)
            .Should().NotDependOnAny(
                Types().That().ResideInNamespace("Stryker.CLI", useRegularExpressions: true))
            .Because("Stryker.Core is Layer-3; Stryker.CLI (Layer-4) is the entry-point and may not be referenced by core engine.")
            .Check(Architecture);
    }
}
