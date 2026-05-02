using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Clients;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.Core.Reporters.WebBrowserOpener;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 65 (v2.51.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: DashboardReporter ctor calls ApplicationLogging.LoggerFactory.CreateLogger.</summary>
public class DashboardReporterTests : TestBase
{
    private static StrykerOptions CreateBaseOptions(Reporter[] reporters, ReportType? typeToOpen = null) => new()
    {
        ReportTypeToOpen = typeToOpen,
        DashboardApiKey = "Access_Token",
        ProjectName = "github.com/JohnDoe/project",
        ProjectVersion = "version/human/readable",
        Reporters = reporters,
    };

    [Fact]
    public void ShouldUploadHumanReadableWhenCompareToDashboardEnabled()
    {
        var options = CreateBaseOptions([Reporter.Dashboard]);

        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        dashboardClientMock.Setup(x => x.PublishReport(It.IsAny<IJsonReport>(), "version/human/readable", false));

        var target = new DashboardReporter(options, dashboardClient: dashboardClientMock.Object, browser: mockProcess.Object);

        target.OnAllMutantsTested(ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>());

        dashboardClientMock.Verify(x => x.PublishReport(It.IsAny<IJsonReport>(), "version/human/readable", false), Times.Once);
    }

    [Fact]
    public void ShouldOpenDashboardReportIfOptionIsProvided()
    {
        var options = CreateBaseOptions([Reporter.Dashboard], ReportType.Dashboard);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();

        dashboardClientMock.Setup(x => x.PublishReport(It.IsAny<IJsonReport>(), "version/human/readable", true))
            .Returns(Task.FromResult<string?>("https://dashboard.com"));

        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnMutantsCreated(mutationTree, It.IsAny<TestProjectsInfo>());

        mockProcess.Verify(m => m.Open("https://dashboard.com"));
    }

    [Fact]
    public void ShouldNotOpenDashboardWithRealTimeDashboardOptionButItShouldUploadTheInitialReport()
    {
        var options = CreateBaseOptions([Reporter.RealTimeDashboard]);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();

        dashboardClientMock.Setup(x => x.PublishReport(It.IsAny<IJsonReport>(), "version/human/readable", true))
            .Returns(Task.FromResult<string?>("https://dashboard.com"));

        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnMutantsCreated(mutationTree, It.IsAny<TestProjectsInfo>());

        mockProcess.VerifyNoOtherCalls();
        dashboardClientMock.Verify(d => d.PublishReport(It.IsAny<IJsonReport>(), It.IsAny<string>(), true));
    }

    [Fact]
    public void ShouldNotDoAnythingIfNotOpeningTheDashboardAndIfNotRealTimeDashboardReporter()
    {
        var options = CreateBaseOptions([Reporter.Dashboard]);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnMutantsCreated(mutationTree, It.IsAny<TestProjectsInfo>());

        mockProcess.VerifyNoOtherCalls();
        dashboardClientMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ReportType.Html)]
    [InlineData(null)]
    public void ShouldNotOpenDashboardReportIfOptionIsProvided(ReportType? reportType)
    {
        var options = CreateBaseOptions([Reporter.Dashboard, Reporter.RealTimeDashboard], reportType);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();

        dashboardClientMock.Setup(x => x.PublishReport(It.IsAny<IJsonReport>(), "version/human/readable", false))
            .Returns(Task.FromResult<string?>("https://dashboard.com"));

        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnMutantsCreated(mutationTree, It.IsAny<TestProjectsInfo>());

        mockProcess.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldSendMutantBatchIfOpenDashboardOptionIsProvided()
    {
        var options = CreateBaseOptions([Reporter.Dashboard], ReportType.Dashboard);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();
        var mutant = mutationTree.Mutants.First();

        reporter.OnMutantTested(mutant);

        mockProcess.VerifyNoOtherCalls();
        dashboardClientMock.Verify(d => d.PublishMutantBatch(It.IsAny<JsonMutant>()));
    }

    [Fact]
    public void ShouldSendMutantBatchWithRealTimeDashboardOption()
    {
        var options = CreateBaseOptions([Reporter.RealTimeDashboard]);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();
        var mutant = mutationTree.Mutants.First();

        reporter.OnMutantTested(mutant);

        mockProcess.VerifyNoOtherCalls();
        dashboardClientMock.Verify(d => d.PublishMutantBatch(It.IsAny<JsonMutant>()));
    }

    [Fact]
    public void ShouldNotSendMutantsIfOpenDashboardOptionIsNotProvided()
    {
        var options = CreateBaseOptions([Reporter.Dashboard]);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();
        var mutant = mutationTree.Mutants.First();

        reporter.OnMutantTested(mutant);

        mockProcess.VerifyNoOtherCalls();
        dashboardClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldSendFinishedIfOpenDashboardOptionIsProvided()
    {
        var options = CreateBaseOptions([Reporter.Dashboard], ReportType.Dashboard);
        var mockProcess = new Mock<IWebbrowserOpener>();
        var dashboardClientMock = new Mock<IDashboardClient>();
        var reporter = new DashboardReporter(options, dashboardClientMock.Object, browser: mockProcess.Object);
        var mutationTree = ReportTestHelper.CreateProjectWith();

        reporter.OnAllMutantsTested(mutationTree, It.IsAny<TestProjectsInfo>());

        dashboardClientMock.Verify(d => d.PublishFinished());
    }
}
