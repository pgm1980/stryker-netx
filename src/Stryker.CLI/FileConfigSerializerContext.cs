using System.Text.Json.Serialization;

namespace Stryker.CLI;

/// <summary>
/// Sprint 2.8: System.Text.Json source-generation context for the CLI's
/// configuration model. The compiler emits cached <c>JsonTypeInfo&lt;T&gt;</c>
/// metadata at build time for <see cref="FileBasedInputOuter"/> and the
/// transitive types it references (<see cref="FileBasedInput"/>,
/// <see cref="ProjectInfo"/>, <see cref="ThresholdsConfig"/>,
/// <see cref="Since"/>, <see cref="Baseline"/>), eliminating the need for
/// runtime reflection in the JSON deserialization hot-path and making the
/// CLI bootstrap AOT-/trim-friendly.
///
/// Wired in via <c>JsonSerializerOptions.TypeInfoResolver</c> on
/// <c>FileConfigReader.DeserializeJsonOptions</c> and
/// <c>FileConfigGenerator.SerializerOptions</c>; existing options
/// (<c>ReadCommentHandling</c>, <c>WriteIndented</c>) are preserved.
/// </summary>
[JsonSerializable(typeof(FileBasedInputOuter))]
internal sealed partial class FileConfigSerializerContext : JsonSerializerContext;
