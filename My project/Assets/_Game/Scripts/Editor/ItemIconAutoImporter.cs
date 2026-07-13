using UnityEditor;

namespace PawnshopKing.EditorTools
{
    /// <summary>
    /// Makes Resources/ItemIcons a true drop folder (zero-editor-wiring): any
    /// texture saved there is imported as a single UI sprite automatically.
    /// Without this, Unity imports PNGs as plain Texture2D and
    /// Resources.Load&lt;Sprite&gt; silently returns null for them.
    /// </summary>
    public class ItemIconAutoImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/ItemIcons/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
        }
    }
}
