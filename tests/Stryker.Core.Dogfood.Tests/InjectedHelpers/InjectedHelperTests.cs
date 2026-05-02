using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Core.InjectedHelpers;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.InjectedHelpers;

/// <summary>Sprint 80 (v2.66.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class InjectedHelperTests : TestBase
{
    [Theory]
    [InlineData(LanguageVersion.CSharp2)]
    [InlineData(LanguageVersion.CSharp3)]
    [InlineData(LanguageVersion.CSharp4)]
    [InlineData(LanguageVersion.CSharp5)]
    [InlineData(LanguageVersion.CSharp6)]
    [InlineData(LanguageVersion.CSharp7)]
    [InlineData(LanguageVersion.CSharp7_1)]
    [InlineData(LanguageVersion.CSharp7_2)]
    [InlineData(LanguageVersion.CSharp7_3)]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.Default)]
    [InlineData(LanguageVersion.Latest)]
    [InlineData(LanguageVersion.LatestMajor)]
    [InlineData(LanguageVersion.Preview)]
    public void InjectHelpers_ShouldCompile_ForAllLanguageVersions(LanguageVersion version)
    {
        var compilation = BuildCompilation(version, nullable: false);

        compilation.GetDiagnostics()
            .Where(diag => diag.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty(BuildErrorMessage(compilation));
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp8)]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp10)]
    [InlineData(LanguageVersion.CSharp11)]
    [InlineData(LanguageVersion.CSharp12)]
    [InlineData(LanguageVersion.Default)]
    [InlineData(LanguageVersion.Latest)]
    [InlineData(LanguageVersion.LatestMajor)]
    [InlineData(LanguageVersion.Preview)]
    public void InjectHelpers_ShouldCompile_ForAllLanguageVersionsWithNullableOptions(LanguageVersion version)
    {
        // Force-load System.IO.Pipes referenced by injected helpers
        _ = new NamedPipeClientStream("test");

        var compilation = BuildCompilation(version, nullable: true);

        compilation.GetDiagnostics()
            .Where(diag => diag.Severity == DiagnosticSeverity.Error)
            .Should().BeEmpty(BuildErrorMessage(compilation));
    }

    private static CSharpCompilation BuildCompilation(LanguageVersion version, bool nullable)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var needed = new[] { ".CoreLib", ".Runtime", "System.IO.Pipes", ".Collections", ".Console" };
        var references = new List<MetadataReference>();
        foreach (var assembly in assemblies)
        {
            if (assembly.FullName is { } fullName && needed.Any(x => fullName.Contains(x, StringComparison.Ordinal)))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        var syntaxes = new List<SyntaxTree>();
        var codeInjection = new CodeInjection();

        foreach (var helper in codeInjection.MutantHelpers)
        {
            syntaxes.Add(CSharpSyntaxTree.ParseText(helper.Value, new CSharpParseOptions(languageVersion: version), helper.Key));
        }

        var options = nullable
            ? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, generalDiagnosticOption: ReportDiagnostic.Error)
            : new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        return CSharpCompilation.Create("dummy.dll", syntaxes, options: options, references: references);
    }

    private static string BuildErrorMessage(CSharpCompilation compilation) =>
        $"errors :{string.Join(Environment.NewLine, compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error).Select(diag => $"{diag.Id}: '{diag.GetMessage(System.Globalization.CultureInfo.InvariantCulture)}' at {diag.Location.SourceTree?.FilePath}, {diag.Location.GetLineSpan().StartLinePosition.Line + 1}:{diag.Location.GetLineSpan().StartLinePosition.Character}"))}";
}
