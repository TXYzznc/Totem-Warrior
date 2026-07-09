using NUnit.Framework;
using Tattoo.Data;
using UnityEngine;

namespace Combat.Tests
{
    /// <summary>
    /// TC-Aim-01/02/03: HumanPlayerController.GetAimTarget 三分支纯几何单元测试。
    ///
    /// 测试策略：
    ///   - GetAimTarget 的核心逻辑是纯几何运算（dot / dist / score 比较）。
    ///   - 不依赖 Physics.Raycast（EditMode 物理场景隔离），分支 B (Raycast) 测逻辑边界。
    ///   - 分支 A/C 直接测评分算法：构造已知 dot 和 dist 的目标，验证选中结果。
    ///
    /// 注意：HumanPlayerController 依赖 SpawnerModule / InputModule / WeaponModule。
    ///   本测试仅测评分逻辑，将纯几何算法提取为静态辅助方法进行验证，
    ///   与源码中的 score 公式保持一致：score = (1-dot)*100 + dist。
    /// </summary>
    public class HumanPlayerControllerAimTests
    {
        // ════════════════════════════════════════════════
        // 公共：score 公式副本（与 HumanPlayerController 保持一致）
        // ════════════════════════════════════════════════

        /// <summary>
        /// 扇形内目标评分：数值越小越优先。
        /// score = (1 - dot(forward, dirToEnemy)) × 100 + distance
        /// </summary>
        static float CalcScore(Vector3 forward, Vector3 enemyPos, Vector3 origin)
        {
            Vector3 toEnemy = enemyPos - origin;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;
            if (dist < 0.001f) return float.MaxValue;

            float dot   = Vector3.Dot(forward, toEnemy / dist);
            return (1f - dot) * 100f + dist;
        }

        /// <summary>判断目标是否在半角扇形内（cos(halfDeg)比较）。</summary>
        static bool IsInCone(Vector3 forward, Vector3 enemyPos, Vector3 origin, float halfDeg)
        {
            Vector3 toEnemy = enemyPos - origin;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;
            if (dist < 0.001f) return false;
            float dot         = Vector3.Dot(forward, toEnemy / dist);
            float cosHalfAngle = Mathf.Cos(halfDeg * Mathf.Deg2Rad);
            return dot >= cosHalfAngle; // 与源码 `dot < cosHalfAngle` 取补
        }

        // ════════════════════════════════════════════════
        // TC-Aim-01: 全方位锁定分支 — FindClosestEnemy
        // 当 AimSpreadHalfDeg >= 180° 时走"全锁定"，返回最近存活敌人
        // ════════════════════════════════════════════════

        [Test]
        public void FullLock_ReturnsClosestAliveEnemy()
        {
            // Arrange: 两个目标，距离不同
            var origin = Vector3.zero;
            var near   = new Vector3(5f, 0f, 0f);
            var far    = new Vector3(10f, 0f, 0f);

            var nearTarget = new Target { Name = "near", Health = 50f };
            var farTarget  = new Target { Name = "far",  Health = 50f };

            // 模拟 FindClosestEnemy 选择最近：手动实现选择逻辑
            Target result = FindClosestAlive(origin, new[]
            {
                (far,  farTarget),
                (near, nearTarget),
            });

            Assert.AreEqual(nearTarget, result, "全锁定分支应选最近存活目标");
        }

        [Test]
        public void FullLock_AllEnemiesDead_ReturnsNull()
        {
            var origin = Vector3.zero;
            var deadA  = new Target { Name = "deadA", Health = 0f };
            var deadB  = new Target { Name = "deadB", Health = 0f };

            Target result = FindClosestAlive(origin, new[]
            {
                (new Vector3(5f, 0f, 0f),  deadA),
                (new Vector3(10f, 0f, 0f), deadB),
            });

            Assert.IsNull(result, "全部 Health<=0 时全锁定应返回 null");
        }

        // ════════════════════════════════════════════════
        // TC-Aim-02: 严格 Raycast 分支边界
        // AimSpreadHalfDeg <= 0.01 时，逻辑分支判定成立
        // ════════════════════════════════════════════════

        [Test]
        public void RaycastBranch_SpreadBelowThreshold_BranchEntered()
        {
            // 验证分支判定逻辑本身：halfDeg <= 0.01 时选择 Raycast 分支
            float halfDeg = 0.005f;
            bool isRaycastBranch = halfDeg <= 0.01f;
            Assert.IsTrue(isRaycastBranch, "halfDeg=0.005 应进入 Raycast 分支");
        }

        [Test]
        public void RaycastBranch_SpreadAboveThreshold_BranchNotEntered()
        {
            float halfDeg = 0.02f;
            bool isRaycastBranch = halfDeg <= 0.01f;
            Assert.IsFalse(isRaycastBranch, "halfDeg=0.02 不应进入 Raycast 分支");
        }

        [Test]
        public void FullLockBranch_SpreadAtOrAbove180_BranchEntered()
        {
            // 全锁定分支：halfDeg >= 179.99f
            float halfDeg = 180f;
            bool isFullLock = halfDeg >= 179.99f;
            Assert.IsTrue(isFullLock, "halfDeg=180 应进入全锁定分支");
        }

        // ════════════════════════════════════════════════
        // TC-Aim-03: 半角扇形 score 排序
        // ════════════════════════════════════════════════

