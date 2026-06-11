using FluentAssertions;
using Stryker.TestRunner.Tests;
using Xunit;

namespace Stryker.TestRunner.VsTest.Tests;

/// <summary>
/// Sprint 179 (issue #294, 360°-Analyse I-02): contract tests for the
/// <see cref="TestIdentifierList"/> set API. The <c>Contains</c> predicate was
/// inverted (<c>is false</c> instead of <c>is true</c>) and had no test coverage —
/// these tests pin the POSITIVE membership semantics, including the delegation
/// paths from <see cref="WrappedIdentifierEnumeration"/> that route through
/// <c>other.Contains</c>.
/// </summary>
public class TestIdentifierListTests
{
    [Fact]
    public void Contains_ShouldReturnTrue_ForMember()
    {
        var list = new TestIdentifierList("a", "b");

        list.Contains("a").Should().BeTrue();
        list.Contains("b").Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalse_ForNonMember()
    {
        var list = new TestIdentifierList("a", "b");

        list.Contains("c").Should().BeFalse();
    }

    [Fact]
    public void Contains_ShouldReturnTrue_OnEveryTest()
    {
        TestIdentifierList.EveryTest().Contains("anything").Should().BeTrue();
    }

    [Fact]
    public void Contains_ShouldReturnFalse_OnNoTest()
    {
        TestIdentifierList.NoTest().Contains("anything").Should().BeFalse();
    }

    [Fact]
    public void WrappedEnumeration_IsIncludedIn_ShouldDelegateWithPositiveSemantics()
    {
        var wrapped = new WrappedIdentifierEnumeration(["a"]);
        var superset = new TestIdentifierList("a", "b");
        var disjoint = new TestIdentifierList("x");

        wrapped.IsIncludedIn(superset).Should().BeTrue();
        wrapped.IsIncludedIn(disjoint).Should().BeFalse();
    }

    [Fact]
    public void WrappedEnumeration_ContainsAny_ShouldDelegateWithPositiveSemantics()
    {
        var wrapped = new WrappedIdentifierEnumeration(["a", "z"]);
        var overlapping = new TestIdentifierList("z");
        var disjoint = new TestIdentifierList("x");

        wrapped.ContainsAny(overlapping).Should().BeTrue();
        wrapped.ContainsAny(disjoint).Should().BeFalse();
    }
}
