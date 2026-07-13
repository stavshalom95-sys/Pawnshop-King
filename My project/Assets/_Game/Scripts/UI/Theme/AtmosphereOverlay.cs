using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Film grain + vignette over every screen (raycast-transparent, so clicks
    /// pass straight through). Both textures are generated at runtime — no art
    /// assets (zero-editor-wiring). The grain re-jitters its UV offset a few
    /// times a second on unscaled time, so the film keeps rolling even paused.
    /// </summary>
    public class AtmosphereOverlay : MonoBehaviour
    {
        /// <summary>One-flag kill switch: false keeps the vignette and gradient but drops the grain entirely.</summary>
        private const bool EnableGrain = true;

        private const int NoiseSize = 128;
        private const float GrainAlpha = 0.012f;   // barely-there shimmer, not visible noise
        private const float GrainFps = 6f;         // gentle roll, not a flicker
        private const float VignetteAlpha = 0.42f;
        private const float GradientBottomAlpha = 0.20f; // static lamplight falloff
        private const float GradientTopAlpha = 0.10f;

        private RawImage grain;
        private float grainTimer;

        private void Awake()
        {
            var canvasGO = new GameObject("AtmosphereCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above everything, including the pause menu

            // Static vertical gradient: soft darkening toward top and bottom edges —
            // depth without any motion at all.
            var gradientGO = new GameObject("Gradient", typeof(RectTransform), typeof(Image));
            gradientGO.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)gradientGO.transform);
            var gradient = gradientGO.GetComponent<Image>();
            gradient.sprite = BuildGradientSprite();
            gradient.color = Color.black;
            gradient.raycastTarget = false;

            var vignetteGO = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            vignetteGO.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)vignetteGO.transform);
            var vignette = vignetteGO.GetComponent<Image>();
            vignette.sprite = BuildVignetteSprite();
            vignette.color = new Color(0f, 0f, 0f, VignetteAlpha);
            vignette.raycastTarget = false;

#pragma warning disable CS0162 // intentional compile-time switch
            if (!EnableGrain) return;

            var grainGO = new GameObject("FilmGrain", typeof(RectTransform), typeof(RawImage));
            grainGO.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)grainGO.transform);
            grain = grainGO.GetComponent<RawImage>();
            grain.texture = BuildNoiseTexture();
            grain.color = new Color(1f, 1f, 1f, GrainAlpha);
            grain.raycastTarget = false;
            grain.uvRect = new Rect(0f, 0f, 5f, 2.8f); // large soft cells, not pixel noise
#pragma warning restore CS0162
        }

        private void Update()
        {
            if (grain == null) return;

            grainTimer += Time.unscaledDeltaTime;
            if (grainTimer < 1f / GrainFps) return;

            grainTimer = 0f;
            var uv = grain.uvRect;
            grain.uvRect = new Rect(Random.value, Random.value, uv.width, uv.height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Texture2D BuildNoiseTexture()
        {
            var texture = new Texture2D(NoiseSize, NoiseSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear, // smeared soft grain, no hard pixels
                hideFlags = HideFlags.HideAndDontSave,
            };

            // Low-contrast noise: alpha stays in a narrow band around the middle,
            // so the effect reads as texture rather than static.
            var pixels = new Color[NoiseSize * NoiseSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1f, 1f, 1f, 0.35f + 0.3f * Random.value);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>Soft vertical falloff: strongest at the bottom edge, faint at the top, clear in the middle.</summary>
        private static Sprite BuildGradientSprite()
        {
            const int height = 256;
            var texture = new Texture2D(4, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1); // 0 = bottom, 1 = top
                float bottom = Mathf.Clamp01(1f - t / 0.35f);           // fades out by 35% height
                float top = Mathf.Clamp01((t - 0.75f) / 0.25f);         // fades in over the top 25%
                float alpha = GradientBottomAlpha * bottom * bottom + GradientTopAlpha * top * top;
                for (int x = 0; x < 4; x++) pixels[y * 4 + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, 4, height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>Radial darkening: transparent center, easing to full alpha at the corners.</summary>
        private static Sprite BuildVignetteSprite()
        {
            const int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.Clamp01((radius - 0.55f) / 0.65f);
                    float alpha = t * t * (3f - 2f * t); // smoothstep to the edge
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
