using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json;

/// <summary>
/// v2.3.0 (Sprint 16): source-generated <see cref="JsonSerializerContext"/>
/// for the JsonReport family. Replaces the v1.x reflection-based
/// <c>JsonSerializer.Serialize&lt;T&gt;(value, options)</c> path with a
/// pre-baked <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>
/// for the entry types <see cref="JsonReport"/> + <see cref="IJsonReport"/>,
/// reducing the runtime-reflection dependency that prevented full AOT/trim
/// compatibility of the JsonReport reporter.
///
/// <para><b>Hybrid design.</b> Custom polymorphic converters
/// (<see cref="SourceFiles.SourceFileConverter"/> for
/// <c>IDictionary&lt;string, ISourceFile&gt;</c>,
/// <see cref="SourceFiles.JsonMutantConverter"/> for
/// <c>IJsonMutant</c>, etc.) cannot be declared on the source-gen attribute
/// because <c>SYSLIB1220</c> rejects them — they handle interface dispatch
/// that source-gen can't validate at compile time. Instead the converters are
/// attached to the runtime <c>Options</c> instance in
/// <see cref="JsonReportSerialization"/>. The source-gen context provides
/// JsonTypeInfo for the entry types; custom converters continue to handle
/// the polymorphic interface-typed properties.</para>
///
/// <para>Net effect: marked AOT-trim-progress, not AOT-trim-complete. Full
/// AOT-trim would require flattening <c>IJsonReport</c> / <c>ISourceFile</c>
/// / <c>IJsonMutant</c> to concrete types — out of scope for v2.3.0.</para>
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(JsonReport))]
[JsonSerializable(typeof(IJsonReport))]
internal sealed partial class JsonReportSerializerContext : JsonSerializerContext
{
}
