using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

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

        // Modest breathing room now that HebrewFont is a well-hinted system
        // font — the earlier 4f was compensating for a font asset whose own
        // glyph metrics were the actual problem (see HebrewFont).
        public const float HebrewCharacterSpacing = 1.5f;

        private static TMP_FontAsset headerFont;

        /// <summary>
        /// Forces every cached font to rebuild from scratch. A lazily-cached
        /// static survives an Editor play session with domain reload disabled —
        /// meaning a code change to CreateFontAssetGenerous's parameters would
        /// silently never take effect for anyone testing without a full domain
        /// reload between changes. Called unconditionally at the very start of
        /// every play session (see SceneInitializer), which IS guaranteed by
        /// Unity regardless of domain-reload settings, so this is the one place
        /// that's safe to assume actually reruns every time.
        /// </summary>
        public static void ResetFontCaches()
        {
            headerFont = null;
            hebrewFont = null;
        }

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

                    headerFont = CreateFontAssetGenerous(osFont);
                    if (headerFont != null) return headerFont;
                }

                headerFont = TMP_Settings.defaultFontAsset;
                return headerFont;
            }
        }

        private static TMP_FontAsset hebrewFont;

        // Every character any Hebrew-language string in the game can contain:
        // the 22 base letters, the 5 final forms (ך ם ן ף ץ — codepoints
        // interspersed among the base letters, not appended after them),
        // geresh/gershayim (used in loanwords like "וינטאג'"), digits, space,
        // and the currency/punctuation set PrepareRtl already treats as a
        // reversible run. Populated into the atlas up front — see HebrewFont.
        private const string HebrewGlyphSet =
            "אבגדהוזחטיכלמנסעפצקרשתךםןףץ" +
            "0123456789 " +
            "$%+-±,.~–'\"()[]{}:;!?";

        /// <summary>
        /// Hebrew-capable font for localized labels — the TMP default atlas is
        /// Latin-only and would render tofu. Built from an OS system font
        /// (Segoe UI on Windows, falling back to Arial/Tahoma) rather than the
        /// project's bundled Hebrew.ttf: that TTF's own glyph metrics were dense
        /// enough that even an aggressively padded, oversampled SDF atlas
        /// (CreateFontAssetGenerous) still rendered overlapping characters at
        /// small UI sizes — the atlas settings were never the real problem, the
        /// source font was. System fonts are hinted for small-scale legibility,
        /// so this uses TMP's own default atlas build (no custom padding or
        /// sampling overrides).
        ///
        /// TryAddCharacters forces every glyph the game can ever display into
        /// the atlas synchronously, right here, instead of leaving TMP to
        /// populate each one dynamically the first time it's laid out. Purely
        /// on-demand population can lay out and render a string in the same
        /// frame its glyphs are still being rasterized, which is what produced
        /// wrong-looking characters in already-correct Hebrew strings (letters
        /// were never reordered — see PrepareRtl, which never touches Hebrew
        /// codepoints) — pre-warming removes that timing window entirely.
        /// </summary>
        public static TMP_FontAsset HebrewFont
        {
            get
            {
                if (hebrewFont != null) return hebrewFont;

                foreach (var name in new[] { "Segoe UI", "Arial", "Tahoma" })
                {
                    var osFont = Font.CreateDynamicFontFromOSFont(name, 32);
                    if (osFont == null) continue;

                    var candidate = TMP_FontAsset.CreateFontAsset(osFont);
                    if (candidate == null) continue;

                    candidate.TryAddCharacters(HebrewGlyphSet, out string missing);
                    if (!string.IsNullOrEmpty(missing))
                    {
                        Debug.LogWarning($"[UITheme] Hebrew font '{name}' is missing glyphs: {missing}");
                    }

                    hebrewFont = candidate;
                    return hebrewFont;
                }

                Debug.LogWarning("[UITheme] No Hebrew-capable system font found (tried Segoe UI, Arial, Tahoma) — Hebrew text will render as boxes.");
                hebrewFont = TMP_Settings.defaultFontAsset;
                return hebrewFont;
            }
        }

        /// <summary>
        /// Builds HeaderFont's monospace display face with extra atlas padding
        /// and sampling resolution — headers render large, so the bare default
        /// overload's padding can look thin at that scale. Hebrew no longer
        /// uses this; see HebrewFont for why a plain default build works better
        /// for that font.
        /// </summary>
        private static TMP_FontAsset CreateFontAssetGenerous(Font sourceFont) =>
            TMP_FontAsset.CreateFontAsset(sourceFont, 180, 18, GlyphRenderMode.SDFAA, 2048, 2048);

        /// <summary>
        /// Prepares mixed Hebrew/Latin text for TMP's isRightToLeftText rendering:
        /// TMP reverses the whole string, which corrects logical-order Hebrew but
        /// mirrors embedded Latin ("Inspect" → "tcepsnI"). Pre-reversing each Latin
        /// run — a whole phrase including its internal spaces, so "Next Customer"
        /// keeps its word order — makes it come out forward again.
        ///
        /// Brackets need a second, independent fix: TMP's reversal is purely
        /// positional (it never swaps which glyph renders at a position), so a
        /// literal "(" and ")" just trade places without trading shape, landing
        /// as ")2(" instead of "(2)". Each bracket is pre-swapped to its mirror
        /// character on its own (not folded into the Latin-run reversal above),
        /// so TMP's later positional reversal puts the correct glyph in the
        /// correct spot — this also fixes standalone brackets around Hebrew text,
        /// like the "[Type]" tag on the counter's mood line.
        ///
        /// Currency/number symbols need the SAME run-reversal treatment as
        /// letters, not just the digits: "$5,000" with only digits in the run
        /// class splits into "5" + "," + "000" as three separate fragments, and
        /// TMP's later whole-string reversal scrambles their relative order into
        /// "000,5$". Folding $ % + - ± , . into the same character class as
        /// letters/digits keeps a signed currency figure ("+$240", "-$85") as one
        /// reversible unit, so it comes out forward and in the right place.
        ///
        /// Not a full bidi engine — enough for one-line UI strings without
        /// nesting or other mirrored punctuation (guillemets etc).
        /// </summary>
        public static string PrepareRtl(string text)
        {
            // Rich-text tags (<color=...>, <size=...>) match first and pass through
            // untouched — reversing their contents would break TMP's parser.
            // "–" (en dash) and "~" cover value-range clues like "~$320–$440" —
            // without them the dash/tilde fall outside the reversible run and
            // land on the wrong side of the numbers after TMP's own reversal,
            // same failure mode the ASCII hyphen fix addressed for signed deltas.
            const string runChars = "A-Za-z0-9$%+\\-±,.~–";
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                $"<[^>]*>|[{runChars}][{runChars} ]*[{runChars}]|[{runChars}]|[()\\[\\]{{}}]",
                match =>
                {
                    string value = match.Value;
                    if (value[0] == '<') return value;

                    if (value.Length == 1)
                    {
                        char mirrored = MirrorBracket(value[0]);
                        if (mirrored != value[0]) return mirrored.ToString();
                    }

                    var chars = value.ToCharArray();
                    System.Array.Reverse(chars);
                    return new string(chars);
                });
        }

        /// <summary>Bidi mirror-pair for a bracket character; returns the input unchanged for anything else.</summary>
        private static char MirrorBracket(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                default: return c;
            }
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
