# Design — 10-settings-form

> 设置面板的具体规格，含数据模型 / 状态机 / 跨模块契约 / 关键决策记录。
> 阶段 4 实现以本文为准；任何偏离需在此文档追加说明。

---

## 1. 决策记录（用户 2026-06-26 锁定）

| # | 决策 | 选择 | 理由 |
|---|---|---|---|
| Q1 | 音量响应时机 | **拖动即时生效** | 玩家拖动时能立刻听到反馈，符合直觉 |
| Q2 | 取消按钮行为 | **完全回滚到打开面板时的快照** | 与 Q1 配套：即时生效但取消可撤销 |
| Q3 | 按键重绑定持久化 | **统一写入 SettingsSave**（与音量画质同档） | 一份存档，避免散落 PlayerPrefs |
| Q4 | 画质档位 | **仅切 URP RP Asset 三档**（Low/Med/High） | MVP 够用，细分参数留后续扩展 |
| Q5 | 重绑定冲突 | **弹提示拒绝**（保持原绑定） | 最简单，避免自动交换带来的误操作 |

---

## 2. 数据模型

### 2.1 `SettingsData`（运行时 + 存档共用）

```csharp
[Serializable]
public class SettingsData
{
    // 音量：0.0 ~ 1.0
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;

    // 画质：0=Low / 1=Med / 2=High
    public int qualityLevel = 1;

    // 重绑定：key = ActionName（"Move"/"Attack"/"Pause"）, value = InputBinding Path
    public Dictionary<string, string> keyBindings = new();
}
```

### 2.2 存档落点

- 文件：`Application.persistentDataPath/settings.json`
- 格式：`JsonUtility` 序列化（与项目其他存档一致），`Dictionary` 用 `SerializableDictionary` 包装或 `List<KVPair>`
- 版本字段：`int version = 1`（预留迁移用）

### 2.3 默认值表（资源加载兜底）

| 字段 | 默认 |
|---|---|
| bgmVolume | 1.0 |
| sfxVolume | 1.0 |
| qualityLevel | 1（Med） |
| Move | `<Keyboard>/wasd`（4 键合一，由 InputAction 复合绑定支持） |
| Attack | `<Mouse>/leftButton` |
| Pause | `<Keyboard>/escape` |

---

## 3. SettingsModule（新建）

### 3.1 职责

- 启动时读 `settings.json`（不存在 → 用默认值）
- 应用到运行时（AudioMixer / QualitySettings / InputModule）
- 提供 `SettingsForm` 的查询 / 暂存 / 提交 / 回滚接口
- 持久化（提交时写盘）

### 3.2 模块分类

```csharp
public class SettingsModule : IGameModule
{
    public ModuleCategory Category => ModuleCategory.Service; // 工具型，无依赖业务模块
    public Type[] Dependencies => new[] { typeof(AudioModule), typeof(InputModule) };
}
```

> AudioModule / InputModule 必须先就绪，因为 InitAsync 中要立即应用上次保存的设置。

### 3.3 公共 API（最小集）

```csharp
// 查询
SettingsData GetCurrent();           // 当前生效值

// 三态分离（配合 Q1 + Q2）
void BeginEdit();                    // SettingsForm 打开时调用，拍快照
void Preview(SettingsData draft);    // 拖动滑动条/换画质时调用，即时应用但不写盘
void Commit();                       // 保存按钮：把当前 preview 值写盘 + 清快照
void Rollback();                     // 取消按钮：恢复到快照值并应用 + 清快照

// 重绑定冲突检测（弹提示用）
bool IsBindingConflict(string actionName, string newPath);
```

### 3.4 状态机

```
Idle ──BeginEdit──▶ Editing(snapshot saved)
Editing ──Preview──▶ Editing（更新 current, 不更 snapshot）
Editing ──Commit──▶ Idle（写盘, 丢弃 snapshot）
Editing ──Rollback──▶ Idle（restore snapshot, 应用, 丢弃 snapshot）
```

**约束**：`Idle` 状态下调 `Preview/Commit/Rollback` → log Warn + 忽略；不抛异常。

---

## 4. 应用机制

### 4.1 音量

- 通过 `AudioModule.SetBgmVolume(float)` / `SetSfxVolume(float)` 暴露的接口（若不存在则阶段 4 由 client-unity 补）
- 内部走 `AudioMixer.SetFloat("BGMVolume", LinearToDb(v))`
- 0 ~ 1 滑动条值 → `Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20`（线性→分贝）

### 4.2 画质

- `QualitySettings.SetQualityLevel(level, applyExpensiveChanges: true)`
- 三档 RP Asset：`Assets/Settings/URP-Low.asset` / `URP-Med.asset` / `URP-High.asset`（阶段 4 创建占位即可）
- `GraphicsSettings.renderPipelineAsset` 同步切换

### 4.3 按键重绑定

- 通过 `InputModule` 提供的接口（如不存在，阶段 4 由 client-unity 加）：

```csharp
// InputModule 需暴露
IDisposable StartRebinding(string actionName, Action<string> onComplete, Action onCancel);
string GetCurrentBindingPath(string actionName);
void ApplyBindingOverride(string actionName, string newPath);
void ResetBindingOverride(string actionName);
string GetActionUsingPath(string path); // 用于冲突检测，返回 null 或冲突 action 名
```

