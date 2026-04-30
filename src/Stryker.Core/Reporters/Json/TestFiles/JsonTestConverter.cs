using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json.TestFiles;

public class JsonTestConverter : JsonConverter<IJsonTest>
{
    public override IJsonTest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into the concrete type
        var jsonTest = JsonSerializer.Deserialize<JsonTest>(ref reader, options);
        return jsonTest;
    }

    public override void Write(Utf8JsonWriter writer, IJsonTest value, JsonSerializerOptions options)
    {
        // Serialize the concrete type
        JsonSerializer.Serialize(writer, (JsonTest)value, options);
    }
}
