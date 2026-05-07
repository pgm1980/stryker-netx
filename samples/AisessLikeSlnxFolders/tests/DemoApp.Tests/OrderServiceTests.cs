using DemoApp.Application;
using FluentAssertions;
using Xunit;

namespace DemoApp.Tests;

/// <summary>
/// Unit tests for <see cref="OrderService"/>.
/// Kills the *→/ mutant in CalculateLineTotal and the -→+ mutant in ApplyDiscount.
/// </summary>
public sealed class OrderServiceTests
{
    [Theory]
    [InlineData(10, 3, 30)]
    [InlineData(0, 5, 0)]
    [InlineData(7, 7, 49)]
    public void CalculateLineTotal_Returns_UnitPrice_Times_Quantity(int unitPrice, int quantity, int expected) =>
        OrderService.CalculateLineTotal(unitPrice, quantity).Should().Be(expected);

    [Theory]
    [InlineData(100, 20, 80)]
    [InlineData(50, 0, 50)]
    [InlineData(10, 15, -5)]
    public void ApplyDiscount_Returns_Total_Minus_Discount(int total, int discount, int expected) =>
        OrderService.ApplyDiscount(total, discount).Should().Be(expected);
}
