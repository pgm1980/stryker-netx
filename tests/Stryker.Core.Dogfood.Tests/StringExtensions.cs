using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stryker.Core.Dogfood.Tests;

/// <summary>
/// Sprint 55 (v2.41.0) port of upstream stryker-net 4.14.1 StringExtensions for Reporter tests.
/// ANSI escape sequence helpers.
/// </summary>
internal static class StringExtensions
{
    private const string Escape = "";

    private static readonly System.Text.RegularExpressions.Regex AnsiRegex = new(Escape + @"\[[\d;]+m", RegexOptions.ECMAScript | RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static string RemoveAnsi(this string value) => AnsiRegex.Replace(value, string.Empty);

    public static int RedSpanCount(this string value) =>
        value.Split(Escape).Count(s => s.StartsWith("[31m", StringComparison.Ordinal) || s.StartsWith("[38;5;9m", StringComparison.Ordinal));

    public static int AnyForegroundColorSpanCount(this string value) =>
        value.Split(Escape).Count(s => s.StartsWith("[3", StringComparison.Ordinal));
}
