# 12-UIModule+各UIForm 模块详设

> **版本**: v2.1 ｜ **修订日期**: 2026-06-25 ｜ 主要变更：UI Toolkit → UGUI + 9 Form prefab

> **主导 Agent**: client-unity
> **协作 Agent**: art-ui（Prefab 设计）/ art-font（CJK 字体）
> **对应系统 GDD**: ../systems/13-UI与HUD.md
> **当前代码状态**: UIModule 已存在（`Assets/Scripts/Modules/UI/UIModule.cs`）；CombatHUDForm 需重建（UGUI 版）；其余 8 个 Form 尚未创建
> **依赖契约**: `openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md` §1.1–1.9
> **作废文件**: `Assets/UI/CombatHUD.uxml` / `CombatHUD.uss`（UI Toolkit 版，全部作废）

---

## 一、模块职责（一句话）

UIModule 是**注册器 + 全局状态转发器**：管理 `IUIForm` 实例列表，监听 `GameStateChangedEvent` 后批量转发给已注册 Form，由各 Form 自决显隐；**不持有任何 Prefab 引用，不耦合具体业务**。

v2.1 在此基础上管理 9 个 UGUI Form，全部以 Prefab 挂载于场景 `UICanvas` 下：

| Form | Prefab 路径 | C# 路径（目标） | 触发条件 |
|---|---|---|---|
| `CombatHUDForm` | `Assets/Prefabs/UI/CombatHUDForm.prefab` | `Modules/UI/CombatHUDForm.cs` | `RunStartedEvent` |
| `TattooStudioForm` | `Assets/Prefabs/UI/TattooStudioForm.prefab` | `Modules/NPC/UI/TattooStudioForm.cs` | `NPCInteractStartEvent`（NPC=TattooArtist）|
| `TattooEnchantForm` | `Assets/Prefabs/UI/TattooEnchantForm.prefab` | `Modules/NPC/UI/TattooEnchantForm.cs` | 纹身师 NPC 附魔选项选中 |
| `ShopForm` | `Assets/Prefabs/UI/ShopForm.prefab` | `Modules/NPC/UI/ShopForm.cs` | `NPCInteractStartEvent`（NPC=Shop）|
| `ThreeChoiceForm` | `Assets/Prefabs/UI/ThreeChoiceForm.prefab` | `Modules/Event/UI/ThreeChoiceForm.cs` | `ThreeChoiceShownEvent` |
| `PauseMenuForm` | `Assets/Prefabs/UI/PauseMenuForm.prefab` | `Modules/UI/PauseMenuForm.cs` | `PauseRequestedEvent` |
| `RunResultForm` | `Assets/Prefabs/UI/RunResultForm.prefab` | `Modules/UI/RunResultForm.cs` | `RunEndedEvent` |
| `MainMenuForm` | `Assets/Prefabs/UI/MainMenuForm.prefab` | `Modules/UI/MainMenuForm.cs` | 游戏启动 / 返回大厅 |
| `CharacterSelectForm` | `Assets/Prefabs/UI/CharacterSelectForm.prefab` | `Modules/UI/CharacterSelectForm.cs` | MainMenuForm「开始」按钮 |

---

## 二、IGameModule 接口签名

```csharp
public sealed class UIModule : IGameModule
{
    public int    ModuleCategory => 2;          // 中间件层（UI 框架）
    public Type[] Dependencies   => Type.EmptyTypes; // 无初始化依赖

    public UIModule(EventBus eventBus, ModuleRunner runner);

    public UniTask InitializeAsync(CancellationToken ct = default);
    // 仅做 FrameworkLogger.Info，不发事件，不加载资源

    public UniTask ShutdownAsync(CancellationToken ct = default);
    // _forms.Clear()；_exclusiveForm = null

    public void Register(IUIForm form);         // Form 在 Start() 中调用
    public void Unregister(IUIForm form);       // Form 在 OnDestroy() 中调用

    // 覆盖层互斥管理（§九.2 解法）
    public void RequestOpenExclusive(IExclusiveUIForm form);
    public void CloseCurrentExclusive();
}

public interface IUIForm
{
    void OnGameStateChanged(GameState oldState, GameState newState);
}

// 覆盖层 Form 额外实现此接口，UIModule 保证同时只有一个处于 Open 状态
public interface IExclusiveUIForm : IUIForm
{
    bool IsOpen { get; }
    void ForceClose(); // UIModule 互斥时强制关闭
}
```

