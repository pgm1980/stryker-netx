using System;
using System.Collections.Generic;
using System.Linq;
using Stryker.Abstractions.Testing;

namespace Stryker.TestRunner.VsTest;

/// <summary>
/// Aggregates per-test-case information (description, framework, run-time results) for the VsTest adapter.
/// </summary>
public sealed class VsTestDescription : IFrameworkTestDescription
{
    private readonly ICollection<ITestResult> _initialResults = [];
    private int _subCases;

    /// <summary>Initializes a new <see cref="VsTestDescription"/> wrapping the given <see cref="ITestCase"/>.</summary>
    public VsTestDescription(ITestCase testCase)
    {
        Case = testCase;
        Description = new TestDescription(testCase.Id, testCase.Name, testCase.CodeFilePath);
    }

    /// <inheritdoc />
    public TestFrameworks Framework
    {
        get
        {
            if (Case.Uri.AbsoluteUri.Contains("nunit", StringComparison.OrdinalIgnoreCase))
            {
                return TestFrameworks.NUnit;
            }
            return Case.Uri.AbsoluteUri.Contains("xunit", StringComparison.OrdinalIgnoreCase) ? TestFrameworks.xUnit : TestFrameworks.MsTest;
        }
    }

    /// <inheritdoc />
    public ITestDescription Description { get; }

    /// <inheritdoc />
    public TimeSpan InitialRunTime
    {
        get
        {
            if (Framework == TestFrameworks.xUnit)
            {
                // xUnit returns the run time for the case within each result, so the first one is sufficient
                return _initialResults.FirstOrDefault()?.Duration ?? TimeSpan.Zero;
            }

            return TimeSpan.FromTicks(_initialResults.Sum(t => t.Duration.Ticks));
        }
    }

    /// <inheritdoc />
    public string Id => Case.Id;

    /// <inheritdoc />
    public ITestCase Case { get; }

    /// <inheritdoc />
    public int NbSubCases => Math.Max(_subCases, _initialResults.Count);

    /// <inheritdoc />
    public void RegisterInitialTestResult(ITestResult result) => _initialResults.Add(result);

    /// <inheritdoc />
    public void AddSubCase() => _subCases++;

    /// <inheritdoc />
    public void ClearInitialResult() => _initialResults.Clear();
}
