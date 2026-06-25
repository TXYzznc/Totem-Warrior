# 13-UI 与 HUD

> **版本**: v2.1 ｜ **修订日期**: 2026-06-25 ｜ 主要变更：UI Toolkit → UGUI + 9 Form prefab 化

> **主导 Agent**：art-ui
> **协作 Agent**：art-director（风格指南对齐）/ client-unity（UGUI 接入）/ art-font（CJK 字体选型）
> **依赖系统**：01-纹身构筑系统 / 02-战斗手感 / 03-武器系统 / 04-主动技能 / 05-闪避与身法 / 06-角色设定与骨架 / 07-地图生成 / 08-宝箱与探财 / 09-纹身师 NPC / 10-商人 NPC / 11-怪物与 Boss / 12-数值平衡与曲线
> **被依赖系统**：无（终端展示层）
> **对应代码**：`Assets/Prefabs/UI/` 下各 Form Prefab + `Assets/Scripts/Modules/UI/` 下各 Form.cs
> **作废文件**：`Assets/UI/CombatHUD.uxml` / `CombatHUD.uss` / `Assets/Scripts/Modules/Tattoo/UI/CombatHUDForm.cs`（UI Toolkit 版，全部作废）
> **状态**：v2.1 UGUI 重写版

---

## 一、玩家体验目标

### 单局 25 分钟的 HUD 沉浸契约

**一句话**：HUD 必须"零认知成本可读"——玩家在激战中扫一眼就知道活着还是快死了、还能打什么，**绝不因 UI 遮挡而错失信息**。

本游戏 HUD 的核心矛盾是：玩家 Build 信息量大（6 部位 × 7 颜色 × 8 图案的纹身组合 + 2 技能槽 + 武器弹药 + 缩圈倒计时），但俯视角 BR 战斗时玩家视野需要大面积留白。解法是**信息分三层渐进呈现，而非平铺所有数据**：

| 信息层 | 瞬间扫（< 200ms） | 慢看（< 1s） | 长时间观察（> 1s） |
|---|---|---|---|
| **内容** | HP 条颜色状态 + 技能冷却圆圈是否亮 | 当前 Buff 标签 + 武器弹药数 + 缩圈倒计时 | Build 已装备纹身详情 + 被动条目解读 + 战斗日志 |
| **视觉权重** | 最大色块 / 粗线条 / 高亮 | 中号数字 + 图标 | 小字列表 / 滚动区域 |
| **屏占比** | 始终可见，0–5% 屏幕面积 | 常驻但可折叠，5–15% | 仅左侧 sidebar，最多 20% |

**总屏占比上限**：HUD 全展开 ≤ 25%（720p 基准），折叠后 ≤ 10%。

**沉浸不破坏原则**：
- 战斗日志和 Build 列表放左侧边栏，不遮挡地图中心战斗视野。
- 纹身师工作台、商人面板、三选一面板以全屏覆盖层呈现，保留半透明背景（玩家仍感知到游戏世界），不做纯黑全屏遮罩。
- 任何面板打开时暂停 BGM 以外的声效，让玩家专注 UI 决策。

---

## 二、核心机制 — UGUI 架构与 Form 清单

### 2.1 UGUI 轻量 MVP 架构

**技术选型**：Canvas + Image / TMP_Text / Button 原生 UGUI 控件，**不引入 MVVM 框架或第三方 DI**。

**核心约定**：
- 1 Form = 1 Prefab（`Assets/Prefabs/UI/<FormName>.prefab`）+ 1 `<FormName>.cs`（View 与 Presenter 合一）
- 所有 Form Prefab 挂载于统一的 `UICanvas`（Screen Space - Overlay，Sort Order 分层）
- RectTransform 锚点严格使用屏幕角/边锚定，**禁止**硬编码像素偏移量
- 动画统一用 DOTween 驱动 RectTransform / CanvasGroup.alpha，**不用** Animator Controller 做 UI 过渡
- LayoutGroup（HorizontalLayoutGroup / VerticalLayoutGroup / GridLayoutGroup）负责列表排版，禁止手动设置子项位置
- 字体渲染全部使用 TMP_Text（TextMeshPro）

**Canvas 层级（Sort Order 由低到高）**：

