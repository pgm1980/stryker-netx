using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.ProjectComponents;
using Stryker.Configuration.Options;
using Stryker.Core.Clients;
using Stryker.Core.Mutants;
using Stryker.Core.ProjectComponents.TestProjects;
using Stryker.Core.Reporters.Json;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.Core.Dogfood.Tests.Reporters.Json;
using Stryker.TestHelpers;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Clients;

/// <summary>Sprint 100 (v2.86.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/Clients/DashboardClientsTest.cs (replaces
/// Sprint 93 placeholder). Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Logger uses [LoggerMessage] source-gen → EnableAllLogLevels (Sprint 96 lesson).
/// HttpMessageHandler-mock pattern via Moq.Protected() preserved verbatim.</summary>
#pragma warning disable CA1063, CA1816, S3881 // simple test-fixture IDisposable, not a finalizer-pattern target
public class DashboardClientsTest : TestBase, IDisposable
{
    private static readonly StrykerOptions OptionsWithoutModule = new()
    {
        DashboardUrl = "http://www.example.com",
        DashboardApiKey = "Access_Token",
        ProjectName = "github.com/JohnDoe/project",
        ProjectVersion = "test/version",
        Reporters = [Reporter.Dashboard],
    };

    private static readonly StrykerOptions OptionsWithModule = new()
    {
        DashboardUrl = "http://www.example.com",
        DashboardApiKey = "Access_Token",
        ModuleName = "testModule",
        ProjectName = "github.com/JohnDoe/project",
        ProjectVersion = "test/version",
        Reporters = [Reporter.Dashboard],
    };

    private static readonly StrykerOptions OptionsWithEmptyModule = new()
    {
        DashboardUrl = "http://www.example.com",
        DashboardApiKey = "Access_Token",
        ModuleName = "",
        ProjectName = "github.com/JohnDoe/project",
        ProjectVersion = "test/version",
        Reporters = [Reporter.Dashboard],
    };

