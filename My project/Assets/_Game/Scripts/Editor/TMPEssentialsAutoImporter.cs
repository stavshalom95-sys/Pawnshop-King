using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PawnshopKing.EditorTools
{
    /// <summary>
    /// Imports TMP Essential Resources automatically so the "Import TMP Essentials"
    /// dialog never needs clicking (zero-editor-wiring). TextMeshProUGUI is broken
    /// without them — no TMP Settings asset, no default font. No-op once imported.
    /// </summary>
    public static class TMPEssentialsAutoImporter
    {
        private const string PackagePath =
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

        [InitializeOnLoadMethod]
        private static void ImportIfMissing()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;

            string fullPath = Path.GetFullPath(PackagePath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"TMP Essential Resources package not found at {PackagePath} — TMP text will not render until it is imported.");
                return;
            }

            AssetDatabase.ImportPackage(fullPath, false);
            Debug.Log("Pawnshop King: imported TMP Essential Resources automatically.");
        }
    }
}
