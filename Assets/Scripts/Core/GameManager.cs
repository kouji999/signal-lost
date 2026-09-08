using UnityEngine;

namespace SignalLost.Core
{
    public enum GameState
    {
        Intro,
        Playing,
        UiOpen,
        Paused,
        Ending
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Intro;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetState(GameState next)
        {
            if (State == next) return;
            State = next;
            Time.timeScale = next == GameState.Paused ? 0f : 1f;
            Cursor.lockState = next == GameState.Playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = next != GameState.Playing;
            EventBus.Publish(new GameStateChanged(next));
        }
    }
}
