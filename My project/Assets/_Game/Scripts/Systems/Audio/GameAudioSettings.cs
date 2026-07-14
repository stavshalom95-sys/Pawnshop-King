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
        private const string MusicEnabledKey = "music_enabled";

        // Cached so the per-frame music watchdog never touches PlayerPrefs.
        private static float? master, sfx, music;
        private static bool? musicEnabled;

        public static float Master
        {
            get => master ??= PlayerPrefs.GetFloat(MasterKey, 1f);
            set { master = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MasterKey, master.Value); Apply(); }
        }

        public static float Sfx
        {
            get => sfx ??= PlayerPrefs.GetFloat(SfxKey, 1f);
            set { sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(SfxKey, sfx.Value); Apply(); }
        }

        public static float Music
        {
            get => music ??= PlayerPrefs.GetFloat(MusicKey, 1f);
            set { music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music.Value); Apply(); }
        }

        public static bool MusicEnabled
        {
            get => musicEnabled ??= PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            set { musicEnabled = value; PlayerPrefs.SetInt(MusicEnabledKey, value ? 1 : 0); Apply(); }
        }

        /// <summary>Pushes the stored settings onto the live audio state. Call once at boot.</summary>
        public static void Apply()
        {
            AudioListener.volume = Master;

            // Explicit Unity null check on purpose: ?. skips the destroyed-object
            // overload, so a stale Instance (statics survive play sessions with
            // domain reload disabled) would swallow the update silently.
            var music = MusicManager.Instance;
            if (music != null) music.ApplySettings();

            var audio = AudioManager.Instance;
            if (audio != null) audio.ApplyAmbienceVolume();
        }
    }
}
