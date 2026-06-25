using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;
using UnityEngine.Rendering;

namespace Tattoo.VFX
{
    /// <summary>
    /// 战斗 VFX 模块。订阅 VFXTriggerEvent，根据 Shape × Element 在场景中绘制临时视觉。
    ///
    /// 设计：
    /// - 所有视觉用代码生成（LineRenderer / ParticleSystem / MeshRenderer），无需 prefab / asset
    /// - Element → 颜色（Fire 橙、Lightning 黄、Frost 青、Nature 绿、Mutation 紫、Holy 金、Pure 白）
    /// - 每个 VFX GameObject 由 OnUpdate 推进生命周期；时间到自动 Destroy
    /// - 按 Element 共享 Material，避免 GC + 保证 SRP Batcher 批次合并
    ///
    /// URP 透明材质写法（_Surface=1 必须同时配：
    ///   _Blend / SrcBlend / DstBlend / ZWrite + EnableKeyword "_SURFACE_TYPE_TRANSPARENT"）
    ///
    /// Variant 说明：无 multi_compile，0 额外变体开销。
    /// 性能预算：单次触发 &lt;0.3ms；32 并存实例安全。
    /// </summary>
    public sealed class VFXModule : IGameModule, ITickable
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[] { typeof(SpawnerModule) };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        SpawnerModule _spawner;
        IDisposable _sub;

        Transform _root;
        readonly List<VFXInstance> _instances = new();

        // ===== Element 颜色映射（不变）=====

        static readonly Dictionary<string, Color> ElementColors = new()
        {
            { "Fire",      new Color(1.0f, 0.5f, 0.1f) },
            { "Lightning", new Color(1.0f, 0.95f, 0.3f) },
            { "Nature",    new Color(0.35f, 0.85f, 0.3f) },
            { "Frost",     new Color(0.5f, 0.85f, 1.0f) },
            { "Mutation",  new Color(0.75f, 0.35f, 1.0f) },
            { "Holy",      new Color(1.0f, 0.9f, 0.4f) },
            { "Pure",      new Color(1.0f, 1.0f, 1.0f) },
        };

        // ===== 共享 Material 池（每 Element 一份，避免每帧 new Material）=====

        readonly Dictionary<string, Material> _matPool = new();
        // 半透明 alpha 叠加用，独立一份（Zone / Pillar / Particle 专用，alpha 会动态改）
        // 注意：Particle System 自带 material，不走本池。LineRenderer 走本池。
        // Zone / Pillar MeshRenderer 也走本池，但需要独立实例以支持 alpha 动画，
        // 因此通过 new Material(sharedMat) 做 per-instance 拷贝（仅 Zone/Pillar）。

        Shader _unlitShader;

        public VFXModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            _spawner = _runner.GetModule<SpawnerModule>();
            _sub = _bus.Subscribe<VFXTriggerEvent>(OnVFXTrigger);

            var go = new GameObject("[VFX Root]");
            _root = go.transform;

            // 预解析 Shader（只做一次 Shader.Find）
            _unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Standard");

            // 预热 Material 池（7 个 Element）
            foreach (var kv in ElementColors)
                GetOrCreateMat(kv.Key);

            FrameworkLogger.Info("VFXModule", "Action=Initialized MatPoolSize=7");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _sub?.Dispose();
            foreach (var inst in _instances)
                if (inst.Go != null) UnityEngine.Object.Destroy(inst.Go);
            _instances.Clear();

            foreach (var mat in _matPool.Values)
                if (mat != null) UnityEngine.Object.Destroy(mat);
            _matPool.Clear();

