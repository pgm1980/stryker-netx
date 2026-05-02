using System.Threading.Tasks;
using Moq;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Clients;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Dogfood.Tests.Reporters;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Baseline.Providers;

/// <summary>Sprint 78 (v2.64.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Production drift: IDashboardClient.PublishReport accepts IJsonReport (interface), not JsonReport (Sprint 65 lesson).
/// Inherits TestBase: JsonReport.Build → SourceFile ctor needs ApplicationLogging.LoggerFactory.</summary>
public class DashboardBaselineProviderTests : TestBase
{
    [Fact]
    public async Task Load_Calls_DashboardClient_With_version()
    {
        var strykerOptions = new StrykerOptions();
        var dashboardClient = new Mock<IDashboardClient>();

        dashboardClient.Setup(x => x.PullReport(It.IsAny<string>()));

        var target = new DashboardBaselineProvider(strykerOptions, dashboardClient.Object);

        await target.Load("version");

        dashboardClient.Verify(client => client.PullReport(It.Is<string>(x => x == "version")), Times.Once);
    }

    [Fact]
    public async Task Save_Calls_DashboardClient_With_version()
    {
        var strykerOptions = new StrykerOptions();
        var dashboardClient = new Mock<IDashboardClient>();

        dashboardClient.Setup(x => x.PublishReport(It.IsAny<IJsonReport>(), It.IsAny<string>(), false));

        var target = new DashboardBaselineProvider(strykerOptions, dashboardClient.Object);

        await target.Save(JsonReport.Build(strykerOptions, ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>()), "version");

        dashboardClient.Verify(client => client.PublishReport(It.IsAny<IJsonReport>(), It.Is<string>(x => x == "version"), false), Times.Once);
    }
}