`ModuleCategory = 2` 表示中间件层，晚于 `TattooModule(1)` / `CombatModule(1)` 初始化，早于 `SpawnerModule(3)` / `MapGenModule(3)`。

---

## 三、订阅 / 发布事件全签名

### 3.1 UIModule 自身订阅（1 条，`[EventHandler]`）

```csharp
[EventHandler] void OnGameStateChanged(GameStateChangedEvent e);
// → foreach (_forms) f.OnGameStateChanged(e.OldState, e.NewState)
```

### 3.2 各 Form 订阅总表（`EventBus.Subscribe`，返回 `IDisposable`，在 `SubscribeEvents()` 中统一管理）

| Form | 订阅事件 | 响应行为 |
|---|---|---|
| **CombatHUDForm** | `RunStartedEvent` | 初始化 HP 基准，SetActive(true) |
| | `DamagedEvent` | 更新 HP 条 `fillAmount` + color 状态 |
| | `EffectAppliedEvent` | 追加战斗日志 TMP_Text 条目 |
| | `TargetKilledEvent` / `ActorDiedEvent` | 追加击杀日志；玩家死亡时 SetActive(false) |
| | `BuildChangedEvent` | 刷新已装备 Build VerticalLayoutGroup |
| | `PassiveRecomputedEvent` | 刷新被动条目列表 + Buff 标签行 |
| | `SkillCastEvent` | DOTween 驱动 Q/E 冷却遮罩 Image radial filled |
| | `ItemPickedEvent` | 切换武器 Image sprite，更新弹药 TMP_Text |
| | `MapGeneratedEvent` / `ZoneShrinkPhaseEvent` | RawImage 更新小地图纹理；更新倒计时 TMP_Text + 颜色 |
| | `BossSpawnedEvent` / `BossPhaseChangedEvent` | Boss HP 条 GameObject SetActive；更新 fillAmount |
| **TattooStudioForm** | `NPCInteractStartEvent`（NPC=TattooArtist）| Open()，DOTween CanvasGroup alpha 进场 0.2s |
| | `TattooSessionEndEvent` | Close()，DOTween 退场 0.2s |
| | `TattooEquippedEvent` | 刷新槽位 Build 预览 |
| | `DeathChestSpawnedEvent` | 地图缩略图叠加红点 Image |
| **TattooEnchantForm** | `EnchantStartEvent` | Open()；读条 ProgressBar DOTween 开始填充 |
| | `EnchantResultEvent` | 结果动效（success 绿脉冲 / fail 红抖动）→ Close() |
| **ShopForm** | `NPCInteractStartEvent`（NPC=Shop）| Open()，进场动画 |
| | `ShopPurchaseEvent` | 刷新 GridLayoutGroup 库存格 |
| | `ShopRefreshEvent` | 重建库存格全量刷新 |
| | `ShopClosedEvent` | Close() |
| | `CoinChangedEvent` | 更新金币 TMP_Text |
| **ThreeChoiceForm** | `ThreeChoiceShownEvent` | Open()；渲染 3 张 CardPanel；Button.interactable = false（3s 防误触锁）|
| | `ThreeChoiceMadeEvent` | Close() |
| **PauseMenuForm** | `PauseRequestedEvent` | Open()；Time.timeScale = 0 |
| **RunResultForm** | `RunEndedEvent` | 等 CombatHUDForm 隐藏后，DOTween 进场；填充统计数据 |
| **MainMenuForm** | 无（启动直接 SetActive）| - |
| **CharacterSelectForm** | 无 | - |

