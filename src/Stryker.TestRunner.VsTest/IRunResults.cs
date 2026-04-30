using System.Collections.Generic;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// Aggregates VsTest run results plus the test cases that timed out.
/// </summary>
public interface IRunResults
{
    /// <summary>Test results captured during the run.</summary>
    IList<TestResult> TestResults { get; }

    /// <summary>Test cases that did not complete within the configured timeout.</summary>
    IReadOnlyList<TestCase> TestsInTimeout { get; }
}
