using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Stryker.CLI;

public class FileBasedInputOuter
{
    [JsonPropertyName("stryker-config")]
    [YamlMember(Alias="stryker-config")]
    public FileBasedInput? Input { get; init; }
}