### 3.3 各 Form 发布事件

各 Form 不主动 Publish 事件——业务行为通过直接调用对应模块 API 完成（如 `TattooModule.EquipTattoo`、`ShopModule.Purchase`），由被调用模块负责发布事件。

---

## 四、DataTable Schema

### UIFormConfig.json（新建，可选）

```json
{
  "table": "UIFormConfig",
  "fields": [
    { "name": "Id",          "type": "int",    "comment": "Form 唯一 ID" },
    { "name": "FormName",    "type": "string", "comment": "Form C# class 名" },
    { "name": "PrefabPath",  "type": "string", "comment": "相对于 Assets/Prefabs/UI/ 的路径" },
    { "name": "SortOrder",   "type": "int",    "comment": "Canvas Sort Order，HUD=0，覆盖层=10，系统=20，全屏=30" },
    { "name": "IsExclusive", "type": "bool",   "comment": "是否实现 IExclusiveUIForm（覆盖层互斥）" }
  ],
  "rows": [
    { "Id": 1, "FormName": "CombatHUDForm",      "PrefabPath": "CombatHUDForm.prefab",      "SortOrder": 0,  "IsExclusive": false },
    { "Id": 2, "FormName": "TattooStudioForm",   "PrefabPath": "TattooStudioForm.prefab",   "SortOrder": 10, "IsExclusive": true  },
    { "Id": 3, "FormName": "TattooEnchantForm",  "PrefabPath": "TattooEnchantForm.prefab",  "SortOrder": 10, "IsExclusive": true  },
    { "Id": 4, "FormName": "ShopForm",           "PrefabPath": "ShopForm.prefab",           "SortOrder": 10, "IsExclusive": true  },
    { "Id": 5, "FormName": "ThreeChoiceForm",    "PrefabPath": "ThreeChoiceForm.prefab",    "SortOrder": 10, "IsExclusive": true  },
    { "Id": 6, "FormName": "PauseMenuForm",      "PrefabPath": "PauseMenuForm.prefab",      "SortOrder": 20, "IsExclusive": false },
    { "Id": 7, "FormName": "RunResultForm",      "PrefabPath": "RunResultForm.prefab",      "SortOrder": 20, "IsExclusive": false },
    { "Id": 8, "FormName": "MainMenuForm",       "PrefabPath": "MainMenuForm.prefab",       "SortOrder": 30, "IsExclusive": false },
    { "Id": 9, "FormName": "CharacterSelectForm","PrefabPath": "CharacterSelectForm.prefab","SortOrder": 30, "IsExclusive": false }
  ]
}
```

> 运行时各 Form 以 MonoBehaviour 挂在 `UICanvas` 子 GameObject 上，Prefab 在 Launch.unity 场景中静态放置；UIFormConfig 的用途是为 art-ui 提供命名约定中心，以及未来动态加载时的 lookup。DataTableGenerator 生成 `Assets/Scripts/DataTable/UIFormConfig.cs` 后，运行时仅做查询，不驱动加载。

---

## 五、与其他模块的交互序列

```
[GameStateModule] --GameStateChangedEvent--> [UIModule] --OnGameStateChanged()--> 各 Form
[InputModule]     --PauseRequestedEvent----> [PauseMenuForm] --> Time.timeScale=0
[NPCModule]       --NPCInteractStartEvent--> [TattooStudioForm / ShopForm] --> Open()
[ShopModule]      --ShopPurchaseEvent------> [ShopForm] --> 刷新库存格
[TattooModule]    --TattooEquippedEvent----> [TattooStudioForm] --> 刷新 Build 预览
[CombatModule]    --DamagedEvent-----------> [CombatHUDForm] --> 更新 HP 条
[SkillModule]     --SkillCastEvent---------> [CombatHUDForm] --> Q/E 冷却遮罩动画
[RewardModule]    --ThreeChoiceShownEvent--> [ThreeChoiceForm] --> 渲染卡片 + 3s 锁
[RunStatsModule]  --RunEndedEvent----------> [RunResultForm] --> 进场动画 + 统计填充
```

