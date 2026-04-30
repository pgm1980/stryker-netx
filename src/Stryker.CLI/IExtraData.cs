using System.Collections.Generic;
using System.Text.Json;

namespace Stryker.CLI;

public interface IExtraData
{
    // Sprint 2.8: `set` instead of `init` — required so the System.Text.Json source
    // generator does NOT treat ExtraData as a synthetic-deserialization-constructor
    // parameter (which is incompatible with [JsonExtensionData]). ExtraData is
    // populated by the deserializer via the property setter; no consumer of
    // IExtraData mutates it after construction, so the API surface change is
    // compatible in practice.
    IDictionary<string, JsonElement>? ExtraData { get; set; }
}
