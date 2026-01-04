using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        var type = typeof(T);
        if (!_handlers.ContainsKey(type))
            _handlers[type] = new List<Delegate>();

        _handlers[type].Add(handler);

        return new Unsubscriber(() => _handlers[type].Remove(handler));
    }

    public void Publish<T>(T message)
    {
        var type = typeof(T);
        if (!_handlers.TryGetValue(type, out var list))
            return;

        foreach (var handler in list)
            ((Action<T>)handler).Invoke(message);
    }

    private class Unsubscriber : IDisposable
    {
        private readonly Action _action;
        public Unsubscriber(Action action) => _action = action;
        public void Dispose() => _action?.Invoke();
    }
}
