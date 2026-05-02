#pragma warning disable IDE0028, IDE0300, CA1859 // collection-expression breaks target-type inference; CA1859 perf-not-test-concern
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stryker.Abstractions;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Abstractions.Reporting;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Baseline.Utils;
using Stryker.Core.MutantFilters;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.Csharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.Core.Dogfood.Tests.Reporters.Json;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.MutantFilters;

/// <summary>Sprint 104 (v2.90.0) full upstream port from BaselineMutantFilterTests (replaces
/// Sprint 93 placeholder). Production matches upstream IBaselineProvider+IGitInfoProvider+
/// IBaselineMutantHelper signatures. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class BaselineMutantFilterTests : TestBase
{
    [Fact]
    public void ShouldHaveName()
    {
        var gitInfoProvider = new Mock<IGitInfoProvider>(MockBehavior.Loose);
        var baselineProviderMock = new Mock<IBaselineProvider>(MockBehavior.Loose);
        var baselineMutantHelperMock = new Mock<IBaselineMutantHelper>(MockBehavior.Loose);

        var target = new BaselineMutantFilter(new StrykerOptions(), baselineProviderMock.Object, gitInfoProvider.Object, baselineMutantHelperMock.Object) as IMutantFilter;

        target.DisplayName.Should().Be("baseline filter");
    }

    [Fact]
    public void GetBaseline_UsesBaselineFallbackVersion_WhenReportForCurrentVersionNotFound()
    {
        var branchName = "refs/heads/master";
        var baselineProvider = new Mock<IBaselineProvider>();
        var gitInfoProvider = new Mock<IGitInfoProvider>();

        var options = new StrykerOptions
        {
            WithBaseline = true,
            DashboardApiKey = "Acces_Token",
            ProjectName = "github.com/JohnDoe/project",
            ProjectVersion = "version/human/readable",
            Reporters = new[] { Reporter.Dashboard },
            FallbackVersion = "fallback/version",
        };

        var inputComponent = new Mock<IReadOnlyProjectComponent>().Object;
        var jsonReport = JsonReport.Build(options, inputComponent, It.IsAny<TestProjectsInfo>());

        gitInfoProvider.Setup(x => x.GetCurrentBranchName()).Returns(branchName);
        baselineProvider.Setup(x => x.Load($"baseline/{branchName}")).Returns(Task.FromResult<IJsonReport?>(null));
        baselineProvider.Setup(x => x.Load($"baseline/{options.FallbackVersion}")).Returns(Task.FromResult<IJsonReport?>(jsonReport));

        _ = new BaselineMutantFilter(options, baselineProvider.Object, gitInfoProvider.Object);

        baselineProvider.Verify(x => x.Load($"baseline/{branchName}"), Times.Once);
        baselineProvider.Verify(x => x.Load($"baseline/{options.FallbackVersion}"), Times.Once);
        baselineProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetBaseline_UsesFallbackVersion_WhenBaselineFallbackVersionNotFound()
    {
        var branchName = "refs/heads/master";
        var baselineProvider = new Mock<IBaselineProvider>();
        var gitInfoProvider = new Mock<IGitInfoProvider>();

        var options = new StrykerOptions
        {
            WithBaseline = true,
            DashboardApiKey = "Acces_Token",
            ProjectName = "github.com/JohnDoe/project",
            ProjectVersion = "version/human/readable",
            Reporters = new[] { Reporter.Dashboard },
            FallbackVersion = "fallback/version",
        };

        var inputComponent = new Mock<IReadOnlyProjectComponent>().Object;
        var jsonReport = JsonReport.Build(options, inputComponent, It.IsAny<TestProjectsInfo>());

        gitInfoProvider.Setup(x => x.GetCurrentBranchName()).Returns(branchName);
        baselineProvider.Setup(x => x.Load(branchName)).Returns(Task.FromResult<IJsonReport?>(null));
        baselineProvider.Setup(x => x.Load($"baseline/{options.FallbackVersion}")).Returns(Task.FromResult<IJsonReport?>(null));
        baselineProvider.Setup(x => x.Load(options.FallbackVersion)).Returns(Task.FromResult<IJsonReport?>(jsonReport));

        _ = new BaselineMutantFilter(options, baselineProvider.Object, gitInfoProvider.Object);

        baselineProvider.Verify(x => x.Load($"baseline/{branchName}"), Times.Once);
        baselineProvider.Verify(x => x.Load($"baseline/{options.FallbackVersion}"), Times.Once);
        baselineProvider.Verify(x => x.Load(options.FallbackVersion), Times.Once);
        baselineProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void GetBaseline_UsesCurrentVersionReport_IfReportExists()
    {
        var branchName = "refs/heads/master";
        var baselineProvider = new Mock<IBaselineProvider>();
        var gitInfoProvider = new Mock<IGitInfoProvider>();

        var options = new StrykerOptions
        {
            WithBaseline = true,
            DashboardApiKey = "Access_Token",
            ProjectName = "github.com/JohnDoe/project",
            ProjectVersion = "version/human/readable",
            Reporters = new[] { Reporter.Dashboard },
            FallbackVersion = "fallback/version",
        };

        var inputComponent = new Mock<IReadOnlyProjectComponent>().Object;
        var jsonReport = JsonReport.Build(options, inputComponent, It.IsAny<TestProjectsInfo>());

        gitInfoProvider.Setup(x => x.GetCurrentBranchName()).Returns(branchName);
        baselineProvider.Setup(x => x.Load($"baseline/{branchName}")).Returns(Task.FromResult<IJsonReport?>(jsonReport));

        _ = new BaselineMutantFilter(options, gitInfoProvider: gitInfoProvider.Object, baselineProvider: baselineProvider.Object);

        baselineProvider.Verify(x => x.Load($"baseline/{branchName}"), Times.Once);
        baselineProvider.Verify(x => x.Load($"baseline/{options.FallbackVersion}"), Times.Never);
        baselineProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public void FilterMutantsReturnAllMutantsWhenCompareToDashboardEnabledAndBaselineNotAvailable()
    {
        var baselineProvider = new Mock<IBaselineProvider>();
        var branchProvider = new Mock<IGitInfoProvider>();

        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var target = new BaselineMutantFilter(options, baselineProvider.Object, branchProvider.Object);
        var file = new CsharpFileLeaf();
        var mutants = new List<Mutant> { new(), new(), new() };

        var results = target.FilterMutants(mutants, file, options);

        results.Count().Should().Be(3);
    }

    [Fact]
    public void FilterMutants_WhenMutantSourceCodeIsNull_MutantIsReturned()
    {
        var branchProvider = new Mock<IGitInfoProvider>();
        var baselineProvider = new Mock<IBaselineProvider>();
        var baselineMutantHelper = new Mock<IBaselineMutantHelper>();

        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var file = new CsharpFileLeaf { RelativePath = "foo.cs" };
        var mutants = new List<Mutant> { new() };
        var jsonMutants = new HashSet<IJsonMutant> { new JsonMutant() };

        var jsonReportFileComponent = new MockJsonReportFileComponent("", "", jsonMutants);
        var jsonFileComponents = new Dictionary<string, ISourceFile>(System.StringComparer.Ordinal) { ["foo.cs"] = jsonReportFileComponent };
        var baseline = new MockJsonReport(null, jsonFileComponents);

        baselineProvider.Setup(mock => mock.Load(It.IsAny<string>())).Returns(Task.FromResult<IJsonReport?>(baseline));
        baselineMutantHelper.Setup(mock => mock.GetMutantSourceCode(It.IsAny<string>(), It.IsAny<JsonMutant>())).Returns(string.Empty);

        var target = new BaselineMutantFilter(options, baselineProvider.Object, branchProvider.Object, baselineMutantHelper.Object);
        var results = target.FilterMutants(mutants, file, options);

        results.Should().ContainSingle();
    }

    [Fact]
    public void FilterMutants_WhenMutantMatchesSourceCode_StatusIsSetToJsonMutant()
    {
        var branchProvider = new Mock<IGitInfoProvider>();
        var baselineProvider = new Mock<IBaselineProvider>();
        var baselineMutantHelper = new Mock<IBaselineMutantHelper>();

        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var file = new CsharpFileLeaf { RelativePath = "foo.cs" };

        var mutants = new List<IMutant> { new Mutant { ResultStatus = MutantStatus.Pending } };
        var jsonMutants = new HashSet<IJsonMutant> { new JsonMutant { Status = "Killed" } };

        var jsonReportFileComponent = new MockJsonReportFileComponent("", "", jsonMutants);
        var jsonFileComponents = new Dictionary<string, ISourceFile>(System.StringComparer.Ordinal) { ["foo.cs"] = jsonReportFileComponent };
        var baseline = new MockJsonReport(null, jsonFileComponents);

        baselineProvider.Setup(mock => mock.Load(It.IsAny<string>())).Returns(Task.FromResult<IJsonReport?>(baseline));
        baselineMutantHelper.Setup(mock => mock.GetMutantSourceCode(It.IsAny<string>(), It.IsAny<IJsonMutant>())).Returns("var foo = \"bar\";");
        baselineMutantHelper.Setup(mock => mock.GetMutantMatchingSourceCode(
            It.IsAny<IEnumerable<IMutant>>(),
            It.Is<IJsonMutant>(m => m == jsonMutants.First()),
            It.Is<string>(source => source == "var foo = \"bar\";"))).Returns(mutants).Verifiable();

        var target = new BaselineMutantFilter(options, baselineProvider.Object, branchProvider.Object, baselineMutantHelper.Object);
        var results = target.FilterMutants(mutants, file, options);

        results.Should().ContainSingle().Which.ResultStatus.Should().Be(MutantStatus.Killed);
        baselineMutantHelper.Verify();
    }

    [Fact]
    public void FilterMutants_WhenMultipleMatchingMutants_ResultIsSetToNotRun()
    {
        var branchProvider = new Mock<IGitInfoProvider>();
        var baselineProvider = new Mock<IBaselineProvider>();
        var baselineMutantHelper = new Mock<IBaselineMutantHelper>();

        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var file = new CsharpFileLeaf { RelativePath = "foo.cs" };

        var mutants = new List<IMutant>
        {
            new Mutant { ResultStatus = MutantStatus.Pending },
            new Mutant { ResultStatus = MutantStatus.Pending },
        };
        var jsonMutants = new HashSet<IJsonMutant> { new JsonMutant { Status = "Killed" } };

        var jsonReportFileComponent = new MockJsonReportFileComponent("", "", jsonMutants);
        var jsonFileComponents = new Dictionary<string, ISourceFile>(System.StringComparer.Ordinal) { ["foo.cs"] = jsonReportFileComponent };
        var baseline = new MockJsonReport(null, jsonFileComponents);

        baselineProvider.Setup(mock => mock.Load(It.IsAny<string>())).Returns(Task.FromResult<IJsonReport?>(baseline));
        baselineMutantHelper.Setup(mock => mock.GetMutantSourceCode(It.IsAny<string>(), It.IsAny<IJsonMutant>())).Returns("var foo = \"bar\";");
        baselineMutantHelper.Setup(mock => mock.GetMutantMatchingSourceCode(
            It.IsAny<IEnumerable<IMutant>>(),
            It.Is<IJsonMutant>(m => m == jsonMutants.First()),
            It.Is<string>(source => source == "var foo = \"bar\";"))).Returns(mutants).Verifiable();

        var target = new BaselineMutantFilter(options, baselineProvider.Object, branchProvider.Object, baselineMutantHelper.Object);
        var results = target.FilterMutants(mutants, file, options);

        foreach (var result in results)
        {
            result.ResultStatus.Should().Be(MutantStatus.Pending);
            result.ResultStatusReason.Should().Be("Result based on previous run was inconclusive");
        }
        results.Count().Should().Be(2);
        baselineMutantHelper.Verify();
    }

    [Fact]
    public void ShouldNotUpdateMutantsWithBaselineIfFileNotInBaseline()
    {
        var branchProvider = new Mock<IGitInfoProvider>();
        var baselineProvider = new Mock<IBaselineProvider>();
        var baselineMutantHelper = new Mock<IBaselineMutantHelper>();

        var options = new StrykerOptions { WithBaseline = true, ProjectVersion = "version" };
        var file = new CsharpFileLeaf { RelativePath = "foo.cs" };
        var mutants = new List<IMutant> { new Mutant() };
        var jsonFileComponents = new Dictionary<string, ISourceFile>(System.StringComparer.Ordinal);
        var baseline = new MockJsonReport(null, jsonFileComponents);

        baselineProvider.Setup(mock => mock.Load(It.IsAny<string>())).Returns(Task.FromResult<IJsonReport?>(baseline));

        var target = new BaselineMutantFilter(options, baselineProvider.Object, branchProvider.Object, baselineMutantHelper.Object);
        var results = target.FilterMutants(mutants, file, options);

        results.Should().ContainSingle();
    }
}
