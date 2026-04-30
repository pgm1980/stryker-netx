namespace Sample.Library;

/// <summary>
/// Minimal arithmetic façade used as the mutation target for the CLI smoke-test.
/// Every method offers a clear binary operator that Stryker can mutate.
/// </summary>
public static class Calculator
{
    public static int Add(int a, int b) => a + b;

    public static int Subtract(int a, int b) => a - b;

    public static int Multiply(int a, int b) => a * b;

    public static bool IsPositive(int value) => value > 0;
}