**覆盖层互斥流程**（玩家在 TattooStudio 打开时误触发 Shop）：

```
[NPCModule] --NPCInteractStartEvent(NPC=Shop)--> [ShopForm].OnEvent()
ShopForm.RequestOpenExclusive() --> [UIModule]
UIModule: _exclusiveForm.ForceClose() (TattooStudioForm)
UIModule: _exclusiveForm = ShopForm; ShopForm.Open()
```

### Form 生命周期（MonoBehaviour 视角）

```
Awake()
  ├─ 缓存 SerializeField 绑定的子控件引用（Button/Image/TMP_Text/RectTransform）
  └─ 初始状态 gameObject.SetActive(false)（或按设计为 true，如 MainMenuForm）

Start()
  ├─ 等待 GameApp 就绪（UniTask.Yield 轮询，超时 10s FrameworkLogger.Error）
  ├─ GetModule<UIModule>().Register(this)
  ├─ GetModule<...>()  缓存业务模块引用
  ├─ BindButtons()     Button.onClick.AddListener(...)
  └─ SubscribeEvents() EventBus.Subscribe → List<IDisposable> _subs

OnDestroy()
  ├─ _subs.ForEach(d => d.Dispose())
  ├─ DOTween.Kill(transform)  防动画泄漏
  └─ GetModule<UIModule>().Unregister(this)

Open()  / Close()
  ├─ gameObject.SetActive(true / false)
  └─ DOTween 驱动 CanvasGroup.alpha 或 RectTransform.anchoredPosition
```

---

## 六、50 actor 性能预算

UI 完全服务玩家单人，与 50 actor 数量无关。以下为 UI 自身预算：

| 项 | 预算 | 说明 |
|---|---|---|
| EffectAppliedEvent 回调 | < 0.1ms | 仅 Instantiate ScrollListRow Prefab + TMP_Text.SetText（或复用对象池）|
| Update（无 Update 方法） | 0ms | HUD 完全事件驱动，无 Update 开销 |
| GC 分配（每秒） | < 5KB | 战斗日志行对象；未来可扩展为对象池（最多 30 行循环复用）|
| DOTween 实例 | ≤ 12 | 各 Form 进/退场 × 5 + 技能冷却 × 2（Q/E）+ HP 条色彩渐变 × 1 + Boss HP × 1，超出触发 `DOTween.Kill` 复用 |
| 覆盖层面板同时打开数 | ≤ 1 | `IExclusiveUIForm` 互斥机制保证，违反则 ForceClose 已有面板 |

> **禁止在 Button/EventSystem 回调中做 GC alloc（string 拼接 / boxing）**：战斗日志文本在 AppendLog 调用处一次性调用 `TMP_Text.SetText("{0}", intValue)`（TMP 零分配重载），回调内只传预构建引用。

---

## 七、伪联机 → 真联机的迁移点

UI 层完全本地，不存在联机迁移风险。以下两点在迁移时需注意：

1. **玩家标识**：当前 Form 直接读 `SpawnerModule.PlayerTarget`（唯一玩家）；真联机后改为读 `NetworkSession.LocalPlayerActor`，Form 代码只需换一处引用。
2. **延迟显示**：真联机下 `EffectAppliedEvent` 可能来自服务器快照（有帧延迟），战斗日志时间戳应改为使用服务器 tick 而非 `Time.unscaledTime`。

---

## 八、测试策略

### EditMode：UIModule 注册/注销单元测试

**文件**：`Assets/Tests/EditMode/UIModuleTests.cs`

