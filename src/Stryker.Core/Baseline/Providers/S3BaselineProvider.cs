using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Baseline;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Baseline.Providers;

public sealed class S3BaselineProvider : IBaselineProvider
{
    private const string DefaultOutputDirectoryName = "StrykerOutput";
    private const string StrykerReportName = "stryker-report.json";

    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3BaselineProvider> _logger;
    private readonly string _bucketName;
    private readonly string _outputPath;

    public S3BaselineProvider(
        IStrykerOptions options,
        IAmazonS3? s3Client = null,
        ILogger<S3BaselineProvider>? logger = null,
        Func<AmazonS3Config, IAmazonS3>? s3ClientFactory = null)
    {
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<S3BaselineProvider>();
        _bucketName = options.S3BucketName ?? string.Empty;
        _outputPath = string.IsNullOrWhiteSpace(options.ProjectName)
            ? DefaultOutputDirectoryName
            : $"{DefaultOutputDirectoryName}/{options.ProjectName}";

        var region = options.S3Region ?? string.Empty;
        var endpoint = options.S3Endpoint ?? string.Empty;
        _s3Client = s3Client ?? (s3ClientFactory?.Invoke(CreateS3Config(region, endpoint)) ?? CreateS3Client(region, endpoint));
    }

    public async Task<IJsonReport?> Load(string version)
    {
        var key = BuildObjectKey(version);

        try
        {
            using var response = await _s3Client.GetObjectAsync(_bucketName, key).ConfigureAwait(false);
            return await response.ResponseStream.DeserializeJsonReportAsync().ConfigureAwait(false);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogDebug(ex, "No baseline was found at s3://{BucketName}/{Key}", _bucketName, key);
            return null;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load baseline from S3: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            return null;
        }
    }

    public async Task Save(IJsonReport report, string version)
    {
        var key = BuildObjectKey(version);

        try
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(report.ToJson()));
            await using (stream.ConfigureAwait(false))
            {
                var request = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = "application/json"
                };

                await _s3Client.PutObjectAsync(request).ConfigureAwait(false);
                _logger.LogDebug("Saved baseline report to s3://{BucketName}/{Key}", _bucketName, key);
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to save baseline to S3: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
        }
    }

    private string BuildObjectKey(string version) => $"{_outputPath}/{version}/{StrykerReportName}";

    private static AmazonS3Client CreateS3Client(string region, string endpoint)
        => new(CreateS3Config(region, endpoint));

    private static AmazonS3Config CreateS3Config(string region, string endpoint)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrWhiteSpace(region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            config.ServiceURL = endpoint;
        }

        return config;
    }
}
