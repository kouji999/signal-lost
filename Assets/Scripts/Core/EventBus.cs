using System;
using System.Collections.Generic;

namespace SignalLost.Core
{
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            Handlers.TryGetValue(typeof(T), out var existing);
            Handlers[typeof(T)] = existing + handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!Handlers.TryGetValue(typeof(T), out var existing)) return;
            var next = existing - handler;
            if (next == null) Handlers.Remove(typeof(T));
            else Handlers[typeof(T)] = next;
        }

        public static void Publish<T>(T evt)
        {
            if (Handlers.TryGetValue(typeof(T), out var existing))
                (existing as Action<T>)?.Invoke(evt);
        }

        public static void Clear() => Handlers.Clear();
    }
}