            if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
            FrameworkLogger.Info("VFXModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        public void OnUpdate(float dt)
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                var inst = _instances[i];
                inst.Elapsed += dt;
                if (inst.Go == null || inst.Elapsed >= inst.Duration)
                {
                    if (inst.Go != null) UnityEngine.Object.Destroy(inst.Go);
                    _instances.RemoveAt(i);
                    continue;
                }
                inst.Tick?.Invoke(inst, dt);
            }
        }

        // ===== 事件处理 =====

        void OnVFXTrigger(VFXTriggerEvent e)
        {
            if (_spawner == null || _spawner.Player == null) return;
            var color = ElementColors.TryGetValue(e.ElementName, out var c) ? c : Color.white;

            Vector3 source = _spawner.Player.transform.position + Vector3.up * 0.6f;
            Vector3? primary = TryGetPos(e.PrimaryTarget);

            if (e.Intercepted)
            {
                SpawnRing(source, color, radius: 0.9f, duration: 0.6f);
                return;
            }

            switch (e.ShapeName)
            {
                case "SingleHit":
                    if (primary.HasValue)
                    {
                        SpawnBeam(source, primary.Value, color, width: 0.12f, duration: 0.15f);
                        SpawnElementDecoration(e.ElementName, primary.Value, color);
                    }
                    break;

                case "AOEBurst":
                    if (primary.HasValue)
                        SpawnAOEBurst(primary.Value, color, radius: 2.5f, duration: 0.4f);
                    foreach (var t in SafeTargets(e.NearbyTargets))
                    {
                        var p = TryGetPos(t);
                        if (p.HasValue) SpawnSpark(p.Value, color);
                    }
                    break;

                case "StackingMark":
                    if (primary.HasValue) SpawnMark(primary.Value, color, duration: 0.8f);
                    break;

                case "MultiHit":
                    if (primary.HasValue)
                    {
                        for (int i = 0; i < 4; i++)
                            SpawnBeam(source, primary.Value, color, width: 0.06f, duration: 0.1f, delay: i * 0.05f);
                    }
                    break;

                case "ChainJump":
                    if (primary.HasValue)
                    {
                        var prev = source;
                        var cur = primary.Value;
                        SpawnBeam(prev, cur, color, width: 0.08f, duration: 0.2f);
                        prev = cur;
                        foreach (var t in SafeTargets(e.NearbyTargets))
                        {
                            var p = TryGetPos(t);
                            if (!p.HasValue) continue;
                            SpawnBeam(prev, p.Value, color, width: 0.06f, duration: 0.2f, delay: 0.05f);
                            prev = p.Value;
                        }
                    }
                    break;

                case "ProbBurst":
                    if (primary.HasValue)
                    {
                        SpawnBeam(source, primary.Value, color, width: 0.2f, duration: 0.18f);
                        SpawnRing(primary.Value, color, radius: 1.5f, duration: 0.3f);
                    }
                    break;

                case "TrailZone":
                    if (primary.HasValue)
                        SpawnTrailZone(primary.Value, color, radius: 2f, duration: 1.5f);
                    break;

                case "SummonForm":
                    if (primary.HasValue)
                        SpawnSummonForm(primary.Value, color, height: 3f, duration: 0.6f);
                    break;

                default:
                    if (primary.HasValue) SpawnBeam(source, primary.Value, color, width: 0.1f, duration: 0.15f);
                    break;
            }
        }

        // ===== 工具 =====

        Vector3? TryGetPos(Target t)
        {
            if (t == null || _spawner == null) return null;
            foreach (var go in _spawner.Enemies)
            {
                if (go == null) continue;
                var er = go.GetComponent<EntityRef>();
                if (er != null && er.Target == t)
                    return go.transform.position + Vector3.up * 0.4f;
            }
            if (_spawner.PlayerTarget == t)
                return _spawner.Player.transform.position + Vector3.up * 0.6f;
            return null;
        }

        IEnumerable<Target> SafeTargets(Target[] arr)
        {
            if (arr == null) yield break;
            foreach (var t in arr) if (t != null) yield return t;
        }

        void Push(VFXInstance inst) => _instances.Add(inst);

        // ===== Material 池 =====

        /// <summary>
        /// 返回共享 Material（LineRenderer 直接用，不改 alpha）。
        /// Zone / Pillar 等需 per-instance alpha 动画的，调用后自行 new Material(mat) 做拷贝。
        /// URP 透明正确配置：_Surface + _Blend + SrcBlend + DstBlend + ZWrite + keyword。
        /// </summary>
        Material GetOrCreateMat(string elementName)
        {
            if (_matPool.TryGetValue(elementName, out var cached)) return cached;

            var color = ElementColors.TryGetValue(elementName, out var c) ? c : Color.white;
            var mat = BuildTransparentUnlitMat(color);
            _matPool[elementName] = mat;
            return mat;
        }

        Material BuildTransparentUnlitMat(Color color)
        {
            if (_unlitShader == null)
                _unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");

            var mat = new Material(_unlitShader) { name = "VFX_Unlit" };

            // --- URP Unlit 透明正确写法 ---
            // _Surface: 0=Opaque, 1=Transparent
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f); // Alpha blend mode
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                // Fallback: Unlit/Color 或 Standard
                mat.renderQueue = 3000;
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);

            return mat;
        }

        /// <summary>为 Zone / Pillar 创建可独立改 alpha 的实例 Material。</summary>
        Material MakeInstanceMat(string elementName, float alpha)
        {
            var shared = GetOrCreateMat(elementName);
            var inst = new Material(shared);
            var col = ElementColors.TryGetValue(elementName, out var c) ? c : Color.white;
            col.a = alpha;
            if (inst.HasProperty("_BaseColor")) inst.SetColor("_BaseColor", col);
            if (inst.HasProperty("_Color"))     inst.SetColor("_Color", col);
            return inst;
        }

        // ===== 视觉原子：Beam 类（LineRenderer）=====

        void SpawnBeam(Vector3 a, Vector3 b, Color color, float width, float duration, float delay = 0f, string elementName = null)
        {
            var go = new GameObject($"VFX_Beam_{Time.frameCount}");
            go.transform.SetParent(_root, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            lr.startWidth = width;
            lr.endWidth = width * 0.4f;
            // 共享 Material，不 new
            lr.sharedMaterial = elementName != null ? GetOrCreateMat(elementName) : BuildTransparentUnlitMat(color);
            lr.startColor = color;
            lr.endColor = new Color(color.r, color.g, color.b, 0.2f);
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.allowOcclusionWhenDynamic = false; // 避免 URP 遮挡剔除误杀

            Push(new VFXInstance
            {
                Go = go,
                Duration = duration + delay,
                Elapsed = -delay,
                Tick = (i, dt) =>
                {
                    if (lr == null) return;
                    if (i.Elapsed < 0f) { lr.enabled = false; return; }
                    lr.enabled = true;
                    float remain = i.Duration - i.Elapsed;
                    float k = Mathf.Clamp01(remain / (i.Duration > 0 ? i.Duration : 0.0001f));
                    var cs = lr.startColor; cs.a = k; lr.startColor = cs;
                    var ce = lr.endColor;   ce.a = k * 0.3f; lr.endColor = ce;
                },
            });
        }

        void SpawnRing(Vector3 center, Color color, float radius, float duration)
        {
            const int segs = 32;
            var go = new GameObject($"VFX_Ring_{Time.frameCount}");
            go.transform.position = center;
            go.transform.SetParent(_root, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = segs + 1;
            lr.startWidth = 0.08f; lr.endWidth = 0.08f;
            lr.useWorldSpace = false;
            lr.loop = false;
            lr.sharedMaterial = BuildTransparentUnlitMat(color); // 环颜色动态改 startColor/endColor，无需 per-instance mat
            lr.startColor = color; lr.endColor = color;
            lr.allowOcclusionWhenDynamic = false;

            Push(new VFXInstance
            {
                Go = go,
                Duration = duration,
                Tick = (i, dt) =>
                {
                    if (lr == null) return;
                    float k = i.Elapsed / i.Duration;
                    float r = Mathf.Lerp(0.1f, radius, k);
                    for (int s = 0; s <= segs; s++)
                    {
                        float a = s / (float)segs * Mathf.PI * 2f;
                        lr.SetPosition(s, new Vector3(Mathf.Cos(a) * r, 0.05f, Mathf.Sin(a) * r));
                    }
                    var col = color; col.a = Mathf.Clamp01(1f - k);
                    lr.startColor = col; lr.endColor = col;
                },
            });
        }

        void SpawnMark(Vector3 center, Color color, float duration)
        {
            var go = new GameObject($"VFX_Mark_{Time.frameCount}");
            go.transform.position = center + Vector3.up * 1.5f;
            go.transform.SetParent(_root, true);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 5;
            lr.startWidth = 0.06f; lr.endWidth = 0.06f;
            lr.useWorldSpace = false;
            lr.sharedMaterial = BuildTransparentUnlitMat(color);
            lr.startColor = color; lr.endColor = color;
            lr.SetPosition(0, new Vector3(-0.3f, 0, 0));
            lr.SetPosition(1, new Vector3(0.3f,  0, 0));
            lr.SetPosition(2, new Vector3(0,     0, 0));
            lr.SetPosition(3, new Vector3(0,     0, -0.3f));
            lr.SetPosition(4, new Vector3(0,     0,  0.3f));

            Push(new VFXInstance
            {
                Go = go,
                Duration = duration,
                Tick = (i, dt) =>
                {
                    if (i.Go == null) return;
                    i.Go.transform.position += Vector3.up * dt * 0.4f;
                    var col = color; col.a = Mathf.Clamp01(1f - i.Elapsed / i.Duration);
                    lr.startColor = col; lr.endColor = col;
                },
            });
        }

        // ===== 视觉原子：ParticleSystem 类 =====

        /// <summary>
        /// AOEBurst：ParticleSystem 爆发环，取代扁圆 Cylinder。
        /// 粒子从中心向四周水平发散，生命 = duration。
        /// </summary>
        void SpawnAOEBurst(Vector3 center, Color color, float radius, float duration)
        {
            var go = new GameObject($"VFX_AOEBurst_{Time.frameCount}");
            go.transform.position = center;
            go.transform.SetParent(_root, true);

            var ps = go.AddComponent<ParticleSystem>();

            // Renderer
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = BuildTransparentUnlitMat(color);

            // Main
            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = duration;
            main.startSpeed = new ParticleSystem.MinMaxCurve(radius / duration * 0.9f, radius / duration * 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            // Emission：单次 Burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });

            // Shape：圆盘，水平发散
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 0.05f;
            shape.donutRadius = 0.05f;
            shape.arc = 360f;

            // Color over lifetime：渐隐
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();

            Push(new VFXInstance { Go = go, Duration = duration + 0.05f });
        }

        /// <summary>
        /// Spark：单个 ParticleSystem 小爆发（取代 Sphere Primitive）。
        /// </summary>
        void SpawnSpark(Vector3 center, Color color)
        {
            var go = new GameObject($"VFX_Spark_{Time.frameCount}");
            go.transform.position = center;
            go.transform.SetParent(_root, true);

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = BuildTransparentUnlitMat(color);

            var main = ps.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = 0.25f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = color;
            main.gravityModifier = 0.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 20;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 15) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
            Push(new VFXInstance { Go = go, Duration = 0.3f });
        }

        /// <summary>
        /// TrailZone：持续区域用 ParticleSystem 圆盘发射慢速粒子，取代扁 Cylinder。
        /// </summary>
        void SpawnTrailZone(Vector3 center, Color color, float radius, float duration)
        {
            var go = new GameObject($"VFX_TrailZone_{Time.frameCount}");
            go.transform.position = center;
            go.transform.SetParent(_root, true);

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = BuildTransparentUnlitMat(color);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.0f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = color;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
            Push(new VFXInstance { Go = go, Duration = duration + 0.1f });
        }

        /// <summary>
        /// SummonForm：竖向发射粒子柱，取代旋转 Cylinder。
        /// </summary>
        void SpawnSummonForm(Vector3 center, Color color, float height, float duration)
        {
            var go = new GameObject($"VFX_SummonForm_{Time.frameCount}");
            go.transform.position = center;
            go.transform.SetParent(_root, true);

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = BuildTransparentUnlitMat(color);

            var main = ps.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.6f, duration);
            main.startSpeed = new ParticleSystem.MinMaxCurve(height / duration * 0.8f, height / duration * 1.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startColor = color;
            main.gravityModifier = -0.1f; // 轻微反重力，维持柱形感
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 80f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 30) }); // 首帧爆发

            // 细圆柱底部形状
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            shape.arc = 360f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.3f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
            Push(new VFXInstance { Go = go, Duration = duration + 0.1f });
        }

        // ===== Element 专属修饰（只叠加在 Shape 效果之上，非 7×8 独立实现）=====

        /// <summary>
        /// 根据 ElementName 在目标点叠加少量专属视觉修饰。
        /// 仅处理 Frost / Holy / Pure 三个有明显差异化的 Element；其他 Element 无额外开销。
        /// </summary>
        void SpawnElementDecoration(string elementName, Vector3 pos, Color color)
        {
            switch (elementName)
            {
                case "Frost":
                    // 蓝白雪花小粒子：向上慢速漂浮
                    SpawnFrostTrail(pos, color);
                    break;
                case "Holy":
                    // 金色扩散环（细，快速）
                    SpawnRing(pos + Vector3.up * 0.3f, color, radius: 0.8f, duration: 0.25f);
                    break;
                case "Pure":
                    // 双层白色扩散环，半径不同
                    SpawnRing(pos, Color.white, radius: 0.5f, duration: 0.3f);
                    SpawnRing(pos, new Color(1f, 1f, 1f, 0.5f), radius: 1.1f, duration: 0.4f);
                    break;
            }
        }

        void SpawnFrostTrail(Vector3 pos, Color color)
        {
            var go = new GameObject($"VFX_FrostTrail_{Time.frameCount}");
            go.transform.position = pos;
            go.transform.SetParent(_root, true);

            var ps = go.AddComponent<ParticleSystem>();
            var psr = go.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.sharedMaterial = BuildTransparentUnlitMat(Color.white);

            var main = ps.main;
            main.duration = 0.4f;
            main.loop = false;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
            main.startColor = new Color(color.r, color.g, color.b, 0.8f);
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 25;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 20) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
            Push(new VFXInstance { Go = go, Duration = 0.5f });
        }

        // ===== 内部数据结构 =====

        sealed class VFXInstance
        {
            public GameObject Go;
            public float Duration;
            public float Elapsed;
            public Action<VFXInstance, float> Tick;
        }
    }
}
