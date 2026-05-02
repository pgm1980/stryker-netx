using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Configuration.Options;
using Stryker.Core.Baseline.Providers;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Dogfood.Tests.Reporters;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Baseline.Providers;

/// <summary>Sprint 83 (v2.69.0) port. MSTest → xUnit, Shouldly → FluentAssertions.
/// Inherits TestBase: JsonReport.Build needs ApplicationLogging.LoggerFactory.
/// Sprint 96 (v2.82.0) un-skipped: root cause of "[LoggerMessage] drift" was actually
/// Mock&lt;ILogger&lt;T&gt;&gt;.IsEnabled returning false by default, which made production guards
/// `if (logger.IsEnabled(LogLevel.X))` skip the Log call entirely. Fixed by calling
/// Mock.Get(logger).EnableAllLogLevels() before any Verify(...) on logger mocks.</summary>
public class S3BaselineProviderTests : TestBase
{
    private const string BucketName = "my-stryker-bucket";

    [Fact]
    public async Task Load_Returns_Null_When_Object_Not_Found()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var logger = Mock.Of<ILogger<S3BaselineProvider>>();
        Mock.Get(logger).EnableAllLogLevels();

        s3Mock.Setup(s => s.GetObjectAsync(BucketName, "StrykerOutput/v1/stryker-report.json", default))
            .ThrowsAsync(new AmazonS3Exception("Not Found") { StatusCode = HttpStatusCode.NotFound });

        var options = new StrykerOptions { S3BucketName = BucketName };
        var provider = new S3BaselineProvider(options, s3Mock.Object, logger);

        var report = await provider.Load("v1");

        report.Should().BeNull();
        Mock.Get(logger).Verify(LogLevel.Debug, "No baseline was found at s3://my-stryker-bucket/StrykerOutput/v1/stryker-report.json");
    }

    [Fact]
    public async Task Load_Returns_Null_On_S3_Error()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var logger = Mock.Of<ILogger<S3BaselineProvider>>();
        Mock.Get(logger).EnableAllLogLevels();

        s3Mock.Setup(s => s.GetObjectAsync(BucketName, "StrykerOutput/v1/stryker-report.json", default))
            .ThrowsAsync(new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden, ErrorCode = "AccessDenied" });

        var options = new StrykerOptions { S3BucketName = BucketName };
        var provider = new S3BaselineProvider(options, s3Mock.Object, logger);

        var report = await provider.Load("v1");

        report.Should().BeNull();
        Mock.Get(logger).Verify(LogLevel.Warning, "Failed to load baseline from S3: AccessDenied - Access Denied");
    }

    [Fact]
    public async Task Load_Returns_Report_When_Object_Exists()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var options = new StrykerOptions { S3BucketName = BucketName };

        var json = JsonReport.Build(new StrykerOptions(), ReportTestHelper.CreateProjectWith(), It.IsAny<ITestProjectsInfo>()).ToJson();
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var response = new GetObjectResponse { ResponseStream = responseStream };
        s3Mock.Setup(s => s.GetObjectAsync(BucketName, "StrykerOutput/v1/stryker-report.json", default))
            .ReturnsAsync(response);

        var provider = new S3BaselineProvider(options, s3Mock.Object);
        var report = await provider.Load("v1");

        report.Should().NotBeNull();
        s3Mock.VerifyAll();
    }

    [Fact]
    public async Task Load_Uses_ProjectName_In_Key()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var options = new StrykerOptions { S3BucketName = BucketName, ProjectName = "MyProject" };

        var json = JsonReport.Build(new StrykerOptions(), ReportTestHelper.CreateProjectWith(), It.IsAny<ITestProjectsInfo>()).ToJson();
        var responseStream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var response = new GetObjectResponse { ResponseStream = responseStream };
        s3Mock.Setup(s => s.GetObjectAsync(BucketName, "StrykerOutput/MyProject/v1/stryker-report.json", default))
            .ReturnsAsync(response);

        var provider = new S3BaselineProvider(options, s3Mock.Object);
        var report = await provider.Load("v1");

        report.Should().NotBeNull();
        s3Mock.VerifyAll();
    }

    [Fact]
    public async Task Save_Uploads_Report()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var logger = Mock.Of<ILogger<S3BaselineProvider>>();
        Mock.Get(logger).EnableAllLogLevels();
        var options = new StrykerOptions { S3BucketName = BucketName };

        s3Mock.Setup(s => s.PutObjectAsync(
                It.Is<PutObjectRequest>(r =>
                    r.BucketName == BucketName &&
                    r.Key == "StrykerOutput/v1/stryker-report.json" &&
                    r.ContentType == "application/json"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutObjectResponse());

        var report = JsonReport.Build(new StrykerOptions(), ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>());

        var provider = new S3BaselineProvider(options, s3Mock.Object, logger);
        await provider.Save(report, "v1");

        s3Mock.VerifyAll();
        Mock.Get(logger).Verify(LogLevel.Debug, "Saved baseline report to s3://my-stryker-bucket/StrykerOutput/v1/stryker-report.json");
    }

    [Fact]
    public async Task Save_Logs_Error_On_Failure()
    {
        var s3Mock = new Mock<IAmazonS3>();
        var logger = Mock.Of<ILogger<S3BaselineProvider>>();
        Mock.Get(logger).EnableAllLogLevels();
        var options = new StrykerOptions { S3BucketName = BucketName };

        s3Mock.Setup(s => s.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Access Denied") { StatusCode = HttpStatusCode.Forbidden, ErrorCode = "AccessDenied" });

        var report = JsonReport.Build(new StrykerOptions(), ReportTestHelper.CreateProjectWith(), It.IsAny<TestProjectsInfo>());

        var provider = new S3BaselineProvider(options, s3Mock.Object, logger);
        await provider.Save(report, "v1");

        Mock.Get(logger).Verify(LogLevel.Error, "Failed to save baseline to S3: AccessDenied - Access Denied");
    }
}
