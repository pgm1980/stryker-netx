using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class Baseline : IExtraData
{
    // See ProjectInfo: explicit parameterless ctor required for source-gen +
    // [JsonExtensionData] interaction.
    [JsonConstructor]
    public Baseline() { }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("provider")]
    public string? Provider { get; init; }

    [JsonPropertyName("azure-fileshare-url")]
    public string? AzureFileShareUrl { get; init; }

    [JsonPropertyName("s3-bucket-name")]
    public string? S3BucketName { get; init; }

    [JsonPropertyName("s3-endpoint")]
    public string? S3Endpoint { get; init; }

    [JsonPropertyName("s3-region")]
    public string? S3Region { get; init; }

    [JsonPropertyName("fallback-version")]
    public string? FallbackVersion { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; set; }
}
