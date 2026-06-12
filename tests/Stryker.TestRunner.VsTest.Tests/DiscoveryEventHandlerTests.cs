using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 182 (issue #297b, 360°-Analyse I-07b): <c>WaitEnd</c> used a Monitor.Wait
/// without timeout — when vstest.console crashed before raising DiscoveryComplete, the
/// discovery hung forever. A bounded wait must end in the existing Aborted path.
/// </summary>
public class DiscoveryEventHandlerTests
{
    [Fact]
    public void WaitEnd_WhenDiscoveryNeverCompletes_TimesOutIntoAbortedPath()
    {
        var handler = new DiscoveryEventHandler([]);

        var completed = handler.WaitEnd(TimeSpan.FromMilliseconds(200));

        completed.Should().BeFalse("the discovery never completed");
        handler.Aborted.Should().BeTrue("a timed-out discovery must surface as aborted");
    }

    [Fact]
    public async Task WaitEnd_WhenDiscoveryCompletes_ReturnsTrue()
    {
        var handler = new DiscoveryEventHandler([]);
        var completion = Task.Run(async () =>
        {
            await Task.Delay(50).ConfigureAwait(false);
            handler.HandleDiscoveryComplete(0, null, false);
        });

        var completed = handler.WaitEnd(TimeSpan.FromSeconds(10));
        await completion;

        completed.Should().BeTrue();
        handler.Aborted.Should().BeFalse();
    }
}
