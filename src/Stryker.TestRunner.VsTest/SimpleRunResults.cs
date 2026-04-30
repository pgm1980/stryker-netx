using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// In-memory <see cref="IRunResults"/> implementation that supports merging additional results.
/// </summary>
public sealed class SimpleRunResults : IRunResults
{
    private List<TestCase> _testsInTimeOut = [];

    /// <inheritdoc />
    public IList<TestResult> TestResults { get; } = new List<TestResult>();

    /// <inheritdoc />
    public IReadOnlyList<TestCase> TestsInTimeout => _testsInTimeOut.AsReadOnly();

    /// <summary>Initializes an empty <see cref="SimpleRunResults"/>.</summary>
    public SimpleRunResults()
    {
    }

    /// <summary>Initializes a <see cref="SimpleRunResults"/> from existing results and timed-out cases.</summary>
    public SimpleRunResults(IEnumerable<TestResult> results, IEnumerable<TestCase>? testsInTimeout)
    {
        foreach (var r in results)
        {
            TestResults.Add(r);
        }
        _testsInTimeOut = testsInTimeout?.ToList() ?? [];
    }

    /// <summary>Replaces the internal timed-out test list.</summary>
    public void SetTestsInTimeOut(ICollection<TestCase> tests) => _testsInTimeOut = tests.ToList();

    /// <summary>Merges results from another <see cref="IRunResults"/> instance into this one.</summary>
    public SimpleRunResults Merge(IRunResults other)
    {
        if (other.TestsInTimeout?.Count > 0)
        {
            if (_testsInTimeOut == null)
            {
                _testsInTimeOut = other.TestsInTimeout.ToList();
            }
            else
            {
                _testsInTimeOut.AddRange(other.TestsInTimeout);
            }
        }

        foreach (var r in other.TestResults)
        {
            TestResults.Add(r);
        }
        return this;
    }
}
