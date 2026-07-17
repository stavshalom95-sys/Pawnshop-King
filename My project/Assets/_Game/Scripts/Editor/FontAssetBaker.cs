using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace PawnshopKing.EditorTools
{
    /// <summary>
    /// Bakes real, statically-populated TMP_FontAssets and checks them into
    /// the project (zero-editor-wiring: this runs itself, nothing to click).
    /// Replaces building font assets at runtime every play session — that
    /// path populated glyphs into its atlas dynamically, on demand, as
    /// strings laid out, and playtesting turned up real glyph corruption in
    /// Hebrew text: wrong or missing individual characters inside otherwise-
    /// correctly-ordered words (e.g. "ימים" losing its leading letter to
    /// render as "מים"). Baking every required glyph once, here, and locking
    /// each atlas to Static removes runtime population as a source of that
    /// failure entirely, for both fonts.
    ///
    /// Re-run manually via Tools > Pawnshop King > Bake Font Assets if the
    /// required character set ever changes (e.g. a new string uses a
    /// punctuation mark not covered below).
    /// </summary>
    public static class FontAssetBaker
    {
        private const string HebrewOutputPath = "Assets/_Game/Resources/Fonts/HebrewFontAsset.asset";
        private const string HeaderOutputPath = "Assets/_Game/Resources/Fonts/HeaderFontAsset.asset";

        // Hebrew base letters + final forms (interspersed in this range, not
        // appended after), geresh/gershayim for loanwords like "וינטאג'",
        // plus the full printable ASCII range so embedded Latin words,
        // digits, and punctuation (PrepareRtl's reversible-run character
        // class) all have glyphs available in the same atlas too.
        private const string HebrewChars = "אבגדהוזחטיכלמנסעפצקרשתךםןףץ׳״";

        [InitializeOnLoadMethod]
        private static void BakeIfMissing()
        {
            if (Resources.Load<TMP_FontAsset>("Fonts/HebrewFontAsset") == null)
            {
                BakeHebrew();
            }

            if (Resources.Load<TMP_FontAsset>("Fonts/HeaderFontAsset") == null)
            {
                BakeHeader();
            }
        }

        [MenuItem("Tools/Pawnshop King/Bake Font Assets")]
        public static void BakeAll()
        {
            BakeHebrew();
            BakeHeader();
        }

        private static void BakeHebrew()
        {
            var osFont = TryGetOsFont("Segoe UI", "Arial", "Tahoma");
            if (osFont == null)
            {
                Debug.LogError("[FontAssetBaker] No Hebrew-capable system font found (tried Segoe UI, Arial, Tahoma) — cannot bake.");
                return;
            }

            var asciiPrintable = new StringBuilder();
            for (char c = (char)0x20; c <= (char)0x7E; c++) asciiPrintable.Append(c);

            Bake(osFont, HebrewChars + asciiPrintable, HebrewOutputPath, "Hebrew");
        }

        private static void BakeHeader()
        {
            // Display face for headers/typewriter text — Latin/digits/symbols
            // only, no Hebrew needed here.
            var osFont = TryGetOsFont("Consolas", "Courier New", "Lucida Console");
            if (osFont == null)
            {
                Debug.LogError("[FontAssetBaker] No header display font found (tried Consolas, Courier New, Lucida Console) — cannot bake.");
                return;
            }

            var asciiPrintable = new StringBuilder();
            for (char c = (char)0x20; c <= (char)0x7E; c++) asciiPrintable.Append(c);

            Bake(osFont, asciiPrintable.ToString(), HeaderOutputPath, "Header");
        }

        private static Font TryGetOsFont(params string[] names)
        {
            foreach (var name in names)
            {
                var font = Font.CreateDynamicFontFromOSFont(name, 32);
                if (font != null) return font;
            }
            return null;
        }

        private static void Bake(Font sourceFont, string characters, string outputPath, string label)
        {
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, true);

            fontAsset.TryAddCharacters(characters, out string missing);
            if (!string.IsNullOrEmpty(missing))
            {
                Debug.LogWarning($"[FontAssetBaker] {label} font is missing glyphs after bake: {missing}");
            }

            // Locks the atlas: nothing can populate further glyphs into it at
            // runtime, so the shipped asset behaves as a true static bake —
            // this is the actual fix, not just "pre-built" as a label.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            AssetDatabase.CreateAsset(fontAsset, outputPath);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[FontAssetBaker] Baked {outputPath} with {fontAsset.characterTable.Count} glyphs, atlas locked to Static.");
        }
    }
}
