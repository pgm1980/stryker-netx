using Moq;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters;
using Stryker.Core.Reporters.Json;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 55 (v2.41.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BaselineReporterTests
{
    [Fact]
    public void Doesnt_Use_ProjectVersion_When_CurrentBranch_Is_Not_Null()
    {
        var gitInfoProvider = new Mock<IGitInfoProvider>();
        var baselineProvider = new Mock<IBaselineProvider>();

        var readOnlyInputComponent = new Mock<IReadOnlyProjectComponent>(MockBehavior.Loose);
        readOnlyInputComponent.Setup(s => s.FullPath).Returns("/home/usr/dev/project");

        var options = new StrykerOptions
        {
            ProjectVersion = "new-feature",
            SinceTarget = "master",
            WithBaseline = true,
        };

        gitInfoProvider.Setup(x => x.GetCurrentBranchName()).Returns("new-feature");

        var target = new BaselineReporter(options, baselineProvider.Object, gitInfoProvider.Object);

        target.OnAllMutantsTested(readOnlyInputComponent.Object, It.IsAny<TestProjectsInfo>());

        baselineProvider.Verify(x => x.Save(It.IsAny<JsonReport>(), It.Is<string>(s => string.Equals(s, "baseline/new-feature", System.StringComparison.Ordinal))), Times.Once);
        baselineProvider.Verify(x => x.Save(It.IsAny<JsonReport>(), It.Is<string>(s => string.Equals(s, "new-feature", System.StringComparison.Ordinal))), Times.Never);
    }
}