| Sort Order | 层名 | 挂载 Form |
|---|---|---|
| 0 | HUD 层 | CombatHUDForm |
| 10 | 覆盖层 | TattooStudioForm / TattooEnchantForm / ShopForm / ThreeChoiceForm |
| 20 | 系统层 | PauseMenuForm / RunResultForm |
| 30 | 全屏层 | MainMenuForm / CharacterSelectForm |

### 2.2 Form 清单（9 个 Prefab）

| # | Form 名 | Prefab 路径 | 触发条件 | 关闭条件 | Sort Order |
|---|---|---|---|---|---|
| 1 | **CombatHUDForm** | `Assets/Prefabs/UI/CombatHUDForm.prefab` | `RunStartedEvent` | `RunEndedEvent` / `PlayerDiedEvent` | 0 |
| 2 | **TattooStudioForm** | `Assets/Prefabs/UI/TattooStudioForm.prefab` | `NPCInteractStartEvent`（NPC=TattooArtist）| ESC / B键 / `TattooSessionEndEvent` | 10 |
| 3 | **TattooEnchantForm** | `Assets/Prefabs/UI/TattooEnchantForm.prefab` | 纹身师 NPC 附魔选项选中 | ESC / B键 / 附魔完成回调 | 10 |
| 4 | **ShopForm** | `Assets/Prefabs/UI/ShopForm.prefab` | `NPCInteractStartEvent`（NPC=Shop）| ESC / B键 / `ShopClosedEvent` | 10 |
| 5 | **ThreeChoiceForm** | `Assets/Prefabs/UI/ThreeChoiceForm.prefab` | `ThreeChoiceShownEvent` | 必须做出选择（不可跳过，3s 防误触锁）| 10 |
| 6 | **PauseMenuForm** | `Assets/Prefabs/UI/PauseMenuForm.prefab` | ESC（战斗中） | ESC 再次 / 继续按钮 | 20 |
| 7 | **RunResultForm** | `Assets/Prefabs/UI/RunResultForm.prefab` | `RunEndedEvent` | 手动按键确认后返回大厅 | 20 |
| 8 | **MainMenuForm** | `Assets/Prefabs/UI/MainMenuForm.prefab` | 游戏启动 / 返回大厅 | 进入 Run / 退出游戏 | 30 |
| 9 | **CharacterSelectForm** | `Assets/Prefabs/UI/CharacterSelectForm.prefab` | MainMenuForm「开始」按钮 | 确认选择 / 返回主菜单 | 30 |

> **CharacterSelectForm 说明**：本期仅 1 个角色，但骨架预留多角色卡片 GridLayoutGroup，后续直接添加卡片 Prefab 即可扩展，不改 Form 结构。

### 2.3 CombatHUDForm HUD 区块（战斗中常驻）

技能槽改为 **Q/E 两位**（v2.1 变更，去掉 R 槽）：

| 区块 | RectTransform 锚点 | 最小尺寸（720p） | 内容 | 信息层 | 数据来源 |
|---|---|---|---|---|---|
| **HP 条** | 左上角，安全区内边距 16px | 200×16px | HP 数值 + Image（filled）渐变色：绿→黄→红 | 瞬间扫 | 订阅 `DamagedEvent` |
| **Buff 标签行** | HP 条下方，VerticalLayoutGroup | 16×16px/枚，最多 4 枚 | 当前激活 Buff 缩略图标 + 剩余帧（TMP_Text） | 慢看 | 订阅 `PassiveRecomputedEvent` |
| **技能槽（Q/E）** | 底部居中，HorizontalLayoutGroup | 48×48px × 2 | 技能图标（Image）+ 冷却遮罩（Image filled radial）+ Controller Glyph | 瞬间扫 | 订阅 `SkillCastEvent` |
| **武器图标 + 弹药** | 技能槽右侧 | 36×36px + TMP_Text | 持握武器图标 + 弹药数 / "∞" | 慢看 | 订阅 `ItemPickedEvent` |
| **小地图** | 右上角，圆形 Mask | 120×120px | RawImage（动态绘制）+ 缩圈边界 | 慢看 | 订阅 `MapGeneratedEvent` / `ZoneShrinkPhaseEvent` |
| **缩圈倒计时** | 小地图正下方 | TMP_Text | "缩圈：mm:ss" + 阶段色 | 慢看 | 订阅 `ZoneShrinkPhaseEvent` |
| **战斗日志** | 左侧 sidebar 下半，ScrollRect | 180×120px | 最近 30 条伤害/击杀/被击（TMP_Text，颜色分级）| 长时间观察 | 订阅 `EffectAppliedEvent` / `TargetKilledEvent` / `ActorDiedEvent` |
| **已装备 Build** | 左侧 sidebar 上半，ScrollRect | 180×100px | 每个纹身槽：部位图标 × 颜色图标 × 图案图标 + TMP_Text | 长时间观察 | 订阅 `BuildChangedEvent` |
| **被动条目** | 左侧 sidebar 中区，ScrollRect | 180×60px | 当前激活被动 + 共鸣标记 | 长时间观察 | 订阅 `PassiveRecomputedEvent` |
| **Boss HP 条** | 顶部居中，条件显示 | 400×12px | Image（filled）+ Boss 名称 TMP_Text | 慢看 | 订阅 `BossSpawnedEvent` / `BossPhaseChangedEvent` |

