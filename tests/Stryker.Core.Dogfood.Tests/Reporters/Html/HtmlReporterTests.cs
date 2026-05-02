#pragma warning disable IDE0028, IDE0300, IDE0301, CA1859, MA0051
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using FluentAssertions;
using Moq;
using Spectre.Console.Testing;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Configuration.Options;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Html;
using Stryker.Core.Reporters.Html.RealTime;
using Stryker.Core.Reporters.WebBrowserOpener;
using Stryker.Core.Dogfood.Tests.Reporters;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html;

/// <summary>Sprint 115 (v3.0.2) full upstream port from HtmlReporterTests (replaces Sprint 110
/// architectural-deferral). Production matches upstream HtmlReporter signatures. Tests assert
/// STRUCTURAL invariants (file-existence, placeholder-replacement, JSON-content presence,
/// browser-opener invocation) — direct port works. Sprint 110's deferral was overly conservative.</summary>
public class HtmlReporterTests : TestBase
{
    private readonly Mock<IRealTimeMutantHandler> _handlerMock = new();

    [Fact]
    public void ShouldWriteJsonToFile()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);

        reporter.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>());
        var reportPath = Path.Combine(options.ReportPath, "mutation-report.html");
        mockFileSystem.FileExists(reportPath).Should().BeTrue();
    }

    [Fact]
    public void ShouldReplacePlaceholdersInHtmlFile()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);

        reporter.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>());
        var reportPath = Path.Combine(options.ReportPath, "mutation-report.html");
        var fileContents = mockFileSystem.GetFile(reportPath).TextContents;

        fileContents.Should().NotContain("##REPORT_JS##");
        fileContents.Should().NotContain("##REPORT_TITLE##");
        fileContents.Should().NotContain("##REPORT_JSON##");
    }

    [Fact]
    public void ShouldSupportSpacesInPath()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = " folder \\ next level",
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);

        reporter.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), null!);
        var reportPath = Path.Combine(options.ReportPath, "mutation-report.html");
        var fileContents = mockFileSystem.GetFile(reportPath).TextContents;

        fileContents.Should().NotContain("##REPORT_JS##");
        fileContents.Should().NotContain("##REPORT_TITLE##");
        fileContents.Should().NotContain("##REPORT_JSON##");
    }

    [Fact]
    public void ShouldContainJsonInHtmlReportFile()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnAllMutantsTested(mutationTree, It.IsAny<TestProjectsInfo>());
        var reportPath = Path.Combine(options.ReportPath, "mutation-report.html");
        var fileContents = mockFileSystem.GetFile(reportPath).TextContents;

        fileContents.Should().Contain("\"thresholds\":{");
        fileContents.Should().Contain("\"high\":80");
        fileContents.Should().Contain("\"low\":60");
    }

    [Fact]
    public void ShouldOpenHtmlReportImmediatelyIfOptionIsProvided()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            ReportTypeToOpen = ReportType.Html,
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var testProjectInfo = new TestProjectsInfo(mockFileSystem) { TestProjects = Array.Empty<TestProject>() };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnMutantsCreated(mutationTree, testProjectInfo);
        var reportUri = Path.Combine(options.ReportPath, $"{options.ReportFileName}.html");

        mockProcess.Verify(m => m.Open(reportUri));
    }

    [Fact]
    public void ShouldOpenHtmlReportImmediatelyIfOptionIsProvidedAndSpacesInPath()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            ReportTypeToOpen = ReportType.Html,
            OutputPath = " folder \\ next level",
            ReportFileName = "mutation-report",
        };

        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();
        var testProjectInfo = new TestProjectsInfo(mockFileSystem) { TestProjects = Array.Empty<TestProject>() };

        reporter.OnMutantsCreated(mutationTree, testProjectInfo);
        var reportUri = Path.Combine(options.ReportPath, $"{options.ReportFileName}.html");

        mockProcess.Verify(m => m.Open(reportUri));
        mockProcess.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldCloseSseEndpointAfterReportingAllMutantsTested()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            ReportTypeToOpen = ReportType.Html,
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnAllMutantsTested(mutationTree, It.IsAny<TestProjectsInfo>());

        _handlerMock.Verify(s => s.CloseSseEndpoint());
    }

    [Fact]
    public void ShouldSendMutantEventIfOpenReportOptionIsProvided()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            ReportTypeToOpen = ReportType.Html,
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);

        reporter.OnMutantTested(new Mutant());

        _handlerMock.Verify(h => h.SendMutantTestedEvent(It.IsAny<IMutant>()));
    }

    [Fact]
    public void ShouldNotSendMutantEventIfOpenReportOptionIsProvided()
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            Thresholds = new Thresholds { High = 80, Low = 60, Break = 0 },
            OutputPath = Directory.GetCurrentDirectory(),
            ReportFileName = "mutation-report",
        };
        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);

        reporter.OnMutantTested(new Mutant());

        _handlerMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ReportType.Dashboard)]
    [InlineData(null)]
    public void ShouldNotOpenHtmlReportIfHtmlOptionIsNotProvided(ReportType? reportType)
    {
        var mockProcess = new Mock<IWebbrowserOpener>();
        var mockFileSystem = new MockFileSystem();
        var options = new StrykerOptions
        {
            ReportTypeToOpen = reportType,
            OutputPath = Directory.GetCurrentDirectory(),
        };

        var reporter = new HtmlReporter(options, mockFileSystem, browser: mockProcess.Object, mutantHandler: _handlerMock.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnAllMutantsTested(mutationTree, It.IsAny<TestProjectsInfo>());

        mockProcess.VerifyNoOtherCalls();
    }
}
