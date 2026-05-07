using DemoApp.Infrastructure;
using FluentAssertions;
using Xunit;

namespace DemoApp.Tests;

/// <summary>
/// Unit tests for <see cref="Repository"/>.
/// Kills mutants in Count, Sum, and HasPositive.
/// </summary>
public sealed class RepositoryTests
{
    [Fact]
    public void Count_Is_Zero_Initially()
    {
        var repo = new Repository();
        repo.Count.Should().Be(0);
    }

    [Fact]
    public void Add_Increments_Count()
    {
        var repo = new Repository();
        repo.Add(1);
        repo.Add(2);
        repo.Count.Should().Be(2);
    }

    [Fact]
    public void Sum_Returns_Zero_For_Empty_Repo()
    {
        var repo = new Repository();
        repo.Sum().Should().Be(0);
    }

    [Fact]
    public void Sum_Returns_Sum_Of_All_Items()
    {
        var repo = new Repository();
        repo.Add(10);
        repo.Add(20);
        repo.Add(30);
        repo.Sum().Should().Be(60);
    }

    [Fact]
    public void HasPositive_Returns_False_For_Empty_Repo()
    {
        var repo = new Repository();
        repo.HasPositive().Should().BeFalse();
    }

    [Fact]
    public void HasPositive_Returns_True_When_At_Least_One_Positive()
    {
        var repo = new Repository();
        repo.Add(-5);
        repo.Add(3);
        repo.HasPositive().Should().BeTrue();
    }

    [Fact]
    public void HasPositive_Returns_False_When_All_NonPositive()
    {
        var repo = new Repository();
        repo.Add(-1);
        repo.Add(0);
        repo.HasPositive().Should().BeFalse();
    }
}
