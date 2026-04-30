using Stryker.TestRunner.MicrosoftTestPlatform.Models;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// Concrete <see cref="ResponseListener"/> for streaming <see cref="TestNodeUpdate"/> messages.
/// </summary>
public sealed class TestNodeUpdatesResponseListener(Guid requestId, Func<TestNodeUpdate[], Task> action)
    : ResponseListener(requestId)
{
    /// <inheritdoc />
    public override async Task OnMessageReceiveAsync(object message)
        => await action((TestNodeUpdate[])message).ConfigureAwait(false);
}
