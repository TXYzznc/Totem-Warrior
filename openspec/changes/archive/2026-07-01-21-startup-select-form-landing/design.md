# Design — 21-startup-select-form-landing

> 阶段 A 共识固化。阶段 B 按本文自决，除非触发例外打断条件。

## 决策日志（阶段 A grill 结果）

| 决策 | 选择 | 备选 | 理由 |
|---|---|---|---|
| **Loop 范围** | 只跑 21 到 5 DoD 全过 | 21+22 并行 / 21 最小可玩 | 22 是打磨，先解决"能玩"再解决"好玩" |
| **美术策略** | 完整走 UI 6 阶段 + 复用优先 | 全占位色块 / 只做角色 icon | 用户要求生产质量，但避免重复造轮 |
| **Warning 边界** | 只清 21 相关 | 顺手清 MainMixer | 范围不蔓延；MainMixer 属 22 |
| **角色 icon** | 3 卡片用同一张占位 | 3 张独立 codex 出图 | 3 角色属性无差异，立绘属独立 change |
| **多 Agent 编排** | 5 阶段 Fan-Out + 顺序混合 | 全串行 / 全并行 | 依赖阶段（mockup 未确认前不能拆）不能并；独立阶段（N 张拆分、Prefab∥标注稿）必须并 |

## 目标

打通 MainMenu → CharacterSelect → StartupSelect → InGame 全 UI 链路，让玩家可以**不依赖 debug 菜单**从主菜单一路点到战斗，且选中的武器真正被装备。

## 技术方案

### 1. UI 层级 / 事件流

```
MainMenuForm.StartButton.onClick
  └─ _uiModule.Open<CharacterSelectForm>()

CharacterSelectForm.NextButton.onClick
  └─ _uiModule.Open<StartupSelectForm>()
  └─ this.Close()

StartupSelectForm.OnConfirm()
  ├─ _bus.Publish(new StartupSelectedEvent(color, weapon, patterns))   // 已实现
  ├─ _runner.GetModule<GameStateModule>().StartGame()                  // ★ 新增：切 InGame
  └─ Close()

SpawnerModule.OnStartupSelected(e)   // 已实现（[EventHandler]）
  └─ 装备玩家颜料/武器/图案

GameStateModule.StartGame()          // 已实现
  └─ StateChanged: MainMenu → InGame
  └─ CombatModule.OnGameStateChanged → RunStartedEvent
```

### 2. CharacterSelectForm 从空壳到实体

**现状**：Awake 里 `SetActive(false)`，Start 里等 GameApp 就绪注册到 UIModule，无 UI 内容。

**改造**：
- `_characterRoot` Transform + `_nextBtn` Button 字段，`Awake` 用 `FindChildTransform/FindChildComponent` 名兜底绑定（同 StartupSelectForm 范式）
- `BuildCards()` 动态生成 3 张卡片（Image + Button + Text 名标签），选中后 `_selectedCharacterId` 记录，`_nextBtn.interactable` 亮起
- Next 按钮打开 StartupSelectForm、关闭自身
- 3 角色数值差异**不做**——只是 UI 上有 3 个可选，`_selectedCharacterId` 目前只用于日志

**为什么不新增 CharacterConfig**：3 角色暂无属性差异，加 DataTable 是过度设计。等真正需要角色差异化时开 change 补 CharacterConfig + 立绘。

### 3. StartupSelectForm.OnConfirm 补 State 迁移

**现状**：只发 `StartupSelectedEvent`，SpawnerModule 装备完成后**没人切 State**，玩家还停在 MainMenu 状态。

**改造**：`OnConfirm` 末尾追加：
```csharp
var gs = _runner.GetModule<GameStateModule>();
gs?.StartGame();   // 触发 StateChanged → CombatModule 发 RunStartedEvent → HUD 初始化
```

时序保证：`StartupSelectedEvent` 是同步发布，SpawnerModule.OnStartupSelected 立即执行完（装备玩家），然后才调 `StartGame()`。所以 `RunStartedEvent` 出来时玩家已经有武器了。

