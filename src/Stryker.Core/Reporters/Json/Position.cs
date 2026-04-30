using System;
using Stryker.Abstractions.Reporting;

namespace Stryker.Core.Reporters.Json;

public sealed class Position : IPosition
{
    public int Line
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentException("Line number must be higher than 0", nameof(value));
    }

    public int Column
    {
        get;
        set => field = value > 0 ? value : throw new ArgumentException("Column number must be higher than 0", nameof(value));
    }
}
