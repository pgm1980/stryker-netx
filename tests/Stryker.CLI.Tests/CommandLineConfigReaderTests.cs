using System;
using System.Linq;
using FluentAssertions;
using McMaster.Extensions.CommandLineUtils;
using Stryker.Abstractions.Options;
using Stryker.CLI;
using Stryker.CLI.CommandLineConfig;
using Stryker.Configuration.Options;
using Xunit;

namespace Stryker.CLI.Tests;

/// <summary>
/// Sprint 37 (v2.24.0) port of upstream stryker-net 4.14.1
/// src/Stryker.CLI/Stryker.CLI.UnitTest/CommandLineConfigReaderTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Performance",
    "CA1859:Use concrete types when possible for improved performance",
    Justification = "Test asserts behavior of IStrykerInputs interface directly; perf-not-test-concern (Sprint 28 lesson).")]
public sealed class CommandLineConfigReaderTests : IDisposable
{
    private readonly CommandLineApplication _app = new()
    {
        Name = "Stryker",
        FullName = "Stryker: Stryker mutator for .Net",
        Description = "Stryker mutator for .Net",
        ExtendedHelpText = "Welcome to Stryker for .Net! Run dotnet stryker to kick off a mutation test run or run dotnet stryker init to start configuring your project.",
    };

    private readonly IStrykerInputs _inputs = new StrykerInputs();
    private readonly CommandLineConfigReader _target = new();

    public CommandLineConfigReaderTests() => _target.RegisterCommandLineOptions(_app, _inputs);

    public void Dispose() => _app.Dispose();

    [Fact]
    public void ShouldHandleNoValue()
    {
        _target.ReadCommandLineConfig(["--diag"], _app, _inputs);

        _inputs.DiagModeInput.SuppliedInput.Should().Be(true);
    }

    [Fact]
    public void ShouldHandleSingleValue()
    {
        _target.ReadCommandLineConfig(["--concurrency 4"], _app, _inputs);

        _inputs.ConcurrencyInput.SuppliedInput.Should().Be(4);
    }

    [Fact]
    public void ShouldHandleSingleOrNoValueWithNoValue()
    {
        _target.ReadCommandLineConfig(["--since"], _app, _inputs);

        _inputs.SinceInput.SuppliedInput.Should().Be(true);
        _inputs.SinceTargetInput.SuppliedInput.Should().BeNull();
    }

    [Fact]
    public void ShouldHandleSingleOrNoValueWithValue()
    {
        _target.ReadCommandLineConfig(["--since:test"], _app, _inputs);

        _inputs.SinceInput.SuppliedInput.Should().Be(true);
        _inputs.SinceTargetInput.SuppliedInput.Should().Be("test");
    }

    [Fact]
    public void ShouldHandleMultiValue()
    {
        _target.ReadCommandLineConfig(["--reporter test", "--reporter test2"], _app, _inputs);

        _inputs.ReportersInput.SuppliedInput.Count().Should().Be(2);
        _inputs.ReportersInput.SuppliedInput.First().Should().Be("test");
        _inputs.ReportersInput.SuppliedInput.Last().Should().Be("test2");
    }
}
