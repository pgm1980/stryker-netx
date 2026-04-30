using System;

namespace Stryker.DataCollector;

internal static class Helpers
{
    public static T? ExtractAttribute<T>(this Type type) where T : Attribute =>
        type.GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;

    private static object? FirstOrDefault(this object[] source) => source.Length == 0 ? null : source[0];
}
