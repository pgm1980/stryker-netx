namespace DemoApp.Domain;

/// <summary>
/// Simple arithmetic calculator in the Domain layer.
/// Each method exposes a binary operator that Stryker can mutate:
/// Add (+→- mutant), Subtract (-→+ mutant), Multiply (*→/ mutant),
/// IsPositive (>→>= mutant). All mutants are killed by DemoApp.Tests.
/// </summary>
public static class Calculator
{
    /// <summary>Returns the sum of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Add(int a, int b) => a + b;

    /// <summary>Returns the difference of <paramref name="a"/> minus <paramref name="b"/>.</summary>
    public static int Subtract(int a, int b) => a - b;

    /// <summary>Returns the product of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Multiply(int a, int b) => a * b;

    /// <summary>Returns <see langword="true"/> when <paramref name="value"/> is strictly positive.</summary>
    public static bool IsPositive(int value) => value > 0;
}
