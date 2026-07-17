using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PawnshopKing.EditorTools
{
    /// <summary>
    /// Manual, on-demand bake of the Hebrew TMP_FontAsset from the project's
    /// checked-in Resources/Fonts/Hebrew.ttf. Run via Tools > Pawnshop King >
    /// Bake Hebrew Font Asset whenever the required character set changes;
    /// it does not run on its own.
    ///
    /// This replaces an earlier version that ran automatically on every
    /// Editor load and sourced glyphs from OS fonts (Segoe UI, falling back
    /// to Arial/Tahoma) via Font.CreateDynamicFontFromOSFont. In this
    /// environment that call fails outright ("Unable to load font face for
    /// [Segoe UI]"), and the bake code didn't check TMP_FontAsset.CreateFontAsset's
    /// result for null before calling TryAddCharacters on it — so a failed OS
    /// font load became an unhandled NullReferenceException on every project
    /// open. Sourcing from the project's own TTF file avoids OS font-face
    /// loading entirely, and running only on explicit request means a bad
    /// bake can never block opening the project again.
    /// </summary>
    public static class FontAssetBaker
    {
        private const string HebrewSourcePath = "Assets/_Game/Resources/Fonts/Hebrew.ttf";
        private const string HebrewOutputPath = "Assets/_Game/Resources/Fonts/HebrewFontAsset.asset";

        // Hebrew base letters + final forms (interspersed in this range, not
        // appended after), geresh/gershayim for loanwords like "וינטאג'",
        // plus the full printable ASCII range so embedded Latin words,
        // digits, and punctuation (PrepareRtl's reversible-run character
        // class) all have glyphs available in the same atlas too.
        private const string HebrewChars = "אבגדהוזחטיכלמנסעפצקרשתךםןףץ׳״";

        [MenuItem("Tools/Pawnshop King/Bake Hebrew Font Asset")]
        public static void BakeHebrew()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(HebrewSourcePath);
            if (sourceFont == null)
            {
                Debug.LogError($"[FontAssetBaker] {HebrewSourcePath} not found — cannot bake. Drop a Hebrew-capable TTF there (see the folder's _README.txt).");
                return;
            }

            var asciiPrintable = new StringBuilder();
            for (char c = (char)0x20; c <= (char)0x7E; c++) asciiPrintable.Append(c);

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            if (fontAsset == null)
            {
                Debug.LogError($"[FontAssetBaker] TMP_FontAsset.CreateFontAsset returned null for {HebrewSourcePath} — the TTF may be corrupt or unreadable. Bake aborted, nothing was written.");
                return;
            }

            fontAsset.TryAddCharacters(HebrewChars + asciiPrintable, out string missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[FontAssetBaker] Hebrew font is missing glyphs after bake: {missing}");
            }

            // Locks the atlas: nothing can populate further glyphs into it at
            // runtime, so the shipped asset behaves as a true static bake.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            string directory = Path.GetDirectoryName(HebrewOutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HebrewOutputPath) != null)
            {
                AssetDatabase.DeleteAsset(HebrewOutputPath);
            }

            AssetDatabase.CreateAsset(fontAsset, HebrewOutputPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FontAssetBaker] Baked {HebrewOutputPath} with {fontAsset.characterTable.Count} glyphs from {HebrewSourcePath}, atlas locked to Static.");
        }
    }
}
