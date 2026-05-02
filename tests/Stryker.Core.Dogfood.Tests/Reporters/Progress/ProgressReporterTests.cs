using Moq;
using Stryker.Core.Mutants;
using Stryker.Core.Reporters.Progress;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Progress;

/// <summary>Sprint 55 (v2.41.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ProgressReporterTests
{
    private readonly Mock<IProgressBarReporter> _progressBarReporter;
    private readonly ProgressReporter _progressReporter;

    public ProgressReporterTests()
    {
        _progressBarReporter = new Mock<IProgressBarReporter>();
        _progressReporter = new ProgressReporter(_progressBarReporter.Object);
    }

    [Fact]
    public void ProgressReporter_ShouldCallBothReporters_OnReportInitialState()
    {
        var mutants = new Mutant[3] { new(), new(), new() };

        _progressReporter.OnStartMutantTestRun(mutants);
        _progressBarReporter.Verify(x => x.ReportInitialState(mutants.Length), Times.Once);
    }

    [Fact]
    public void ProgressReporter_ShouldCallBothReporters_OnReportRunTest()
    {
        var mutant = new Mutant();
        _progressReporter.OnMutantTested(mutant);

        _progressBarReporter.Verify(x => x.ReportRunTest(mutant), Times.Once);
    }
}
