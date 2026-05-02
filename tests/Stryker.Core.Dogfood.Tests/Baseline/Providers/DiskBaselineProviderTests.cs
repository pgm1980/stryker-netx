using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Dogfood.Tests.Reporters;
using Stryker.TestHelpers;
using Stryker.Utilities;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Baseline.Providers;

/// <summary>Sprint 78 (v2.64.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: JsonReport.Build → SourceFile ctor needs ApplicationLogging.LoggerFactory.</summary>
public class DiskBaselineProviderTests : TestBase
{
    [Fact]
    public async Task ShouldWriteToDiskAsync()
    {
        var fileSystemMock = new MockFileSystem();
        var options = new StrykerOptions
        {
            ProjectPath = "C:/Users/JohnDoe/Project/TestFolder",
        };
        var sut = new DiskBaselineProvider(options, fileSystemMock);

        await sut.Save(JsonReport.Build(options, ReportTestHelper.CreateProjectWith(), It.IsAny<ITestProjectsInfo>()), "baseline/version");

        var path = FilePathUtils.NormalizePathSeparators("C:/Users/JohnDoe/Project/TestFolder/StrykerOutput/baseline/version/stryker-report.json");

        var file = fileSystemMock.GetFile(path);
        file.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldHandleFileNotFoundExceptionOnLoadAsync()
    {
        var fileSystemMock = new MockFileSystem();
        var options = new StrykerOptions { ProjectPath = "C:/Dev" };
        var sut = new DiskBaselineProvider(options, fileSystemMock);

        var result = await sut.Load("testversion");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ShouldLoadReportFromDiskAsync()
    {
        var fileSystemMock = new MockFileSystem();
        var options = new StrykerOptions
        {
            ProjectPath = "C:/Users/JohnDoe/Project/TestFolder",
        };
        var report = JsonReport.Build(options, ReportTestHelper.CreateProjectWith(), It.IsAny<ITestProjectsInfo>());

        fileSystemMock.AddFile("C:/Users/JohnDoe/Project/TestFolder/StrykerOutput/baseline/version/stryker-report.json", report.ToJson());

        var target = new DiskBaselineProvider(options, fileSystemMock);

        var result = await target.Load("baseline/version");

        result.Should().NotBeNull();
        result!.ToJson().Should().Be(report.ToJson());
    }
}
