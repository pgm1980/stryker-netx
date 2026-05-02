using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Exceptions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Abstractions.Testing;
using Stryker.Configuration.Options;
using Stryker.Core;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Initialisation;
using Stryker.Core.Mutants;
using Stryker.Core.MutationTest;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.SourceProjects;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters;
using Stryker.TestHelpers;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.Core.Dogfood.Tests;

/// <summary>Sprint 87 (v2.73.0) port. MSTest → xUnit, Shouldly → FluentAssertions. Block B close.</summary>
public class StrykerRunnerTests : TestBase
{
    [Fact]
    public async Task Stryker_ShouldInvokeAllProcesses()
    {
        var projectOrchestratorMock = new Mock<IProjectOrchestrator>(MockBehavior.Strict);
        var mutationTestProcessMock = new Mock<IMutationTestProcess>(MockBehavior.Strict);
        var reporterFactoryMock = new Mock<IReporterFactory>(MockBehavior.Strict);
        var reporterMock = new Mock<IReporter>(MockBehavior.Strict);
        var inputsMock = new Mock<IStrykerInputs>(MockBehavior.Strict);

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            Mutants = [new Mutant { Id = 1 }],
        });

        var projectInfo = Mock.Of<SourceProjectInfo>();
        projectInfo.ProjectContents = folder;

        var mutationTestInput = new MutationTestInput { SourceProjectInfo = projectInfo };

        inputsMock.Setup(x => x.ValidateAll()).Returns(new StrykerOptions
        {
            ProjectPath = "C:/test",
            LogOptions = new LogOptions(),
            OptimizationMode = OptimizationModes.SkipUncoveredMutants,
        });

        projectOrchestratorMock.Setup(x => x.MutateProjectsAsync(It.IsAny<StrykerOptions>(), It.IsAny<IReporter>(), It.IsAny<ITestRunner>()))
            .ReturnsAsync([mutationTestProcessMock.Object]);

        reporterFactoryMock.Setup(x => x.Create(It.IsAny<StrykerOptions>(), It.IsAny<IGitInfoProvider>())).Returns(reporterMock.Object);

        reporterMock.Setup(x => x.OnStartMutantTestRun(It.IsAny<IEnumerable<IReadOnlyMutant>>()));
        reporterMock.Setup(x => x.OnAllMutantsTested(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()));

        mutationTestProcessMock.SetupGet(x => x.Input).Returns(mutationTestInput);
        mutationTestProcessMock.Setup(x => x.GetCoverage());
        mutationTestProcessMock.Setup(x => x.TestAsync(It.IsAny<IEnumerable<IMutant>>()))
            .Returns(Task.FromResult(new StrykerRunResult(It.IsAny<StrykerOptions>(), It.IsAny<double>())));
        mutationTestProcessMock.Setup(x => x.Restore());

        var seq = new MockSequence();
        mutationTestProcessMock.InSequence(seq).Setup(x => x.FilterMutants());
        reporterMock.InSequence(seq).Setup(x => x.OnMutantsCreated(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()));

        projectOrchestratorMock.Setup(x => x.Dispose());

        var target = new StrykerRunner(reporterFactoryMock.Object, projectOrchestratorMock.Object, TestLoggerFactory.CreateLogger<StrykerRunner>());

        await target.RunMutationTestAsync(inputsMock.Object);

        projectOrchestratorMock.Verify(x => x.MutateProjectsAsync(It.Is<StrykerOptions>(x => x.ProjectPath == "C:/test"), It.IsAny<IReporter>(), It.IsAny<ITestRunner>()), Times.Once);
        mutationTestProcessMock.Verify(x => x.GetCoverage(), Times.Once);
        mutationTestProcessMock.Verify(x => x.TestAsync(It.IsAny<IEnumerable<IMutant>>()), Times.Once);
        reporterMock.Verify(x => x.OnMutantsCreated(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()), Times.Once);
        reporterMock.Verify(x => x.OnStartMutantTestRun(It.IsAny<IEnumerable<IReadOnlyMutant>>()), Times.Once);
        reporterMock.Verify(x => x.OnAllMutantsTested(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()), Times.Once);
    }

    [Fact]
    public async Task ShouldStop_WhenAllMutationsWereIgnored()
    {
        var projectOrchestratorMock = new Mock<IProjectOrchestrator>(MockBehavior.Strict);
        var mutationTestProcessMock = new Mock<IMutationTestProcess>(MockBehavior.Strict);
        var reporterFactoryMock = new Mock<IReporterFactory>(MockBehavior.Strict);
        var reporterMock = new Mock<IReporter>(MockBehavior.Strict);
        var inputsMock = new Mock<IStrykerInputs>(MockBehavior.Strict);

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            Mutants = [new Mutant { Id = 1, ResultStatus = MutantStatus.Ignored }],
        });

        var mutationTestInput = new MutationTestInput
        {
            SourceProjectInfo = new SourceProjectInfo { ProjectContents = folder },
        };

        inputsMock.Setup(x => x.ValidateAll()).Returns(new StrykerOptions
        {
            ProjectPath = "C:/test",
            OptimizationMode = OptimizationModes.None,
            LogOptions = new LogOptions(),
        });

        projectOrchestratorMock.Setup(x => x.MutateProjectsAsync(It.IsAny<StrykerOptions>(), It.IsAny<IReporter>(), It.IsAny<ITestRunner>()))
            .ReturnsAsync([mutationTestProcessMock.Object]);

        mutationTestProcessMock.Setup(x => x.FilterMutants());
        mutationTestProcessMock.SetupGet(x => x.Input).Returns(mutationTestInput);

        reporterFactoryMock.Setup(x => x.Create(It.IsAny<StrykerOptions>(), It.IsAny<IGitInfoProvider>())).Returns(reporterMock.Object);

        reporterMock.Setup(x => x.OnMutantsCreated(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()));
        reporterMock.Setup(x => x.OnStartMutantTestRun(It.IsAny<IEnumerable<IReadOnlyMutant>>()));
        reporterMock.Setup(x => x.OnAllMutantsTested(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()));

        projectOrchestratorMock.Setup(x => x.Dispose());

        var target = new StrykerRunner(reporterFactoryMock.Object, projectOrchestratorMock.Object, TestLoggerFactory.CreateLogger<StrykerRunner>());

        var result = await target.RunMutationTestAsync(inputsMock.Object);

        result.MutationScore.Should().Be(double.NaN);

        reporterMock.Verify(x => x.OnStartMutantTestRun(It.IsAny<IList<IMutant>>()), Times.Never);
        reporterMock.Verify(x => x.OnMutantTested(It.IsAny<IMutant>()), Times.Never);
        reporterMock.Verify(x => x.OnAllMutantsTested(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()), Times.Once);
    }

    [Fact]
