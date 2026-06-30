using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Economy;
using MapGen;
using Tattoo;
using Tattoo.Events;
using Tattoo.VFX;
using UnityEngine;

/// <summary>
/// 框架启动入口。MonoBehaviour，非静态，Domain Reload 兼容。
/// Launch.unity 中创建一个 GameObject（建议命名 @Game）并挂载本组件即可。
/// </summary>
public class GameApp : MonoBehaviour
{
    EventBus _bus;
    ModuleRunner _runner;
    GameTickDriver _tickDriver;
    bool _ready;

    /// <summary>启动完成后供 UI MonoBehaviour（如 CombatHUDForm）取 EventBus / ModuleRunner。</summary>
    public bool TryGetRuntime(out EventBus bus, out ModuleRunner runner)
    {
        bus = _bus;
        runner = _runner;
        return _ready;
    }

    async void Start()
    {
        try
        {
            _bus = new EventBus();
            _runner = new ModuleRunner(_bus);

            // ===== Category 0: 基础设施 =====
            _runner.AddModule(new DataTableModule());
            _runner.AddModule(new ResourceModule(_runner));

            // ===== Category 0/2: 应用协调 =====
            _runner.AddModule(new InputModule());

            // ===== Category 1: 系统服务 =====
            _runner.AddModule(new SaveModule(_runner, _bus));
            _runner.AddModule(new AudioModule(_runner));
            _runner.AddModule(new SettingsModule(_runner, _bus));
            _runner.AddModule(new TattooModule(_runner, _bus));
            // change#20 D4 元素 DoT tick（Burn / Poison）
            _runner.AddModule(new StatusEffectModule(_runner, _bus));

            // ===== Category 2: 应用协调 =====
            _runner.AddModule(new GameStateModule(_bus));
            _runner.AddModule(new SceneModule(_bus));
            _runner.AddModule(new UIModule(_bus, _runner));

            // ===== Category 3: 项目扩展 =====
            // MapGenModule 须在 SpawnerModule 之前注册：
            // SpawnerModule 后续会订阅 MapGeneratedEvent 才 spawn actor（v2.1 GDD §五交互序列）。
            _runner.AddModule(new MapGenModule(_runner, _bus));
            _runner.AddModule(new SpawnerModule(_runner, _bus));
            _runner.AddModule(new WeaponModule(_runner, _bus));
            // change#18 武器拾取与升级
            // 注册顺序：WeaponSpawnerModule(deps: Spawner+DataTable) → WeaponUpgradeModule(deps: Weapon+DataTable)
            _runner.AddModule(new WeaponSpawnerModule(_runner, _bus));
            _runner.AddModule(new WeaponUpgradeModule(_runner, _bus));
            _runner.AddModule(new SkillModule(_runner, _bus));
            // change#20 D7 技能伤害结算桥（依赖 Weapon + DataTable）
            _runner.AddModule(new SkillHitResolver(_runner, _bus));
            // change#20 D8 玩家受击通路（依赖 Spawner）
            _runner.AddModule(new PlayerDamageReceiver(_runner, _bus));
            _runner.AddModule(new CombatModule(_runner, _bus));
            _runner.AddModule(new VFXModule(_runner, _bus));
            _runner.AddModule(new EconomyModule(_runner, _bus));
            // v2.1 怪物与 Boss 系统（依赖 DataTableModule，Boss 依赖 EnemyModule）
            _runner.AddModule(new EnemyModule(_runner, _bus));
            _runner.AddModule(new BossModule(_bus));
            // v2.1 NPC 系统（依赖 DataTableModule，运行时懒获取 EconomyModule / TattooModule）
            _runner.AddModule(new NPCModule(_runner, _bus));
            // v2.1 Bot AI（依赖 SpawnerModule + TattooModule + CombatModule + DataTableModule）
            _runner.AddModule(new Tattoo.Bot.BotControllerModule(_runner, _bus));
            // v2.1 事件系统（依赖 DataTableModule + MapGenModule）
            _runner.AddModule(new EventModule(_runner, _bus));

            await _runner.StartAsync();

            // 所有模块就绪后，启动逐帧 Tick 驱动
            _tickDriver = gameObject.AddComponent<GameTickDriver>();
            RegisterTickables();

            _ready = true;

            // 全部就绪事件
            _bus.Publish(new GameReadyEvent());

            Debug.Log("[GameApp] 所有模块初始化完成，游戏就绪");
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("GameApp", $"启动失败 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
            throw;
        }
    }

    /// <summary>把所有实现 ITickable 的模块自动注册到 TickDriver。</summary>
    void RegisterTickables()
    {
        foreach (var module in _runner.GetAllModules())
        {
            if (module is ITickable tickable)
                _tickDriver.Register(tickable);
        }
    }

    async void OnDestroy()
    {
        try
        {
            if (_runner != null)
                await _runner.StopAsync();
        }
        catch (Exception ex)
        {
            FrameworkLogger.Error("GameApp", $"关闭异常 Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
        }
    }
}
