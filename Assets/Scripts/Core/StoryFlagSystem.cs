using System.Collections.Generic;

namespace SignalLost.Core
{
    public static class StoryFlagSystem
    {
        private static readonly HashSet<string> Flags = new();

        public static void Set(string key, bool value = true)
        {
            if (value)
            {
                if (Flags.Add(key)) EventBus.Publish(new StoryFlagChanged(key, true));
            }
            else if (Flags.Remove(key))
            {
                EventBus.Publish(new StoryFlagChanged(key, false));
            }
        }

        public static bool IsSet(string key) => Flags.Contains(key);

        public static IReadOnlyCollection<string> AllFlags => Flags;

        public static void LoadFrom(IEnumerable<string> keys)
        {
            Flags.Clear();
            foreach (var key in keys) Flags.Add(key);
        }
    }
}
