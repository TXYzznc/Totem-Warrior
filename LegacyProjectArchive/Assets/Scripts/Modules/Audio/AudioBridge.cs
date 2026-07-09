using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Tattoo.Events;
using UnityEngine;
using Weapon.Events;

namespace Tattoo.Audio
{
    /// <summary>
    /// change #22 子项 D：把战斗/游戏事件桥接到 AudioModule.PlayOneShot/PlayBgm。
    ///
    /// 依赖：AudioModule（PlayOneShot / PlayBgm 已就绪）+ SpawnerModule（拿 Player 位置）
    /// 事件：GameStateChangedEvent / WeaponAttackHitEvent / TargetKilledEvent / PlayerDiedEvent
    ///
    /// 音效路径按 WeaponConfig.Class 硬编码（Melee / Ranged / Special），不改 WeaponConfig schema。
    /// Resources 下没有相应 clip 时 AudioModule 自会 Warn 兜底，接线依旧算跑通。
    /// </summary>
    public sealed class AudioBridge : IGameModule
    {
        public int ModuleCategory => 3;
        public Type[] Dependencies => new[] { typeof(AudioModule), typeof(SpawnerModule), typeof(DataTableModule) };

        readonly ModuleRunner _runner;
        readonly EventBus _bus;

        AudioModule _audio;
        SpawnerModule _spawner;
        WeaponConfig _weaponCfg;

        IDisposable _subState;
        IDisposable _subHit;
        IDisposable _subKilled;
        IDisposable _subDied;

        // change #22：BGM 路径按 GameState 分类
        const string BgmMainMenu = "Audio/BGM/main_menu";
        const string BgmInGame   = "Audio/BGM/in_game";

        // change #22：SFX 分类路径（按 WeaponConfig.Class）
        const string SfxHitMelee   = "Audio/SFX/hit_melee";
        const string SfxHitRanged  = "Audio/SFX/hit_ranged";
        const string SfxHitSpecial = "Audio/SFX/hit_special";
        const string SfxHitDefault = "Audio/SFX/hit_default";

        const string SfxKill      = "Audio/SFX/kill";
        const string SfxPlayerDie = "Audio/SFX/player_died";

        public AudioBridge(ModuleRunner runner, EventBus bus)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _bus    = bus    ?? throw new ArgumentNullException(nameof(bus));
        }

        public UniTask InitializeAsync(CancellationToken ct = default)
        {
            _audio      = _runner.GetModule<AudioModule>();
            _spawner    = _runner.GetModule<SpawnerModule>();
            _weaponCfg  = _runner.GetModule<DataTableModule>().GetTable<WeaponConfig>();

            _subState  = _bus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            _subHit    = _bus.Subscribe<WeaponAttackHitEvent>(OnWeaponHit);
            _subKilled = _bus.Subscribe<TargetKilledEvent>(OnTargetKilled);
            _subDied   = _bus.Subscribe<PlayerDiedEvent>(OnPlayerDied);

            FrameworkLogger.Info("AudioBridge", "Action=Init Subscribe=[State,Hit,Killed,Died]");
            return UniTask.CompletedTask;
        }

        public UniTask ShutdownAsync(CancellationToken ct = default)
        {
            _subState?.Dispose();
            _subHit?.Dispose();
            _subKilled?.Dispose();
            _subDied?.Dispose();
            return UniTask.CompletedTask;
        }

        void OnStateChanged(GameStateChangedEvent e)
        {
            switch (e.NewState)
            {
                case GameState.MainMenu:
                    _audio.PlayBgm(BgmMainMenu);
                    break;
                case GameState.InGame:
                    _audio.PlayBgm(BgmInGame);
                    break;
                case GameState.GameOver:
                    _audio.PlayBgm(null); // 停 BGM
                    break;
                // Loading / Paused：保持当前 BGM
            }
        }

        void OnWeaponHit(WeaponAttackHitEvent e)
        {
            var pos = _spawner.Player != null ? _spawner.Player.transform.position : Vector3.zero;
            var path = ResolveHitSfx(e.WeaponId);
            _audio.PlayOneShot(path, pos, e.IsCrit ? 1.2f : 1f);
        }

        void OnTargetKilled(TargetKilledEvent e)
        {
            var pos = _spawner.Player != null ? _spawner.Player.transform.position : Vector3.zero;
            _audio.PlayOneShot(SfxKill, pos);
        }

        void OnPlayerDied(PlayerDiedEvent _)
        {
            var pos = _spawner.Player != null ? _spawner.Player.transform.position : Vector3.zero;
            _audio.PlayOneShot(SfxPlayerDie, pos, 1.5f);
        }

        /// <summary>按 WeaponConfig.Class 映射命中音效路径。</summary>
        string ResolveHitSfx(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return SfxHitDefault;
            if (!_weaponCfg.TryGetById(weaponId, out var row)) return SfxHitDefault;
            return row.Class switch
            {
                "Melee"   => SfxHitMelee,
                "Ranged"  => SfxHitRanged,
                "Special" => SfxHitSpecial,
                _         => SfxHitDefault,
            };
        }
    }
}