### 4. Prefab 结构

**CharacterSelect.prefab**（已有壳，补内容）：
```
CharacterSelect (Canvas Panel)
├─ Background (Image)
├─ Title (Text: "选择角色")
├─ CharacterRoot (HorizontalLayoutGroup)
│  └─ (卡片由 code 动态生成，Prefab 只放空的 Transform)
└─ NextButton (Button, "下一步")
```

**StartupSelect.prefab**（新建）：
```
StartupSelect (Canvas Panel)
├─ Background (Image)
├─ Title (Text: "起手 Build")
├─ ColorRoot (HorizontalLayoutGroup, 空 Transform)
├─ WeaponRoot (HorizontalLayoutGroup, 空 Transform)
├─ PatternRoot (HorizontalLayoutGroup, 空 Transform)
├─ ConfirmButton (Button, "确定")
└─ CancelButton (Button, "取消")
```

Prefab 只做**空的层级 + 名字**——StartupSelectForm.cs 已有 `FindChildTransform("ColorRoot")` 名兜底绑定，拖引用可省。

### 5. 美术素材清单（复用盘点后决定 codex 数）

盘点顺序：
1. 先看 `Assets/Resources/Sprite/UI/CharacterSelectForm/` / `MainMenuForm/` 已有素材
2. 看已归档 change 17-gameplay-character-art 是否留下可复用角色 sprite
3. 决定 codex 出图清单（不重复造轮）

清单表（最坏情况全新出）：
| 类别 | 数量 | 说明 | 优先复用 |
|---|---|---|---|
| Panel Background | 2 | CharSel / StartupSel 面板底图 | 若 MainMenuForm 有可用 → 复用 |
| Button | 2 | Next / Confirm（Cancel 复用 Confirm 色变） | 若 Buttons/ 目录有 → 复用 |
| 角色 icon | 1 | 3 张卡片共用一张 | 若已归档 17 有玩家 sprite → 复用 |
| 颜料 icon | 3 | 红/蓝/黄圆片（可用 Image+纯色替代） | 直接色块 Image，不出图 |
| 武器 icon | 5 | 短刀/重锤/手枪/弓/能量拳 | 若有 → 复用；无 → codex |
| 图案 icon | 2 | Line / Ring | 直接白色线条 sprite 替代 |

### 6. 多 Agent 编排（Fan-Out 模式 1 + Skeleton 混合）

```
Phase 0 顺序: 主对话写 design/tasks + 盘点素材
Phase 1 顺序: art-ui 写 requirements 三表 + prompts.md
Phase 2 顺序: 主对话调 codex-image-gen 出 mockup（最多 3 轮重试）
Phase 3 Fan-Out: N 个 Agent 并行 ui-asset-splitting
Phase 4 Fan-Out:
  ├─ art-ui: 写标注稿
  └─ client-unity: unity-skills MCP 建 Prefab + 补 code
       (WhenAll 汇合)
Phase 5 顺序: 主对话 editor_play 联调，出问题→ delegate 修
Phase 6 顺序: 主对话产出 playtest 报告 + archive
```

## 例外打断触发条件（阶段 B 才 halt）

- StartupSelectForm.cs 现有 `[EventHandler]` 或事件签名需要**破坏性变更**（改事件签名/删事件）→ halt 交主对话确认
- 需要新增/删除 `Assets/Scripts/Core/` 框架类 → halt
- codex-image-gen 3 轮重试仍不通过 → halt 交人工

## 验收（对应 proposal DoD）

1. ✅ MainMenu → InGame 全按钮走完，无需 debug 菜单
2. ✅ StartupSelect 选武器 X → 日志显示装备武器 X
3. ✅ 13 元素全显示（复用后可能减少 codex 出图数）
4. ✅ 二次开局无异常
5. ✅ 0 Console Error + 0 与 21 相关的 Warning（MainMixer 属 22 不管）
