using UnityEngine;

namespace Tattoo.Bot
{
    /// <summary>
    /// change #22 子项 A：给 49 Bot 加色板 + 黑色描边，Smart / Light 两组一眼可分辨。
    ///
    /// 实现：单 SpriteRenderer + 自定义 shader `Tattoo/SpriteTintOutline`
    ///   · Tint       → 走 MaterialPropertyBlock，各 Bot 独立染色，SRP Batcher 合并不破
    ///   · Outline    → shader 内采样 4 邻域 alpha 检测边缘，不再复制 SpriteRenderer 子对象
    ///
    /// sharedMaterial 懒 init 一份全 Bot 共用；shader 找不到时 fallback 到 SpriteRenderer.color 兜底。
    /// </summary>
    public static class BotVisualBinder
    {
        static readonly Color[] SmartColors =
        {
            new Color(0.90f, 0.35f, 0.30f), // 砖红
            new Color(0.95f, 0.55f, 0.25f), // 橘
            new Color(0.85f, 0.75f, 0.30f), // 金黄
            new Color(0.85f, 0.40f, 0.55f), // 玫红
        };

        static readonly Color[] LightColors =
        {
            new Color(0.30f, 0.55f, 0.90f), // 天蓝
            new Color(0.30f, 0.75f, 0.75f), // 青
            new Color(0.55f, 0.55f, 0.90f), // 淡紫
            new Color(0.35f, 0.80f, 0.55f), // 薄荷
        };

        const string ShaderName        = "Tattoo/SpriteTintOutline";
        static readonly int TintId         = Shader.PropertyToID("_Tint");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");

        static Material _sharedMat;
        static MaterialPropertyBlock _mpb;
        static bool _shaderMissing;

        public static void ApplyColorAndOutline(GameObject bot, bool isSmart, int index)
        {
            if (bot == null) return;
            var sr = bot.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null) return;

            var palette = isSmart ? SmartColors : LightColors;
            var color   = palette[Mathf.Abs(index) % palette.Length];

            var mat = EnsureMaterial();
            if (mat == null)
            {
                // fallback：无 shader → 只染色，无描边
                sr.color = color;
                return;
            }

            sr.sharedMaterial = mat;

            _mpb ??= new MaterialPropertyBlock();
            sr.GetPropertyBlock(_mpb);
            _mpb.SetColor(TintId, color);
            sr.SetPropertyBlock(_mpb);
        }

        static Material EnsureMaterial()
        {
            if (_sharedMat != null) return _sharedMat;
            if (_shaderMissing)     return null;

            var sh = Shader.Find(ShaderName);
            if (sh == null)
            {
                _shaderMissing = true;
                FrameworkLogger.Warn("BotVisualBinder",
                    $"Shader={ShaderName} 未找到，Bot 降级到 SpriteRenderer.color（无描边）");
                return null;
            }

            _sharedMat = new Material(sh)
            {
                name      = "BotSharedMat_ch22",
                hideFlags = HideFlags.HideAndDontSave,
            };
            _sharedMat.SetColor(OutlineColorId, Color.black);
            _sharedMat.SetFloat(OutlineWidthId, 3f); // 3 texel 描边（含内圈+外圈检测）
            return _sharedMat;
        }
    }
}
