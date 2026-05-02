#pragma warning disable CA1859, CA1852, MA0048, MA0051, IDE0028, IDE0300, IDE0301, IDE0305
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Testing;
using Stryker.Core.Mutants;
using Stryker.TestRunner.Results;
using Stryker.TestRunner.Tests;
using Stryker.TestRunner.VsTest;
using static Stryker.Abstractions.Testing.ITestRunner;

namespace Stryker.Core.Dogfood.Tests.MutationTest;

/// <summary>Sprint 127 (v3.0.14) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/MutationTest/FullRunScenario.cs.
/// Test harness that simplifies the creation of mutant+test+coverage scenarios for
/// MutationTestProcess+CoverageAnalyser tests.</summary>
internal class FullRunScenario
{
    private readonly Dictionary<int, Mutant> _mutants = new();
    private readonly Dictionary<int, TestDescription> _tests = new();
    private readonly Dictionary<int, TestIdentifierList> _coverageResult = new();
    private readonly Dictionary<int, TestIdentifierList> _failedTestsPerRun = new();
    private readonly Dictionary<Guid, List<int>> _testCoverage = new();
    private const int InitialRunId = -1;
    private OptimizationModes _mode = OptimizationModes.CoverageBasedTest | OptimizationModes.SkipUncoveredMutants;

    public TestSet TestSet { get; } = new();
    public IDictionary<int, Mutant> Mutants => _mutants;

    public Mutant CreateMutant(int id = -1)
    {
        if (id == -1)
        {
            id = _mutants.Keys.Append(-1).Max() + 1;
        }
        var mutant = new Mutant { Id = id };
        _mutants[id] = mutant;
        return mutant;
    }

    public void CreateMutants(params int[] ids)
    {
        foreach (var id in ids)
        {
            CreateMutant(id);
        }
    }

    public IEnumerable<Mutant> GetMutants() => _mutants.Values;

    public IEnumerable<Mutant> GetCoveredMutants() => _coverageResult.Keys.Select(i => _mutants[i]);

    public MutantStatus GetMutantStatus(int id) => _mutants[id].ResultStatus;

    public void DeclareCoverageForMutant(int mutantId, params int[] testIds)
    {
        _coverageResult[mutantId] = GetGuidList(testIds);
        foreach (var testId in testIds.Length == 0 ? _tests.Keys.ToArray() : testIds)
        {
            var id = Guid.Parse(_tests[testId].Id);
            if (!_testCoverage.TryGetValue(id, out var list))
            {
                _testCoverage[id] = list = new List<int>();
            }
            list.Add(mutantId);
        }
    }

    public void DeclareTestsFailingAtInit(params int[] ids) => DeclareTestsFailingWhenTestingMutant(InitialRunId, ids);

    public void SetMode(OptimizationModes mode) => _mode = mode;

    public void DeclareTestsFailingWhenTestingMutant(int id, params int[] ids)
    {
        var testsGuidList = GetGuidList(ids);
        if (!testsGuidList.IsIncludedIn(GetCoveringTests(id)))
        {
            throw new InvalidOperationException($"You tried to declare a failing test but it does not cover mutant {id}");
        }
        _failedTestsPerRun[id] = testsGuidList;
    }

    public TestDescription CreateTest(int id = -1, string? name = null, string file = "TestFile.cs")
    {
        if (id == -1)
        {
            id = _tests.Keys.Append(-1).Max() + 1;
        }
        var test = new TestDescription(Guid.NewGuid().ToString(), name ?? $"test {id}", file);
        _tests[id] = test;
        TestSet.RegisterTests(new[] { test });
        return test;
    }

    public void CreateTests(params int[] ids)
    {
        foreach (var id in ids)
        {
            CreateTest(id);
        }
    }

    public TestIdentifierList GetGuidList(params int[] ids)
    {
        var selectedIds = ids.Length > 0 ? ids.Select(i => _tests[i]) : _tests.Values;
        return new TestIdentifierList(selectedIds.Select(t => t.Id));
    }

    private ITestIdentifiers GetFailedTests(int runId)
    {
        if (_failedTestsPerRun.TryGetValue(runId, out var list))
        {
            return list;
        }
        return TestIdentifierList.NoTest();
    }

    private ITestIdentifiers GetCoveringTests(int id)
    {
        if (id == InitialRunId)
        {
            return new TestIdentifierList(_tests.Values.Select(t => t.Id));
        }
        if (!_mode.HasFlag(OptimizationModes.CoverageBasedTest))
        {
            return TestIdentifierList.EveryTest();
        }
        return _coverageResult.TryGetValue(id, out var list) ? list : TestIdentifierList.NoTest();
    }

    private TestRunResult GetRunResult(int id) => new(Enumerable.Empty<VsTestDescription>(), GetCoveringTests(id), GetFailedTests(id), TestIdentifierList.NoTest(), string.Empty, Enumerable.Empty<string>(), TimeSpan.Zero);

    public TestRunResult GetInitialRunResult() => GetRunResult(InitialRunId);

    public Mock<ITestRunner> GetTestRunnerMock()
    {
        var runnerMock = new Mock<ITestRunner>();
        var successResult = new TestRunResult(
            Enumerable.Empty<VsTestDescription>(),
            GetGuidList(),
            TestIdentifierList.NoTest(),
            TestIdentifierList.NoTest(),
            string.Empty,
            Enumerable.Empty<string>(),
            TimeSpan.Zero);
        runnerMock.Setup(x => x.DiscoverTestsAsync(It.IsAny<string>())).Returns(Task.FromResult(true));
        runnerMock.Setup(x => x.GetTests(It.IsAny<IProjectAndTests>())).Returns(TestSet);
        runnerMock.Setup(x => x.InitialTestAsync(It.IsAny<IProjectAndTests>())).Returns(Task.FromResult(GetRunResult(InitialRunId) as ITestRunResult));
        runnerMock.Setup(x => x.CaptureCoverage(It.IsAny<IProjectAndTests>()))
            .Returns(() =>
            {
                var result = new List<CoverageRunResult>(_tests.Count);
                foreach (var (guid, mutations) in _testCoverage)
                {
                    result.Add(CoverageRunResult.Create(guid.ToString(), CoverageConfidence.Normal, mutations, Enumerable.Empty<int>(), Enumerable.Empty<int>()));
                }
                return result;
            });
        runnerMock.Setup(x => x.TestMultipleMutantsAsync(It.IsAny<IProjectAndTests>(), It.IsAny<ITimeoutValueCalculator>(),
                It.IsAny<IReadOnlyList<IMutant>>(), It.IsAny<TestUpdateHandler>()))
            .Callback<IProjectAndTests, ITimeoutValueCalculator?, IReadOnlyList<IMutant>, TestUpdateHandler?>((_, _, list, update) =>
            {
                foreach (var m in list)
                {
                    update?.Invoke(list, GetFailedTests(m.Id), GetCoveringTests(m.Id), TestIdentifierList.NoTest());
                }
            })
            .Returns(Task.FromResult(successResult as ITestRunResult));
        return runnerMock;
    }
}
