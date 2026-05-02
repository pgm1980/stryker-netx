using System;
using FluentAssertions;
using Stryker.Configuration;
using Xunit;

namespace Stryker.Core.Dogfood.Tests;

/// <summary>
/// Sprint 46 (v2.33.0) port of upstream stryker-net 4.14.1
/// src/Stryker.Core/Stryker.Core.UnitTest/ExclusionPatternTests.cs.
/// Framework conversion: MSTest → xUnit, Shouldly → FluentAssertions.
/// `: TestBase` dropped — xUnit doesn't need test base class for [Fact] discovery.
/// </summary>
public class ExclusionPatternTests
{
    [Fact]
    public void ExclusionPattern_Null()
    {
        Action act = () => _ = new ExclusionPattern(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExclusionPattern_Globs()
    {
        var s1 = new ExclusionPattern(@"Person.cs");
        var s2 = new ExclusionPattern(@"!Person.cs");

        s1.IsExcluded.Should().BeFalse();
        s2.IsExcluded.Should().BeTrue();
        s1.Glob.ToString().Should().Be(s2.Glob.ToString());
    }

    [Fact]
    public void ExclusionPattern_MutantSpans()
    {
        var s1 = new ExclusionPattern(@"src/Person.cs{10..100}");
        var s2 = new ExclusionPattern(@"src/Person.cs");

        s1.MutantSpans.Should().BeEquivalentTo([(10, 100)]);
        s2.MutantSpans.Should().BeEmpty();
    }
}
