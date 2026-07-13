using UnityEngine;

namespace PawnshopKing.Systems.Audio
{
    /// <summary>
    /// Volume settings persisted in PlayerPrefs. Master drives the global
    /// AudioListener today; SFX and Music are stored and exposed so the audio
    /// pass can route through them without touching the settings UI again.
    /// </summary>
    public static class GameAudioSettings
    {
        private const string MasterKey = "volume_master";
        private const string SfxKey = "volume_sfx";
        private const string MusicKey = "volume_music";

        public static float Master
        {
            get => PlayerPrefs.GetFloat(MasterKey, 1f);
            set { PlayerPrefs.SetFloat(MasterKey, Mathf.Clamp01(value)); Apply(); }
        }

        public static float Sfx
        {
            get => PlayerPrefs.GetFloat(SfxKey, 1f);
            set => PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value));
        }

        public static float Music
        {
            get => PlayerPrefs.GetFloat(MusicKey, 1f);
            set => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value));
        }

        /// <summary>Pushes the stored settings onto the live audio state. Call once at boot.</summary>
        public static void Apply()
        {
            AudioListener.volume = Master;
        }
    }
}
