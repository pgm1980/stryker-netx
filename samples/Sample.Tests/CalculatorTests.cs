using FluentAssertions;
using Sample.Library;
using Xunit;

namespace Sample.Tests;

public sealed class CalculatorTests
{
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(int.MaxValue - 1, 1, int.MaxValue)]
    public void Add_Returns_The_Sum(int a, int b, int expected) =>
        Calculator.Add(a, b).Should().Be(expected);

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(-2, -3, 1)]
    [InlineData(10, 100, -90)]
    public void Subtract_Returns_The_Difference(int a, int b, int expected) =>
        Calculator.Subtract(a, b).Should().Be(expected);

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(0, 5, 0)]
    [InlineData(-2, 3, -6)]
    [InlineData(-2, -3, 6)]
    public void Multiply_Returns_The_Product(int a, int b, int expected) =>
        Calculator.Multiply(a, b).Should().Be(expected);

    [Theory]
    [InlineData(1, true)]
    [InlineData(int.MaxValue, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(int.MinValue, false)]
    public void IsPositive_Detects_Positive_Numbers(int value, bool expected) =>
        Calculator.IsPositive(value).Should().Be(expected);
}
