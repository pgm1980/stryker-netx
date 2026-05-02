using FluentAssertions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 54 (v2.40.0) port.</summary>
public class FallbackVersionInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new FallbackVersionInput();
        // Cross-platform line-ending tolerance (Sprint 53 lesson)
        target.HelpText.Replace("\r\n", "\n", System.StringComparison.Ordinal).Should().Be("Commitish used as a fallback when no report could be found based on Git information for the baseline feature.\nCan be semver, git commit hash, branch name or anything else to indicate what version of your software you're testing.\nWhen you don't specify a fallback version the since target will be used as fallback version.\nExample: If the current branch is based on the main branch, set 'main' as the fallback version | default: 'master'");
    }

    [Fact]
    public void ShouldNotValidate_IfNotEnabled()
    {
        var input = new FallbackVersionInput { SuppliedInput = "master" };

        var validatedInput = input.Validate(withBaseline: false, projectVersion: "master", sinceTarget: "master");

        validatedInput.Should().Be(new SinceTargetInput().Default);
    }

    [Fact]
    public void ShouldUseProvidedInputValue()
    {
        var input = new FallbackVersionInput { SuppliedInput = "development" };

        var validatedInput = input.Validate(withBaseline: true, projectVersion: "feat/feat4", sinceTarget: "master");

        validatedInput.Should().Be("development");
    }

    [Fact]
    public void ShouldUseSinceTarget_IfNotExplicitlySet()
    {
        var input = new FallbackVersionInput();

        var validatedInput = input.Validate(withBaseline: true, projectVersion: "development", sinceTarget: "main");

        validatedInput.Should().Be("main");
    }
}
