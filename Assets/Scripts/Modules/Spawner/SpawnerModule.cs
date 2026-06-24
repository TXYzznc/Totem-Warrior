using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Data;
using Tattoo.Events;
using UnityEngine;

namespace Tattoo
{
    /// <summary>
    /// 场景实体生成器。Init 时程序化创建相机/灯/地面/玩家/敌人。
    ///
    /// 现阶段：用 GameObject.CreatePrimitive 起步，未来需 Prefab 时改为通过 ResourceModule 加载。
    /// 实体与 Target 通过 EntityRef MonoBehaviour 双向绑定。
    /// </summary>
    public sealed class SpawnerModule : IGameModule
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => Type.EmptyTypes; // 现阶段不依赖 Resource/Scene

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        public GameObject  Player    { get; private set; }
        public Target      PlayerTarget { get; private set; }
        public List<GameObject> Enemies { get; } = new();

        /// <summary>初始敌人数量。可在挂 GameApp 的 Inspector 上配置（如果暴露），或代码中改。</summary>
        public int InitialEnemyCount = 4;

        public SpawnerModule(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            CreateScene();
            FrameworkLogger.Info("SpawnerModule",
                $"Action=Initialized Player=1 Enemies={Enemies.Count}");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            if (Player != null) UnityEngine.Object.Destroy(Player);
            foreach (var e in Enemies) if (e != null) UnityEngine.Object.Destroy(e);
            Enemies.Clear();
            FrameworkLogger.Info("SpawnerModule", "Action=Shutdown");
            return UniTask.CompletedTask;
        }

        void CreateScene()
        {
            // 相机
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("MainCamera");
                mainCam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }
            mainCam.transform.position    = new Vector3(0, 18, -10);
            mainCam.transform.eulerAngles = new Vector3(55, 0, 0);
            mainCam.backgroundColor       = new Color(0.18f, 0.18f, 0.22f);
            mainCam.clearFlags            = CameraClearFlags.SolidColor;

            // 地面
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(6, 1, 6);
            SetColor(ground, new Color(0.3f, 0.3f, 0.34f));

            // 太阳灯
            var sun = new GameObject("Sun");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.eulerAngles = new Vector3(50, 30, 0);

            // 玩家
            var pGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pGo.name = "Player";
            pGo.transform.position = new Vector3(0, 0.4f, 0);
            pGo.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
            SetColor(pGo, new Color(0.3f, 0.7f, 1f));

            var playerRef = pGo.AddComponent<EntityRef>();
            playerRef.IsPlayer = true;
            playerRef.MaxHP = 100f;
            playerRef.Target = new Target { Name = "玩家", Health = 100f };

            Player = pGo;
            PlayerTarget = playerRef.Target;

            // 敌人
            for (int i = 0; i < InitialEnemyCount; i++)
            {
                var eGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                eGo.name = $"敌人{i + 1}";
                float a = i * Mathf.PI * 2f / InitialEnemyCount;
                eGo.transform.position = new Vector3(Mathf.Cos(a) * 6f, 0.4f, Mathf.Sin(a) * 6f);
                eGo.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                SetColor(eGo, new Color(0.9f, 0.3f, 0.3f));

                var eRef = eGo.AddComponent<EntityRef>();
                eRef.IsPlayer = false;
                eRef.MaxHP = 50f;
                eRef.Target = new Target { Name = $"敌人{i + 1}", Health = eRef.MaxHP };

                Enemies.Add(eGo);
            }
        }

        static void SetColor(GameObject go, Color color)
        {
            var rd = go.GetComponent<Renderer>();
            if (rd == null) return;
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var mat = new Material(sh);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))     mat.SetColor("_Color", color);
            rd.material = mat;
        }
    }
}
