using System.Text.Json.Serialization;

namespace Stryker.E2E.Tests.Infrastructure;

/// <summary>Thresholds section of the mutation-testing-elements report schema.</summary>
public sealed class ReportThresholds
{
    [JsonPropertyName("high")] public int High { get; init; }
    [JsonPropertyName("low")] public int Low { get; init; }
}
