using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json.TestFiles;

public class JsonTestFileConverter : JsonConverter<IJsonTestFile>
{
    public override IJsonTestFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into the concrete type
        var jsonTestFile = JsonSerializer.Deserialize<JsonTestFile>(ref reader, options);
        return jsonTestFile;
    }

    public override void Write(Utf8JsonWriter writer, IJsonTestFile value, JsonSerializerOptions options)
    {
        // Serialize the concrete type
        JsonSerializer.Serialize(writer, (JsonTestFile)value, options);
    }
}
