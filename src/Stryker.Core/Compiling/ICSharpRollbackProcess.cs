using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Stryker.Core.Compiling;

public interface ICSharpRollbackProcess
{
    CSharpRollbackProcessResult Start(CSharpCompilation compiler, ImmutableArray<Diagnostic> diagnostics,
        bool lastAttempt, bool devMode);

    SyntaxTree CleanUpFile(SyntaxTree file);
}
