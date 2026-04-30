using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json.SourceFiles;

public sealed class JsonMutantConverter : JsonConverter<IJsonMutant>
{
    public override IJsonMutant? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into the concrete type
        var sourceFile = JsonSerializer.Deserialize<JsonMutant>(ref reader, options);
        return sourceFile;
    }

    public override void Write(Utf8JsonWriter writer, IJsonMutant value, JsonSerializerOptions options)
    {
        // Serialize the concrete type
        JsonSerializer.Serialize(writer, (JsonMutant)value, options);
    }
}
