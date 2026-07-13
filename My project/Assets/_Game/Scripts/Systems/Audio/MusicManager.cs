using UnityEngine;

namespace PawnshopKing.Systems.Audio
{
    /// <summary>
    /// Looping background music, routed through the Music volume setting and the
    /// music on/off toggle. Plays Resources/Audio/Music when a track is dropped
    /// in; until then it synthesizes a quiet minor-triad drone as a stand-in —
    /// components use whole cycles over the loop length, so it loops seamlessly.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        public static MusicManager Instance { get; private set; }

        private const int SampleRate = 44100;
        private const float LoopSeconds = 8f;
        private const float PadLevel = 0.11f;

        private AudioSource source;

        private void Awake()
        {
            Instance = this;

            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;

            var track = Resources.Load<AudioClip>(AudioManager.AudioResourcePath + "/Music");
            source.clip = track != null ? track : SynthesizePad();

            ApplySettings();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Pushes the current music settings onto the source. Called by GameAudioSettings.Apply.</summary>
        public void ApplySettings()
        {
            if (source == null) return;

            source.volume = GameAudioSettings.Music;
            if (GameAudioSettings.MusicEnabled && !source.isPlaying) source.Play();
            else if (!GameAudioSettings.MusicEnabled && source.isPlaying) source.Stop();
        }

        /// <summary>
        /// A minor triad in just intonation (110/132/165 Hz — all integer cycles
        /// over the 8s loop) with a slow swell: enough noir to not be silence.
        /// </summary>
        private static AudioClip SynthesizePad()
        {
            int samples = Mathf.CeilToInt(LoopSeconds * SampleRate);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * t / LoopSeconds);
                float pad = Mathf.Sin(2f * Mathf.PI * 110f * t)
                          + 0.7f * Mathf.Sin(2f * Mathf.PI * 132f * t)
                          + 0.5f * Mathf.Sin(2f * Mathf.PI * 165f * t);
                data[i] = pad * swell * PadLevel / 2.2f;
            }

            var clip = AudioClip.Create("NoirPad", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
