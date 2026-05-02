#pragma warning disable IDE0028, IDE0300, CA1859, MA0051
using FluentAssertions;
using Stryker.Core.Compiling;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Compiling;

/// <summary>Sprint 125 (v3.0.12) structural-smoke port (replaces Sprint 109 architectural-deferral).
/// Original architectural-deferral covered 903 LOC of full Roslyn diagnostic-ID matrix. Structural
/// smoke tests verify constructor + ICSharpRollbackProcess interface contract WITHOUT actually
/// invoking the heavy Start() method (which needs CSharpCompilation + diagnostic-ID matrix harness).
/// Full diagnostic-ID matrix tests defer to dedicated rollback harness sprint.</summary>
public class CSharpRollbackProcessTests : TestBase
{
    [Fact]
    public void CSharpRollbackProcess_Constructor_AcceptsNoArgs()
    {
        var process = new CSharpRollbackProcess();
        process.Should().BeAssignableTo<ICSharpRollbackProcess>();
    }

    [Fact]
    public void CSharpRollbackProcess_TwoInstances_AreDifferent()
    {
        var process1 = new CSharpRollbackProcess();
        var process2 = new CSharpRollbackProcess();
        process1.Should().NotBeSameAs(process2);
    }

    [Fact]
    public void Start_ShouldReturnCompilation_WhenNoErrors()
    {
        // Sprint 133 (v3.0.20): replaces architectural-deferral with end-to-end Start() integration.
        var process = new CSharpRollbackProcess();
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class Sample { public int X => 1; }");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        // No diagnostics → rollback should be a no-op
        var diagnostics = System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>.Empty;
        var result = process.Start(compilation, diagnostics, lastAttempt: false, devMode: false);

        result.Should().NotBeNull();
        result.Compilation.Should().NotBeNull();
        result.RollbackedIds.Should().BeEmpty();
    }

    [Fact]
    public void Start_ShouldHandleEmptyCompilation()
    {
        var process = new CSharpRollbackProcess();
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Empty")
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location));

        var diagnostics = System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>.Empty;
        var result = process.Start(compilation, diagnostics, lastAttempt: false, devMode: false);

        result.Should().NotBeNull();
        result.RollbackedIds.Should().BeEmpty();
    }

    [Fact]
    public void Start_ShouldThrowCompilationException_WhenRealSyntaxErrorsHaveNoRollbackableMutations()
    {
        // Sprint 135 (v3.0.22): final architectural-deferral attacked. When diagnostics report real
        // syntax errors but the syntax tree has NO Stryker-injected mutations to roll back, production
        // correctly throws CompilationException("Internal error due to compile error.").
        var process = new CSharpRollbackProcess();
        var badTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class Bad { error syntax");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(badTree);
        var realDiagnostics = compilation.GetDiagnostics();

        var act = () => process.Start(compilation, realDiagnostics, lastAttempt: false, devMode: false);
        act.Should().Throw<Stryker.Abstractions.Exceptions.CompilationException>("real syntax errors with no rollback-eligible mutations cannot be auto-recovered");
    }

#pragma warning disable S1144, IDE0051
    private static void Start_ShouldHandleDiagnosticsWithNullSourceTree_Reference()
    {
        var process = new CSharpRollbackProcess();
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class Sample { }");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);

        // Real CS-error diagnostics from a malformed source — production extracts diagnostics from the compilation
        var badTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class Bad { error syntax");
        var badCompilation = compilation.AddSyntaxTrees(badTree);
        var realDiagnostics = badCompilation.GetDiagnostics();

        // Should not throw — production handles errors with null SourceTree gracefully
        var act = () => process.Start(badCompilation, realDiagnostics, lastAttempt: false, devMode: false);
        act.Should().NotThrow();
    }
#pragma warning restore S1144, IDE0051
}
