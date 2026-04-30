using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Stryker.CLI;

public class ProjectInfo : IExtraData
{
    // Parameterless ctor explicitly tagged so the System.Text.Json source generator
    // uses property-setter deserialization. Without it, the generator synthesises a
    // ctor taking every init property as a parameter, which is incompatible with
    // [JsonExtensionData] (it cannot bind to a constructor parameter).
    [JsonConstructor]
    public ProjectInfo() { }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("module")]
    public string? Module { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtraData { get; set; }
}
