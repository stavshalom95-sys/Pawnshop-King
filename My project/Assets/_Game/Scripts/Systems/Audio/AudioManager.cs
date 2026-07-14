using System;
using UnityEngine;

namespace PawnshopKing.Systems.Audio
{
    /// <summary>
    /// UI sound effects (Click / Accept / Reject / Stamp / CashGain), routed
    /// through the SFX volume setting, plus a constant low-volume shop-ambience
    /// bed on its own looping source (also SFX-scaled, but quieter — see
    /// AmbienceVolumeScale). Clips come from Resources/Audio when present, else
    /// from tiny synthesized fallbacks — so the game has feel today and swaps to
    /// real sounds the moment files are dropped in (zero-editor-wiring).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public const string AudioResourcePath = "Audio";
        private const int SampleRate = 44100;

        // Ambience should read as "always there, never intrusive" — quieter than
        // a one-shot SFX even at full slider.
        private const float AmbienceVolumeScale = 0.35f;

        private AudioSource sfxSource;
        private AudioSource ambienceSource;
        private AudioClip clickClip;
        private AudioClip acceptClip;
        private AudioClip rejectClip;
        private AudioClip stampClip;
        private AudioClip cashGainClip;

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

            stampClip = LoadOrSynth("Stamp", 0.14f, t =>
            {
                float thud = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * 90f * t)) * 0.5f
                           + Mathf.Sin(2f * Mathf.PI * 90f * t) * 0.5f; // a short, blunt thump
                return thud * Mathf.Exp(-t * 20f) * 0.55f;
            });

            cashGainClip = LoadOrSynth("CashGain", 0.16f, t =>
            {
                float freq = Mathf.Lerp(880f, 1320f, t / 0.16f); // quick upward chirp — "cha-ching"
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 10f) * 0.4f;
            });

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            var ambienceClip = Resources.Load<AudioClip>(AudioResourcePath + "/Ambience");
            ambienceSource.clip = ambienceClip != null ? ambienceClip : SynthesizeRoomTone();
            ApplyAmbienceVolume();
            ambienceSource.Play();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayClick() => Play(clickClip, 0.8f);
        public void PlayAccept() => Play(acceptClip, 1f);
        public void PlayReject() => Play(rejectClip, 1f);
        public void PlayStamp() => Play(stampClip, 1f);
        public void PlayCashGain() => Play(cashGainClip, 0.9f);

        /// <summary>Called by GameAudioSettings whenever Master/SFX changes, so the ambience bed stays live.</summary>
        public void ApplyAmbienceVolume()
        {
            if (ambienceSource == null) return;
            ambienceSource.volume = GameAudioSettings.Sfx * AmbienceVolumeScale;
        }

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

        /// <summary>
        /// Placeholder "room presence" — a soft low drone plus quiet noise, not
        /// literal crowd chatter (that needs real recordings). Whole-cycle
        /// components over an 8s loop, so it repeats seamlessly.
        /// </summary>
        private static AudioClip SynthesizeRoomTone()
        {
            const float loopSeconds = 8f;
            int samples = Mathf.CeilToInt(loopSeconds * SampleRate);
            var data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)SampleRate;
                float tone = Mathf.Sin(2f * Mathf.PI * 70f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.3f;
                float noise = UnityEngine.Random.Range(-1f, 1f) * 0.15f;
                data[i] = (tone * 0.5f + noise) * 0.18f;
            }

            var clip = AudioClip.Create("RoomTone", samples, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
