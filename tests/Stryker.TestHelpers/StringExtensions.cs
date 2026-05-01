using System;
using System.Linq;

namespace Stryker.TestHelpers;

/// <summary>
/// Sprint 24 (v2.11.0) port of upstream stryker-net 4.14.0
/// src/Stryker.Core/Stryker.Core.UnitTest/StringExtensions.cs. ANSI-escape
/// helpers used by reporter / console-output tests to count colored spans
/// without coupling to the exact escape-sequence shape.
/// </summary>
public static partial class StringExtensions
{
    private const string Escape = "";

    // MA0009 false positive: source-generated regex compiles to a DFA at build time (no backtracking),
    // so the `[\d;]+` quantifier cannot induce catastrophic-backtracking ReDoS even on adversarial input.
    // Plus the input is reporter test-output, not user-supplied.
#pragma warning disable MA0009
    [System.Text.RegularExpressions.GeneratedRegex(Escape + @"\[[\d;]+m")]
    private static partial System.Text.RegularExpressions.Regex AnsiRegex();
#pragma warning restore MA0009

    /// <summary>Removes ANSI escape sequences from <paramref name="value"/>.</summary>
    public static string RemoveAnsi(this string value) =>
        AnsiRegex().Replace(value, string.Empty);

    /// <summary>Counts the number of green spans in <paramref name="value"/>.</summary>
    public static int GreenSpanCount(this string value) =>
        value.Split(Escape).Count(s =>
            s.StartsWith("[32m", StringComparison.Ordinal) ||
            s.StartsWith("[38;5;2m", StringComparison.Ordinal));

    /// <summary>Counts the number of red spans in <paramref name="value"/>.</summary>
    public static int RedSpanCount(this string value) =>
        value.Split(Escape).Count(s =>
            s.StartsWith("[31m", StringComparison.Ordinal) ||
            s.StartsWith("[38;5;9m", StringComparison.Ordinal));

    /// <summary>Counts the number of blue spans in <paramref name="value"/>.</summary>
    public static int BlueSpanCount(this string value) =>
        value.Split(Escape).Count(s =>
            s.StartsWith("[36m", StringComparison.Ordinal) ||
            s.StartsWith("[38;5;14m", StringComparison.Ordinal));

    /// <summary>Counts the number of yellow spans in <paramref name="value"/>.</summary>
    public static int YellowSpanCount(this string value) =>
        value.Split(Escape).Count(s =>
            s.StartsWith("[33m", StringComparison.Ordinal) ||
            s.StartsWith("[38;5;11m", StringComparison.Ordinal));

    /// <summary>Counts the number of bright-black ("dark gray") spans in <paramref name="value"/>.</summary>
    public static int DarkGraySpanCount(this string value) =>
        value.Split(Escape).Count(s =>
            s.StartsWith("[30;1m", StringComparison.Ordinal) ||
            s.StartsWith("[38;5;8m", StringComparison.Ordinal));

    /// <summary>Counts the number of any-foreground-colored span in <paramref name="value"/>.</summary>
    public static int AnyForegroundColorSpanCount(this string value) =>
        value.Split(Escape).Count(s => s.StartsWith("[3", StringComparison.Ordinal));
}
