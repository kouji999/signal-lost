using NUnit.Framework;
using SignalLost.Core;

namespace SignalLost.Tests
{
    public class EventBusTests
    {
        [Test]
        public void Subscribe_Publish_Unsubscribe_Works()
        {
            int received = 0;
            System.Action<LifeSupportRestored> handler = _ => received++;
            EventBus.Subscribe(handler);
            EventBus.Publish(LifeSupportRestored.Instance);
            EventBus.Unsubscribe(handler);
            EventBus.Publish(LifeSupportRestored.Instance);
            Assert.AreEqual(1, received);
        }

        [Test]
        public void StoryFlags_Set_And_Clear()
        {
            StoryFlagSystem.Set("flag_a");
            Assert.IsTrue(StoryFlagSystem.IsSet("flag_a"));
            StoryFlagSystem.Set("flag_a", false);
            Assert.IsFalse(StoryFlagSystem.IsSet("flag_a"));
        }
    }
}
