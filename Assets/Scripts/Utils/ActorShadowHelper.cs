using UnityEngine;

namespace Tattoo.Utils
{
    /// <summary>
    /// change #22 子项 C：给玩家 / 敌人 / Bot 脚下挂一张黑色半透明径向渐变阴影，增强空间感。
    ///
    /// 素材：运行时生成 64×64 Texture2D + Sprite.Create，全项目共用一份缓存（不落 PNG，避免手动出图）。
    /// 性能：单张 sprite 复用，所有 shadow SpriteRenderer 走 SRP Batcher 合并；49 Bot × 1 shadow 无额外 SetPass。
    /// </summary>
    public static class ActorShadowHelper
    {
        const string ShadowName = "Shadow_ch22";
        const int TexSize = 64;
        const float DefaultRadius = 0.5f;
        const float DefaultYOffset = -0.4f;

        static Sprite _shadowSprite;

        public static void Attach(GameObject actor, float radius = DefaultRadius, float yOffset = DefaultYOffset)
        {
            if (actor == null) return;
            if (actor.transform.Find(ShadowName) != null) return;

            EnsureSprite();

            var shadow = new GameObject(ShadowName);
            shadow.transform.SetParent(actor.transform, false);
            shadow.transform.localPosition = new Vector3(0f, yOffset, 0f);
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale    = Vector3.one * radius * 2f;

            var sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = _shadowSprite;
            sr.color  = new Color(0f, 0f, 0f, 0.4f);
            sr.sortingOrder = -100;
        }

        static void EnsureSprite()
        {
            if (_shadowSprite != null) return;

            var tex = new Texture2D(TexSize, TexSize, TextureFormat.RGBA32, false)
            {
                name       = "ShadowSprite_ch22",
                wrapMode   = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags  = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[TexSize * TexSize];
            float cx = (TexSize - 1) * 0.5f;
            float cy = (TexSize - 1) * 0.5f;
            float maxR = TexSize * 0.5f;

            for (int y = 0; y < TexSize; y++)
            {
                for (int x = 0; x < TexSize; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / maxR;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a; // 平方衰减，边缘更快消失
                    pixels[y * TexSize + x] = new Color32(0, 0, 0, (byte)(a * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);

            _shadowSprite = Sprite.Create(
                tex,
                new Rect(0, 0, TexSize, TexSize),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: TexSize
            );
            _shadowSprite.name = "ShadowSprite_ch22";
            _shadowSprite.hideFlags = HideFlags.HideAndDontSave;
        }
    }
}
