using System;

namespace Stryker.Core.Reporters.Html.RealTime.Events;

public static class SseEventTypeExtensions
{
    public static string Serialize(this SseEventType source) =>
        source switch
        {
            SseEventType.MutantTested => "mutant-tested",
            SseEventType.Finished => "finished",
            _ => throw new ArgumentException($"Invalid {nameof(SseEventType)} given: {source}", nameof(source)),
        };
}
