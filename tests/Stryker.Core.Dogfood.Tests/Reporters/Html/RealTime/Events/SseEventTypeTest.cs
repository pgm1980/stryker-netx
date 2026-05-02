using System;
using FluentAssertions;
using Stryker.Core.Reporters.Html.RealTime.Events;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters.Html.RealTime.Events;

/// <summary>Sprint 55 (v2.41.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class SseEventTypeTest
{
    [Fact]
    public void ShouldSerializeFinishedCorrectly() =>
        SseEventType.Finished.Serialize().Should().Be("finished");

    [Fact]
    public void ShouldSerializeMutantTestedCorrectly() =>
        SseEventType.MutantTested.Serialize().Should().Be("mutant-tested");

    [Fact]
    public void ShouldThrowWhenPassingUnknownEnum()
    {
        Action act = () => ((SseEventType)100).Serialize();
        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("Invalid SseEventType given: 100");
    }
}
