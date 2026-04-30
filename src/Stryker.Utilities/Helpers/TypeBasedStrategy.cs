using System;
using System.Collections.Generic;
using System.Linq;

namespace Stryker.Utilities.Helpers;

// type based strategy pattern implementation: finds the proper implementation according the type of a given object
// keeping a cache for faster resolution
public class TypeBasedStrategy<T, THandler> where T : class where THandler : class, ITypeHandler<T>
{
    private readonly Dictionary<Type, IList<THandler>> _handlerMapping = [];

    public void RegisterHandler(THandler handler)
    {
        if (!_handlerMapping.TryGetValue(handler.ManagedType, out var list))
        {
            list = [];
            _handlerMapping[handler.ManagedType] = list;
        }
        list.Add(handler);
    }

    public void RegisterHandlers(IEnumerable<THandler> handlers)
    {
        foreach (var handler in handlers)
        {
            RegisterHandler(handler);
        }
    }

    public THandler? FindHandler(T item) => FindHandler(item, item.GetType());

    private THandler? FindHandler(T item, Type? type)
    {
        for (; item != null && type != null; type = type.BaseType)
        {
            if (!_handlerMapping.TryGetValue(type, out var handlers))
            {
                continue;
            }
            var match =  handlers.FirstOrDefault( th => th.CanHandle(item));
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
