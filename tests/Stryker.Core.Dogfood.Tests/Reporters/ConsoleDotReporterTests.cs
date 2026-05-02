using FluentAssertions;
using Spectre.Console.Testing;
using Stryker.Abstractions;
using Stryker.Core.Mutants;
using Stryker.Core.Reporters;
using Xunit;

namespace Stryker.Core.Dogfood.Tests.Reporters;

/// <summary>Sprint 55 (v2.41.0) port. MSTest → xUnit, Shouldly → FluentAssertions.</summary>
public class ConsoleDotReporterTests
{
    [Theory]
    [InlineData(MutantStatus.Killed, ".", "default")]
    [InlineData(MutantStatus.Survived, "S", "red")]
    [InlineData(MutantStatus.Timeout, "T", "default")]
    public void ConsoleDotReporter_ShouldPrintRightCharOnMutation(MutantStatus givenStatus, string expectedOutput, string color)
    {
        var console = new TestConsole().EmitAnsiSequences();
        var target = new ConsoleDotProgressReporter(console);

        target.OnMutantTested(new Mutant { ResultStatus = givenStatus });

        if (string.Equals(color, "default", System.StringComparison.Ordinal))
        {
            console.Output.AnyForegroundColorSpanCount().Should().Be(0);
        }

        if (string.Equals(color, "red", System.StringComparison.Ordinal))
        {
            console.Output.RedSpanCount().Should().Be(1);
        }

        console.Output.RemoveAnsi().Should().Be(expectedOutput);
    }
}
