using System.Text.Json.Serialization;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>Per-mutant section of the mutation-testing-elements report schema.</summary>
public sealed class MutantEntry
{
    [JsonPropertyName("id")] public string? Id { get; init; }
    [JsonPropertyName("mutatorName")] public string? MutatorName { get; init; }
    [JsonPropertyName("replacement")] public string? Replacement { get; init; }
    [JsonPropertyName("status")] public string? Status { get; init; }
}