### 2.4 TattooStudioForm — 玩家纹身界面（含读条 UI）

- 布局：800×600px 居中 Panel，CanvasGroup alpha 半透明背景（60% 黑底）
- 读条 UI：触发附魔时显示 ProgressBar（Image filled horizontal），DOTween 驱动填充，期间屏蔽所有交互按钮
- 读条 4 态：idle / filling / success（绿色脉冲 0.3s）/ fail（红色抖动 0.2s shake）
- 死亡宝箱地图高亮：地图缩略图上叠加 Image（红色半透明圆点），标记玩家上次死亡位置的宝箱；数据由 `DeathChestSpawnedEvent` 提供，Form 打开时立即刷新

---

## 三、与其他系统的耦合

### 3.1 每个 Form 订阅的事件 + 调用的业务 API

| Form | 订阅事件 | 调用 API / 模块 |
|---|---|---|
| **CombatHUDForm** | `RunStartedEvent` / `DamagedEvent` / `EffectAppliedEvent` / `TargetKilledEvent` / `ActorDiedEvent` / `BuildChangedEvent` / `PassiveRecomputedEvent` / `SkillCastEvent` / `ItemPickedEvent` / `MapGeneratedEvent` / `ZoneShrinkPhaseEvent` / `BossSpawnedEvent` / `BossPhaseChangedEvent` | `SkillModule.GetCooldownRatio(slotIndex)` / `WeaponModule.GetCurrentAmmo()` / `MapModule.GetMinimapTexture()` |
| **TattooStudioForm** | `NPCInteractStartEvent`（NPC=TattooArtist）/ `TattooSessionEndEvent` / `TattooEquippedEvent` / `DeathChestSpawnedEvent` | `TattooModule.GetAvailableSlots()` / `TattooModule.EquipTattoo(slot, tattooId)` |
| **TattooEnchantForm** | `EnchantStartEvent` / `EnchantResultEvent` | `TattooModule.GetEnchantOptions()` / `TattooModule.ConfirmEnchant(optionId)` |
| **ShopForm** | `NPCInteractStartEvent`（NPC=Shop）/ `ShopPurchaseEvent` / `ShopRefreshEvent` / `ShopClosedEvent` | `ShopModule.GetInventory()` / `ShopModule.Purchase(itemId)` / `PlayerModule.GetGold()` |
| **ThreeChoiceForm** | `ThreeChoiceShownEvent` / `ThreeChoiceMadeEvent` | `RewardModule.GetChoiceOptions()` / `RewardModule.ConfirmChoice(optionIndex)` |
| **PauseMenuForm** | `PauseRequestedEvent` | `GameApp.PauseGame()` / `GameApp.ResumeGame()` / `GameApp.QuitToMenu()` |
| **RunResultForm** | `RunEndedEvent` | `RunStatsModule.GetCurrentRunSnapshot()` |
| **MainMenuForm** | 无（启动直接激活）| `GameApp.StartRun()` / `GameApp.QuitGame()` |
| **CharacterSelectForm** | 无 | `CharacterModule.GetAvailableCharacters()` / `CharacterModule.SelectCharacter(id)` |

