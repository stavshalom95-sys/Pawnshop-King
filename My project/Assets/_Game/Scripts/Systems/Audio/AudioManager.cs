using System;
using UnityEngine;

namespace PawnshopKing.Systems.Audio
{
    /// <summary>
    /// UI sound effects, routed through the SFX volume setting (Master rides the
    /// global AudioListener). Clips come from Resources/Audio when present
    /// (Click / Accept / Reject), else from tiny synthesized fallbacks — so the
    /// game has feel today and swaps to real sounds the moment files are dropped
    /// in (zero-editor-wiring).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public const string AudioResourcePath = "Audio";
        private const int SampleRate = 44100;

        private AudioSource sfxSource;
        private AudioClip clickClip;
        private AudioClip acceptClip;
        private AudioClip rejectClip;

        private void Awake()
        {
            Instance = this;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            clickClip = LoadOrSynth("Click", 0.06f, t =>
                Mathf.Sin(2f * Mathf.PI * 1300f * t) * Mathf.Exp(-t * 70f) * 0.5f);

            acceptClip = LoadOrSynth("Accept", 0.18f, t =>
            {
                float freq = t < 0.09f ? 660f : 990f; // rising two-tone: "deal!"
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 14f) * 0.45f;
            });

            rejectClip = LoadOrSynth("Reject", 0.22f, t =>
            {
                float square = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 160f * t)) * 0.6f
                             + Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.4f; // softened low buzz
                return square * Mathf.Exp(-t * 11f) * 0.35f;
            });
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayClick() => Play(clickClip, 0.8f);
        public void PlayAccept() => Play(acceptClip, 1f);
        public void PlayReject() => Play(rejectClip, 1f);

        private void Play(AudioClip clip, float scale)
        {
            if (clip == null) return;
            sfxSource.PlayOneShot(clip, GameAudioSettings.Sfx * scale);
        }

        private static AudioClip LoadOrSynth(string name, float seconds, Func<float, float> wave)
        {
            var loaded = Resources.Load<AudioClip>(AudioResourcePath + "/" + name);
            if (loaded != null) return loaded;

            int samples = Mathf.CeilToInt(seconds * SampleRate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++) data[i] = wave(i / (float)SampleRate);

            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
