using System.Text.Json;
using FluentAssertions;
using Stryker.TestRunner.MicrosoftTestPlatform.RPC;
using Xunit;

namespace Stryker.TestRunner.MicrosoftTestPlatform.Tests;

/// <summary>
/// Sprint 30 (v2.17.0) port of upstream stryker-net 4.14.0
/// src/Stryker.TestRunner.MicrosoftTestPlatform.UnitTest/RpcJsonSerializerOptionsTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// Smoke test that establishes the MTP test-project pipeline.
/// </summary>
public class RpcJsonSerializerOptionsTests
{
    [Fact]
    public void Default_ShouldReturnConfiguredOptions()
    {
        var options = RpcJsonSerializerOptions.Default;

        options.Should().NotBeNull();
        options.PropertyNamingPolicy.Should().NotBeNull();
        options.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        options.PropertyNameCaseInsensitive.Should().BeTrue();
    }

    [Fact]
    public void Default_ShouldReturnSameInstance()
    {
        var options1 = RpcJsonSerializerOptions.Default;
        var options2 = RpcJsonSerializerOptions.Default;

        options1.Should().BeSameAs(options2);
    }
}
