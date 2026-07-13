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
            label.text = rtl ? UITheme.PrepareRtl(text) : text;
        }
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
