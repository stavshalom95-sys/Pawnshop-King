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
        private const int NoiseSize = 128;
        private const float GrainAlpha = 0.035f;
        private const float GrainFps = 12f;
        private const float VignetteAlpha = 0.42f;

        private RawImage grain;
        private float grainTimer;

        private void Awake()
        {
            var canvasGO = new GameObject("AtmosphereCanvas", typeof(Canvas));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above everything, including the pause menu

            var vignetteGO = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            vignetteGO.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)vignetteGO.transform);
            var vignette = vignetteGO.GetComponent<Image>();
            vignette.sprite = BuildVignetteSprite();
            vignette.color = new Color(0f, 0f, 0f, VignetteAlpha);
            vignette.raycastTarget = false;

            var grainGO = new GameObject("FilmGrain", typeof(RectTransform), typeof(RawImage));
            grainGO.transform.SetParent(canvasGO.transform, false);
            Stretch((RectTransform)grainGO.transform);
            grain = grainGO.GetComponent<RawImage>();
            grain.texture = BuildNoiseTexture();
            grain.color = new Color(1f, 1f, 1f, GrainAlpha);
            grain.raycastTarget = false;
            grain.uvRect = new Rect(0f, 0f, 12f, 6.75f); // ~160px grain cells at 1080p
        }

        private void Update()
        {
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
                filterMode = FilterMode.Point, // crisp grain, not smeared
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[NoiseSize * NoiseSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color(1f, 1f, 1f, Random.value);
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
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
