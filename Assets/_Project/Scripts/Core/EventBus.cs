using System;
using System.Collections.Generic;

/// <summary>
/// 全局事件总线：各系统通过它通信，不互相引用。
/// </summary>
public static class EventBus
{
    private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

    public static void Subscribe<T>(Action<T> handler)
    {
        Type t = typeof(T);
        if (_handlers.TryGetValue(t, out Delegate d))
            _handlers[t] = Delegate.Combine(d, handler);
        else
            _handlers[t] = handler;
    }

    public static void Unsubscribe<T>(Action<T> handler)
    {
        Type t = typeof(T);
        if (_handlers.TryGetValue(t, out Delegate d))
        {
            d = Delegate.Remove(d, handler);
            if (d == null) _handlers.Remove(t);
            else _handlers[t] = d;
        }
    }

    public static void Publish<T>(T data)
    {
        if (_handlers.TryGetValue(typeof(T), out Delegate d) && d is Action<T> action)
            action.Invoke(data);
    }
}