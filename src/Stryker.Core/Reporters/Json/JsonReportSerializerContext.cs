using System.Collections.Generic;
using System.Text.Json.Serialization;
using Stryker.Abstractions.Reporting;
using Stryker.Core.Reporters.Json.SourceFiles;
using Stryker.Core.Reporters.Json.TestFiles;

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
/// <para>
/// v3.2.8 (Sprint 154 / ADR-034): full AOT-trim. The source-gen context now
/// covers ALL concrete types touched by the JsonReport pipeline — not just
/// the entry types. The 6 polymorphic interface converters
/// (<see cref="SourceFiles.SourceFileConverter"/>,
/// <see cref="SourceFiles.JsonMutantConverter"/>,
/// <see cref="LocationConverter"/>,
/// <see cref="PositionConverter"/>,
/// <see cref="TestFiles.JsonTestFileConverter"/>,
/// <see cref="TestFiles.JsonTestConverter"/>) delegate to
/// <c>JsonSerializer.{Serialize,Deserialize}&lt;TConcrete&gt;</c> on
/// <see cref="SourceFile"/>, <see cref="JsonMutant"/>, <see cref="Location"/>,
/// <see cref="Position"/>, <see cref="JsonTestFile"/>, <see cref="JsonTest"/>
/// respectively. Each of those has a <see cref="JsonSerializableAttribute"/>
/// here, so the resolver finds source-gen <c>JsonTypeInfo</c> for them.
/// </para>
///
/// <para><b>Hybrid design preserved.</b> Custom polymorphic converters
/// continue to handle interface dispatch (SYSLIB1220 rejects them on the
/// source-gen attribute). The converters are attached to the runtime
/// <c>Options</c> instance in <see cref="JsonReportSerialization"/>. With ALL
/// concrete types now in source-gen, <see cref="JsonReportSerialization"/>
/// no longer needs the <c>DefaultJsonTypeInfoResolver</c> reflection-fallback
/// in its <c>JsonTypeInfoResolver.Combine</c> call — full AOT-trim is now
/// achievable for the JsonReport pipeline.
/// </para>
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(JsonReport))]
[JsonSerializable(typeof(IJsonReport))]
// Sprint 154 ADR-034 — concrete types for the polymorphic converter chain:
[JsonSerializable(typeof(SourceFile))]
[JsonSerializable(typeof(JsonMutant))]
[JsonSerializable(typeof(Location))]
[JsonSerializable(typeof(Position))]
[JsonSerializable(typeof(JsonTestFile))]
[JsonSerializable(typeof(JsonTest))]
// Concrete dictionary types used by JsonReport.Files / .TestFiles:
[JsonSerializable(typeof(Dictionary<string, SourceFile>))]
[JsonSerializable(typeof(Dictionary<string, JsonTestFile>))]
[JsonSerializable(typeof(Dictionary<string, int>))]
// Sprint 156 ADR-038 supplementary (Sprint-154 follow-up): DashboardClient
// batch publishing uses JsonReportSerialization.Options for `List<IJsonMutant>`.
// Without these registrations, PostAsJsonAsync(_batch, options) throws
// NotSupportedException at runtime when Reporter.Dashboard is active. The
// JsonReportSerializationDashboardBatchTests test is the regression-prevention
// guard so this gap doesn't recur.
[JsonSerializable(typeof(IJsonMutant))]
[JsonSerializable(typeof(List<IJsonMutant>))]
internal sealed partial class JsonReportSerializerContext : JsonSerializerContext
{
}
