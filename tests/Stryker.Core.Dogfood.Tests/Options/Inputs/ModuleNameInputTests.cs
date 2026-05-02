using System;
using FluentAssertions;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class ModuleNameInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new ModuleNameInput();
        target.HelpText.Should().Be(@"Module name used by reporters. Usually a project in your solution would be a module. | default: ''");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new ModuleNameInput { SuppliedInput = null! };
        target.Validate().Should().Be(string.Empty);
    }

    [Fact]
    public void ShouldReturnName()
    {
        var target = new ModuleNameInput { SuppliedInput = "TestName" };
        target.Validate().Should().Be("TestName");
    }

    [Fact]
    public void ShouldThrowOnNull()
    {
        var target = new ModuleNameInput { SuppliedInput = string.Empty };

        Action act = () => target.Validate();
        act.Should().Throw<InputException>()
            .Which.Message.Should().Be("Module name cannot be empty. Either fill the option or leave it out.");
    }
}
