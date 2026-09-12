using UnityEngine;

namespace SignalLost.UI
{
    public static class GameSettings
    {
        const string KS = "sens", VS = "vol";

        public static float Sensitivity => Mathf.Clamp(PlayerPrefs.GetFloat(KS, 1.5f), 0.4f, 4f);
        public static float Volume => Mathf.Clamp01(PlayerPrefs.GetFloat(VS, 0.8f));

        public static void SetSensitivity(float v) { PlayerPrefs.SetFloat(KS, v); }
        public static void SetVolume(float v) { PlayerPrefs.SetFloat(VS, v); AudioListener.volume = v; }

        public static void Apply()
        {
            AudioListener.volume = Volume;
        }
    }
}
