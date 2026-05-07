using DemoApp.Domain;
using FluentAssertions;
using Xunit;

namespace DemoApp.Tests;

/// <summary>
/// Unit tests for <see cref="Calculator"/>.
/// Each test method kills at least one Stryker mutation:
/// Add tests kill the +→- and +→* mutants;
/// Subtract tests kill the -→+ mutant;
/// Multiply tests kill the *→/ and *→+ mutants;
/// IsPositive tests kill the >→>= and >→== mutants.
/// </summary>
public sealed class CalculatorTests
{
    [Theory]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    [InlineData(-1, 1, 0)]
    [InlineData(10, 5, 15)]
    public void Add_Returns_The_Sum(int a, int b, int expected) =>
        Calculator.Add(a, b).Should().Be(expected);

    [Theory]
    [InlineData(5, 3, 2)]
    [InlineData(0, 0, 0)]
    [InlineData(10, 100, -90)]
    [InlineData(-2, -3, 1)]
    public void Subtract_Returns_The_Difference(int a, int b, int expected) =>
        Calculator.Subtract(a, b).Should().Be(expected);

    [Theory]
    [InlineData(2, 3, 6)]
    [InlineData(0, 5, 0)]
    [InlineData(-2, 3, -6)]
    [InlineData(4, 4, 16)]
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
