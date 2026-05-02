using System;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Stryker.Abstractions.Exceptions;
using Stryker.Configuration.Options.Inputs;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Options.Inputs;

/// <summary>Sprint 70 (v2.56.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class LanguageVersionInputTests
{
    [Fact]
    public void ShouldHaveHelpText()
    {
        var target = new LanguageVersionInput();
        target.HelpText.Should().Be("The c# version used in compilation. | default: 'latest' | allowed: Default, CSharp2, CSharp3, CSharp4, CSharp5, CSharp6, CSharp7, CSharp7_1, CSharp7_2, CSharp7_3, CSharp8, CSharp9, CSharp10, CSharp11, CSharp12, CSharp13, CSharp14, LatestMajor, Preview, Latest");
    }

    [Fact]
    public void ShouldHaveDefault()
    {
        var target = new LanguageVersionInput { SuppliedInput = null! };

        var result = target.Validate();

        result.Should().Be(LanguageVersion.Default);
    }

    [Fact]
    public void ShouldReturnLanguageVersion()
    {
        var target = new LanguageVersionInput { SuppliedInput = "CSharp9" };

        var result = target.Validate();

        result.Should().Be(LanguageVersion.CSharp9);
    }

    [Fact]
    public void ShouldValidateLanguageVersion()
    {
        var target = new LanguageVersionInput { SuppliedInput = "gibberish" };

        var act = () => target.Validate();

        act.Should().Throw<InputException>()
            .WithMessage($"The given c# language version (gibberish) is invalid. Valid options are: [{string.Join(", ", Enum.GetValues<LanguageVersion>().Where(l => l != LanguageVersion.CSharp1))}]");
    }
}
