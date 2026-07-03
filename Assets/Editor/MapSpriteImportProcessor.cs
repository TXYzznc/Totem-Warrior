using UnityEditor;
using UnityEngine;

namespace TotemWarrior.EditorTools
{
    /// <summary>
    /// 地图美术素材自动导入设置。
    /// </summary>
    public sealed class MapSpriteImportProcessor : AssetPostprocessor
    {
        const string MapSpriteRoot = "Assets/Resources/Sprite/Map/";

        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith(MapSpriteRoot))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = assetPath.Replace('\\', '/').Contains("/Formal/")
                ? FilterMode.Bilinear
                : FilterMode.Point;
            importer.spritePixelsPerUnit = assetPath.Replace('\\', '/').Contains("/Formal/")
                ? 512f
                : 32f;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterPlatformSettings
            {
                maxTextureSize = 512,
                textureCompression = TextureImporterCompression.Uncompressed,
            };
            importer.SetPlatformTextureSettings(settings);
        }
    }
}
