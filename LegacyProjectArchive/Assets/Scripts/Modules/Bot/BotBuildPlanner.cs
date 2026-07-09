using System.Collections.Generic;
using Tattoo.Data;

namespace Tattoo.Bot
{
    /// <summary>
    /// v2.1 Bot Build 决策器（无状态工具类）。
    /// 输入：preset / 当前已刻 build / Run 进度 / 拥有资源（本期仅金币占位）
    /// 输出：下一个想刻的 slot（PartId / ColorId / PatternId）+ 是否要刻
    ///
    /// 实现策略：
    ///   1) 优先按 preset.RecommendedArr 顺序补槽 —— 检查当前 build 中 PartId 未占用即可
    ///   2) RecommendedArr 用完或全占用 → 回退到 preset.PreferredPartsArr：找第一个未占用部位 +
    ///      按 preset.TendencyVec argmax 选元素 → 默认图案 Line(1)
    ///   3) 所有部位都占用 → 返回 false（不再刻新槽，下一阶段交给附魔规划器）
    ///
    /// 0 GC：内部不分配 List/Array；只读 preset 已 parse 好的字段。
    /// </summary>
    public static class BotBuildPlanner
    {
        const int SkipPattern = 1; // Line / SingleHit 兜底

        /// <summary>核心 API。</summary>
        /// <param name="preset">Bot 偏好的 build preset（已 Load 时 parse）</param>
        /// <param name="currentBuild">Bot 当前已经刻好的槽（按 PartId 唯一）</param>
        /// <param name="next">下一槽位</param>
        /// <returns>true 表示有 slot 想刻；false 表示已经刻满或无候选</returns>
        public static bool PlanNext(
            BotBuildPresetRow preset,
            IReadOnlyList<TattooSlot> currentBuild,
            out BotPlannedSlot next)
        {
            next = default;
            if (preset == null) return false;

            // ===== 路径 A：跟 RecommendedArr =====
            if (preset.RecommendedArr != null)
            {
                for (int i = 0; i < preset.RecommendedArr.Length; i++)
                {
                    var plan = preset.RecommendedArr[i];
                    if (!HasPart(currentBuild, plan.PartId))
                    {
                        next = plan;
                        return true;
                    }
                }
            }

            // ===== 路径 B：preferredParts + tendency argmax =====
            if (preset.PreferredPartsArr != null && preset.TendencyVec != null)
            {
                int colorId = TendencyArgmaxColor(preset.TendencyVec);
                for (int i = 0; i < preset.PreferredPartsArr.Length; i++)
                {
                    int partId = preset.PreferredPartsArr[i];
                    if (!HasPart(currentBuild, partId))
                    {
                        next = new BotPlannedSlot(partId, colorId, SkipPattern);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>v2.1 安全度 → 是否可以蹲下来读条（Smart AI 专用）。</summary>
        /// <param name="safetyScore">0..1：1=完全安全，0=被围殴</param>
        /// <param name="boldness">BotConfig.SelfTattooBoldness 0..1</param>
        public static bool ShouldStartReading(float safetyScore, float boldness)
        {
            // 越大胆的 AI 阈值越低（更容易开读条），但仍需 safety > (1 - boldness * 0.8)
            float threshold = 1f - boldness * 0.8f;
            return safetyScore >= threshold;
        }

        // ===== helpers =====
        static bool HasPart(IReadOnlyList<TattooSlot> build, int partId)
        {
            if (build == null) return false;
            for (int i = 0; i < build.Count; i++)
            {
                if (build[i].PartId == partId) return true;
            }
            return false;
        }

        static int TendencyArgmaxColor(float[] tendency)
        {
            // TendencyVec[i] 对应 ColorId = i + 1（1..7）
            int best = 0;
            float bestVal = -1f;
            for (int i = 0; i < tendency.Length; i++)
            {
                if (tendency[i] > bestVal) { bestVal = tendency[i]; best = i; }
            }
            return best + 1;
        }
    }
}
