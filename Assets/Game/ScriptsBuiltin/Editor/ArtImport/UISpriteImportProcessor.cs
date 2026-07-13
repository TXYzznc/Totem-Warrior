using UnityEditor;
using UnityEngine;

namespace TotemWarrior.EditorTools
{
    internal static class UISpriteImportSettings
    {
        internal const string UISpriteRoot = "Assets/Game/Sprite/UI/";
        internal const int DefaultMaxTextureSize = 2048;
        internal const int DefaultPixelsPerUnit = 100;

        internal static void Apply(TextureImporter importer, int maxTextureSize)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.spritePixelsPerUnit = DefaultPixelsPerUnit;
            importer.textureCompression = TextureImporterCompression.Compressed;

            TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
            platformSettings.maxTextureSize = maxTextureSize;
            platformSettings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(platformSettings);
        }
    }

    /// <summary>
    /// Applies default import settings for UI sprites under Assets/Game/Sprite/UI.
    /// </summary>
    public sealed class UISpriteImportProcessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(UISpriteImportSettings.UISpriteRoot))
            {
                return;
            }

            UISpriteImportSettings.Apply((TextureImporter)assetImporter, UISpriteImportSettings.DefaultMaxTextureSize);
        }
    }
}
