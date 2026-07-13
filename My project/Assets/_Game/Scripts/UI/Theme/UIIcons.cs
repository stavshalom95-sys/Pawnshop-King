using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Procedural category glyphs for item rows (zero-editor-wiring: drawn into
    /// textures at runtime, no sprite assets). Each ItemCategory gets a simple
    /// shape and a neon accent so items are recognizable at a glance without
    /// reading their names. An ItemDefinition's authored icon, when present,
    /// overrides the category glyph.
    /// </summary>
    public static class UIIcons
    {
        private const int TextureSize = 64;
        private const int Supersamples = 3;

        private static readonly Dictionary<ItemCategory, Sprite> Cache = new Dictionary<ItemCategory, Sprite>();

        private static readonly Vector2[] Star = BuildStarPolygon(new Vector2(0.5f, 0.52f), 0.36f, 0.15f);
        private static readonly Vector2[] Gem =
        {
            new Vector2(0.40f, 0.76f), new Vector2(0.60f, 0.76f), new Vector2(0.70f, 0.62f),
            new Vector2(0.50f, 0.20f), new Vector2(0.30f, 0.62f),
        };
        private static readonly Vector2[] Crown =
        {
            new Vector2(0.24f, 0.34f), new Vector2(0.24f, 0.66f), new Vector2(0.38f, 0.48f),
            new Vector2(0.50f, 0.70f), new Vector2(0.62f, 0.48f), new Vector2(0.76f, 0.66f),
            new Vector2(0.76f, 0.34f),
        };
        private static readonly Vector2[] HourglassTop =
        {
            new Vector2(0.30f, 0.76f), new Vector2(0.70f, 0.76f), new Vector2(0.50f, 0.50f),
        };
        private static readonly Vector2[] HourglassBottom =
        {
            new Vector2(0.30f, 0.24f), new Vector2(0.70f, 0.24f), new Vector2(0.50f, 0.50f),
        };

        /// <summary>Accent per category, tuned against the noir surface colors.</summary>
        public static Color CategoryColor(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Watches: return UITheme.NeonCyan;
                case ItemCategory.Jewelry: return Hex(0xD96BD4);            // neon violet
                case ItemCategory.Electronics: return Hex(0x5B9CF5);        // electric blue
                case ItemCategory.MusicalInstruments: return Hex(0xE8935B); // amber
                case ItemCategory.RetroCollectibles: return Hex(0x7FE85B);  // phosphor green
                case ItemCategory.AntiquesCurios: return UITheme.Gold;
                case ItemCategory.LuxuryAccessories: return Hex(0xE86B7A);  // rose
                default: return Hex(0xA8B4C8);                              // steel (tools/practical)
            }
        }

        public static Sprite CategoryIcon(ItemCategory category)
        {
            // Unity null check on purpose: statics survive play sessions when
            // domain reload is disabled, and destroyed sprites must be rebuilt.
            if (Cache.TryGetValue(category, out var sprite) && sprite != null) return sprite;

            sprite = BuildIcon(category);
            Cache[category] = sprite;
            return sprite;
        }

        /// <summary>
        /// Standard item-row icon: a dark rounded chip holding the glyph. Uses the
        /// definition's authored icon when one exists, else the tinted category glyph.
        /// A null definition falls back to a muted tools glyph rather than nothing.
        /// </summary>
        public static void CreateIconChip(Transform parent, ItemDefinition definition, float size = 48f)
        {
            var chipGO = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            chipGO.transform.SetParent(parent, false);
            var layoutElement = chipGO.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size;
            layoutElement.preferredHeight = size;

            var chip = chipGO.GetComponent<Image>();
            chip.color = UITheme.Surface;
            chip.sprite = UITheme.RoundedSprite;
            chip.type = Image.Type.Sliced;
            chip.raycastTarget = false;

            var glyphGO = new GameObject("Glyph", typeof(RectTransform), typeof(Image));
            glyphGO.transform.SetParent(chipGO.transform, false);
            var glyphRect = (RectTransform)glyphGO.transform;
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = new Vector2(5f, 5f);
            glyphRect.offsetMax = new Vector2(-5f, -5f);

            var glyph = glyphGO.GetComponent<Image>();
            glyph.raycastTarget = false;
            glyph.preserveAspect = true;

            if (definition != null && definition.icon != null)
            {
                glyph.sprite = definition.icon;
                glyph.color = Color.white;
            }
            else
            {
                var category = definition != null ? definition.category : ItemCategory.ToolsPracticalGoods;
                glyph.sprite = CategoryIcon(category);
                glyph.color = definition != null ? CategoryColor(category) : UITheme.TextMuted;
            }
        }

        // ---- Rasterization ---------------------------------------------------

        /// <summary>White-on-transparent glyph, supersampled for smooth edges; color comes from the Image tint.</summary>
        private static Sprite BuildIcon(ItemCategory category)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[TextureSize * TextureSize];
            float step = 1f / (TextureSize * Supersamples);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < Supersamples; sy++)
                    {
                        for (int sx = 0; sx < Supersamples; sx++)
                        {
                            var p = new Vector2(
                                (x * Supersamples + sx + 0.5f) * step,
                                (y * Supersamples + sy + 0.5f) * step);
                            if (Covered(category, p)) hits++;
                        }
                    }

                    float alpha = hits / (float)(Supersamples * Supersamples);
                    pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>Glyph coverage test in normalized icon space (y up).</summary>
        private static bool Covered(ItemCategory category, Vector2 p)
        {
            switch (category)
            {
                case ItemCategory.Watches:
                    // Dial ring, 10:10 hands, and lugs top and bottom.
                    return InRing(p, new Vector2(0.5f, 0.5f), 0.30f, 0.05f)
                        || NearSegment(p, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.68f), 0.035f)
                        || NearSegment(p, new Vector2(0.5f, 0.5f), new Vector2(0.63f, 0.55f), 0.035f)
                        || InBox(p, new Vector2(0.5f, 0.85f), new Vector2(0.08f, 0.05f))
                        || InBox(p, new Vector2(0.5f, 0.15f), new Vector2(0.08f, 0.05f));

                case ItemCategory.Jewelry:
                    return InPolygon(p, Gem);

                case ItemCategory.Electronics:
                    // Device shell with a cut-out screen and a home dot.
                    return (InBox(p, new Vector2(0.5f, 0.5f), new Vector2(0.20f, 0.32f))
                            && !InBox(p, new Vector2(0.5f, 0.55f), new Vector2(0.145f, 0.215f)))
                        || InCircle(p, new Vector2(0.5f, 0.26f), 0.035f);

                case ItemCategory.MusicalInstruments:
                    // Eighth note: head, stem, flag.
                    return InCircle(p, new Vector2(0.40f, 0.30f), 0.085f)
                        || NearSegment(p, new Vector2(0.475f, 0.31f), new Vector2(0.475f, 0.74f), 0.028f)
                        || NearSegment(p, new Vector2(0.475f, 0.74f), new Vector2(0.64f, 0.58f), 0.05f);

                case ItemCategory.RetroCollectibles:
                    return InPolygon(p, Star);

                case ItemCategory.AntiquesCurios:
                    // Hourglass with end caps.
                    return InPolygon(p, HourglassTop)
                        || InPolygon(p, HourglassBottom)
                        || InBox(p, new Vector2(0.5f, 0.80f), new Vector2(0.24f, 0.035f))
                        || InBox(p, new Vector2(0.5f, 0.20f), new Vector2(0.24f, 0.035f));

                case ItemCategory.LuxuryAccessories:
                    return InPolygon(p, Crown)
                        || InBox(p, new Vector2(0.5f, 0.29f), new Vector2(0.26f, 0.045f));

                default: // ToolsPracticalGoods — hammer: diagonal handle, perpendicular head.
                    return NearSegment(p, new Vector2(0.32f, 0.22f), new Vector2(0.58f, 0.58f), 0.045f)
                        || NearSegment(p, new Vector2(0.45f, 0.674f), new Vector2(0.71f, 0.486f), 0.085f);
            }
        }

        // ---- Shape primitives --------------------------------------------------

        private static bool InCircle(Vector2 p, Vector2 center, float radius) =>
            (p - center).sqrMagnitude <= radius * radius;

        private static bool InRing(Vector2 p, Vector2 center, float radius, float halfWidth) =>
            Mathf.Abs((p - center).magnitude - radius) <= halfWidth;

        private static bool InBox(Vector2 p, Vector2 center, Vector2 halfSize)
        {
            var d = p - center;
            return Mathf.Abs(d.x) <= halfSize.x && Mathf.Abs(d.y) <= halfSize.y;
        }

        private static bool NearSegment(Vector2 p, Vector2 a, Vector2 b, float width)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + t * ab)).sqrMagnitude <= width * width;
        }

        /// <summary>Even-odd ray-cast point-in-polygon.</summary>
        private static bool InPolygon(Vector2 p, Vector2[] verts)
        {
            bool inside = false;
            for (int i = 0, j = verts.Length - 1; i < verts.Length; j = i++)
            {
                if ((verts[i].y > p.y) != (verts[j].y > p.y)
                    && p.x < (verts[j].x - verts[i].x) * (p.y - verts[i].y) / (verts[j].y - verts[i].y) + verts[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        private static Vector2[] BuildStarPolygon(Vector2 center, float outerRadius, float innerRadius)
        {
            var verts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                float radius = i % 2 == 0 ? outerRadius : innerRadius;
                verts[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            return verts;
        }

        private static Color Hex(int rgb) => new Color(
            ((rgb >> 16) & 0xFF) / 255f,
            ((rgb >> 8) & 0xFF) / 255f,
            (rgb & 0xFF) / 255f);
    }
}