#pragma warning disable MA0004 // ConfigureAwait — disabled because xUnit1030 forbids it in test methods
    public async Task ShouldThrow_WhenNoProjectsFound()
    {
        var projectOrchestratorMock = new Mock<IProjectOrchestrator>(MockBehavior.Strict);
        var reporterFactoryMock = new Mock<IReporterFactory>(MockBehavior.Strict);
        var reporterMock = new Mock<IReporter>(MockBehavior.Strict);
        var inputsMock = new Mock<IStrykerInputs>(MockBehavior.Strict);

        inputsMock.Setup(x => x.ValidateAll()).Returns(new StrykerOptions
        {
            ProjectPath = "C:/test",
            OptimizationMode = OptimizationModes.None,
            LogOptions = new LogOptions(),
        });

        projectOrchestratorMock.Setup(x => x.MutateProjectsAsync(It.IsAny<StrykerOptions>(), It.IsAny<IReporter>(), It.IsAny<ITestRunner>()))
            .ReturnsAsync([]);

        reporterFactoryMock.Setup(x => x.Create(It.IsAny<StrykerOptions>(), It.IsAny<IGitInfoProvider>())).Returns(reporterMock.Object);

        projectOrchestratorMock.Setup(x => x.Dispose());

        var target = new StrykerRunner(reporterFactoryMock.Object, projectOrchestratorMock.Object, TestLoggerFactory.CreateLogger<StrykerRunner>());

        var act = async () => await target.RunMutationTestAsync(inputsMock.Object);

        await act.Should().ThrowAsync<NoTestProjectsException>();
#pragma warning restore MA0004

        reporterMock.Verify(x => x.OnStartMutantTestRun(It.IsAny<IList<IMutant>>()), Times.Never);
        reporterMock.Verify(x => x.OnMutantTested(It.IsAny<IMutant>()), Times.Never);
        reporterMock.Verify(x => x.OnAllMutantsTested(It.IsAny<IReadOnlyProjectComponent>(), It.IsAny<TestProjectsInfo>()), Times.Never);
    }
}
