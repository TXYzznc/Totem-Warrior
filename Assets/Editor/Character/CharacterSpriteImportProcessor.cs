using UnityEditor;
using UnityEngine;

namespace Tattoo.EditorTools.Character
{
    /// <summary>
    /// 角色 sprite sheet 自动导入设置。
    ///
    /// 范围：仅 Assets/Resources/Sprite/Character/ 下的贴图（含子目录）。
    /// 触发：首次导入或贴图变更时自动重跑（Unity AssetPostprocessor 标准生命周期）。
    ///
    /// 每张 .png 都是 4 帧水平排列的 sprite sheet，自动切为 4 等份。
    /// PixelsPerUnit = 64，FilterMode = Point（像素美术）。
    /// </summary>
    public sealed class CharacterSpriteImportProcessor : AssetPostprocessor
    {
        const string CharacterSpriteRoot = "Assets/Resources/Sprite/Character/";

        void OnPreprocessTexture()
        {
            var normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(CharacterSpriteRoot)) return;

            var importer = (TextureImporter)assetImporter;

            // 基础设置
            importer.textureType       = TextureImporterType.Sprite;
            importer.spriteImportMode  = SpriteImportMode.Multiple;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled     = false;
            importer.filterMode        = FilterMode.Point;
            // PPU=256: codex 输出 1536x1024 sprite sheet（每帧 384x1024）
            // → 角色高 ≈ 1024/256 = 4 units，再叠 SpawnerModule 的 localScale 0.8 后约 3.2 units 高，
            // 对 (0,18,-10) 55° 俯角相机来说在合理体量；改原 PPU=64 会把角色撑到 16 units 高、完全遮挡视线。
            importer.spritePixelsPerUnit = 256f;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;

            // 读取贴图宽高以计算 4 帧等分 Rect
            // 注意：此时贴图尚未写入磁盘，需通过 TextureImporter 拿到原始尺寸
            importer.GetSourceTextureWidthAndHeight(out int texWidth, out int texHeight);

            if (texWidth > 0 && texHeight > 0)
            {
                int frameWidth = texWidth / 4;

                var rects = new SpriteMetaData[4];
                for (int i = 0; i < 4; i++)
                {
                    rects[i] = new SpriteMetaData
                    {
                        name   = $"{System.IO.Path.GetFileNameWithoutExtension(assetPath)}_{i}",
                        rect   = new Rect(i * frameWidth, 0, frameWidth, texHeight),
                        pivot  = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero,
                    };
                }
                importer.spritesheet = rects;
            }

            var platformSettings = new TextureImporterPlatformSettings
            {
                maxTextureSize     = 2048,
                textureCompression = TextureImporterCompression.Uncompressed,
            };
            importer.SetPlatformTextureSettings(platformSettings);
        }
    }
}
