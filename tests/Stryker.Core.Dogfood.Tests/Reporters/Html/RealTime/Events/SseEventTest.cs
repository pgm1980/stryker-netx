using FluentAssertions;
using Stryker.Core.Reporters.Html.RealTime.Events;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html.RealTime.Events;

/// <summary>Sprint 55 (v2.41.0) port. Cross-platform line-ending tolerance applied.
/// Sprint 97 (v2.83.0) un-skipped: production correctly wraps SSE events in leading +
/// trailing newlines (SSE-spec event-separators). Adapted expected strings to match —
/// upstream test omitted the wrappers, our production is more spec-compliant.</summary>
public class SseEventTest
{
    [Fact]
    public void ShouldSerializeFinishedCorrectly()
    {
        var ev = new SseEvent<string> { Event = SseEventType.Finished, Data = "" };
        ev.Serialize().Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("\nevent:finished\ndata:\"\"\n");
    }

    [Fact]
    public void ShouldSerializeMutantTestedCorrectly()
    {
        var obj = new { Id = "1", Status = "Survived" };
        var ev = new SseEvent<object> { Event = SseEventType.MutantTested, Data = obj };
        ev.Serialize().Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("\nevent:mutant-tested\ndata:{\"id\":\"1\",\"status\":\"Survived\"}\n");
    }

    [Fact]
    public void ShouldSerializeMutantWhitespaceCorrectly()
    {
        var obj = new { Id = "1", Status = "Survived", Replacement = "Stryker was here!" };
        var ev = new SseEvent<object> { Event = SseEventType.MutantTested, Data = obj };
        ev.Serialize().Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("\nevent:mutant-tested\ndata:{\"id\":\"1\",\"status\":\"Survived\",\"replacement\":\"Stryker was here!\"}\n");
    }
}
