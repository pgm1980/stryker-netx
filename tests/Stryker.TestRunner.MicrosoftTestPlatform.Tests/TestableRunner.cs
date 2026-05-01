using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Stryker.TestRunner.MicrosoftTestPlatform;
using Stryker.TestRunner.MicrosoftTestPlatform.Models;
using Stryker.TestRunner.Tests;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 30 (v2.17.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/TestableRunner.cs.
/// Subclass that exposes the dispose-callback hook so per-test cleanup can be
/// observed. Used by Sprints 31+ test files; ported now alongside the project
/// foundation so the larger ports inherit the helper.
/// </summary>
internal sealed class TestableRunner : SingleMicrosoftTestPlatformRunner
{
    private readonly Action _onDispose;

    public TestableRunner(int id, Action onDispose)
        : base(
            id,
            new Dictionary<string, List<TestNode>>(StringComparer.Ordinal),
            new Dictionary<string, MtpTestDescription>(StringComparer.Ordinal),
            new TestSet(),
            new Lock(),
            NullLogger.Instance) =>
        _onDispose = onDispose;

    protected override void Dispose(bool disposing)
    {
        _onDispose?.Invoke();
        base.Dispose(disposing);
    }
}
