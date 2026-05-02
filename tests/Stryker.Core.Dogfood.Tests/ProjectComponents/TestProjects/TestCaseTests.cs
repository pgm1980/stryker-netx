using System;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Core.ProjectComponents.TestProjects;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.ProjectComponents.TestProjects;

/// <summary>Sprint 76 (v2.62.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class TestCaseTests
{
    [Fact]
    public void TestCaseEqualsWhenAllPropertiesEqual()
    {
        var id = Guid.NewGuid().ToString();
        var node = SyntaxFactory.Block();
        var testCaseA = new TestCase
        {
            Id = id,
            Name = "1",
            Node = node,
        };
        var testCaseB = new TestCase
        {
            Id = id,
            Name = "1",
            Node = node,
        };

        testCaseA.Should().Be(testCaseB);
        testCaseA.GetHashCode().Should().Be(testCaseB.GetHashCode());
    }

    [Theory]
    [InlineData("fd4896a2-1bd9-4e83-9e81-308059525bc9", "node2")]
    [InlineData("00000000-0000-0000-0000-000000000000", "node1")]
    public void TestCaseNotEqualsWhenNotAllPropertiesEqual(string guid, string name)
    {
        var node = SyntaxFactory.Block();
        var testCaseA = new TestCase
        {
            Id = guid,
            Name = name,
            Node = node,
        };
        var testCaseB = new TestCase
        {
            Id = "00000000-0000-0000-0000-000000000000",
            Name = "node2",
            Node = node,
        };

        testCaseA.Should().NotBe(testCaseB);
    }
}