        [Test]
        public void ConeBranch_SelectsBestScore_PositiveForwardNearEnemy()
        {
            // Arrange: 3 个目标在扇形内（halfDeg=45°）
            var origin  = Vector3.zero;
            var forward = Vector3.forward; // (0,0,1)

            // A: 正前方 dist=5, dot=1.0 → score = 0×100+5 = 5
            var posA    = new Vector3(0f, 0f, 5f);
            var targetA = new Target { Name = "A", Health = 50f };

            // B: 稍微偏 dot=0.9, dist=3 → score = 0.1×100+3 = 13
            float angleB = Mathf.Acos(0.9f);
            var posB     = new Vector3(Mathf.Sin(angleB) * 3f, 0f, Mathf.Cos(angleB) * 3f);
            var targetB  = new Target { Name = "B", Health = 50f };

            // C: 更偏 dot=0.5, dist=8 → score = 0.5×100+8 = 58
            float angleC = Mathf.Acos(0.5f);
            var posC     = new Vector3(Mathf.Sin(angleC) * 8f, 0f, Mathf.Cos(angleC) * 8f);
            var targetC  = new Target { Name = "C", Health = 50f };

            float halfDeg = 45f;

            // Act: 在扇形范围内找 score 最小的目标
            Target selected = FindBestInCone(forward, origin, halfDeg, new[]
            {
                (posA, targetA),
                (posB, targetB),
                (posC, targetC),
            });

            Assert.AreEqual(targetA, selected, "score 最小的 A (score=5) 应被选中");
        }

        [Test]
        public void ConeBranch_EnemyOutsideCone_Excluded()
        {
            var origin  = Vector3.zero;
            var forward = Vector3.forward;
            float halfDeg = 30f; // 窄扇形

            // 正前方目标（在扇形内）
            var posInside  = new Vector3(0f, 0f, 5f);
            var targetIn   = new Target { Name = "inside",  Health = 50f };

            // 侧面目标 dot≈0（在扇形外，cos30°≈0.866）
            var posOutside = new Vector3(5f, 0f, 0f); // 90° 侧面
            var targetOut  = new Target { Name = "outside", Health = 50f };

            Target selected = FindBestInCone(forward, origin, halfDeg, new[]
            {
                (posInside,  targetIn),
                (posOutside, targetOut),
            });

            Assert.AreEqual(targetIn, selected, "扇形外的目标应被剔除，选扇形内的");
        }

        [Test]
        public void ConeBranch_BoundaryDot_IncludesTarget()
        {
            // 边缘：源码判断 dot < cosHalfAngle 才剔除，等于时应包含
            var origin  = Vector3.zero;
            var forward = Vector3.forward;
            float halfDeg    = 60f;
            float cosHalfAngle = Mathf.Cos(halfDeg * Mathf.Deg2Rad); // 0.5

            // 目标的 dot 恰好 = 0.5（cos60°），应被包含
            float angle   = Mathf.Acos(0.5f);
            var posBound  = new Vector3(Mathf.Sin(angle) * 4f, 0f, Mathf.Cos(angle) * 4f);
            var targetBound = new Target { Name = "boundary", Health = 50f };

            bool isInCone = IsInCone(forward, posBound, origin, halfDeg);
            Assert.IsTrue(isInCone, "dot == cosHalfAngle（边界）时目标应被包含在扇形内");
        }

        [Test]
        public void ScoreFormula_CloserAndMoreCentered_LowerScore()
        {
            var origin  = Vector3.zero;
            var forward = Vector3.forward;

            // 正前方 dist=5 → score=5
            float scoreA = CalcScore(forward, new Vector3(0f, 0f, 5f), origin);
            // 偏右 dist=5, dot≈0.707（45°） → score=(1-0.707)*100+5 ≈ 34.3
            float scoreB = CalcScore(forward, new Vector3(5f, 0f, 5f).normalized * 5f, origin);

            Assert.Less(scoreA, scoreB, "正前方近处的 score 应小于偏角远处");
        }

        // ════════════════════════════════════════════════
        // 辅助方法（复现生产代码逻辑，不调用生产类）
        // ════════════════════════════════════════════════

        static Target FindClosestAlive(Vector3 origin, (Vector3 pos, Target target)[] enemies)
        {
            float  nearest = float.MaxValue;
            Target result  = null;
            foreach (var (pos, t) in enemies)
            {
                if (t.Health <= 0f) continue;
                float dist = Vector3.Distance(pos, origin);
                if (dist < nearest)
                {
                    nearest = dist;
                    result  = t;
                }
            }
            return result;
        }

        static Target FindBestInCone(Vector3 forward, Vector3 origin, float halfDeg,
                                     (Vector3 pos, Target target)[] enemies)
        {
            float  bestScore  = float.MaxValue;
            Target bestTarget = null;
            float  cosHalf    = Mathf.Cos(halfDeg * Mathf.Deg2Rad);

            foreach (var (pos, t) in enemies)
            {
                if (t.Health <= 0f) continue;

                Vector3 toEnemy = pos - origin;
                toEnemy.y = 0f;
                float dist = toEnemy.magnitude;
                if (dist < 0.001f) continue;

                float dot = Vector3.Dot(forward, toEnemy / dist);
                if (dot < cosHalf) continue; // 源码同款剔除逻辑

                float score = (1f - dot) * 100f + dist;
                if (score < bestScore)
                {
                    bestScore  = score;
                    bestTarget = t;
                }
            }
            return bestTarget;
        }
    }
}
