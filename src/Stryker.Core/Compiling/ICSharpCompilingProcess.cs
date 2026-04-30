using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;

namespace Stryker.Core.Compiling;

public interface ICSharpCompilingProcess
{
    CompilingProcessResult Compile(IEnumerable<SyntaxTree> syntaxTrees, Stream ilStream, Stream? symbolStream);
    IEnumerable<SemanticModel> GetSemanticModels(IEnumerable<SyntaxTree> syntaxTrees);
}
