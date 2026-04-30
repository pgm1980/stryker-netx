using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class Since : IExtraData
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; init; }

    [JsonPropertyName("ignore-changes-in")]
    public string[]? IgnoreChangesIn { get; init; }

    [JsonPropertyName("target")]
    public string? Target { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; init; }
}