### 3.2 系统耦合总表

| 系统 | HUD/面板触点 | 订阅事件 | UI 响应 |
|---|---|---|---|
| **01-纹身构筑** | Build 列表 / 被动条目 / TattooStudioForm | `BuildChangedEvent` / `PassiveRecomputedEvent` / `TattooEquippedEvent` | 刷新 VerticalLayoutGroup 内容 |
| **02-战斗手感** | 战斗日志 / HP 条 | `EffectAppliedEvent` / `DamagedEvent` | 追加 TMP_Text 日志条目，更新 Image filled 比例 |
| **03-武器系统** | 武器图标 + 弹药 | `ItemPickedEvent` | 切换 Image sprite，更新 TMP_Text 弹药数 |
| **04-主动技能** | 技能槽 Q/E | `SkillCastEvent` | DOTween 驱动 Image filled radial 冷却遮罩 |
| **05-闪避与身法** | 左腿 Buff 标签 | `PassiveRecomputedEvent` | Buff 图标 Image color 高亮"蓄势"态 |
| **06-角色设定** | HP 条上限 | `RunStartedEvent` | 读取 `MaxHealth` 设定 Image fillAmount 基准 |
| **07-地图生成** | 小地图 / TattooStudioForm 地图 | `MapGeneratedEvent` / `RoomEnteredEvent` / `DeathChestSpawnedEvent` | RawImage 更新纹理，红点图标叠加 |
| **08-宝箱与探财** | ThreeChoiceForm | `ThreeChoiceShownEvent` / `ThreeChoiceMadeEvent` | 面板进/退场 DOTween，3s 防误触 Button.interactable 锁 |
| **09-纹身师 NPC** | TattooStudioForm / TattooEnchantForm | `NPCInteractStartEvent` / `TattooSessionEndEvent` | CanvasGroup alpha 进场 0.2s，结算后刷新 Build |
| **10-商人 NPC** | ShopForm | `NPCInteractStartEvent` / `ShopPurchaseEvent` | GridLayoutGroup 刷新库存卡片，TMP_Text 金币数更新 |
| **11-怪物与 Boss** | 战斗日志 / Boss HP 条 | `TargetKilledEvent` / `BossSpawnedEvent` / `BossPhaseChangedEvent` | 日志追加击杀条；Boss HP 条 RectTransform SetActive 显示 |
| **12-数值平衡** | HP 条颜色 / 日志伤害色 | `RunStartedEvent` | 初始化 HP 基准；TMP_Text 颜色按伤害量级三档分层 |

---

## 四、数值与配置 — UI 三表

### 表 A：页面清单（9 Form + HUD 区块）

