# Tasks — 17-gameplay-character-art

> ✅ = 完成；🟡 = 进行中；🔲 = 未开始

## 阶段 1 — 文档骨架（已完成）

- [✅] proposal.md
- [✅] design.md
- [✅] tasks.md（本文件）
- [✅] specs/gameplay-character-art/spec.md
- [✅] tests/min-plan.md（5 条 TC-Art）

## 阶段 2 — Editor 工具（tools-engineer 主导）

- [✅] 新增 `Assets/Editor/Character/AnimatorGenerator.cs`
  - 菜单：`Tools/Character/Generate Animator from Sprite Folder`、`.../Generate All (Headless)`、`.../Reimport Then Generate All`、`.../Force Reimport Sprites`
  - 输入：扫 `Assets/Resources/Sprite/Character/<Name>/` 目录
  - 输出：`.anim` + `.controller` + `.prefab`
  - 修复：Walk 不可达（删 State 参数，用 IsMoving 直驱）；Death 被抢（加 Dead Bool 网关 Idle/Walk/Attack）；Direction 顺序对齐 Bridge.ComputeDirection（Down=0/Up=1/Left=2/Right=3）
- [✅] 新增 `Assets/Editor/Character/CharacterSpriteImportProcessor.cs`
  - 自动设 sprite 导入参数：TextureType=Sprite, SpriteMode=Multiple, **PPU=256**（适配 1536×1024 sprite sheet）, FilterMode=Point
  - 自动按宽度均切 4 份（每帧 384×1024）
- [🔲] 编译通过 + 自测一次（待 Step 5 Unity 跑菜单时验证）

## 阶段 3 — 美术需求与提示词（art-director 主导，并行）

- [✅] `art/requirements.md` 三表（角色清单 / 复用组件清单 / 动作×方向矩阵 共 40 张）
- [✅] `art/prompts.md`（Player1×16 + Player2×4 + Player3×4 + Boss1×16）

## 阶段 4 — SpawnerModule 改造（client-unity，与阶段 3 并行）

- [✅] 改 `Assets/Scripts/Modules/Spawner/SpawnerModule.cs`
  - 玩家：Cube → `Resources.Load<GameObject>("Prefab/Character/Player1")` + Instantiate（fallback Cube + Warn）
  - 49 个 actor：同上，全部 Player1 prefab（占位多人位）
  - 新增 `SpawnBoss()`：实例化 Boss1 prefab 到 (0, 0.4, 15)
  - EntityRef 防重：3 个 spawn 点全部用 `GetComponent ?? AddComponent`
- [✅] 新增 `Assets/Scripts/Modules/Combat/PlayerAnimatorBridge.cs`
  - 订阅 PlayerDiedEvent；OnUpdate 拉 InputModule 驱动 Direction / IsMoving / Dead / Die
  - 死后 `_isDead=true` 早退避免 AnyState 抢回 Idle
- [🔲] Unity 编译通过（待 Step 5 验证）

## 阶段 5 — 美术资源出图（Fan-Out 并行，40 个任务）

- [⚠️] Player1 × 16 → Idle/Attack/Death 各 4 已 ✅；Walk × 4 = Idle 占位（codex 配额耗尽 19:30 复刻）；Death/Down = Right 占位
- [✅] Player2 × 4（Idle × 4 方向）
- [✅] Player3 × 4（Idle × 4 方向）
- [✅] Boss1 × 16 → Idle/Walk/Attack/Death × 16 全到位
- [🟡] 生成记录.md 落盘（骨架已写，落地 tally 待 group_*_result.json 汇总后回填）
- [✅] 3 轮重试上限策略已写入 codex_runner（per-batch 3 retries）

## 阶段 6 — Animator 与 Prefab 生成（主对话 + tools-engineer）

- [✅] Unity Editor 跑 `Tools/Character/Reimport Then Generate All`
  - 输出 `Assets/Resources/Anim/Character/Player1/{16 .anim + Controller.controller}` ✅
  - 输出 `Assets/Resources/Anim/Character/Player2/{4 .anim + Controller.controller}` ✅
  - 输出 `Assets/Resources/Anim/Character/Player3/{4 .anim + Controller.controller}` ✅
  - 输出 `Assets/Resources/Anim/Character/Boss1/{16 .anim + Controller.controller}` ✅
- [✅] 输出 `Assets/Resources/Prefab/Character/{Player1, Player2, Player3, Boss1}.prefab`
- [✅] 编译 / 校验通过（修复 SpawnerModule.cs `SetColor` 残留调用 → 改用 Renderer.material.color；Console Errors=0）
- [⚠️] 占位 sprite 注记：Player1/Walk × 4 = Idle 复制（codex 配额耗尽 19:30 复刻）；Player1/Death/Down = Right 复制

## 阶段 7 — TC-Art 测试（qa-engineer 主导 loop，≤5 轮）

- [✅] tests/min-plan.md 已建（阶段 1）
- [✅] tests/results.md — Round 1 全 PASS（18/18）
- [✅] tests/bugs.md — BUG-17-01（非阻塞，Animator reference 时序）记录在案
- [✅] loop-state.md — Round 1 终态
- [✅] 5/5 TC-Art PASS + 0 Console Errors → 退出 loop（1 轮通过）

## 阶段 8 — 归档

- [🟡] `openspec archive-change 17-gameplay-character-art`（进行中）
- [🔲] 更新 `项目知识库（AI自行维护）/INDEX.md`

## 阶段 9 — 后续修补（不阻塞归档，待 codex 配额恢复 19:30 后执行）

- [🔲] 重生 Player1/Walk × 4 真图（当前为 Idle 复制占位）
- [🔲] 重生 Player1/Death/Down 真图（当前为 Right 复制占位）
- [🔲] 排查 BUG-17-01：`ReimportThenGenerateAll` 末尾追加 `AssetDatabase.Refresh()` 或 frame-delay
