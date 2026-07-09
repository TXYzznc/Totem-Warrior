/// <summary>
/// HitsparkBehaviour — MonoBehaviour，订阅 WeaponAttackHitEvent，在命中点播放 hitspark 粒子。
///
/// *** 用户手动任务 ***
/// 需在 Unity Editor 中手动创建 Prefab：
///   路径：Assets/Resources/Prefab/VFX/Hitspark.prefab
///   组成：空 GameObject + ParticleSystem 组件（默认参数即可，运行时会通过代码覆盖参数）
///   步骤：Hierarchy 新建空 GO -> 添加 ParticleSystem 组件 -> 拖到上述 Resources 路径 -> 保存
/// 若 Prefab 缺失，代码会 Warn 日志并跳过，不抛异常（保证游戏不因 VFX 缺失崩溃）。
///
/// 对象池：pool size 16，普攻 Burst 8，暴击 Burst 14。
/// 颜色：白心 + 橙红 #FF6622 外环（通过 ColorOverLifetime 渐变实现）。
/// Variant 说明：无 multi_compile，0 额外变体开销。
/// </summary>

using System;
using System.Collections.Generic;
using Tattoo;
using Tattoo.Data;
using UnityEngine;
using Weapon.Events;

namespace Tattoo.VFX
{
    public sealed class HitsparkBehaviour : MonoBehaviour
    {
        // ===== 池参数 =====
        const int PoolSize = 16;
        const int BurstNormal = 8;
        const int BurstCrit   = 14;

        // ===== 内部状态 =====
        EventBus _bus;
        IDisposable _sub;
        readonly Queue<ParticleSystem> _pool = new();
        readonly List<ParticleSystem>  _active = new();

        // ===== 颜色 =====
        static readonly Color ColorCore  = Color.white;
        static readonly Color ColorOuter = new Color(1f, 0.4f, 0.133f, 1f); // #FF6622

        // ===== 初始化（由挂载者或 VFXModule 调用） =====

        /// <summary>
        /// 注入 EventBus 并初始化对象池。
        /// </summary>
        public void Initialize(EventBus bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));

            // 尝试加载 Prefab
            var prefab = Resources.Load<GameObject>("Prefab/VFX/Hitspark");
            if (prefab == null)
            {
                FrameworkLogger.Warn("HitsparkBehaviour",
                    "Action=Initialize Prefab=Prefab/VFX/Hitspark missing — " +
                    "请在 Assets/Resources/Prefab/VFX/Hitspark.prefab 手动创建 Prefab（空 GO + ParticleSystem）。" +
                    "HitsparkBehaviour 将跳过所有命中特效直到 Prefab 存在。");
            }

            // 建立池
            for (int i = 0; i < PoolSize; i++)
            {
                var ps = CreateParticleSystem(prefab);
                if (ps != null)
                {
                    ps.gameObject.SetActive(false);
                    _pool.Enqueue(ps);
                }
            }

            _sub = _bus.Subscribe<WeaponAttackHitEvent>(OnHit);
            FrameworkLogger.Info("HitsparkBehaviour",
                $"Action=Initialized PoolSize={_pool.Count} PrefabLoaded={prefab != null}");
        }

        void OnDestroy()
        {
            _sub?.Dispose();
            _sub = null;
        }

        // ===== 事件处理 =====

        void OnHit(WeaponAttackHitEvent e)
        {
            if (_pool.Count == 0) return; // 池耗尽静默跳过

            // 决定位置：Target 不为空则用 target.position + up*0.5
            Vector3 pos = Vector3.zero;
            if (e.Target != null)
            {
                // 尝试找 Target 对应 GameObject（通过场景 EntityRef）
                var targetGo = FindTargetGameObject(e.Target);
                pos = targetGo != null
                    ? targetGo.transform.position + Vector3.up * 0.5f
                    : Vector3.up * 0.5f;
            }

            var ps = _pool.Dequeue();
            if (ps == null) return;

            // 配置粒子参数（运行时覆盖，prefab 参数仅为占位）
            ConfigureParticles(ps, e.IsCrit);

            ps.gameObject.SetActive(true);
            ps.transform.position = pos;
            ps.Play();
            _active.Add(ps);
        }

        // ===== Update：回收已完成粒子 =====

        void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var ps = _active[i];
                if (ps == null) { _active.RemoveAt(i); continue; }
                if (!ps.IsAlive(true))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                    _active.RemoveAt(i);
                    _pool.Enqueue(ps);
                }
            }
        }

        // ===== 粒子参数配置 =====

        static void ConfigureParticles(ParticleSystem ps, bool isCrit)
        {
            int burstCount = isCrit ? BurstCrit : BurstNormal;

            // Main
            var main = ps.main;
            main.duration       = 0.3f;
            main.loop           = false;
            main.startLifetime  = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startSpeed     = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize      = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor     = ColorCore;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles   = 80;

            // Emission：单次 Burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });
            emission.rateOverTime = 0f;

            // Shape：Cone / 圆锥向上
            var shape = ps.shape;
            shape.enabled    = true;
            shape.shapeType  = ParticleSystemShapeType.Cone;
            shape.angle      = 35f;
            shape.radius     = 0.05f;

            // Color over Lifetime：白心 -> 橙红外环 -> 透明
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(ColorCore,  0f),
                    new GradientColorKey(ColorOuter, 0.4f),
                    new GradientColorKey(ColorOuter, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(1f,  0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f,  1f),
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        // ===== 辅助：根据 Target 查找场景 GameObject =====

        static GameObject FindTargetGameObject(Target target)
        {
            if (target == null) return null;
            // 遍历场景中的 EntityRef 找到匹配的 GO
            var refs = FindObjectsByType<EntityRef>(FindObjectsSortMode.None);
            foreach (var r in refs)
                if (r != null && r.Target == target) return r.gameObject;
            return null;
        }

        // ===== 工厂：从 prefab 或临时 GO 创建 ParticleSystem =====

        static ParticleSystem CreateParticleSystem(GameObject prefab)
        {
            GameObject go;
            if (prefab != null)
            {
                go = Instantiate(prefab);
            }
            else
            {
                // Prefab 缺失时创建占位 GO（仅在池建立阶段调用）
                go = new GameObject("Hitspark_Placeholder");
                go.AddComponent<ParticleSystem>();
            }
            go.name = "Hitspark_Pooled";

            var ps = go.GetComponent<ParticleSystem>();
            if (ps == null) ps = go.AddComponent<ParticleSystem>();

            // 停止自动播放
            var main = ps.main;
            main.playOnAwake = false;

            return ps;
        }
    }
}
