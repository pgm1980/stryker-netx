using System.Text.Json;

namespace Stryker.Core.Reporters.Html.RealTime.Events;

internal static class SseEventSerializerOptions
{
    public static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