    private static readonly JsonMutant Mutant = new(new Mutant
    {
        Id = 1,
        Mutation = new Mutation
        {
            DisplayName = "test mutation",
            OriginalNode = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("A"))),
            ReplacementNode = SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("B"))),
        },
        ResultStatus = MutantStatus.Killed,
    });

    private readonly Mock<ILogger<DashboardClient>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly HttpClient _httpClient;
    private DashboardClient _sut;
    private bool _disposed;

    public DashboardClientsTest()
    {
        _loggerMock = new Mock<ILogger<DashboardClient>>();
        _loggerMock.EnableAllLogLevels();
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_handlerMock.Object);
        _sut = new DashboardClient(OptionsWithoutModule, _httpClient, _loggerMock.Object);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _sut.Dispose();
        _httpClient.Dispose();
        _disposed = true;
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingReport_ShouldLogAndReturnNullWhenApiDoesNotReturn200()
    {
        ArrangeHandlerReturnsBadRequest();

        var options = new StrykerOptions
        {
            DashboardUrl = "http://www.example.com/",
            DashboardApiKey = "Access_Token"
        };
        using var localHttp = new HttpClient(_handlerMock.Object);
        using var sut = new DashboardClient(options, localHttp, _loggerMock.Object);

        var result = await sut.PublishReport(new MockJsonReport(null, null), "version");

        VerifyErrorLogged();
        result.Should().BeNull();
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingReport_ShouldCallWithTheCorrectUri()
    {
        const string Href = """{"Href": "http://www.example.com/api/projectName/version"}""";
        ArrangeHandlerReturnsOk(Href);

        var result = await _sut.PublishReport(new MockJsonReport(null, null), "version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().Be("http://www.example.com/api/projectName/version");
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingReport_WithModule_ShouldCallTheCorrectUri()
    {
        const string Href = """{"Href": "http://www.example.com/api/projectName/version"}""";
        ArrangeHandlerReturnsOk(Href);

        using var localHttp = new HttpClient(_handlerMock.Object);
        using var sut = new DashboardClient(OptionsWithModule, localHttp, _loggerMock.Object);

        var result = await sut.PublishReport(new MockJsonReport(null, null), "version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version?module=testModule");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().Be("http://www.example.com/api/projectName/version");
    }

    [Fact]
    public async Task DashboardClient_ShouldNotAppendModuleIfOptionIsAnEmptyString()
    {
        const string Href = """{"Href": "http://www.example.com/api/projectName/version"}""";
        ArrangeHandlerReturnsOk(Href);

        using var localHttp = new HttpClient(_handlerMock.Object);
        using var sut = new DashboardClient(OptionsWithEmptyModule, localHttp, _loggerMock.Object);

        var result = await sut.PublishReport(new MockJsonReport(null, null), "version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().Be("http://www.example.com/api/projectName/version");
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingRealTimeReport_ShouldCallTheCorrectUri()
    {
        ArrangeHandlerReturnsOk();
        await _sut.PublishReport(new MockJsonReport(null, null), "version", true);

        var expected = new Uri("http://www.example.com/api/real-time/github.com/JohnDoe/project/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(message => message.Method == HttpMethod.Put && message.RequestUri == expected),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingRealTimeReport_WithModule_ShouldCallTheCorrectUri()
    {
        ArrangeHandlerReturnsOk();
        _sut.Dispose();
        using var localHttp = new HttpClient(_handlerMock.Object);
        _sut = new DashboardClient(OptionsWithModule, localHttp, _loggerMock.Object);

        await _sut.PublishReport(new MockJsonReport(null, null), "version", true);

        var expected = new Uri("http://www.example.com/api/real-time/github.com/JohnDoe/project/version?module=testModule");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(message => message.Method == HttpMethod.Put && message.RequestUri == expected),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DashboardClient_WhilePullingReport_ShouldCallWithTheCorrectUri()
    {
        var readonlyInputComponent = new Mock<IReadOnlyProjectComponent>(MockBehavior.Loose).Object;
        var jsonReport = JsonReport.Build(OptionsWithoutModule, readonlyInputComponent, It.IsAny<TestProjectsInfo>());
        var json = jsonReport.ToJson();

        ArrangeHandlerReturnsOk(json);

        var result = await _sut.PullReport("version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().NotBeNull();
        result!.ToJson().Should().Be(json);
    }

    [Fact]
    public async Task DashboardClient_WhilePullingReport_WithModule_ShouldCallWithTheCorrectUri()
    {
        var readonlyInputComponent = new Mock<IReadOnlyProjectComponent>(MockBehavior.Loose).Object;
        var jsonReport = JsonReport.Build(OptionsWithModule, readonlyInputComponent, It.IsAny<TestProjectsInfo>());
        var json = jsonReport.ToJson();
        using var localHttp = new HttpClient(_handlerMock.Object);
        using var sut = new DashboardClient(OptionsWithModule, localHttp, _loggerMock.Object);

        ArrangeHandlerReturnsOk(json);

        var result = await sut.PullReport("version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version?module=testModule");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().NotBeNull();
        result!.ToJson().Should().Be(json);
    }

    [Fact]
    public async Task DashboardClient_WhilePullingReport_ShouldLogAndReturnNullWhenApiDoesNotReturn200()
    {
        ArrangeHandlerReturnsBadRequest();

        var result = await _sut.PullReport("version");

        var expectedUri = new Uri("http://www.example.com/api/reports/github.com/JohnDoe/project/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri == expectedUri),
            ItExpr.IsAny<CancellationToken>());
        result.Should().BeNull();
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingBatchOnce_ShouldNotCallApi()
    {
        await _sut.PublishMutantBatch(Mutant);

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DashBoardClient_WhilePublishingBatch_ShouldCallWithTheCorrectUri()
    {
        ArrangeHandlerReturnsOk();

        for (var i = 0; i <= 10; i++)
        {
            await _sut.PublishMutantBatch(Mutant);
        }

        var expected = new Uri("http://www.example.com/api/real-time/github.com/JohnDoe/project/test/version");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(message => message.Method == HttpMethod.Post && message.RequestUri == expected),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingBatch_ShouldLogErrorWhenApiDoesNotReturn200()
    {
        ArrangeHandlerReturnsBadRequest();

        for (var i = 0; i <= 10; i++)
        {
            await _sut.PublishMutantBatch(Mutant);
        }

        VerifyErrorLogged();
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingFinishedEvent_ShouldLogErrorWhenApiDoesNotReturn200()
    {
        ArrangeHandlerReturnsBadRequest();

        await _sut.PublishFinished();

        VerifyErrorLogged();
    }

    [Fact]
    public async Task DashboardClient_WhilePublishingFinishedEvent_ShouldEmptyBatchBeforeSendingFinished()
    {
        ArrangeHandlerReturnsOk();

        await _sut.PublishMutantBatch(Mutant);
        await _sut.PublishFinished();

        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
            ItExpr.IsAny<CancellationToken>());
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Delete),
            ItExpr.IsAny<CancellationToken>());
    }

    private void ArrangeHandlerReturnsOk(string data = "") =>
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(data, Encoding.UTF8, "application/json"),
            })
            .Verifiable();

    private void ArrangeHandlerReturnsBadRequest() =>
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Error message", Encoding.UTF8, "text/html"),
            })
            .Verifiable();

    private void VerifyErrorLogged() =>
#pragma warning disable CA1873 // Moq.Verify expression — not an actual log call
        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
#pragma warning restore CA1873
}
#pragma warning restore CA1063, CA1816, S3881