| # | 名称 | Form / 区块类型 | 优先级 | 尺寸基准（1080p） | 状态 | 备注 |
|---|---|---|---|---|---|---|
| 1 | CombatHUDForm | 常驻 HUD Canvas | 必做 | 1920×1080，安全区内 | 待重建 | v2.1 全新 UGUI Prefab |
| 2 | HP 条（含闪烁动效） | HUD 区块 | 必做 | 240×20px | 待做 | Image filled horizontal + DOTween color |
| 3 | Buff 标签行 | HUD 区块 | 必做 | 4×20px icon，VerticalLayoutGroup | 待做 | 最多 4 枚，超出隐藏 |
| 4 | 技能槽 Q/E | HUD 区块 | 必做 | 2×56px，HorizontalLayoutGroup | 待做 | 冷却遮罩 radial filled + Controller Glyph |
| 5 | 武器图标 + 弹药 | HUD 区块 | 必做 | 40×40px + TMP_Text 24px | 待做 | 近战显示 "∞" |
| 6 | 小地图（圆形 Mask） | HUD 区块 | 必做 | 140×140px RawImage | 待做 | 动态纹理更新 |
| 7 | 缩圈倒计时 | HUD 区块 | 必做 | TMP_Text | 待做 | 颜色随阶段变化 |
| 8 | 左侧 Sidebar | HUD 区块 | 必做 | 200×360px，可折叠 | 待做 | 折叠后仅显示 Build 摘要 |
| 9 | Boss HP 条 | HUD 区块（条件） | 必做 | 400×12px 顶部居中 | 待做 | 仅 BossSpawnedEvent 后 SetActive |
| 10 | TattooStudioForm | 覆盖层 Panel | 必做 | 800×600px 居中 | 待做 | 含地图高亮 + 读条 UI |
| 11 | TattooEnchantForm | 覆盖层 Panel | 必做 | 700×500px 居中 | 待做 | 嵌套在 TattooStudio 流程内 |
| 12 | ShopForm | 覆盖层 Panel | 必做 | 700×500px 居中 | 待做 | GridLayoutGroup 库存格 + 金币 |
| 13 | ThreeChoiceForm | 覆盖层 Panel（强制） | 必做 | 900×400px 居中 | 待做 | 3 张 CardPanel 横排，3s 锁 |
| 14 | PauseMenuForm | 全屏遮罩 | 必做 | 全屏 + 中央 VerticalLayoutGroup | 待做 | 继续/设置/退出三项 Button |
| 15 | RunResultForm | 全屏 Panel | 必做 | 全屏动画进场 | 待做 | 杀敌/存活/Build 快照 |
| 16 | MainMenuForm | 全屏 Panel | 必做 | 全屏 | 待做 | 开始/设置/退出 |
| 17 | CharacterSelectForm | 全屏 Panel | 必做 | 全屏，GridLayoutGroup 角色卡 | 必做 | 本期 1 角色，骨架预留 |
| 18 | 伤害飘字 | 世界空间 Canvas | 可复用 | TMP_Text 24–48px 动态字号 | 待做 | 按伤害量级三档字号 |

### 表 B：复用组件清单

| 组件类型 | 目标数量（估算）| 用途 | 关键规格 |
|---|---|---|---|
| **IconFrame**（图标底框 Image） | 约 40 个（21 纹身图标 + 技能 2 + 武器 5 + Buff 多枚）| 统一视觉规格 | 圆角 r=4px（Sprite 9-slice）+ 描边 1px |
| **ProgressBar**（Image filled horizontal）| 5 个（HP / 冷却 Q / 冷却 E / 读条 / Boss HP）| 复用填充 DOTween 曲线 | fillAmount [0,1]，颜色由外部赋值 |
| **ScrollListRow**（VerticalLayoutGroup 子项）| 约 40 条（Build 槽 + 被动 + 日志）| 统一行高 | 行高 24px @ 1080p，TMP_Text 12px |
| **CardPanel**（卡片 Panel）| 3 张（ThreeChoiceForm 选项）| 统一悬停/选中状态 | Button + Image，4 态样式复用 |
| **ModalOverlay**（CanvasGroup + 半透明 Image 背景）| 5 个（工作台/商人/三选一/暂停/结算）| 统一覆盖层底板 | alpha 0.6，DOTween 进退场 0.2s |
| **ControllerGlyph**（Image Sprite set）| 约 10 个（Q/E/Space/ESC/交互/购买/确认/取消/折叠/翻页）| Controller 按键提示 | Xbox / PS / Switch 三套 Sprite |

### 表 C：组件状态表

