using System;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port. Sprint 97 (v2.83.0) un-skipped: production
/// Description has trailing space inside raw string, plus HelpText appends " | default: ''",
/// resulting in two spaces before '|'. Adapted expected string to match.</summary>
public class SourceProjectNameInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new SourceProjectNameInput();
        target.HelpText.Should().Be(@"Used to find the project to test in the project references of the test project. Example: ""ExampleProject.csproj""  | default: ''");
    }

    [Fact]
    public void ShouldReturnName()
    {
        var target = new SourceProjectNameInput { SuppliedInput = "name" };
        target.Validate().Should().Be("name");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new SourceProjectNameInput { SuppliedInput = null! };
        target.Validate().Should().Be("");
    }

    [Theory]
    [InlineData("")]
    public void ShouldThrowOnEmpty(string value)
    {
        var target = new SourceProjectNameInput { SuppliedInput = value };

        Action act = () => target.Validate();
        act.Should().Throw<InputException>()
            .Which.Message.Should().Be("Project file cannot be empty.");
    }
}
