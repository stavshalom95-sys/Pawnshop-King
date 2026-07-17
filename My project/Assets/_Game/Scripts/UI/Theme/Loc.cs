using PawnshopKing.Systems.Localization;
using TMPro;
using UnityEngine;

namespace PawnshopKing.UI
{
    /// <summary>
    /// UI-side localization glue: writes a string into a TMP label with the
    /// correct font and direction for the active language. Hebrew swaps to the
    /// Hebrew-capable font, enables TMP's RTL mode, and pre-reverses embedded
    /// LTR runs (Latin words, digits, prices) so they render forward.
    /// </summary>
    public static class Loc
    {
        public static string T(string key) => LanguageManager.T(key);

        public static string F(string key, params object[] args) => string.Format(LanguageManager.T(key), args);

        /// <summary>Sets localized text on a label. englishFont restores a custom face (e.g. header) when back in English.</summary>
        public static void Set(TMP_Text label, string text, TMP_FontAsset englishFont = null)
        {
            bool rtl = LanguageManager.IsRtl;
            label.isRightToLeftText = rtl;
            label.font = rtl
                ? UITheme.HebrewFont
                : (englishFont != null ? englishFont : TMP_Settings.defaultFontAsset);

            // Header-styled labels (CreateText's header:true) get their spacing
            // set once at creation; re-derive it here rather than reset to 0, or
            // switching languages would silently strip the typewriter look the
            // first time this runs in English. Everything else gets Hebrew's
            // modest breathing-room boost only when actually rendering RTL.
            float englishSpacing = englishFont == UITheme.HeaderFont ? UITheme.HeaderCharacterSpacing : 0f;
            label.characterSpacing = rtl ? UITheme.HebrewCharacterSpacing : englishSpacing;

            label.text = rtl ? UITheme.PrepareRtl(text) : text;

            // Mirrors alignment around the label's original LTR baseline (stashed
            // by CreateText — see LabelBaseAlignment) every time text is set, not
            // just once at creation. Automatic for every label built through the
            // shared helper, so a new call site can't reintroduce left-aligned
            // Hebrew by forgetting a manual flip — that's exactly how dialogue and
            // item-description panels ended up left-aligned in the first place.
            var baseAlignment = label.GetComponent<LabelBaseAlignment>();
            if (baseAlignment != null)
            {
                label.alignment = rtl ? MirrorAlignment(baseAlignment.Value) : baseAlignment.Value;
            }
        }

        internal static TextAlignmentOptions MirrorAlignment(TextAlignmentOptions ltrAlignment)
        {
            switch (ltrAlignment)
            {
                case TextAlignmentOptions.Left: return TextAlignmentOptions.Right;
                case TextAlignmentOptions.Right: return TextAlignmentOptions.Left;
                case TextAlignmentOptions.TopLeft: return TextAlignmentOptions.TopRight;
                case TextAlignmentOptions.TopRight: return TextAlignmentOptions.TopLeft;
                default: return ltrAlignment; // Center and friends need no mirroring.
            }
        }
    }

    /// <summary>
    /// Stashes the alignment a label was created with (its LTR baseline) so
    /// Loc.Set can mirror around it on every call, in both directions, instead
    /// of only being correct the first time or drifting after repeated
    /// language switches. Attached by HUDUIManager.CreateText.
    /// </summary>
    internal class LabelBaseAlignment : MonoBehaviour
    {
        public TextAlignmentOptions Value;
    }

    /// <summary>
    /// Attach-and-forget localization for static labels: binds a key, renders it
    /// for the current language, and re-renders whenever the language changes.
    /// Dynamic labels (with values baked in) should call Loc.Set at refresh time
    /// and re-run their refresh on LanguageManager.LanguageChanged instead.
    /// </summary>
    public class LocalizedLabel : MonoBehaviour
    {
        private TMP_Text label;
        private string key;
        private TMP_FontAsset englishFont;

        public static void Bind(TMP_Text label, string key)
        {
            var localized = label.gameObject.AddComponent<LocalizedLabel>();
            localized.label = label;
            localized.key = key;
            localized.englishFont = label.font; // whatever face the builder chose is the English face
            localized.Refresh();
            LanguageManager.LanguageChanged += localized.Refresh;
        }

        private void OnDestroy()
        {
            LanguageManager.LanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            Loc.Set(label, LanguageManager.T(key), englishFont);
        }
    }
}