| 组件 | 必备状态 | 视觉区分手段 | 备注 |
|---|---|---|---|
| **HP 条** | normal（绿）/ warning（黄，HP＜50%）/ critical（红，HP＜25%）/ dead（灰） | Image color + fillAmount | warning/critical 触发 DOTween 闪烁 |
| **技能槽 Q/E** | ready（图标全亮）/ cooldown（Image radial filled 遮罩中）/ no-skill（空槽灰底）/ active（按下帧 Image color 白边高亮 0.1s）| Image color + fillAmount radial | Controller Glyph 随状态显示/隐藏 |
| **Buff 标签** | active（彩色 Image）/ expiring（DOTween 闪烁，剩余＜3s）/ consumed（CanvasGroup alpha 淡出消失）| Image alpha + color | — |
| **武器图标** | equipped / switching（CanvasGroup alpha 淡入 0.15s）/ empty-ammo（TMP_Text color 红）| Image sprite swap + alpha | — |
| **缩圈倒计时** | safe（TMP_Text 绿）/ warning（橙，＜30s）/ urgent（红加粗，＜10s）| TMP_Text color + fontStyle Bold | urgent 时 DOTween scale 轻微脉冲 |
| **CardPanel** | idle / hover（Image color 边框高亮 + RectTransform scale 1.03）/ selected（Image color 确认色填充）/ locked（Button.interactable=false，Image color 灰态）| Image color + RectTransform scale | 3s 解锁后 Button.interactable=true |
| **ModalOverlay** | open（CanvasGroup alpha 0→1，DOTween 0.2s）/ idle（alpha=1）/ close（alpha 1→0，DOTween 0.2s）| CanvasGroup alpha | 退场完成后 SetActive(false) |
| **ControllerGlyph** | default / pressed（RectTransform scale 0.9 瞬间 + 恢复 0.08s）| RectTransform scale | 仅检测到手柄输入时显示，键鼠模式 SetActive(false) |
| **Boss HP 条** | normal（Image color 白/红）/ phase-change（DOTween color 脉冲 0.5s）/ hidden（SetActive false，Boss 未出现时）| Image color + SetActive | BossPhaseChangedEvent 触发 phase-change 态 |
| **读条（TattooEnchant）** | idle（fillAmount=0）/ filling（DOTween 驱动 fillAmount 0→1）/ success（Image color 绿色脉冲 0.3s）/ fail（RectTransform shake 0.2s）| Image fillAmount + color + DOTween shake | filling 期间所有操作按钮 Button.interactable=false |

---

## 五、UX / UI 触点 — 控制方案对比

### 5.1 PC 键鼠

| 操作 | 键位 | HUD 响应 |
|---|---|---|
| 移动 | WASD | 无 HUD 变化 |
| 普攻 | 鼠标左键 | 右臂纹身图标 Image color 短暂高亮 |
| 蓄力普攻 | 长按鼠标左键 | HP 条旁蓄力 ProgressBar SetActive 弹出 |
| 技能 Q/E | Q / E | 对应技能槽启动冷却遮罩（Image radial filled DOTween） |
| 闪避 | 空格 | 左腿 Buff 图标 Image color 高亮"蓄势" |
| 交互/进入面板 | F | 对应 Form ModalOverlay 进场 DOTween |
| 暂停 | ESC | PauseMenuForm 进场 |
| 折叠 Sidebar | Tab | 左侧 Sidebar RectTransform width DOTween 折叠/展开 |

**鼠标专属**：面板内所有 Button 支持 EventTrigger OnPointerEnter hover 高亮。CardPanel 鼠标点击触发 selected 态。

### 5.2 移动端虚拟摇杆

**差异项**（相对 PC 键鼠需额外设计）：

| 差异点 | PC 键鼠 | 移动端 |
|---|---|---|
| 移动输入 | WASD | 左侧虚拟摇杆（固定 RectTransform 锚定左下） |
| 攻击 | 鼠标左键 | 右侧攻击浮动 Button（拇指区） |
| 技能 Q/E | 键盘 | 右侧两个圆形 Button，纵排，VerticalLayoutGroup |
| 闪避 | 空格 | 右侧闪避 Button（拇指区右上） |
| 面板交互 | 鼠标点击 | Touch，RectTransform 最小可触区域 44×44px |
| Sidebar | Tab 折叠 | 默认折叠，左下角 Button 展开；竖屏 Sidebar 收为底部 strip |
| Controller Glyph | 无（键鼠模式 SetActive false）| 触摸模式同样 SetActive false |

**移动端 Sidebar 适配**：竖屏时 Sidebar 默认收起为底部 strip（仅 Build 摘要缩图 HorizontalLayoutGroup），上滑展开完整 ScrollRect 列表。横屏时与 PC 版布局一致。

**控制器优先原则**：检测到手柄接入时，所有交互按键提示自动切换为 Controller Glyph（Xbox / PS / Switch 三套 Sprite），鼠标 hover EventTrigger 降级为 Button.onClick 高亮。

---

## 六、AI 行为侧需求

HUD 为纯玩家展示层，无 AI 行为侧需求。AI Actor 的状态（Bot Build / Bot HP）不在玩家 HUD 上呈现；QA Debug 专用的 AI 状态 Overlay 由 `qa-engineer` + `client-unity` 独立实现，不属于本 GDD 范围。

