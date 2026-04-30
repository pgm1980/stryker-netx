using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json;

public class LocationConverter : JsonConverter<ILocation>
{
    public override ILocation? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Deserialize the JSON into the concrete type
        var location = JsonSerializer.Deserialize<Location>(ref reader, options);
        return location;
    }

    public override void Write(Utf8JsonWriter writer, ILocation value, JsonSerializerOptions options)
    {
        // Serialize the concrete type
        JsonSerializer.Serialize(writer, (Location)value, options);
    }
}
