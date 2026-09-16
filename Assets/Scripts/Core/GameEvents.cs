using SignalLost.Interaction;
using UnityEngine;

namespace SignalLost.Core
{
    public readonly struct GameStateChanged
    {
        public readonly GameState State;
        public GameStateChanged(GameState state) => State = state;
    }

    public readonly struct VitalsChanged
    {
        public readonly float Health;
        public readonly float Oxygen;
        public readonly float Battery;
        public VitalsChanged(float health, float oxygen, float battery)
        {
            Health = health;
            Oxygen = oxygen;
            Battery = battery;
        }
    }

    public readonly struct FlashlightToggled
    {
        public readonly bool On;
        public FlashlightToggled(bool on) => On = on;
    }

    public readonly struct InteractableFocused
    {
        public readonly IInteractable Target;
        public InteractableFocused(IInteractable target) => Target = target;
    }

    public readonly struct InteractPerformed
    {
        public readonly IInteractable Target;
        public readonly GameObject Interactor;
        public InteractPerformed(IInteractable target, GameObject interactor)
        {
            Target = target;
            Interactor = interactor;
        }
    }

    public readonly struct DoorStateChanged
    {
        public readonly string DoorId;
        public readonly bool Open;
        public DoorStateChanged(string doorId, bool open)
        {
            DoorId = doorId;
            Open = open;
        }
    }

    public readonly struct InventoryChanged
    {
        public static readonly InventoryChanged Instance = new();
    }

    public readonly struct ObjectiveChanged
    {
        public static readonly ObjectiveChanged Instance = new();
    }

    public readonly struct StoryFlagChanged
    {
        public readonly string Key;
        public readonly bool Value;
        public StoryFlagChanged(string key, bool value)
        {
            Key = key;
            Value = value;
        }
    }

    public readonly struct LifeSupportRestored
    {
        public static readonly LifeSupportRestored Instance = new();
    }

    public readonly struct SubtitleEvent
    {
        public readonly string Speaker;
        public readonly string Text;
        public readonly float Duration;
        public readonly bool Clear;
        public SubtitleEvent(string speaker, string text, float duration, bool clear = false)
        {
            Speaker = speaker;
            Text = text;
            Duration = duration;
            Clear = clear;
        }
    }

    public readonly struct PlayerDamaged
    {
        public readonly float Amount;
        public PlayerDamaged(float amount) => Amount = amount;
    }

    public readonly struct LogDiscovered
    {
        public readonly string LogId;
        public LogDiscovered(string logId) => LogId = logId;
    }

    public readonly struct SignalReceived
    {
        public static readonly SignalReceived Instance = new();
    }

    public readonly struct GameOverEvent
    {
        public readonly string Reason;
        public GameOverEvent(string reason) => Reason = reason;
    }

    public readonly struct SliceCompleteEvent
    {
        public static readonly SliceCompleteEvent Instance = new();
    }

    public readonly struct FootstepPlayed
    {
        public readonly bool Crouching;
        public readonly bool Sprinting;
        public FootstepPlayed(bool crouching, bool sprinting)
        {
            Crouching = crouching;
            Sprinting = sprinting;
        }
    }

    public readonly struct EnemyChaseStarted
    {
        public readonly bool Attacking;
        public EnemyChaseStarted(bool attacking) => Attacking = attacking;
    }

    public readonly struct EnemyLostPlayer
    {
        public static readonly EnemyLostPlayer Instance = new();
    }

    public readonly struct DoorDenied
    {
        public readonly string DoorId;
        public readonly int AccessLevel;
        public DoorDenied(string doorId, int accessLevel)
        {
            DoorId = doorId;
            AccessLevel = accessLevel;
        }
    }

    public readonly struct ChapterCardRequested
    {
        public readonly string Chapter;
        public readonly string Title;
        public ChapterCardRequested(string chapter, string title)
        {
            Chapter = chapter;
            Title = title;
        }
    }

    public readonly struct PowerRestored
    {
        public static readonly PowerRestored Instance = new();
    }

    public readonly struct ScannerResult
    {
        public readonly string Lines;
        public ScannerResult(string lines) => Lines = lines;
    }
}
