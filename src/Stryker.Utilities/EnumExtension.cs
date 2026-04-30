using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Stryker.Abstractions;

namespace Stryker.Utilities;

public static class EnumExtension
{
    public static IEnumerable<string> GetDescriptions<T>(this T e) where T : IConvertible
    {
        if (e is not Enum)
            return [];
        var type = e.GetType();
        var values = Enum.GetValues(type);

        foreach (int val in values)
        {
            if (val != e.ToInt32(CultureInfo.InvariantCulture))
            {
                continue;
            }
            var enumName = type.GetEnumName(val);
            if (enumName is null)
            {
                continue;
            }
            var memInfo = type.GetMember(enumName);

            var descriptions = memInfo[0].GetCustomAttributes<MutatorDescriptionAttribute>(false).Select(descriptionAttribute => descriptionAttribute.Description);
            return descriptions;
        }

        return [];
    }
}
