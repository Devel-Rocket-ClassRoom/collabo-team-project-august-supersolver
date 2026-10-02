using System;
using System.Collections.Generic;

public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<T>(T service) where T : class
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        _services[typeof(T)] = service;
    }

    public static T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;
        throw new InvalidOperationException(
            $"[ServiceLocator] {typeof(T).Name} is not registered.");
    }

    public static bool TryGet<T>(out T service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }
        service = null;
        return false;
    }

    public static void Unregister<T>() where T : class
    {
        _services.Remove(typeof(T));
    }

    public static void Clear()
    {
        _services.Clear();
    }
}
