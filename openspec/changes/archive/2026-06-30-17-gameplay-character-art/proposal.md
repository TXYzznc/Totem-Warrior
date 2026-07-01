# Proposal — 17-gameplay-character-art

## Why

最小游戏闭环（change #16）已跑通，但 GamePlay 场景里**全是 Cube 占位**：
- 玩家 Cube（蓝）
- 49 个其它玩家位 Cube（橙/暗红）
- Boss 未实现（场景中没有显式 Boss 对象）

UI 美术已在前几轮 change（#10 SettingsForm / #06 v2.1）完成，**GamePlay 视觉是当前最大缺口**。本 change 替换 Cube 为 2D 4 方向 sprite + Animator，使游戏从「能跑」升级到「能看」。

## What

把 SpawnerModule 用 `GameObject.CreatePrimitive(Cube)` 创建的角色替换为：

1. **2D 4 方向 sprite**（上 / 下 / 左 / 右，因角色非左右对称，**不用 flipX**）
2. **AnimationClip + AnimatorController**（按动作 × 方向组合）
3. **PlayerAnimatorBridge**（监听 CombatModule 事件 → 切动画状态/参数）

## Scope

**做（本轮）**：

- **3 个可选玩家**（其它两位 Boss/Bot 复用，见下）
  - Player1：**全套动画**（Idle / Walk / Death / Attack 占位 × 4 方向 = 16 个 clip）
  - Player2 / Player3：**仅 Idle**（4 方向 = 4 clip 每个，供 CharacterSelect 预览）
- **1 个 Boss**：全套动画同 Player1（16 clip）
- **49 个"其它玩家"位**（实质是多人占位 Bot）：**直接复用 Player1 美术**，不出新图（后续靠 shader 染色区分，见 change #19）
- **SpawnerModule 改造**：Cube → Resources.Load 角色 Prefab
- **PlayerAnimatorBridge**：监听 MoveTickEvent / AttackHitEvent / PlayerDiedEvent 等，驱动 Animator 参数
- **Editor 工具**：sprite 目录（按动作×方向）→ AnimationClip 批量生成（tools-engineer 写）
- **Attack 占位**：触发动画但不改伤害逻辑（数值已在 #06/#16 跑通）
- **TC-Art**：5 条验收用例 + qa-engineer 编排 playtest loop

**不做（推迟到他 change）**：

- ❌ Bot shader 染色 / VFX / 阴影描边 / 音效 → **#19 gameplay-visual-polish-deferred**
- ❌ WeaponSelect 流程（CharacterSelect→WeaponSelect→Combat）→ **#18 weapon-select-flow**（本轮 SpawnerModule 默认给 Player 固定一把武器占位即可）
- ❌ Player2 / Player3 全套动画（仅 Idle 够选择界面预览）
- ❌ 第 2 / 3 个 Boss 全套动画

## DoD

1. **5 条 TC-Art 全部 PASS**（playtest loop ≤5 轮收敛，详见 [tests/min-plan.md](./tests/min-plan.md)）
2. SpawnerModule 不再调用 `CreatePrimitive(Cube)` 创建角色（地面/灯保留 primitive 可以）
3. Animator 状态切换走 PlayerAnimatorBridge 桥接 CombatModule 事件，**不在 Update 里轮询输入**
4. 美术素材落到 `Assets/Resources/Sprite/Character/<Name>/<Action>/<Dir>.png`，导入参数走 `UISpriteImportProcessor` 或新增的 `CharacterSpriteImportProcessor`
5. AnimationClip 落到 `Assets/Resources/Anim/Character/<Name>/<Action>_<Dir>.anim`，AnimatorController 落到 `Assets/Resources/Anim/Character/<Name>/Controller.controller`
6. **0 Console Error** 退出条件（同 #16 标准）

## Risk

| 风险 | 影响 | 缓解 |
|---|---|---|
| codex-image-gen 4 方向风格不一致 | 高 | 每个角色先出一个方向 + 用户确认后批量；3 轮重试上限 |
| 4 帧 × 4 方向 × 4 动作 × 4 角色 = 256 张图，token / 时间预算大 | 高 | Fan-Out 并行（每个 角色×动作×方向 一个子 Agent） |
| unity-skills MCP 没有 native `animationclip_create` skill | 高 | tools-engineer 写 Editor 工具：扫 sprite 目录批量生成 AnimationClip + 配 AnimatorController |
| SpawnerModule 改 Prefab 加载后 49 个 actor 性能下降 | 中 | 先复用同一份 sprite + Material，后续 #19 上 GPU instancing / SRP Batcher |
| Boss 与 Player 动画状态机差异（攻击模式不同） | 中 | 第一版 Boss Controller 与 Player 同结构（Idle/Walk/Death/Attack 占位），行为差异留给后续 change |

## Out-of-scope（明确不在本 change）

- Bot 行为差异化（智能 vs 轻量）—— 已在 BotControllerModule 里实现，本轮不动
- Boss AI 行为 —— 占位（保持现有移动/受击逻辑）
- 性能优化 —— SRP Batcher / Instancing 留给 #19
- 音效 —— 见 #19
