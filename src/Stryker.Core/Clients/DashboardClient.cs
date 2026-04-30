using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stryker.Abstractions;
using Stryker.Abstractions.Options;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json;
using Stryker.Utilities.Logging;

namespace Stryker.Core.Clients;

public class DashboardClient : IDashboardClient, IDisposable
{
    private const int MutantBatchSize = 10;

    private readonly IStrykerOptions _options;
    private readonly ILogger<DashboardClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly List<IJsonMutant> _batch = new();
    private bool _disposed;

    public DashboardClient(IStrykerOptions options, HttpClient? httpClient = null, ILogger<DashboardClient>? logger = null)
    {
        _options = options;
        _logger = logger ?? ApplicationLogging.LoggerFactory.CreateLogger<DashboardClient>();
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.DashboardApiKey);
            _ownsHttpClient = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing && _ownsHttpClient)
        {
            _httpClient.Dispose();
        }
        _disposed = true;
    }

    public async Task<string?> PublishReport(IJsonReport report, string version, bool realTime = false)
    {
        var url = GetUrl(version, realTime);

        _logger.LogDebug("Sending PUT to {DashboardUrl}", url);

        try
        {
            using var response = await _httpClient.PutAsJsonAsync(url, report, JsonReportSerialization.Options).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<DashboardResult>().ConfigureAwait(false);
            return result?.Href;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to upload report to the dashboard at {DashboardUrl}", url);
            return null;
        }
    }

    public async Task PublishMutantBatch(IJsonMutant mutant)
    {
        _batch.Add(mutant);
        if (_batch.Count != MutantBatchSize)
        {
            return;
        }

        var url = GetUrl(_options.ProjectVersion ?? string.Empty, true);
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, _batch, JsonReportSerialization.Options).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            _batch.Clear();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to upload mutant to the dashboard at {DashboardUrl}", url);
        }
    }

    public async Task PublishFinished()
    {
        var url = GetUrl(_options.ProjectVersion ?? string.Empty, true);

        try
        {
            if (_batch.Count != 0)
            {
                var batchResponse = await _httpClient.PostAsJsonAsync(url, _batch, JsonReportSerialization.Options).ConfigureAwait(false);
                batchResponse.EnsureSuccessStatusCode();
                _batch.Clear();
            }

            var deleteResponse = await _httpClient.DeleteAsync(url).ConfigureAwait(false);
            deleteResponse.EnsureSuccessStatusCode();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed send finished event to the dashboard at {DashboardUrl}", url);
        }
    }

    public async Task<JsonReport?> PullReport(string version)
    {
        var url = GetUrl(version, false);

        _logger.LogDebug("Sending GET to {DashboardUrl}", url);
        try
        {
            var report = await _httpClient.GetFromJsonAsync<JsonReport>(url, JsonReportSerialization.Options).ConfigureAwait(false);
            return report;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to retrieve the report at {DashboardUrl}", url);
            return null;
        }
    }

    private Uri GetUrl(string version, bool realTime)
    {
        var module = !string.IsNullOrEmpty(_options.ModuleName) ? $"?module={_options.ModuleName}" : "";
        var url = new Uri($"{_options.DashboardUrl}/api/reports/{_options.ProjectName}/{version}{module}");
        if (realTime)
        {
            url = new Uri($"{_options.DashboardUrl}/api/real-time/{_options.ProjectName}/{version}{module}");
        }

        return url;
    }

    private sealed record DashboardResult(string? Href);
}
