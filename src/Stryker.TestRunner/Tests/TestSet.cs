using System.Collections.Generic;
using System.Linq;
using Stryker.Abstractions.Testing;

namespace Stryker.TestRunner.Tests;

public class TestSet : ITestSet
{
    private readonly Dictionary<string, ITestDescription> _tests = new(System.StringComparer.Ordinal);
    public int Count => _tests.Count;
    public ITestDescription this[string id] => _tests[id];

    public void RegisterTests(IEnumerable<ITestDescription> tests)
    {
        foreach (var test in tests)
        {
            RegisterTest(test);
        }
    }

    public void RegisterTest(ITestDescription test) => _tests[test.Id] = test;

    public IEnumerable<ITestDescription> Extract(IEnumerable<string> ids) => ids?.Select(i => _tests[i]) ?? [];
}