```csharp
[Test]
public void Register_DuplicateForm_IsIgnored()
{
    var bus    = new EventBus();
    var runner = new ModuleRunner(bus);
    var module = new UIModule(bus, runner);
    var form   = new MockUIForm();

    module.Register(form);
    module.Register(form); // 重复注册

    bus.Publish(new GameStateChangedEvent { OldState = GameState.MainMenu, NewState = GameState.InGame });
    Assert.AreEqual(1, form.CallCount);
}

[Test]
public void Unregister_ThenEvent_FormNotNotified()
{
    var bus    = new EventBus();
    var runner = new ModuleRunner(bus);
    var module = new UIModule(bus, runner);
    var form   = new MockUIForm();

    module.Register(form);
    module.Unregister(form);
    bus.Publish(new GameStateChangedEvent { OldState = GameState.MainMenu, NewState = GameState.InGame });
    Assert.AreEqual(0, form.CallCount);
}

[Test]
public void RequestOpenExclusive_ClosesExistingExclusiveForm()
{
    var bus    = new EventBus();
    var runner = new ModuleRunner(bus);
    var module = new UIModule(bus, runner);
    var formA  = new MockExclusiveUIForm();
    var formB  = new MockExclusiveUIForm();

    module.Register(formA);
    module.Register(formB);
    module.RequestOpenExclusive(formA);
    module.RequestOpenExclusive(formB); // 应先 ForceClose formA

    Assert.IsTrue(formA.WasForceClosed);
    Assert.IsTrue(formB.IsOpen);
}
```

### PlayMode：各 Form 渲染验证（各一例）

**文件**：`Assets/Tests/PlayMode/UIFormRenderTests.cs`

```csharp
// CombatHUDForm：DamagedEvent 后 HP 条 fillAmount 正确更新
[UnityTest]
public IEnumerator CombatHUDForm_DamagedEvent_HpBarFillAmountUpdated()
{
    var hud = Object.FindObjectOfType<CombatHUDForm>();
    yield return new WaitUntil(() => hud != null && hud.IsReady);

    var bus = Object.FindObjectOfType<GameApp>().EventBus;
    bus.Publish(new RunStartedEvent { MaxHealth = 100 });
    yield return null;

    bus.Publish(new DamagedEvent { NewHp = 50, MaxHp = 100 });
    yield return null; // 等一帧 UI 刷新

    Assert.AreApproximatelyEqual(0.5f, hud.HpBarFillAmount);
}

// ThreeChoiceForm：ThreeChoiceShownEvent 后按钮 3s 内 interactable=false
[UnityTest]
public IEnumerator ThreeChoiceForm_ShownEvent_ButtonsLockedFor3s()
{
    var form = Object.FindObjectOfType<ThreeChoiceForm>();
    var bus  = Object.FindObjectOfType<GameApp>().EventBus;

    bus.Publish(new ThreeChoiceShownEvent { Options = MockOptions(3) });
    yield return null;

    Assert.IsFalse(form.AreChoiceButtonsInteractable);
    yield return new WaitForSecondsRealtime(3.1f);
    Assert.IsTrue(form.AreChoiceButtonsInteractable);
}

// PauseMenuForm：PauseRequestedEvent 后面板激活，ResumeGame 后隐藏
[UnityTest]
public IEnumerator PauseMenuForm_PauseAndResume_ShowHide()
{
    var form = Object.FindObjectOfType<PauseMenuForm>();
    var bus  = Object.FindObjectOfType<GameApp>().EventBus;

    bus.Publish(new PauseRequestedEvent());
    yield return null;
    Assert.IsTrue(form.gameObject.activeSelf);

    form.OnResumeClicked(); // 模拟按钮点击
    yield return null;
    Assert.IsFalse(form.gameObject.activeSelf);
}
```

---

## 九、风险与开放问题

### 9.1 9 个 Form Prefab 尚未创建

