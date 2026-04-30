using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Options;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Tests;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// The default implementation of <see cref="ISingleRunnerFactory"/> that creates <see cref="SingleMicrosoftTestPlatformRunner"/> instances.
/// </summary>
public sealed class DefaultRunnerFactory : ISingleRunnerFactory
{
    /// <inheritdoc />
    public SingleMicrosoftTestPlatformRunner CreateRunner(
        int id,
        IDictionary<string, List<TestNode>> testsByAssembly,
        IDictionary<string, MtpTestDescription> testDescriptions,
        TestSet testSet,
        Lock discoveryLock,
        ILogger logger,
        IStrykerOptions? options = null) =>
        new(id, testsByAssembly, testDescriptions, testSet, discoveryLock, logger, options);
}
