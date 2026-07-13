using TMPro;
using UnityEngine;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Design tokens for the Gritty Noir restyle: semantic palette, header/body fonts,
    /// procedural rounded-corner and soft-shadow sprites, and motion timings.
    /// Everything is generated in code — no editor assets — so zero-editor-wiring holds.
    /// UI construction helpers pull from here; screens never hardcode hex values.
    /// </summary>
    public static class UITheme
    {
        // ---- Noir palette (dark blue-gray base, neon interactive accents) ----
        public static readonly Color Background = Hex(0x0B0E14);
        public static readonly Color Surface = Hex(0x131826);        // panels
        public static readonly Color SurfaceRaised = Hex(0x1A2030);  // rows, inputs
        public static readonly Color TopBarColor = Hex(0x0E1119);
        public static readonly Color TextPrimary = Hex(0xE8ECF4);
        public static readonly Color TextMuted = Hex(0x8A94A8);
        public static readonly Color NeonCyan = Hex(0x41E8D8);       // interactive elements
        public static readonly Color OnNeon = Hex(0x06131A);         // text on neon surfaces
        public static readonly Color Gold = Hex(0xE8B54D);           // cash and value figures
        public static readonly Color GoldBright = Hex(0xF5D07A);     // focus-pulse peak on armed controls
        public static readonly Color Danger = Hex(0xE5484D);         // heat, raids, game over
        public static readonly Color Success = Hex(0x5BC777);
        public static readonly Color DisabledButton = Hex(0x2A3040);
        public static readonly Color DisabledLabel = Hex(0x6B7488);
        public static readonly Color ShadowTint = new Color(0f, 0f, 0f, 0.5f);

        // ---- Motion (subtle; transform/alpha only, unscaled time) ----
        public const float FadeDuration = 0.22f;
        public const float HoverDuration = 0.12f;
        public const float HoverScale = 1.03f;
        public const float PressScale = 0.96f;
        public const float FocusPulsePeriod = 1.1f;                  // seconds per armed-state pulse

        // ---- Typography ----
        public const float HeaderCharacterSpacing = 5f;

        private static TMP_FontAsset headerFont;

        /// <summary>
        /// Typewriter/tech display face for headers, built at runtime from an OS font
        /// (no editor-authored font assets). Falls back to the TMP default sans.
        /// Uses Unity's overloaded null check so a destroyed cache (statics survive
        /// play sessions with domain reload disabled) is rebuilt, not reused.
        /// </summary>
        public static TMP_FontAsset HeaderFont
        {
            get
            {
                if (headerFont != null) return headerFont;

                foreach (var name in new[] { "Consolas", "Courier New", "Lucida Console" })
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (osFont == null) continue;

                    headerFont = TMP_FontAsset.CreateFontAsset(osFont);
                    if (headerFont != null) return headerFont;
                }

                headerFont = TMP_Settings.defaultFontAsset;
                return headerFont;
            }
        }

        private static TMP_FontAsset rtlBodyFont;

        /// <summary>
        /// Dynamic OS font with Hebrew coverage for the onboarding tips — the TMP
        /// default atlas is Latin-only and would render tofu. Glyphs populate on
        /// demand, so no pre-baked atlas asset is needed (zero-editor-wiring).
        /// </summary>
        public static TMP_FontAsset RtlBodyFont
        {
            get
            {
                if (rtlBodyFont != null) return rtlBodyFont;

                foreach (var name in new[] { "Segoe UI", "Arial", "Tahoma" })
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (osFont == null) continue;

                    rtlBodyFont = TMP_FontAsset.CreateFontAsset(osFont);
                    if (rtlBodyFont != null) return rtlBodyFont;
                }

                rtlBodyFont = TMP_Settings.defaultFontAsset;
                return rtlBodyFont;
            }
        }

        /// <summary>
        /// Prepares mixed Hebrew/Latin text for TMP's isRightToLeftText rendering:
        /// TMP reverses the whole string, which corrects logical-order Hebrew but
        /// mirrors embedded Latin ("Inspect" → "tcepsnI"). Pre-reversing each Latin
        /// run — a whole phrase including its internal spaces, so "Next Customer"
        /// keeps its word order — makes it come out forward again. Not a full bidi
        /// engine — enough for one-line tips without nesting or numerals-in-Hebrew.
        /// </summary>
        public static string PrepareRtl(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                "[A-Za-z0-9][A-Za-z0-9 ]*[A-Za-z0-9]|[A-Za-z0-9]",
                match =>
                {
                    var chars = match.Value.ToCharArray();
                    System.Array.Reverse(chars);
                    return new string(chars);
                });
        }

        // ---- Procedural sprites (rounded corners, soft shadows) ----

        private static Sprite roundedSprite;
        private static Sprite shadowSprite;

        /// <summary>9-sliceable rounded rectangle for buttons, panels, and rows.</summary>
        public static Sprite RoundedSprite
        {
            get
            {
                // Unity null check on purpose: statics survive play sessions when
                // domain reload is disabled, and destroyed sprites must be rebuilt.
                if (roundedSprite == null) roundedSprite = BuildRoundedSprite(64, radius: 14, falloff: 1.5f, border: 24);
                return roundedSprite;
            }
        }

        /// <summary>9-sliceable rounded rectangle with a wide alpha falloff — a soft drop shadow.</summary>
        public static Sprite SoftShadowSprite
        {
            get
            {
                if (shadowSprite == null) shadowSprite = BuildRoundedSprite(96, radius: 20, falloff: 22f, border: 44);
                return shadowSprite;
            }
        }

        /// <summary>
        /// Antialiased rounded-rect alpha mask via signed distance; falloff widens the
        /// edge gradient (≈1px = crisp shape, large = shadow blur).
        /// </summary>
        private static Sprite BuildRoundedSprite(int size, int radius, float falloff, int border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            float half = size * 0.5f;
            var inner = new Vector2(half - radius - falloff, half - radius - falloff);
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(Mathf.Abs(x + 0.5f - half), Mathf.Abs(y + 0.5f - half));
                    var q = Vector2.Max(p - inner, Vector2.zero);
                    float distance = q.magnitude - radius;
                    float alpha = Mathf.Clamp01(0.5f - distance / Mathf.Max(falloff, 0.0001f));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);
    }
}
