using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class ThresholdsConfig : IExtraData
{
    [JsonPropertyName("high")]
    public int? High { get; init; }

    [JsonPropertyName("low")]
    public int? Low { get; init; }

    [JsonPropertyName("break")]
    public int? Break { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; init; }
}
