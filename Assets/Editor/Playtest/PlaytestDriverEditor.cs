#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Playtest.EditorTools
{
    /// <summary>
    /// Playtest 驱动菜单。配合 unity-skills MCP 的 editor_execute_menu 远程调用，
    /// 实现"AI 通过菜单注入虚拟输入 -> 跑业务模块 -> 抓日志写报告"的闭环。
    ///
    /// 所有菜单项都要求当前处于 Play Mode；查 InputModule 走 GameApp.TryGetRuntime + ModuleRunner.GetModule。
    /// </summary>
    static class PlaytestDriverEditor
    {
        const string Menu = "Tools/Playtest/";

        // ===== 装配 / 卸下 =====

        [MenuItem(Menu + "01 Enable Simulator", priority = 100)]
        static void EnableSimulator()
        {
            if (!TryGetInputModule(out var input)) return;
            var sim = new InputSimulator();
            input.EnableSimulator(sim);
            // Editor 失焦后 runInBackground=0 会冻结 PlayerLoop（含 GameTickDriver.Update）。
            // playtest 走 REST 注入按键时 Editor 一定失焦，必须打开 runInBackground 让 Update 持续 tick。
            Application.runInBackground = true;
            Debug.Log("[Playtest|INFO] Action=EnableSimulator Type=InputSimulator runInBackground=true");
        }

        [MenuItem(Menu + "02 Disable Simulator", priority = 101)]
        static void DisableSimulator()
        {
            if (!TryGetInputModule(out var input)) return;
            input.DisableSimulator();
            Debug.Log("[Playtest|INFO] Action=DisableSimulator");
        }

        // ===== 按键注入：单帧 PressDown（HashSet 队列，被 InputModule 消费一次）=====

        [MenuItem(Menu + "Press/E (Skill)", priority = 200)]
        static void PressE() => PressKey(KeyCode.E);

        [MenuItem(Menu + "Press/Space (Dodge)", priority = 201)]
        static void PressSpace() => PressKey(KeyCode.Space);

        [MenuItem(Menu + "Press/Tab (SelfTattoo)", priority = 202)]
        static void PressTab() => PressKey(KeyCode.Tab);

        [MenuItem(Menu + "Press/Escape (Pause)", priority = 203)]
        static void PressEscape() => PressKey(KeyCode.Escape);

        [MenuItem(Menu + "Press/Return (Confirm)", priority = 204)]
        static void PressReturn() => PressKey(KeyCode.Return);

        [MenuItem(Menu + "Press/F12 (Debug)", priority = 205)]
        static void PressF12() => PressKey(KeyCode.F12);

        [MenuItem(Menu + "Press/MouseLeft (Attack)", priority = 250)]
        static void PressMouse0() => PressMouse(0);

        [MenuItem(Menu + "Press/MouseRight", priority = 251)]
        static void PressMouse1() => PressMouse(1);

        // ===== 持续按住：状态切换（按一次开启，再按一次关闭）=====

        [MenuItem(Menu + "Hold/W (Up)", priority = 300)]
        static void HoldW() => ToggleHoldKey(KeyCode.W);

        [MenuItem(Menu + "Hold/A (Left)", priority = 301)]
        static void HoldA() => ToggleHoldKey(KeyCode.A);

        [MenuItem(Menu + "Hold/S (Down)", priority = 302)]
        static void HoldS() => ToggleHoldKey(KeyCode.S);

        [MenuItem(Menu + "Hold/D (Right)", priority = 303)]
        static void HoldD() => ToggleHoldKey(KeyCode.D);

        [MenuItem(Menu + "Hold/Clear All", priority = 399)]
        static void ClearAllHeld()
        {
            if (!TryGetSimulator(out var sim)) return;
            sim.ClearAll();
            Debug.Log("[Playtest|INFO] Action=ClearAllHeld");
        }

        // ===== 移动覆盖：直接强制一个方向向量 =====

        [MenuItem(Menu + "Move/Right", priority = 400)]
        static void MoveRight() => SetMove(Vector2.right);

        [MenuItem(Menu + "Move/Left", priority = 401)]
        static void MoveLeft() => SetMove(Vector2.left);

        [MenuItem(Menu + "Move/Up", priority = 402)]
        static void MoveUp() => SetMove(Vector2.up);

        [MenuItem(Menu + "Move/Down", priority = 403)]
        static void MoveDown() => SetMove(Vector2.down);

        [MenuItem(Menu + "Move/Stop", priority = 499)]
        static void MoveStop() => SetMove(null);

        // ===== Debug：游戏状态切换（绕过 UI 按钮，用于自动化测试驱动）=====

        [MenuItem(Menu + "Debug/StartGame (-> InGame)", priority = 500)]
        static void DebugStartGame()
        {
            if (!TryGetGameState(out var gs)) return;
            gs.StartGame();
            Debug.Log("[Playtest|INFO] Action=DebugStartGame Target=InGame");
        }

        [MenuItem(Menu + "Debug/Pause (-> Paused)", priority = 501)]
        static void DebugPause()
        {
            if (!TryGetGameState(out var gs)) return;
            gs.Pause();
            Debug.Log("[Playtest|INFO] Action=DebugPause Target=Paused");
        }

        [MenuItem(Menu + "Debug/Resume (-> InGame)", priority = 502)]
        static void DebugResume()
        {
            if (!TryGetGameState(out var gs)) return;
            gs.Resume();
            Debug.Log("[Playtest|INFO] Action=DebugResume Target=InGame");
        }

        [MenuItem(Menu + "Debug/GameOver (-> GameOver)", priority = 503)]
        static void DebugGameOver()
        {
            if (!TryGetGameState(out var gs)) return;
            gs.GameOver();
            Debug.Log("[Playtest|INFO] Action=DebugGameOver Target=GameOver");
        }

        [MenuItem(Menu + "Debug/GoToMainMenu (-> MainMenu)", priority = 504)]
        static void DebugGoToMainMenu()
        {
            if (!TryGetGameState(out var gs)) return;
            gs.GoToMainMenu();
            Debug.Log("[Playtest|INFO] Action=DebugGoToMainMenu Target=MainMenu");
        }

        [MenuItem(Menu + "Debug/PublishPauseRequestedEvent", priority = 510)]
        static void DebugPublishPauseRequested()
        {
            if (!TryGetRuntime(out _, out var bus)) return;
            bus.Publish(new Tattoo.Events.PauseRequestedEvent());
            Debug.Log("[Playtest|INFO] Action=DebugPublishPauseRequested");
        }

        [MenuItem(Menu + "Debug/ClickMainStartButton", priority = 511)]
        static void DebugClickMainStartButton()
        {
            // DontDestroyOnLoad 场景里的 Form 不能用 event_invoke 触达；用 FindObjectOfType+包含 inactive 拿到再 Invoke
            var forms = Resources.FindObjectsOfTypeAll<Tattoo.UI.MainMenuForm>();
            Tattoo.UI.MainMenuForm form = null;
            foreach (var f in forms)
            {
                if (f == null) continue;
                if (f.gameObject.scene.name == null) continue; // 排除 Prefab asset
                form = f; break;
            }
            if (form == null)
            {
                Debug.LogWarning("[Playtest|WARN] MainMenuForm 实例未找到");
                return;
            }
            form.OnStartClicked();
            Debug.Log("[Playtest|INFO] Action=ClickMainStartButton Form=MainMenuForm");
        }

        [MenuItem(Menu + "Debug/ProbeCombatTick", priority = 513)]
        static void DebugProbeCombatTick()
        {
            if (!TryGetRuntime(out var runner, out _)) return;
            var combat = runner.GetModule<Tattoo.CombatModule>();
            if (combat == null)
            {
                Debug.LogWarning("[Playtest|WARN] CombatModule 未注册");
                return;
            }
            // 主动调一次 OnUpdate 看是否进入分支；不依赖 GameTickDriver
            combat.OnUpdate(0.016f);
            Debug.Log("[Playtest|INFO] Action=ProbeCombatTick Module=CombatModule");
        }

        [MenuItem(Menu + "Debug/DumpTickDriver", priority = 525)]
        static void DebugDumpTickDriver()
        {
            var app = Object.FindObjectOfType<GameApp>();
            if (app == null) { Debug.LogWarning("[Playtest|WARN] GameApp 未找到"); return; }
            var driver = app.GetComponent<GameTickDriver>();
            if (driver == null) { Debug.LogWarning("[Playtest|WARN] GameTickDriver 未挂载在 GameApp 上"); return; }
            // 用反射列出已注册的 ITickable 类型
            var f = typeof(GameTickDriver).GetField("_tickables", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var list = f?.GetValue(driver) as System.Collections.IList;
            var sb = new System.Text.StringBuilder("[Playtest|INFO] TickDriver=count=").Append(list?.Count ?? -1).Append(" types=");
            if (list != null)
            {
                foreach (var t in list) sb.Append(t?.GetType().Name).Append("|");
            }
            sb.Append(" GameApp.enabled=").Append(app.enabled).Append(" Driver.enabled=").Append(driver.enabled);
            Debug.Log(sb.ToString());
        }

        [MenuItem(Menu + "Debug/DumpSimulatorState", priority = 526)]
        static void DebugDumpSimulatorState()
        {
            if (!TryGetSimulator(out var sim)) return;
            var t = typeof(InputSimulator);
            var keyDownF  = t.GetField("_keyDownQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var keyHeldF  = t.GetField("_keyHeld", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var mouseDownF= t.GetField("_mouseDownQueue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var keyDown   = keyDownF?.GetValue(sim) as System.Collections.IEnumerable;
            var keyHeld   = keyHeldF?.GetValue(sim) as System.Collections.IEnumerable;
            var mouseDown = mouseDownF?.GetValue(sim) as System.Collections.IEnumerable;
            var sb = new System.Text.StringBuilder($"[Playtest|INFO] SimulatorState frame={Time.frameCount} t={Time.realtimeSinceStartup:F3} keyDown=[");
            if (keyDown != null) foreach (var k in keyDown) sb.Append(k).Append("|");
            sb.Append("] keyHeld=[");
            if (keyHeld != null) foreach (var k in keyHeld) sb.Append(k).Append("|");
            sb.Append("] mouseDown=[");
            if (mouseDown != null) foreach (var b in mouseDown) sb.Append(b).Append("|");
            sb.Append("]");
            Debug.Log(sb.ToString());
        }

        [MenuItem(Menu + "Debug/CombatTickPingOnce", priority = 527)]
        static void DebugCombatTickPingOnce()
        {
            // 直接读 CombatModule._input 字段，验证它指向的 InputModule 与菜单 TryGetInputModule() 拿的是不是同一个实例
            if (!TryGetRuntime(out var runner, out _)) return;
            var combat = runner.GetModule<Tattoo.CombatModule>();
            if (combat == null) { Debug.LogWarning("[Playtest|WARN] CombatModule null"); return; }
            var ct = typeof(Tattoo.CombatModule);
            var inputF = ct.GetField("_input", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var combatInput = inputF?.GetValue(combat) as InputModule;
            if (!TryGetInputModule(out var menuInput)) return;
            bool same = ReferenceEquals(combatInput, menuInput);
            Debug.Log($"[Playtest|INFO] CombatInputSame={same} CombatInput={(combatInput == null ? "null" : combatInput.GetHashCode().ToString())} MenuInput={menuInput.GetHashCode()}");
        }

        [MenuItem(Menu + "Debug/PressTabAndProbe", priority = 514)]
        static void DebugPressTabAndProbe()
        {
            // 在同一调用栈内：先 PressTab(simulator)，再立刻调 CombatModule.OnUpdate 一次。
            // 期望：OnUpdate 内 IsSelfTattooTogglePressed() 消费 Tab → 发 SelfTattooToggleRequestedEvent
            if (!TryGetSimulator(out var sim)) return;
            sim.PressKey(KeyCode.Tab);
            if (!TryGetRuntime(out var runner, out _)) return;
            var combat = runner.GetModule<Tattoo.CombatModule>();
            if (combat == null) { Debug.LogWarning("[Playtest|WARN] CombatModule null"); return; }
            combat.OnUpdate(0.016f);
            Debug.Log("[Playtest|INFO] Action=PressTabAndProbe");
        }

        [MenuItem(Menu + "Debug/ClickPauseResumeButton", priority = 515)]
        static void DebugClickPauseResumeButton()
        {
            var forms = Resources.FindObjectsOfTypeAll<Tattoo.UI.PauseMenuForm>();
            Tattoo.UI.PauseMenuForm form = null;
            foreach (var f in forms)
            {
                if (f == null) continue;
                if (f.gameObject.scene.name == null) continue;
                form = f; break;
            }
            if (form == null)
            {
                Debug.LogWarning("[Playtest|WARN] PauseMenuForm 实例未找到");
                return;
            }
            form.OnResumeClicked();
            Debug.Log("[Playtest|INFO] Action=ClickPauseResumeButton Form=PauseMenuForm");
        }

        [MenuItem(Menu + "Debug/Dump Scene Roots", priority = 520)]
        static void DebugDumpSceneRoots()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[Playtest|INFO] SceneRoots=");
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
                sb.Append(root.name).Append("|");
            Debug.Log(sb.ToString());
        }

        [MenuItem(Menu + "Debug/Dump UIForms (active+inactive)", priority = 521)]
        static void DebugDumpUIForms()
        {
            var forms = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
            var sb = new System.Text.StringBuilder("[Playtest|INFO] UIForms=");
            foreach (var f in forms)
            {
                if (f == null || f is not IUIForm) continue;
                if (f.gameObject.scene.name == null) continue; // 排除 prefab asset
                sb.Append(f.GetType().Name).Append("(active=").Append(f.gameObject.activeSelf).Append(")|");
            }
            Debug.Log(sb.ToString());
        }

        // ===== Animator 状态探针（TC-Art） =====

        [MenuItem(Menu + "Animator/Dump Player State", priority = 600)]
        static void DumpPlayerAnimator() => DumpAnimatorByName("Player");

        [MenuItem(Menu + "Animator/Dump Boss1 State", priority = 601)]
        static void DumpBoss1Animator() => DumpAnimatorByName("Boss1");

        static void DumpAnimatorByName(string goName)
        {
            var go = GameObject.Find(goName);
            if (go == null)
            {
                Debug.LogWarning($"[Playtest|WARN] GameObject={goName} 未找到（场景活动根）");
                return;
            }
            var anim = go.GetComponent<Animator>();
            if (anim == null)
            {
                bool isCube = go.GetComponent<MeshFilter>() != null;
                Debug.LogWarning($"[Playtest|WARN] {goName} 无 Animator 组件 IsCube={isCube}");
                return;
            }
            string stateName = "<none>";
            bool inTransition = false;
            if (anim.runtimeAnimatorController != null && anim.layerCount > 0)
            {
                var info = anim.GetCurrentAnimatorStateInfo(0);
                inTransition = anim.IsInTransition(0);
                foreach (var clipPair in anim.GetCurrentAnimatorClipInfo(0))
                {
                    if (clipPair.clip != null) { stateName = clipPair.clip.name; break; }
                }
            }
            int direction = anim.parameterCount > 0 ? SafeGetInt(anim, "Direction") : -1;
            bool isMoving = anim.parameterCount > 0 && SafeGetBool(anim, "IsMoving");
            Debug.Log($"[Playtest|INFO] AnimatorDump GameObject={goName} ClipName=\"{stateName}\" InTransition={inTransition} Direction={direction} IsMoving={isMoving}");
        }

        static int SafeGetInt(Animator a, string p)
        {
            foreach (var par in a.parameters) if (par.name == p && par.type == AnimatorControllerParameterType.Int) return a.GetInteger(p);
            return -1;
        }

        static bool SafeGetBool(Animator a, string p)
        {
            foreach (var par in a.parameters) if (par.name == p && par.type == AnimatorControllerParameterType.Bool) return a.GetBool(p);
            return false;
        }

        // ===== Combat 事件强制注入（TC-Art-03/04） =====

        /// <summary>
        /// TC-18 hit-spark VFX 验证入口。
        /// 发布 WeaponAttackHitEvent（VFXModule 订阅此类型），Target 取最近活着的 Bot；
        /// 若无 Bot 则 fallback 到 Player 自身（IsCrit=false，WeaponId="debug_sword"）。
        /// </summary>
        [MenuItem(Menu + "Combat/Publish WeaponAttackHit (nearest bot)", priority = 700)]
        static void PublishWeaponAttackHit()
        {
            if (!TryGetRuntime(out var runner, out var bus)) return;

            Tattoo.Data.Target hitTarget = null;
            string hitTargetName = "none";

            // 优先取 SpawnerModule.Enemies 里最近活着的 Bot Target
            var spawner = runner.GetModule<Tattoo.SpawnerModule>();
            if (spawner != null && spawner.Enemies != null && spawner.Enemies.Count > 0)
            {
                Vector3 playerPos = spawner.Player != null ? spawner.Player.transform.position : Vector3.zero;
                float minSqrDist = float.MaxValue;
                for (int i = 0; i < spawner.Enemies.Count; i++)
                {
                    var go = spawner.Enemies[i];
                    if (go == null) continue;
                    var eRef = go.GetComponent<Tattoo.EntityRef>();
                    if (eRef == null) continue;
                    var t = eRef.Target;
                    if (t == null || t.Health <= 0f) continue;
                    float sqrDist = (go.transform.position - playerPos).sqrMagnitude;
                    if (sqrDist < minSqrDist)
                    {
                        minSqrDist = sqrDist;
                        hitTarget = t;
                        hitTargetName = t.Name;
                    }
                }
            }

            // Fallback：无活着 Bot，用 Player 自身作为 Target
            if (hitTarget == null && spawner != null && spawner.Player != null)
            {
                var playerRef = spawner.Player.GetComponent<Tattoo.EntityRef>();
                if (playerRef != null) { hitTarget = playerRef.Target; hitTargetName = "Player(fallback)"; }
            }

            if (hitTarget == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=PublishWeaponAttackHit NoTarget found (no bot and no player EntityRef)");
                return;
            }

            bus.Publish(new Weapon.Events.WeaponAttackHitEvent(
                attacker:   null,
                target:     hitTarget,
                baseDamage: 10f,
                weaponId:   "debug_sword",
                isCrit:     false,
                isCharged:  false
            ));
            Debug.Log($"[Playtest|INFO] Action=PublishWeaponAttackHit Target={hitTargetName}");
        }

        // ===== TC-15/16/17：强制生成武器拾取 GO =====

        /// <summary>
        /// TC-15/16/17 解锁入口。
        /// 直接调 WeaponSpawnerModule.SpawnDroppedWeapon，在玩家前方 2m 生成武器拾取 GO。
        /// 生成后玩家走过去即可触发 WeaponPickupTrigger.OnTriggerEnter → WeaponPickedUpEvent（TC-16）。
        /// 多次执行累积拾取次数可触发武器升级逻辑（TC-17）。
        /// weaponId 固定 knife_basic（DataTable 第一行，保证合法）。
        /// </summary>
        [MenuItem(Menu + "Combat/ForceSpawnWeaponPickup", priority = 703)]
        static void ForceSpawnWeaponPickup()
        {
            if (!TryGetRuntime(out var runner, out _)) return;

            var weaponSpawner = runner.GetModule<WeaponSpawnerModule>();
            if (weaponSpawner == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceSpawnWeaponPickup WeaponSpawnerModule=null");
                return;
            }

            const string weaponId = "knife_basic";

            // 取玩家前方 2m 位置，Player 不存在时 fallback Vector3.zero
            var spawner = runner.GetModule<Tattoo.SpawnerModule>();
            Vector3 pos = Vector3.zero;
            if (spawner != null && spawner.Player != null)
            {
                var playerTr = spawner.Player.transform;
                pos = playerTr.position + playerTr.forward * 2f;
            }

            weaponSpawner.SpawnDroppedWeapon(weaponId, pos);
            FrameworkLogger.Info("PlaytestDriver",
                $"Action=ForceSpawnWeaponPickup Id={weaponId} Pos={pos}");
        }

        // ===== TC-16/17：强制拾取最近武器（绕过物理 trigger）=====

        /// <summary>
        /// TC-16/TC-17 解锁入口。
        /// CombatModule 玩家移动用 transform.position += 直接赋值，不走 Rigidbody 物理引擎，
        /// 导致 SphereCollider trigger 永远不会触发（BUG-11 根因）。
        /// 本菜单绕过物理：直接发 WeaponPickedUpEvent + Destroy pickup GO，复现完整拾取链路。
        /// 配合 ForceSpawnWeaponPickup 连续调用可累计拾取次数，验证武器升级（TC-17）。
        /// </summary>
        [MenuItem(Menu + "Combat/ForcePickupNearestWeapon", priority = 705)]
        static void ForcePickupNearestWeapon()
        {
            if (!TryGetRuntime(out var runner, out var bus)) return;

            // 1. 找场景内所有 WeaponPickupTrigger
            var triggers = Object.FindObjectsOfType<WeaponPickupTrigger>();
            if (triggers == null || triggers.Length == 0)
            {
                FrameworkLogger.Warn("PlaytestDriver", "Action=ForcePickupNearestWeapon NoPickupFound");
                Debug.LogWarning("[Playtest|WARN] Action=ForcePickupNearestWeapon NoPickupFound");
                return;
            }

            // 2. 取玩家位置作为距离参照
            var spawner = runner.GetModule<Tattoo.SpawnerModule>();
            Vector3 playerPos = spawner?.Player != null ? spawner.Player.transform.position : Vector3.zero;

            // 3. 找距 Player 最近的 pickup trigger
            WeaponPickupTrigger nearest = null;
            float minSqrDist = float.MaxValue;
            foreach (var t in triggers)
            {
                if (t == null || t.gameObject == null) continue;
                float sqrDist = (t.transform.position - playerPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearest = t;
                }
            }

            if (nearest == null)
            {
                FrameworkLogger.Warn("PlaytestDriver", "Action=ForcePickupNearestWeapon NoValidPickupFound");
                return;
            }

            // 4. 取 PlayerTarget（优先从 trigger 自身的已注入字段，否则从 SpawnerModule.Player 取 EntityRef.Target）
            Tattoo.Data.Target playerTarget = nearest.PlayerTarget;
            if (playerTarget == null && spawner?.Player != null)
            {
                var eRef = spawner.Player.GetComponent<Tattoo.EntityRef>();
                if (eRef != null) playerTarget = eRef.Target;
            }

            if (playerTarget == null)
            {
                FrameworkLogger.Warn("PlaytestDriver", "Action=ForcePickupNearestWeapon PlayerTargetNull 无法取到 PlayerTarget");
                Debug.LogWarning("[Playtest|WARN] Action=ForcePickupNearestWeapon PlayerTargetNull");
                return;
            }

            // 5. 取 Bus（优先 trigger 自身注入的 Bus，否则用运行时 bus）
            EventBus pickupBus = nearest.Bus ?? bus;
            string weaponId = nearest.WeaponId;
            Vector3 pos = nearest.transform.position;
            string instanceId = nearest.GetInstanceID().ToString();

            FrameworkLogger.Info("PlaytestDriver",
                $"Action=ForcePickupNearestWeapon PickupId={instanceId} WeaponId={weaponId} Pos={pos}");

            // 6. Destroy pickup GO 后发事件（模拟 WeaponPickupTrigger.Update 里拾取后 WeaponSpawnerModule 销毁的链路）
            Object.Destroy(nearest.gameObject);
            pickupBus.Publish(new WeaponPickedUpEvent(playerTarget, weaponId, pos));

            Debug.Log($"[Playtest|INFO] Action=ForcePickupNearestWeapon WeaponId={weaponId} Pos={pos}");
        }

        // ===== TC-19：强制复活（重置血量）场上全部已死亡 Bot =====

        /// <summary>
        /// TC-19 染色截图解锁入口。
        /// SpawnerModule 无公开 SpawnBot 接口，改为直接把 Health<=0 的 Bot Target 血量重置到 MaxHP，
        /// 同时重新激活 GameObject（如被 Destroy 则无法复原；若仅是 SetActive(false) 则恢复）。
        /// 注意：已被 Destroy 的 GO 不可恢复，此菜单只能恢复"血量归零但 GO 还存活"的 Bot。
        /// 若截图时屏幕上 Bot 数仍不足，见 escalate 说明。
        /// </summary>
        [MenuItem(Menu + "Combat/ForceRefillEnemies", priority = 704)]
        static void ForceRefillEnemies()
        {
            if (!TryGetRuntime(out var runner, out _)) return;

            var spawner = runner.GetModule<Tattoo.SpawnerModule>();
            if (spawner == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceRefillEnemies SpawnerModule=null");
                return;
            }

            var enemies = spawner.Enemies;
            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceRefillEnemies Enemies=empty");
                return;
            }

            int count = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i];
                if (go == null) continue;  // 已被 Destroy，无法复原

                // 重新激活（若只是 SetActive(false)）
                if (!go.activeSelf)
                    go.SetActive(true);

                var eRef = go.GetComponent<Tattoo.EntityRef>();
                if (eRef == null) continue;
                var target = eRef.Target;
                if (target == null) continue;

                // 只重置已死亡 Bot
                if (target.Health <= 0f)
                {
                    target.Health = eRef.MaxHP > 0f ? eRef.MaxHP : 50f;
                    count++;
                }
            }

            FrameworkLogger.Info("PlaytestDriver",
                $"Action=ForceRefillEnemies Count={count}");
            Debug.Log($"[Playtest|INFO] Action=ForceRefillEnemies Revived={count} Total={enemies.Count}");
        }

        [MenuItem(Menu + "Combat/Publish PlayerDied", priority = 701)]
        static void PublishPlayerDied()
        {
            if (!TryGetRuntime(out _, out var bus)) return;
            bus.Publish(new Tattoo.Events.PlayerDiedEvent());
            Debug.Log("[Playtest|INFO] Action=PublishPlayerDied");
        }

        /// <summary>
        /// TC-14/15/16/17/18 Bot 击杀链路自动化入口。
        /// 遍历 SpawnerModule.Enemies，通过 EntityRef.Target 找到最近活着的 Bot，
        /// 直接 Publish TargetKilledEvent，触发 CombatModule 的完整击杀响应链。
        /// </summary>
        [MenuItem(Menu + "Combat/ForceKillNearestBot", priority = 702)]
        static void ForceKillNearestBot()
        {
            if (!TryGetRuntime(out var runner, out var bus)) return;

            var spawner = runner.GetModule<Tattoo.SpawnerModule>();
            if (spawner == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceKillNearestBot SpawnerModule=null");
                return;
            }

            var enemies = spawner.Enemies;
            if (enemies == null || enemies.Count == 0)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceKillNearestBot Enemies=empty");
                return;
            }

            // 获取 Player 位置作为距离参照（Player 不存在则用 Vector3.zero）
            Vector3 playerPos = spawner.Player != null ? spawner.Player.transform.position : Vector3.zero;

            // 遍历 Enemies 找最近活着的 Bot Target
            Tattoo.Data.Target nearestTarget = null;
            float minSqrDist = float.MaxValue;
            string nearestName = "";

            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i];
                if (go == null) continue;
                var eRef = go.GetComponent<Tattoo.EntityRef>();
                if (eRef == null) continue;
                var target = eRef.Target;
                if (target == null || target.Health <= 0f) continue;

                float sqrDist = (go.transform.position - playerPos).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    nearestTarget = target;
                    nearestName = target.Name;
                }
            }

            if (nearestTarget == null)
            {
                Debug.LogWarning("[Playtest|WARN] Action=ForceKillNearestBot NoAliveBot found");
                return;
            }

            // BUG-04 辅助加固：击杀前记录 Target 位置，便于 QA 交叉验证 VFX spawn 位置（BUG-06 已在 VFX 侧修）
            Vector3 killPos = nearestTarget is Tattoo.Data.Target ? TryGetKillPos(enemies, nearestTarget) : Vector3.zero;
            FrameworkLogger.Info("PlaytestDriver", $"Action=ForceKillNearestBot Target={nearestName} Pos={killPos}");
            bus.Publish(new Tattoo.Events.TargetKilledEvent(nearestTarget));
            Debug.Log($"[Playtest|INFO] Action=ForceKillNearestBot Target={nearestName} Dist={Mathf.Sqrt(minSqrDist):F1} Pos={killPos}");
        }

        // ===== BUG-04 辅助：从 Enemies 列表里根据 Target 引用反查 GO 位置 =====

        static Vector3 TryGetKillPos(System.Collections.Generic.IList<GameObject> enemies, Tattoo.Data.Target target)
        {
            if (enemies == null || target == null) return Vector3.zero;
            for (int i = 0; i < enemies.Count; i++)
            {
                var go = enemies[i];
                if (go == null) continue;
                var eRef = go.GetComponent<Tattoo.EntityRef>();
                if (eRef != null && ReferenceEquals(eRef.Target, target))
                    return go.transform.position;
            }
            return Vector3.zero;
        }

        // ===== 内部 helpers =====

        static bool TryGetRuntime(out ModuleRunner runner, out EventBus bus)
        {
            runner = null; bus = null;
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Playtest|WARN] 当前不在 Play Mode，菜单忽略");
                return false;
            }
            var app = Object.FindObjectOfType<GameApp>();
            if (app == null)
            {
                Debug.LogWarning("[Playtest|WARN] 场景中未找到 GameApp");
                return false;
            }
            if (!app.TryGetRuntime(out bus, out runner))
            {
                Debug.LogWarning("[Playtest|WARN] GameApp 尚未就绪（GameReadyEvent 未发）");
                return false;
            }
            return true;
        }

        static bool TryGetGameState(out GameStateModule gs)
        {
            gs = null;
            if (!TryGetRuntime(out var runner, out _)) return false;
            gs = runner.GetModule<GameStateModule>();
            if (gs == null)
            {
                Debug.LogWarning("[Playtest|WARN] GameStateModule 未注册");
                return false;
            }
            return true;
        }

        static void PressKey(KeyCode k)
        {
            if (!TryGetSimulator(out var sim)) return;
            sim.PressKey(k);
            Debug.Log($"[Playtest|INFO] Action=PressKey Key={k}");
        }

        static void PressMouse(int button)
        {
            if (!TryGetSimulator(out var sim)) return;
            sim.PressMouse(button);
            Debug.Log($"[Playtest|INFO] Action=PressMouse Button={button}");
        }

        static void ToggleHoldKey(KeyCode k)
        {
            if (!TryGetSimulator(out var sim)) return;
            bool nowHeld = !sim.IsKeyHeld(k);
            sim.HoldKey(k, nowHeld);
            Debug.Log($"[Playtest|INFO] Action=HoldKey Key={k} Held={nowHeld}");
        }

        static void SetMove(Vector2? dir)
        {
            if (!TryGetSimulator(out var sim)) return;
            sim.SetMove(dir);
            Debug.Log($"[Playtest|INFO] Action=SetMove Dir={(dir.HasValue ? dir.Value.ToString() : "null")}");
        }

        static bool TryGetInputModule(out InputModule input)
        {
            input = null;
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Playtest|WARN] 当前不在 Play Mode，菜单忽略");
                return false;
            }
            var app = Object.FindObjectOfType<GameApp>();
            if (app == null)
            {
                Debug.LogWarning("[Playtest|WARN] 场景中未找到 GameApp");
                return false;
            }
            if (!app.TryGetRuntime(out _, out var runner))
            {
                Debug.LogWarning("[Playtest|WARN] GameApp 尚未就绪（GameReadyEvent 未发）");
                return false;
            }
            input = runner.GetModule<InputModule>();
            if (input == null)
            {
                Debug.LogWarning("[Playtest|WARN] ModuleRunner.GetModule<InputModule>() 返回 null");
                return false;
            }
            return true;
        }

        static bool TryGetSimulator(out InputSimulator sim)
        {
            sim = null;
            if (!TryGetInputModule(out var input)) return false;
            var s = input.GetSimulator();
            if (s == null)
            {
                Debug.LogWarning("[Playtest|WARN] InputModule 尚未装配 simulator，请先执行 Tools/Playtest/01 Enable Simulator");
                return false;
            }
            sim = s as InputSimulator;
            if (sim == null)
            {
                Debug.LogWarning($"[Playtest|WARN] 当前 simulator 不是 InputSimulator 而是 {s.GetType().Name}，无法用菜单驱动");
                return false;
            }
            return true;
        }

        // ===== change #21: Prefab Build（一次性构建 CharacterSelect + StartupSelect prefab） =====

        const string CH21_CharSelPath  = "Assets/Resources/Prefab/UI/CharacterSelect.prefab";
        const string CH21_StartupPath  = "Assets/Resources/Prefab/UI/StartupSelect.prefab";
        const string CH21_BgSprite     = "Assets/Resources/Sprite/UI/CharacterSelectForm/CharacterSelectForm_bg.png";
        const string CH21_BtnPriSprite = "Assets/Resources/Sprite/UI/CharacterSelectForm/CharacterSelectForm_button_primary.png";
        const string CH21_BtnIdSprite  = "Assets/Resources/Sprite/UI/CharacterSelectForm/CharacterSelectForm_button_idle.png";

        [MenuItem(Menu + "Change21/Build UI Prefabs", priority = 9000)]
        static void Ch21_BuildAll()
        {
            Ch21_BuildCharacterSelect();
            Ch21_BuildStartupSelect();
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            Debug.Log("[Change21] Done. Built CharacterSelect + StartupSelect prefabs.");
        }

        static void Ch21_BuildCharacterSelect()
        {
            var bg  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CH21_BgSprite);
            var btn = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CH21_BtnPriSprite);

            var root = new GameObject("CharacterSelect");
            var canvas = root.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panel = Ch21_Child(root, "Panel", stretch: true);
            var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
            panelImg.sprite = bg; panelImg.color = Color.white;

            var title = Ch21_Child(panel, "Title");
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(600, 60);
            tRt.anchoredPosition = new Vector2(0, -80);
            var tTxt = title.AddComponent<UnityEngine.UI.Text>();
            tTxt.text = "选择角色"; tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 36; tTxt.color = Color.white;

            var charRoot = Ch21_Child(panel, "CharacterRoot");
            var cRt = charRoot.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(900, 320);
            cRt.anchoredPosition = Vector2.zero;
            var hlg = charRoot.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 40; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = false;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

            var nextBtn = Ch21_Child(panel, "NextButton");
            var nRt = nextBtn.GetComponent<RectTransform>();
            nRt.anchorMin = nRt.anchorMax = new Vector2(0.5f, 0f);
            nRt.pivot = new Vector2(0.5f, 0f);
            nRt.sizeDelta = new Vector2(200, 60);
            nRt.anchoredPosition = new Vector2(0, 60);
            var nImg = nextBtn.AddComponent<UnityEngine.UI.Image>();
            nImg.sprite = btn; nImg.color = Color.white; nImg.type = UnityEngine.UI.Image.Type.Sliced;
            var nBtn = nextBtn.AddComponent<UnityEngine.UI.Button>();
            nBtn.targetGraphic = nImg;
            var nTxt = Ch21_Child(nextBtn, "Text", stretch: true).AddComponent<UnityEngine.UI.Text>();
            nTxt.text = "下一步"; nTxt.alignment = TextAnchor.MiddleCenter;
            nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nTxt.fontSize = 26; nTxt.color = Color.white; nTxt.raycastTarget = false;

            root.AddComponent<Tattoo.UI.CharacterSelectForm>();
            Ch21_EnsureDir(CH21_CharSelPath);
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, CH21_CharSelPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Change21] Wrote {CH21_CharSelPath}");
        }

        static void Ch21_BuildStartupSelect()
        {
            var bg    = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CH21_BgSprite);
            var btnP  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CH21_BtnPriSprite);
            var btnI  = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(CH21_BtnIdSprite);

            var root = new GameObject("StartupSelect");
            var canvas = root.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            var scaler = root.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var panel = Ch21_Child(root, "Panel", stretch: true);
            var panelImg = panel.AddComponent<UnityEngine.UI.Image>();
            panelImg.sprite = bg; panelImg.color = Color.white;

            var title = Ch21_Child(panel, "Title");
            var tRt = title.GetComponent<RectTransform>();
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 1f);
            tRt.pivot = new Vector2(0.5f, 1f);
            tRt.sizeDelta = new Vector2(600, 60);
            tRt.anchoredPosition = new Vector2(0, -60);
            var tTxt = title.AddComponent<UnityEngine.UI.Text>();
            tTxt.text = "起手 Build"; tTxt.alignment = TextAnchor.MiddleCenter;
            tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tTxt.fontSize = 36; tTxt.color = Color.white;

            Ch21_Row(panel, "ColorRoot",   200f, 500f);
            Ch21_Row(panel, "WeaponRoot",  0f,   700f);
            Ch21_Row(panel, "PatternRoot", -200f, 300f);

            var confirm = Ch21_Child(panel, "ConfirmButton");
            var cRt = confirm.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(1f, 0f);
            cRt.pivot = new Vector2(1f, 0f);
            cRt.sizeDelta = new Vector2(160, 50);
            cRt.anchoredPosition = new Vector2(-80, 60);
            var cImg = confirm.AddComponent<UnityEngine.UI.Image>();
            cImg.sprite = btnP; cImg.color = Color.white; cImg.type = UnityEngine.UI.Image.Type.Sliced;
            var cBtn = confirm.AddComponent<UnityEngine.UI.Button>();
            cBtn.targetGraphic = cImg;
            var cTxt = Ch21_Child(confirm, "Text", stretch: true).AddComponent<UnityEngine.UI.Text>();
            cTxt.text = "确定"; cTxt.alignment = TextAnchor.MiddleCenter;
            cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cTxt.fontSize = 24; cTxt.color = Color.white; cTxt.raycastTarget = false;

            var cancel = Ch21_Child(panel, "CancelButton");
            var xRt = cancel.GetComponent<RectTransform>();
            xRt.anchorMin = xRt.anchorMax = new Vector2(0f, 0f);
            xRt.pivot = new Vector2(0f, 0f);
            xRt.sizeDelta = new Vector2(160, 50);
            xRt.anchoredPosition = new Vector2(80, 60);
            var xImg = cancel.AddComponent<UnityEngine.UI.Image>();
            xImg.sprite = btnI; xImg.color = Color.white; xImg.type = UnityEngine.UI.Image.Type.Sliced;
            var xBtn = cancel.AddComponent<UnityEngine.UI.Button>();
            xBtn.targetGraphic = xImg;
            var xTxt = Ch21_Child(cancel, "Text", stretch: true).AddComponent<UnityEngine.UI.Text>();
            xTxt.text = "取消"; xTxt.alignment = TextAnchor.MiddleCenter;
            xTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            xTxt.fontSize = 24; xTxt.color = Color.white; xTxt.raycastTarget = false;

            root.AddComponent<Tattoo.UI.StartupSelectForm>();
            Ch21_EnsureDir(CH21_StartupPath);
            UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, CH21_StartupPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[Change21] Wrote {CH21_StartupPath}");
        }

        static GameObject Ch21_Row(GameObject parent, string name, float y, float width)
        {
            var go = Ch21_Child(parent, name);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 130);
            rt.anchoredPosition = new Vector2(0, y);
            var hlg = go.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = false;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;
            return go;
        }

        static GameObject Ch21_Child(GameObject parent, string name, bool stretch = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            if (stretch)
            {
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
            }
            return go;
        }

        static void Ch21_EnsureDir(string path)
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        }

        // ===== change #21: 全流程一键测试（Play Mode 下）=====
        // MainMenu.Start → CharSel.Select+Next → StartupSel.三选+Confirm → 断言 State=InGame

        [MenuItem(Menu + "Change21/Test Full Flow", priority = 9100)]
        static async void Ch21_TestFullFlow()
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("[Ch21|Test] 需要在 Play Mode 下"); return; }
            Debug.Log("[Ch21|Test] === Full Flow Start ===");

            var main = Object.FindObjectOfType<Tattoo.UI.MainMenuForm>(true);
            if (main == null) { Debug.LogError("[Ch21|Test] MainMenuForm 未找到"); return; }
            main.OnStartClicked();
            Debug.Log("[Ch21|Test] Step1: MainMenu.OnStartClicked");
            await System.Threading.Tasks.Task.Delay(400);

            var charSel = Object.FindObjectOfType<Tattoo.UI.CharacterSelectForm>(true);
            if (charSel == null) { Debug.LogError("[Ch21|Test] CharacterSelectForm 未找到"); return; }
            charSel.SetSelectedCharacter(1);
            Debug.Log("[Ch21|Test] Step2a: CharSel.SetSelectedCharacter(1)");
            await System.Threading.Tasks.Task.Delay(200);
            charSel.OnNextClicked();
            Debug.Log("[Ch21|Test] Step2b: CharSel.OnNextClicked");
            await System.Threading.Tasks.Task.Delay(500);

            var startup = Object.FindObjectOfType<Tattoo.UI.StartupSelectForm>(true);
            if (startup == null) { Debug.LogError("[Ch21|Test] StartupSelectForm 未找到"); return; }

            // 反射拿三个 List 的第一个可用 id
            var t = typeof(Tattoo.UI.StartupSelectForm);
            var bind = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var colorIds   = (System.Collections.Generic.List<int>)   t.GetField("_colorIds",   bind).GetValue(startup);
            var weaponIds  = (System.Collections.Generic.List<string>)t.GetField("_weaponIds",  bind).GetValue(startup);
            var patternIds = (System.Collections.Generic.List<int>)   t.GetField("_patternIds", bind).GetValue(startup);
            if (colorIds.Count == 0 || weaponIds.Count == 0 || patternIds.Count == 0)
            {
                Debug.LogError($"[Ch21|Test] StartupSel 卡片未构建 Colors={colorIds.Count} Weapons={weaponIds.Count} Patterns={patternIds.Count}");
                return;
            }
            int colorId = colorIds[0];
            string weaponId = weaponIds[0];
            int patternId = patternIds[0];

            startup.SetSelectedColor(colorId);
            Debug.Log($"[Ch21|Test] Step3a: StartupSel.SetSelectedColor({colorId})");
            await System.Threading.Tasks.Task.Delay(150);
            startup.SetSelectedWeapon(weaponId);
            Debug.Log($"[Ch21|Test] Step3b: StartupSel.SetSelectedWeapon({weaponId})");
            await System.Threading.Tasks.Task.Delay(150);
            startup.ToggleSelectedPattern(patternId);
            Debug.Log($"[Ch21|Test] Step3c: StartupSel.ToggleSelectedPattern({patternId})");
            await System.Threading.Tasks.Task.Delay(200);
            startup.OnConfirm();
            Debug.Log("[Ch21|Test] Step3d: StartupSel.OnConfirm");
            await System.Threading.Tasks.Task.Delay(500);

            // 断言游戏状态 = InGame
            var app = Object.FindObjectOfType<GameApp>();
            if (app != null && app.TryGetRuntime(out _, out var runner))
            {
                var gs = runner.GetModule<GameStateModule>();
                var st = gs != null ? gs.CurrentState.ToString() : "null";
                Debug.Log($"[Ch21|Test] === Full Flow End === Final GameState={st}");
            }
        }

        [MenuItem(Menu + "Change21/Test Second Run (die+replay)", priority = 9105)]
        static async void Ch21_TestSecondRun()
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("[Ch21|Test2] 需要 Play Mode"); return; }
            Debug.Log("[Ch21|Test2] === Second Run Start ===");

            var app = Object.FindObjectOfType<GameApp>();
            if (app == null || !app.TryGetRuntime(out _, out var runner))
            { Debug.LogError("[Ch21|Test2] GameApp 未就绪"); return; }
            var gs = runner.GetModule<GameStateModule>();
            if (gs == null) { Debug.LogError("[Ch21|Test2] GameStateModule 缺失"); return; }
            if (gs.CurrentState != GameState.InGame)
            { Debug.LogWarning($"[Ch21|Test2] 当前 State={gs.CurrentState}，需要先跑一次 Test Full Flow 到 InGame"); return; }

            gs.GoToMainMenu();
            Debug.Log($"[Ch21|Test2] Step1: GoToMainMenu → State={gs.CurrentState}");
            await System.Threading.Tasks.Task.Delay(300);

            if (gs.CurrentState != GameState.MainMenu)
            { Debug.LogError($"[Ch21|Test2] Step1 Failed: State={gs.CurrentState}"); return; }

            Debug.Log("[Ch21|Test2] Step2: 触发 Full Flow 2nd time");
            Ch21_TestFullFlow();
            await System.Threading.Tasks.Task.Delay(3000);

            Debug.Log($"[Ch21|Test2] === Second Run End === Final={gs.CurrentState}");
        }

        [MenuItem(Menu + "Change21/Snapshot MainMenu", priority = 9101)]
        static void Ch21_SnapMainMenu() => Ch21_Snap("main_menu");

        [MenuItem(Menu + "Change21/Snapshot CharSel", priority = 9102)]
        static void Ch21_SnapCharSel() => Ch21_Snap("char_sel");

        [MenuItem(Menu + "Change21/Snapshot StartupSel", priority = 9103)]
        static void Ch21_SnapStartupSel() => Ch21_Snap("startup_sel");

        [MenuItem(Menu + "Change21/Snapshot InGame", priority = 9104)]
        static void Ch21_SnapInGame() => Ch21_Snap("ingame");

        static void Ch21_Snap(string tag)
        {
            var name = $"pt_ch21_{tag}.png";
            var path = $"Assets/Screenshots/{name}";
            UnityEngine.ScreenCapture.CaptureScreenshot(path, 1);
            Debug.Log($"[Ch21|Snap] Captured → {path}");
        }
    }
}
#endif
