using System;
using System.Collections.Concurrent;

namespace Stryker.Core.Initialisation;

/// <summary>
/// Non-generic registry for folder composite caches keyed by element type.
/// </summary>
public static class FolderCompositeCacheRegistry
{
    private static readonly ConcurrentDictionary<Type, object> _caches = new();

    /// <summary>
    /// Gets the cache instance for the specified element type.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <returns>The cache instance for type T.</returns>
    public static FolderCompositeCache<T> Get<T>()
    {
        return (FolderCompositeCache<T>)_caches.GetOrAdd(typeof(T), _ => new FolderCompositeCache<T>());
    }
}
