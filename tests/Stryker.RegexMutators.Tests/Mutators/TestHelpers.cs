using System.Collections.Generic;
using System.Linq;
using Stryker.Regex.Parser;
using Stryker.Regex.Parser.Nodes;
using Stryker.RegexMutators;
using Stryker.RegexMutators.Mutators;

namespace Stryker.RegexMutators.Tests.Mutators;

/// <summary>
/// Sprint 42 (v2.29.0) port of upstream stryker-net 4.14.1
/// src/Stryker.RegexMutators/Stryker.RegexMutators.UnitTest/Mutators/TestHelpers.cs.
/// Helper for parsing a regex pattern and applying a single mutator across the AST.
/// </summary>
public static class TestHelpers
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Major Code Smell",
        "S1944:Inappropriate cast",
        Justification = "RegexMutatorBase<T> implements IRegexMutator in production code (Stryker.RegexMutators.Mutators); analyzer false-positive across assembly boundary.")]
    public static IEnumerable<RegexMutation> ParseAndMutate<T>(string pattern, RegexMutatorBase<T> mutator)
        where T : RegexNode
    {
        var root = new Parser(pattern).Parse().Root;
        IEnumerable<RegexNode> allNodes = [.. root.GetDescendantNodes(), root];
        return allNodes.Where(((IRegexMutator)mutator).CanHandle).OfType<T>().SelectMany(node => mutator.ApplyMutations(node, root));
    }
}
