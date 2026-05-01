using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.Core.Reporters.Json.TestFiles;

namespace Stryker.Core.Reporters.Json;

public static class JsonReportSerialization
{
    /// <summary>
    /// v2.3.0 (Sprint 16): hybrid serialization options. The
    /// <see cref="JsonReportSerializerContext"/> source-gen provides
    /// <c>JsonTypeInfo</c> for the entry types (<see cref="JsonReport"/>,
    /// <see cref="IJsonReport"/>) — eliminating reflection there. Custom
    /// polymorphic converters
    /// (<see cref="SourceFileConverter"/>, <see cref="JsonMutantConverter"/>,
    /// <see cref="LocationConverter"/>, <see cref="PositionConverter"/>,
    /// <see cref="JsonTestFileConverter"/>, <see cref="JsonTestConverter"/>)
    /// handle the polymorphic interface-typed properties at runtime.
    ///
    /// Net AOT progress: source-gen for the entry-type metadata graph.
    /// Custom converters continue to use runtime reflection for interface
    /// dispatch — full AOT-trim would require flattening interface
    /// properties to concrete types (out of scope for v2.3.0).
    /// </summary>
    public static readonly JsonSerializerOptions Options = BuildOptions();

    private static JsonSerializerOptions BuildOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            TypeInfoResolver = JsonTypeInfoResolver.Combine(
                JsonReportSerializerContext.Default,
                new DefaultJsonTypeInfoResolver()),
        };
        options.Converters.Add(new SourceFileConverter());
        options.Converters.Add(new JsonMutantConverter());
        options.Converters.Add(new LocationConverter());
        options.Converters.Add(new PositionConverter());
        options.Converters.Add(new JsonTestFileConverter());
        options.Converters.Add(new JsonTestConverter());
        return options;
    }

    public static async Task<IJsonReport?> DeserializeJsonReportAsync(this Stream stream) =>
        await JsonSerializer.DeserializeAsync<JsonReport>(stream, Options).ConfigureAwait(false);

    public static async Task SerializeAsync(this IJsonReport report, Stream stream) =>
        await JsonSerializer.SerializeAsync(stream, report, Options).ConfigureAwait(false);

    public static async Task<byte[]> SerializeAsync(this IJsonReport report)
    {
        var stream = new MemoryStream();
        await using (stream.ConfigureAwait(false))
        {
            await report.SerializeAsync(stream).ConfigureAwait(false);
            return stream.ToArray();
        }
    }

    public static void Serialize(this IJsonReport report, Stream stream)
    {
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = Options.WriteIndented });
        JsonSerializer.Serialize(writer, report, Options);
    }

    public static string ToJson(this IJsonReport report) =>
        JsonSerializer.Serialize(report, Options);

    public static string ToJsonHtmlSafe(this IJsonReport report) =>
        report.ToJson().Replace("<", "<\" + \"", System.StringComparison.Ordinal);
}
