# Proposal — 22-gameplay-visual-polish

> **状态**：待启动（前身 `19-gameplay-visual-polish-deferred` 于 2026-07-01 重编号并重写。VFX 底盘发现已存在，音效部分范围扩大）

## Why

change#17 把玩家从 Cube 换成 2D sprite + Animator 后，视觉/听觉仍很素：

| 维度 | 现状 | 缺失 |
|---|---|---|
| **Bot 视觉区分** | 49 个 Bot 全用同一 Player 美术 | 无颜色 / 描边区分，无法一眼分辨 Smart / Light |
| **VFX 底盘** | ✅ `VFXModule` + `HitsparkBehaviour` 已有（元素配色 / 生命周期 / SRP Batcher 友好） | ❌ **未接**到攻击命中 / 死亡 / 击退等具体事件 |
| **阴影** | 无 | 无脚下贴地阴影，角色浮空感强 |
| **音效** | 🟡 `AudioModule` 只有 BGM/SFX 音量总控；MainMixer 缺失（playtest 报告 warning） | ❌ 无 SfxPlayer / BgmPlayer；无攻击/命中/死亡/UI 音效 clip |

这些不阻止「能跑」，但阻止「能看/能感」——玩家看不清战场，也听不到打击反馈。

## What

四个子项，可**并行**（无强依赖，各自独立）：

### 子项 A：Bot 染色 + 描边（最高优先级）

- 复用现有 sprite renderer，加**材质变体或 MaterialPropertyBlock 染色**（HSV 色相偏移）——不写 shader graph，直接用 Sprites/Default + `_Color` MaterialPropertyBlock，49 Bot 无 SetPass 增加
- BotControllerModule 在 `BuildControllers()` 里给每个 Bot 分配一个 preset color（按 Smart/Light 分组各配一套色板）
- 描边：简单方案用 sprite 加**背景描边贴图**（同 sprite 放大 5%、纯黑）；不用 URP RenderFeature
- **目标**：49 Bot 视觉可区分，Smart 偏暖色 / Light 偏冷色

### 子项 B：VFX 事件接线（复用现有 VFXModule）

- **不重写 VFXModule**，只是补事件订阅
- 攻击命中：订阅 `WeaponAttackHitEvent` → `VFXModule` 在目标位置绘制 hit spark（元素配色已存在）
- 击杀：订阅 `TargetKilledEvent` → 死亡消散粒子
- 玩家死亡：订阅 `PlayerDiedEvent` → 屏幕特写 + 消散
- **不做**：击退波纹 / 屏幕震动（推后续 change）；`HitsparkBehaviour` 已存在，重点是**接进战斗事件流**

### 子项 C：阴影

- 每个战斗单位（玩家 + 49 Bot + 敌人）脚下加一张**圆形半透明 sprite**（灰色 alpha 0.4）
- SpawnerModule 在 `SpawnActor` 时挂一个子对象 Shadow
- 不做动态阴影 / 光照——完全静态贴地
- **目标**：所有战斗单位有脚下阴影，画面立体感提升

### 子项 D：音效接入（范围扩大）

- 补 **MainMixer.mixer** asset（`Assets/Resources/Audio/MainMixer.mixer`，含 BGM / SFX 两个 Group 各暴露 `BgmVolume` / `SfxVolume`）
- 新增 **SfxPlayer**（简单模块或 static helper）——`PlayOneShot(clipPath, position)` + `AudioSource.PlayClipAtPoint` 兜底
- 新增 **BgmPlayer**——`PlayBgm(clipPath, loop=true)` + 淡入淡出
- 接**关键事件**：
  - MainMenu → BGM
  - InGame → 战斗 BGM
  - `WeaponAttackHitEvent` → 攻击/命中音（按 WeaponConfig.Class 分近战 / 远程 / 特殊）
  - `TargetKilledEvent` → 击杀音
  - `PlayerDiedEvent` → 玩家死亡音
  - UI Button click → 通用点击音
- **不做**：3D 空间衰减 / occlusion / dynamic mixer snapshot / 环境音；只做基础触发

## Scope

**做**：上述 4 子项 + 配置表扩展：

- `WeaponConfig` 加 3 字段：`HitSfxPath` / `AttackSfxPath` / `KillSfxPath`
- 音效资源清单（每项 1 段 CC0 音效，能听即可）
- Bot 色板配置：新建 `BotColorPresetConfig`（按 Smart/Light 各配 4~6 色）
- 每子项 1 条验收 TC，共 4 条

**不做**（推后续 change）：

- ❌ URP custom RenderFeature 描边
- ❌ 屏幕震动 / Time-stop / hit-lag
- ❌ VFX Graph（保持代码生成的 Particle）
- ❌ 音乐 BGM 正稿（本轮用占位 loop 即可）
- ❌ 音效随距离衰减 / 3D 音频
- ❌ 玩家角色本身的视觉进阶（保持 change#17 现状）

## DoD

1. ✅ 49 Bot 截图肉眼可见颜色差异（Smart 组和 Light 组也能一眼分辨）
2. ✅ 攻击命中 / 击杀 / 玩家死亡都有 VFX + 音效反馈
3. ✅ 所有战斗单位有脚下阴影
4. ✅ MainMenu 有 BGM，InGame 有战斗 BGM，UI 按钮有 click 音
5. ✅ 0 Console Error（AudioModule 的 MainMixer 未找到 warning 消失）
6. ✅ 无性能退化（SetPass calls 增幅 < 10%；不改批次）

## 依赖 / 前置

- **依赖**：change#16 MVP 闭环已通过；change#17 sprite + Animator 已完成
- **前置**：VFXModule / AudioModule 骨架已在，无需重写
- **并行度**：4 子项**互相独立**——可 fan-out 到 4 个 impl agent 并行推进（Bot 染色 → client-ta；VFX 接线 → client-unity；阴影 → art-2d + client-unity；音效 → client-unity）

## 当前状态

**Not started**。VFX 有底盘不用推倒；Bot 染色 / 阴影 / 音效需从 0 补。
