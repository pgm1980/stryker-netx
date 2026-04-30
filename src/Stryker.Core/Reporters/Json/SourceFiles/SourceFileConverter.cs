using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json.SourceFiles;

public class SourceFileConverter : JsonConverter<ISourceFile>
{
    public override ISourceFile? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into the concrete type
        var sourceFile = JsonSerializer.Deserialize<SourceFile>(ref reader, options);
        return sourceFile;
    }

    public override void Write(Utf8JsonWriter writer, ISourceFile value, JsonSerializerOptions options)
    {
        // Serialize the concrete type
        JsonSerializer.Serialize(writer, (SourceFile)value, options);
    }
}
