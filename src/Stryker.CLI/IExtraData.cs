using System.Collections.Generic;
using System.Text.Json;

namespace Stryker.CLI;

public interface IExtraData
{
    IDictionary<string, JsonElement>? ExtraData { get; init; }
}
