using SignalLost.Core;
using UnityEngine;

namespace SignalLost.Narrative
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private CanvasGroup panel;
        private bool _open;

        private void Start()
        {
            if (panel != null) panel.alpha = 0f;
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.Instance.State == GameState.Paused) Close();
                else if (GameManager.Instance.State == GameState.Playing) Open();
            }
            if (GameManager.Instance.State == GameState.Paused)
            {
                if (Input.GetKeyDown(KeyCode.R)) Close();
                else if (Input.GetKeyDown(KeyCode.T)) Restart();
                else if (Input.GetKeyDown(KeyCode.Q)) Quit();
            }
        }

        private void Restart()
        {
            GameManager.Instance.SetState(GameState.Playing);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        private void Quit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void Open()
        {
            _open = true;
            GameManager.Instance.SetState(GameState.Paused);
            if (panel != null) panel.alpha = 1f;
            EventBus.Publish(new SubtitleEvent("SYSTEM", "[R] RESUME   [T] RESTART   [Q] QUIT", 3f));
        }

        public void Close()
        {
            _open = false;
            GameManager.Instance.SetState(GameState.Playing);
            if (panel != null) panel.alpha = 0f;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_open) Close();
        }
    }
}
