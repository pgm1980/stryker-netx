using System;
using System.Collections.Generic;

namespace Stryker.Core.Initialisation;

/// <summary>
/// Stores a cache of folder composites for a given element type.
/// </summary>
/// <typeparam name="T">Type of the cached folder composites.</typeparam>
public sealed class FolderCompositeCache<T>
{
    internal FolderCompositeCache()
    {
    }

    /// <summary>
    /// Gets or sets the cache dictionary keyed by folder path.
    /// </summary>
    public IDictionary<string, T> Cache { get; set; } = new Dictionary<string, T>(StringComparer.Ordinal);
}
