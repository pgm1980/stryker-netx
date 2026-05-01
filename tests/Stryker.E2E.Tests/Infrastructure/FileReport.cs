using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>Per-file section of the mutation-testing-elements report schema.</summary>
public sealed class FileReport
{
    [JsonPropertyName("language")] public string? Language { get; init; }
    [JsonPropertyName("source")] public string? Source { get; init; }
    [JsonPropertyName("mutants")] public IList<MutantEntry> Mutants { get; init; } = [];
}