- UI 流程：
  1. 玩家点击「重绑定」按钮 → 按钮文字变「按任意键...」
  2. 调 `StartRebinding(actionName, ...)`
  3. 玩家按键 → InputModule 回调返回 `newPath`
  4. SettingsForm 调 `SettingsModule.IsBindingConflict(actionName, newPath)`
     - true → 弹 Toast「该按键已被『暂停』占用」+ 不写入
     - false → 调 `Preview()` 更新 draft + 按钮文字变新键名

### 4.4 启动时应用

- `SettingsModule.InitAsync`：读 `settings.json` → 立即调内部 `ApplyAll(data)` 把音量 / 画质 / 重绑定全套上
- 不发任何事件（在 Init 阶段，符合项目约束）

---

## 5. 跨模块契约

### 5.1 新增事件

```csharp
// Assets/Scripts/Events/SettingsEvents.cs

public readonly struct SettingsAppliedEvent
{
    public readonly SettingsData Data;
    public SettingsAppliedEvent(SettingsData data) { Data = data; }
}
```

**何时发**：`Commit()` 写盘成功后 + `InitAsync` 启动应用后。
**谁订阅**：未来需要响应设置变化的模块（如 UI 主题、HUD 字号等），MVP 期可能无订阅者。

> **不**新增 `BgmVolumeChangedEvent` / `SfxVolumeChangedEvent` 这类细粒度事件——AudioModule 走 SetXxxVolume 直接调，无需广播。

### 5.2 触达点

| 模块 | 变化 |
|---|---|
| `AudioModule` | 新增 `SetBgmVolume(float)` / `SetSfxVolume(float)`（如已有则复用） |
| `InputModule` | 新增 §4.3 列出的 5 个 API（如已有则复用） |
| `UIModule` | 注册 `SettingsForm` 到 `UIFormConfig` |
| `UIFormConfig.json` | 加一行 `SettingsForm` 配置 |

### 5.3 入口接入

- **主菜单**：`MainMenuForm` 已有「设置」按钮位 → 触发 `UIModule.OpenAsync<SettingsForm>()`
- **暂停菜单**：`PauseMenuForm` 已有「设置」按钮位 → 同上
- 两处入口 = 同一个 SettingsForm prefab（无独占状态）

---

## 6. 关键时序

### 6.1 玩家拖动 BGM 滑动条

```
SliderOnValueChanged(v)
  └─▶ SettingsModule.Preview(draft.with(bgm=v))
        └─▶ AudioModule.SetBgmVolume(v)   // 即时听到
        // 不写盘，不发事件
```

### 6.2 玩家按「取消」

```
CancelBtnClick
  └─▶ SettingsModule.Rollback()
        ├─▶ data = snapshot
        ├─▶ ApplyAll(snapshot)   // 音量恢复 / 画质恢复 / 重绑定撤销
        └─▶ snapshot = null
  └─▶ UIModule.Close(SettingsForm)
```

### 6.3 玩家按「保存」

```
SaveBtnClick
  └─▶ SettingsModule.Commit()
        ├─▶ WriteJsonAsync(settings.json, current)
        ├─▶ snapshot = null
        └─▶ EventBus.Publish(new SettingsAppliedEvent(current))
  └─▶ UIModule.Close(SettingsForm)
```

---

## 7. UI 信息架构

> 详细布局走阶段 2 art-ui 的效果图，这里只列骨架。

```
┌────────────────────────────────┐
│ 设置                       [X] │
├────────────────────────────────┤
│ ▸ 音量                          │
│   BGM   [────●────────] 0.7    │
│   SFX   [─────────●───] 0.85   │
│                                 │
│ ▸ 画质                          │
│   ( ) 低   (●) 中   ( ) 高     │
│                                 │
│ ▸ 按键                          │
│   移动   [   WASD   ]          │
│   攻击   [ 鼠标左键 ]          │
│   暂停   [   Esc    ]          │
│                                 │
├────────────────────────────────┤
│              [ 取消 ]  [ 保存 ] │
└────────────────────────────────┘
```

- 三大组（音量 / 画质 / 按键）垂直堆叠，每组带标题
- 右上 X 关闭等同「取消」（走 Rollback）
- 重绑定按钮被点击时进入「等待按键」状态，文字变「按任意键...」+ 按钮高亮，按 Esc 取消重绑定

---

## 8. 验收门禁

- [ ] 编译通过
- [ ] EditMode 单测：`SettingsModule_RebindConflict_RejectsAndKeepsOriginal()`
- [ ] PlayMode 手测：拖音量即时变 → 取消恢复 → OK
- [ ] PlayMode 手测：换画质即时变 → 保存 → 重启游戏画质保留
- [ ] PlayMode 手测：重绑定冲突弹提示，原绑定不变
- [ ] 阶段 5 运行时截图与 mockups 视觉一致（间距 / 字号 / 配色误差 ≤ 玩家可察觉阈值）

---

## 9. 未尽事项（留给后续 change）

- 灵敏度 / 反转轴 / 死区
- 手柄 / 触屏支持
- 多语言切换
- 画质细分参数
- Cloud Save 同步
- 设置项搜索 / 分类多 Tab
