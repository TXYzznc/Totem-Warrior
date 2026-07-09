using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Tattoo;
using Tattoo.VFX;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tattoo.Tests
{
    /// <summary>
    /// v2.1 PlayMode 烟雾测试：装载 Launch 场景，验证 GameApp 与 ModuleRunner 全模块就绪。
    /// 不跑完整 10-15min run，仅验证启动链路与关键事件触发。
    /// </summary>
    public class V21SmokeTests
    {
        [UnityTest]
        public IEnumerator Launch_GameApp就绪_所有v2_1模块在线()
        {
            // 加载 Launch 场景
            SceneManager.LoadScene("Launch", LoadSceneMode.Single);
            yield return null; // 等一帧
            yield return new WaitForSeconds(1f); // 给 ModuleRunner 时间初始化

            var app = Object.FindObjectOfType<GameApp>();
            Assert.IsNotNull(app, "Launch 场景应含 GameApp 节点");

            // 等待 runtime 就绪（最多 8s）
            EventBus bus = null;
            ModuleRunner runner = null;
            float timeout = Time.unscaledTime + 8f;
            while (Time.unscaledTime < timeout)
            {
                if (app.TryGetRuntime(out bus, out runner)) break;
                yield return null;
            }
            Assert.IsNotNull(bus,    "GameApp.TryGetRuntime 应返回 EventBus");
            Assert.IsNotNull(runner, "GameApp.TryGetRuntime 应返回 ModuleRunner");

            // 抽查 v2.1 关键模块
            Assert.IsNotNull(runner.GetModule<UIModule>(),      "UIModule 应在线");
            Assert.IsNotNull(runner.GetModule<TattooModule>(),  "TattooModule 应在线");
            Assert.IsNotNull(runner.GetModule<CombatModule>(),  "CombatModule 应在线");
            Assert.IsNotNull(runner.GetModule<SpawnerModule>(), "SpawnerModule 应在线");
            Assert.IsNotNull(runner.GetModule<VFXModule>(),     "VFXModule 应在线");

            // SpawnerModule 应有 1 玩家 + 49 enemy（含 Bot 占位）
            var spawner = runner.GetModule<SpawnerModule>();
            Assert.IsNotNull(spawner.Player,        "玩家 GameObject 应存在");
            Assert.IsNotNull(spawner.PlayerTarget,  "PlayerTarget 应已注入");
            Assert.GreaterOrEqual(spawner.Enemies.Count, 1, "至少应生成 1 个 enemy 占位");
        }
    }
}
