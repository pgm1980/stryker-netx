using System.Collections.Generic;
using DemoApp.Domain;

namespace DemoApp.Infrastructure;

/// <summary>
/// In-memory repository that stores integer values.
/// Provides add and sum operations backed by Calculator to create
/// Infrastructure-layer mutation targets.
/// </summary>
public sealed class Repository
{
    private readonly List<int> _items = [];

    /// <summary>Adds an item to the store.</summary>
    /// <param name="value">The value to store.</param>
    public void Add(int value) => _items.Add(value);

    /// <summary>Returns the number of stored items.</summary>
    public int Count => _items.Count;

    /// <summary>
    /// Returns the running sum of all stored items using Calculator.Add.
    /// An empty store returns zero.
    /// </summary>
    public int Sum()
    {
        var total = 0;
        foreach (var item in _items)
        {
            total = Calculator.Add(total, item);
        }
        return total;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the store contains at least one
    /// positive value.
    /// </summary>
    public bool HasPositive()
    {
        foreach (var item in _items)
        {
            if (Calculator.IsPositive(item))
            {
                return true;
            }
        }
        return false;
    }
}