**现状**：所有 Form Prefab（`Assets/Prefabs/UI/`）及对应 UGUI C# 脚本尚未建立。

**推荐方案**：另开 art-ui change（如 `06-ugui-forms-prefab`），由 art-ui 完成 Prefab 层级搭建后，client-unity 再写绑定代码。在 Prefab 就绪前，可先建空 C# Form 类（仅实现 `IUIForm.OnGameStateChanged`），待 Prefab 交付后补充 `BindButtons` + `SubscribeEvents`。

> **手动步骤（需用户先完成）**：在 Launch.unity 场景 UICanvas 下为每个 Form 创建子 GameObject，挂 Form.cs MonoBehaviour，RectTransform 按 §2.3 规格设置，再告知 client-unity 继续写绑定逻辑。

### 9.2 覆盖层面板互斥管理

**方案（已纳入接口）**：`IExclusiveUIForm` 子接口 + `UIModule.RequestOpenExclusive`，新 Form 尝试 Open 时 UIModule 先 `ForceClose` 当前活跃覆盖层 Form。实现约 30 行，不影响现有 `IUIForm` 接口。TattooEnchantForm 嵌套于 TattooStudioForm 流程内，不参与互斥（由 TattooStudioForm 自行管理子 Panel）。

### 9.3 ThreeChoiceForm 的 3 秒防误触锁

**方案**：Form 内用 `UniTask.Delay(3000, DelayType.UnscaledDeltaTime)` 驱动锁定（不受 `Time.timeScale = 0` 暂停影响），3s 到期后 `Button.interactable = true`。边界情况：若 ThreeChoice 在 Pause 状态下触发，需验证 `DelayType.UnscaledDeltaTime` 行为——预期正常计时，无需额外处理。

### 9.4 移动端性能

**方案**：覆盖层背景用 CanvasGroup alpha 0.6 + 纯色半透明 Image 代替模糊效果（UGUI 无原生 blur，Shader blur 在低端 Android 帧数不达标）。如需高端设备 blur，可在 `UIFormConfig` 加 `UseBlurBackground` 字段，运行时按 `SystemInfo.graphicsMemorySize` 阈值切换。

### 9.5 RunResultForm 进场动画时序

**方案**：`RunResultForm` 收到 `RunEndedEvent` 后，`await UniTask.Yield()` 等一帧（CombatHUDForm 同帧响应 `RunEndedEvent` 并 SetActive(false)），再执行 DOTween 进场动画。依赖 `EventBus` 同帧广播顺序：CombatHUDForm 先于 RunResultForm 收到事件（注册顺序保证，HUD 在 GameState=InGame 时先 Register）。若顺序无法保证，改用 `await UniTask.Delay(1, DelayType.Realtime)` 做安全缓冲。

### 9.6 CombatHUDForm UGUI 重建

**现状**：原 `CombatHUDForm.cs`（`Assets/Scripts/Modules/Tattoo/UI/`）基于 UI Toolkit 已作废，需在新路径 `Assets/Scripts/Modules/UI/CombatHUDForm.cs` 重建 UGUI 版，并将所有 `VisualElement` / `Q<T>` 查询改为 `SerializeField` 引用绑定的 UGUI 控件（Image / TMP_Text / Button）。

---

## 引用

| 文档 | 引用内容 |
|---|---|
| [systems/13-UI与HUD.md](../systems/13-UI与HUD.md) | UI 三表（页面清单 / 复用组件清单 / 组件状态表）；Canvas Sort Order；HUD 区块规格 |
| [CONTRACT.md](../../../openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md) §1.1–1.9 | 所有订阅事件类的完整签名 |
| [UIModule.cs](../../../Assets/Scripts/Modules/UI/UIModule.cs) | UIModule 现有实现（注册器 + `[EventHandler]`）|
| [01-TattooModule.md](./01-TattooModule.md) | TattooModule API（`EquipTattoo` / `GetAvailableSlots`）|
