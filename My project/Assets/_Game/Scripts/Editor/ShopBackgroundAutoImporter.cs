using UnityEditor;

namespace PawnshopKing.EditorTools
{
    /// <summary>
    /// Makes Resources/Backgrounds a drop folder for room art (zero-editor-wiring):
    /// any image saved there imports as a single UI sprite automatically, so
    /// ShopSceneBackdrop's Resources.Load&lt;Sprite&gt; finds it without manual
    /// Inspector configuration.
    /// </summary>
    public class ShopBackgroundAutoImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Backgrounds/")) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
        }
    }
}
