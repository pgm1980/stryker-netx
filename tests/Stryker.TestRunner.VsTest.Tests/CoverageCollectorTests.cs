using System;
using System.Collections.Generic;
using System.Diagnostics;
using FluentAssertions;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollector.InProcDataCollector;
using Moq;
using Stryker.DataCollector;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 27 (v2.14.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.VsTest.UnitTest/CoverageCollectorTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Test logic preserved 1:1; references the local <see cref="MutantControl"/>
/// mock which the upstream test-file also defined inline (public-state
/// reflection target replicating the production-injected MutantControl shape).
/// </summary>
public class CoverageCollectorTests : TestBase
{
    [Fact]
    public void ProperlyCaptureParams()
    {
        var collector = new CoverageCollector();

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(true, null, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);
        collector.TestCaseStart(new TestCaseStartArgs(new TestCase("theTest", new Uri("xunit://"), "source.cs")));
        MutantControl.CaptureCoverage.Should().BeTrue();
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void RedirectDebugAssert()
    {
        var collector = new CoverageCollector();

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(false, null, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
        collector.Initialize(mock.Object);
        collector.TestSessionStart(start);

        Debug.Write("This is lost.");
        Debug.WriteLine("This also.");

        var assert = () => Debug.Fail("test");

        assert.Should().Throw<ArgumentException>();
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void ProperlySelectMutant()
    {
        var collector = new CoverageCollector();

        var testCase = new TestCase("theTest", new Uri("xunit://"), "source.cs");
        var nonCoveringTestCase = new TestCase("theOtherTest", new Uri("xunit://"), "source.cs");
        var mutantMap = new List<(int, IEnumerable<Guid>)>
        {
            (10, new List<Guid> { testCase.Id }),
            (5, new List<Guid> { nonCoveringTestCase.Id }),
        };

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(false, mutantMap, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);
        MutantControl.ActiveMutant.Should().Be(-1);

        collector.TestCaseStart(new TestCaseStartArgs(testCase));

        MutantControl.ActiveMutant.Should().Be(10);
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void SelectMutantEarlyIfSingle()
    {
        var collector = new CoverageCollector();

        var testCase = new TestCase("theTest", new Uri("xunit://"), "source.cs");
        var mutantMap = new List<(int, IEnumerable<Guid>)>
        {
            (5, new List<Guid> { testCase.Id }),
        };

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(false, mutantMap, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);
        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);

        MutantControl.ActiveMutant.Should().Be(5);
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void ProperlyCaptureCoverage()
    {
        var collector = new CoverageCollector();

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(true, null, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);

        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);
        var testCase = new TestCase("theTest", new Uri("xunit://"), "source.cs");
        collector.TestCaseStart(new TestCaseStartArgs(testCase));
        MutantControl.HitNormal(0);
        MutantControl.HitNormal(1);
        MutantControl.HitStatic(1);
        var dataCollection = new DataCollectionContext(testCase);
        collector.TestCaseEnd(new TestCaseEndArgs(dataCollection, TestOutcome.Passed));

        mock.Verify(sink => sink.SendData(dataCollection, CoverageCollector.PropertyName, "0,1;1"), Times.Once);
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void ProperlyReportNoCoverage()
    {
        var collector = new CoverageCollector();

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(true, null, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);

        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);
        var testCase = new TestCase("theTest", new Uri("xunit://"), "source.cs");
        collector.TestCaseStart(new TestCaseStartArgs(testCase));
        var dataCollection = new DataCollectionContext(testCase);
        collector.TestCaseEnd(new TestCaseEndArgs(dataCollection, TestOutcome.Passed));

        mock.Verify(sink => sink.SendData(dataCollection, CoverageCollector.PropertyName, ";"), Times.Once);
        collector.TestSessionEnd(new TestSessionEndArgs());
    }

    [Fact]
    public void ProperlyReportLeakedMutations()
    {
        var collector = new CoverageCollector();

        var start = new TestSessionStartArgs
        {
            Configuration = CoverageCollector.GetVsTestSettings(true, null, GetType().Namespace ?? string.Empty),
        };
        var mock = new Mock<IDataCollectionSink>(MockBehavior.Loose);

        collector.Initialize(mock.Object);

        collector.TestSessionStart(start);
        var testCase = new TestCase("theTest", new Uri("xunit://"), "source.cs");
        MutantControl.HitNormal(0);
        collector.TestCaseStart(new TestCaseStartArgs(testCase));
        var dataCollection = new DataCollectionContext(testCase);
        MutantControl.HitNormal(1);
        collector.TestCaseEnd(new TestCaseEndArgs(dataCollection, TestOutcome.Passed));

        mock.Verify(sink => sink.SendData(dataCollection, CoverageCollector.PropertyName, "1;"), Times.Once);
        mock.Verify(sink => sink.SendData(dataCollection, CoverageCollector.OutOfTestsPropertyName, "0"), Times.Once);
        collector.TestSessionEnd(new TestSessionEndArgs());
    }
}

