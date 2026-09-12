using System.Collections;
using SignalLost.Core;
using UnityEngine;
using UnityEngine.UI;

namespace SignalLost.UI
{
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private CanvasGroup settingsPanel;
        [SerializeField] private Slider sensSlider;
        [SerializeField] private Slider volSlider;
        [SerializeField] private Text sensLabel;
        [SerializeField] private Text volLabel;

        public bool Completed { get; private set; }
        private bool _settingsOpen;
        private float _lastSens, _lastVol;

        private void Start()
        {
            GameSettings.Apply();
            if (root != null) root.alpha = 1f;
            if (settingsPanel != null) settingsPanel.alpha = 0f;
            if (sensSlider != null) sensSlider.value = (GameSettings.Sensitivity - 0.4f) / 3.6f;
            if (volSlider != null) volSlider.value = GameSettings.Volume;
            _lastSens = sensSlider != null ? sensSlider.value : -1f;
            _lastVol = volSlider != null ? volSlider.value : -1f;
            RefreshLabels();
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.State != GameState.Title) return;

            if (_settingsOpen)
            {
                if (sensSlider != null && Mathf.Abs(sensSlider.value - _lastSens) > 0.001f)
                {
                    _lastSens = sensSlider.value;
                    GameSettings.SetSensitivity(0.4f + sensSlider.value * 3.6f);
                }
                if (volSlider != null && Mathf.Abs(volSlider.value - _lastVol) > 0.001f)
                {
                    _lastVol = volSlider.value;
                    GameSettings.SetVolume(volSlider.value);
                }
                RefreshLabels();
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.Escape)) CloseSettings();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
                Begin();
            else if (Input.GetKeyDown(KeyCode.S))
                OpenSettings();
            else if (Input.GetKeyDown(KeyCode.Q))
                Application.Quit();
        }

        public void Begin()
        {
            if (Completed) return;
            Completed = true;
            if (root != null) root.alpha = 0f;
            if (settingsPanel != null) settingsPanel.alpha = 0f;
            Time.timeScale = 1f;
        }

        private void OpenSettings()
        {
            _settingsOpen = true;
            if (settingsPanel != null) settingsPanel.alpha = 1f;
        }

        private void CloseSettings()
        {
            _settingsOpen = false;
            PlayerPrefs.Save();
            if (settingsPanel != null) settingsPanel.alpha = 0f;
        }

        private void RefreshLabels()
        {
            if (sensLabel != null) sensLabel.text = "MOUSE SENSITIVITY  " + new string('#', Mathf.Max(1, Mathf.CeilToInt(GameSettings.Sensitivity / 4f * 18f)));
            if (volLabel != null) volLabel.text = "MASTER VOLUME  " + Mathf.RoundToInt(GameSettings.Volume * 100) + "%";
        }
    }
}
