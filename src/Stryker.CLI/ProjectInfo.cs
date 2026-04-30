using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class ProjectInfo : IExtraData
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("module")]
    public string? Module { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; init; }
}
