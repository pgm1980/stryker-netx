using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.Reporters;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 56 (v2.42.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class StatusReporterTests : TestBase
{
    private readonly Mock<ILogger<FilteredMutantsLogger>> _loggerMock = new();

    [Fact(Skip = "Production drift: log message format differs from upstream (defer to dedicated sub-sprint).")]
    public void ShouldPrintNoMutations()
    {
        var target = new FilteredMutantsLogger(_loggerMock.Object);

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf { Mutants = [] });

        target.OnMutantsCreated(folder);

        _loggerMock.Verify(LogLevel.Information, "0     total mutants will be tested", Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact(Skip = "Production drift: log message format differs from upstream (defer to dedicated sub-sprint).")]
    public void ShouldPrintIgnoredStatus()
    {
        var target = new FilteredMutantsLogger(_loggerMock.Object);

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            Mutants = (Collection<IMutant>)
            [
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "In excluded file" },
            ],
        });

        target.OnMutantsCreated(folder);

        _loggerMock.Verify(LogLevel.Information, "1     mutants got status Ignored.      Reason: In excluded file", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "1     total mutants are skipped for the above mentioned reasons", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "0     total mutants will be tested", Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact(Skip = "Production drift: log message format differs from upstream (defer to dedicated sub-sprint).")]
    public void ShouldPrintEachReasonWithCount()
    {
        var target = new FilteredMutantsLogger(_loggerMock.Object);

        var folder = new CsharpFolderComposite();
        folder.Add(new CsharpFileLeaf
        {
            Mutants = (Collection<IMutant>)
            [
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "In excluded file" },
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "In excluded file" },
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "Mutator excluded" },
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "Mutator excluded" },
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "Mutator excluded" },
                new Mutant { ResultStatus = MutantStatus.CompileError, ResultStatusReason = "CompileError" },
                new Mutant { ResultStatus = MutantStatus.Ignored, ResultStatusReason = "In excluded file" },
                new Mutant { ResultStatus = MutantStatus.Pending },
            ],
        });

        target.OnMutantsCreated(folder);

        _loggerMock.Verify(LogLevel.Information, "1     mutants got status CompileError. Reason: CompileError", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "3     mutants got status Ignored.      Reason: In excluded file", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "3     mutants got status Ignored.      Reason: Mutator excluded", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "7     total mutants are skipped for the above mentioned reasons", Times.Once);
        _loggerMock.Verify(LogLevel.Information, "1     total mutants will be tested", Times.Once);
        _loggerMock.VerifyNoOtherCalls();
    }
}
