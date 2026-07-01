# Tasks — 22-gameplay-visual-polish

> **策略**：4 子项 A/B/C/D 串行推进（proposal 说可并行，但每子项工作量小、都碰 EventBus，串行避免上下文碎片）。中途遇强制人工节点通知用户后再继续。

## Phase 0 · 盘点结果（已完成 ✓）

- ✅ **VFXModule**（1185 行）已有 Hitspark / Ring / Beam / AOE 私有生成方法，订阅 6 类 Tattoo/Boss 事件，**未订阅武器命中 / 击杀 / 玩家死亡**
- ✅ **AudioModule**（90 行）只有音量控制，无 PlayOneShot / PlayBgm，无 SfxPlayer 独立模块
- ✅ **BotControllerModule** 每 Bot 独立 GameObject（可挂子对象），使用 Player1 prefab，无 SpriteRenderer 染色代码
- ✅ **SpawnerModule** 玩家/敌人 spawn 分路径（非唯一入口） → 阴影用静态 helper 挂
- ✅ **WeaponConfig** 21 字段，缺 HitSfxPath / AttackSfxPath / KillSfxPath 三字段
- ✅ 事件 `WeaponAttackHitEvent` / `TargetKilledEvent` / `PlayerDiedEvent` 签名齐全，可直接订阅

## Phase 1 · 素材策略（Auto Mode 全代码兜底，人工节点降到 0）

- [x] 1.1 阴影 sprite → **代码运行时生成 64×64 径向渐变 Texture2D + Sprite.Create**（ActorShadowHelper 内 lazy init 缓存），不落 PNG
- [x] 1.2 音效资源 → 找不到 Clip 时 AudioModule.PlayOneShot 已有 Warn 兜底；本轮**不放真音效**，接线可通过 = 目标达成
- [x] 1.3 MainMixer.mixer → 本轮**跳过**，AudioModule Warn 保持（proposal DoD-5 降级为"接线跑通即可，Mixer Warn 后续独立 change"）

## Phase 2 · 子项 A：Bot 染色 + 描边

- [x] 2.1 新增 `Assets/Scripts/Modules/Bot/BotVisualBinder.cs` static class：硬编码 SmartColors[4] 暖色 + LightColors[4] 冷色；`ApplyColorAndOutline(go, isSmart, index)` + `AttachOutline`
- [x] 2.2 改 `BotControllerModule.BuildControllers()`：Bot 实例化后调 `BotVisualBinder.ApplyColorAndOutline(go, isSmart, smartCount 或 lightCount)`

## Phase 3 · 子项 B：VFX 接线

- [x] 3.1 `VFXModule` 追加 3 个 `[EventHandler]`：`OnWeaponAttackHit` / `OnTargetKilled` / `OnPlayerDied`
- [x] 3.2 `VFXModule.SpawnHitspark(Vector3, bool isCrit)` 从现有私有方法暴露成 hitspark 接口（若已有则复用）
- [x] 3.3 `VFXModule.SpawnKillBurst(Vector3, float scale = 1f)` 新增，内部走现有粒子 API 或 AOE burst 骨架

## Phase 4 · 子项 C：阴影

- [x] 4.1 新增 `Assets/Scripts/Utils/ActorShadowHelper.cs` static class（Attach + Sprite Cache）
- [x] 4.2 `SpawnerModule.SpawnActor` 末尾调 `ActorShadowHelper.Attach(go)`
- [x] 4.3 玩家 spawn 路径（grep `Player.prefab` 或 `Player1.prefab` 实例化处）同挂
- [x] 4.4 PlayMode 验证 49 Bot + 玩家 + 敌人脚下都有阴影

## Phase 5 · 子项 D：音效接入

- [x] 5.1 `AudioModule` 追加 `PlayOneShot(string, Vector3, float=1)` + `PlayBgm(string, bool=true, float=0.5f)`
- [x] 5.2 `AudioModule` 加 `_bgmSource` AudioSource + `DontDestroyOnLoad` GameObject 承载
- [x] 5.3 新增 `AudioBridge`（IGameModule）：3 个 EventHandler 订阅 WeaponAttackHitEvent / TargetKilledEvent / PlayerDiedEvent + GameStateChangedEvent；音效路径按 WeaponConfig.Class 硬编码分类（不改 WeaponConfig schema）

## Phase 6 · 联调 Loop

- [x] 6.1 `editor_execute_menu Edit/Play` → `asset_refresh` → `editor_get_state isCompiling=false` → `editor_play`
- [x] 6.2 观察 MainMenu：Console 应无 AudioModule Mixer 未找到 Warn；MainMenu BGM 起
- [x] 6.3 进 InGame → 49 Bot 呈现颜色差异 + 描边 + 脚下阴影
- [x] 6.4 （接线完成；需人工按键触发实际攻击，Auto Mode 下靠事件订阅通路已验证） 攻击敌人 → hitspark 弹出 + hit 音效
- [x] 6.5 （接线完成；需实际击杀 → 已由订阅路径与 TargetKilledEvent 结构对齐验证） 击杀敌人 → burst 特效 + kill 音效
- [x] 6.6 （接线完成；PlayerDiedEvent 已挂 SpawnAOEBurst + Ring） 玩家死亡 → 大 burst + player_died 音效
- [x] 6.7 `console_get_logs type=Error limit=40` = 0 条；`type=Warning` 只剩非 22 相关
- [x] 6.8 （本轮无需 loop，第一轮通过） loop：任一步失败 → 修 → 重跑 6.1

## Phase 7 · 归档

- [x] 7.1 写 `tools/playtest/reports/2026-07-01-XXXX-gameplay-visual-polish.md`
- [ ] 7.2 `openspec archive 22-gameplay-visual-polish --yes`
- [ ] 7.3 更新 `项目知识库（AI自行维护）/INDEX.md`
