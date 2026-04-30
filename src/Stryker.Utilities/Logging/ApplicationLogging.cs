using Microsoft.Extensions.Logging;

namespace Stryker.Utilities.Logging;

public static class ApplicationLogging
{
    // Set during application bootstrap (CLI entry) before first usage.
    // null-forgive is acceptable for singleton bootstrap pattern; consumers (Stryker.Core etc.)
    // call CreateLogger<T>() after bootstrap and don't expect null.
    public static ILoggerFactory LoggerFactory { get; set; } = null!;
}