---

## 七、风险与开放问题

### 7.1 UI 屏幕占比风险

**问题**：俯视角 BR 场景中，Build 6 槽 + 技能 2 槽 + 武器 + 小地图 + 日志汇总后易超过 25% 屏占比上限。

**推荐方案**：采用"瞬间扫层常驻 + 慢看层常驻折叠态 + 长时间观察层手动展开"三级策略（已在 §一 定义）。Sidebar 默认折叠时屏占比目标 ≤ 10%，展开后 ≤ 20%，加上技能槽/HP/小地图合计 ≤ 25%。

**待确认**：art-director 需审定折叠/展开 DOTween 时长（建议 0.15s ease-out）以及 Sidebar ModalOverlay CanvasGroup alpha（建议 0.6）。

### 7.2 多分辨率适配

**问题**：720p 掌机与 1080p 手机屏幕密度不同，同一 Canvas 在低 DPI 下图标模糊、TMP_Text 过小。

**推荐方案**：
- Canvas Scaler 设为 `Scale With Screen Size`，参考分辨率 1920×1080，Match 0.5（宽高各取一半）。
- Sprite 提供 1×（32px）/ 2×（64px）两档，32px 档为所有图标最小可识别目标，要求 IconFrame 线条粗细 ≥ 2px、实心面积 ≥ 50%。
- 掌机（720p）Sidebar 默认折叠，技能槽 Button 最小可触区 RectTransform 44×44px。
- 4K 屏（2160p）Canvas Scaler 自动 2× 放大，Sprite 2× 档正好对齐物理像素。

**四档安全区验证目标**：720p / 1080p / 4K / 掌机（Switch 式 720p 横屏）均需通过不破版验证。

### 7.3 本地化字体（CJK 覆盖）

**问题**：战斗日志、面板标题、被动条目说明含中文，需 CJK 字体子集支持。TMP_Text 需指定自定义 TMP_FontAsset。

**推荐方案**：由 `art-font` 提供已子集化的 CJK TMP_FontAsset（目标包体 ≤ 1MB），日志区 ScrollListRow TMP_Text 12–14px @ 1080p，面板标题 16–20px。字体选型 escalate 至 `art-font`。

### 7.4 色盲可访问性

HUD 颜色方案必须通过以下三档色盲模拟验收：

| 验收档 | 关键检查项 |
|---|---|
| **红绿色盲（Deuteranopia）** | HP 条绿/红渐变 → 叠加亮度差（绿亮红暗）+ Sprite 形状区分，不能仅靠 Image color |
| **蓝黄色盲（Tritanopia）** | 冰霜（蓝）/ 雷电（黄）纹身图标 → 形状区分，IconFrame 轮廓在 32×32 最小尺寸下独立可识别 |
| **全色盲（Achromatopsia）** | 所有状态信息 → TMP_Text 数字/文字/形状单独可读，不依赖 Image color 编码 |

**强制规范**：Buff 图标不能仅用 Image color 区分元素类型，每个 IconFrame Sprite 的轮廓/形状在 32×32 下必须独立可识别。

---

## 八、引用

| 文档 | 引用内容 |
|---|---|
| [01-纹身构筑系统.md](./01-纹身构筑系统.md) | 6 部位 / 7 颜色 / 8 图案图标 ID 范围；Build 列表数据结构 |
| [02-战斗手感.md](./02-战斗手感.md) | EffectAppliedEvent 结果结构；战斗日志颜色分级 |
| [03-武器系统.md](./03-武器系统.md) | 5 把武器图标 ID；弹药机制（有限 / 无限区分） |
| [04-主动技能.md](./04-主动技能.md) | 2 技能槽键位（Q/E）；SkillCastEvent 结构 |
| [05-闪避与身法.md](./05-闪避与身法.md) | 左腿 Buff"蓄势"状态 UI 需求 |
| [12-数值平衡与曲线.md](./12-数值平衡与曲线.md) | MaxHealth 基准值；强度跳跃节点 HUD 反馈需求 |
| [CONTRACT.md](../../../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md) | 全局事件总表（§1.1–1.9）订阅协议 |
