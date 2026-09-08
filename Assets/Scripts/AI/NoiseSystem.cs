using System.Collections.Generic;
using UnityEngine;

namespace SignalLost.AI
{
    public static class NoiseSystem
    {
        private static readonly List<INoiseListener> Listeners = new();

        public static void Register(INoiseListener listener)
        {
            if (!Listeners.Contains(listener)) Listeners.Add(listener);
        }

        public static void Unregister(INoiseListener listener) => Listeners.Remove(listener);

        public static void EmitNoise(Vector3 position, float radius, GameObject source)
        {
            for (int i = Listeners.Count - 1; i >= 0; i--)
            {
                var l = Listeners[i];
                if (l == null || (l is Object o && o == null)) { Listeners.RemoveAt(i); continue; }
                l.OnNoiseHeard(position, radius, source);
            }
        }
    }

    public interface INoiseListener
    {
        void OnNoiseHeard(Vector3 position, float radius, GameObject source);
    }
}
