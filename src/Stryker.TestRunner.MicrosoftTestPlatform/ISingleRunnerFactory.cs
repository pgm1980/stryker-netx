using Microsoft.Extensions.Logging;
using Stryker.Abstractions.Options;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Tests;

namespace Stryker.TestRunner.MicrosoftTestPlatform;

/// <summary>
/// A factory to create SingleMicrosoftTestPlatformRunner instances. Useful for dependency injection and mocking in tests.
/// </summary>
public interface ISingleRunnerFactory
{
    /// <summary>
    /// Creates a single MTP runner bound to the supplied identity, discovered tests, and runtime context.
    /// </summary>
    SingleMicrosoftTestPlatformRunner CreateRunner(
        int id,
        IDictionary<string, List<TestNode>> testsByAssembly,
        IDictionary<string, MtpTestDescription> testDescriptions,
        TestSet testSet,
        Lock discoveryLock,
        ILogger logger,
        IStrykerOptions? options = null);
}
