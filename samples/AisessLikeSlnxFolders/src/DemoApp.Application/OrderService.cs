using DemoApp.Domain;

namespace DemoApp.Application;

/// <summary>
/// Application-layer service that computes order totals using the Domain calculator.
/// Exposes mutations via the arithmetic operations (+, *) used internally.
/// </summary>
public sealed class OrderService
{
    /// <summary>
    /// Calculates the total price for a line item:
    /// <c>unitPrice * quantity</c>.
    /// </summary>
    /// <param name="unitPrice">Price per unit (non-negative).</param>
    /// <param name="quantity">Number of units ordered (non-negative).</param>
    /// <returns>Total line-item cost.</returns>
    public static int CalculateLineTotal(int unitPrice, int quantity)
        => Calculator.Multiply(unitPrice, quantity);

    /// <summary>
    /// Applies a flat discount to a total: <c>total - discount</c>.
    /// </summary>
    /// <param name="total">Original total before discount.</param>
    /// <param name="discount">Amount to subtract.</param>
    /// <returns>Discounted total.</returns>
    public static int ApplyDiscount(int total, int discount)
        => Calculator.Subtract(total, discount);
}
