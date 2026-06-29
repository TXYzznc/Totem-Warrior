# Design — 13-ui-screens-complete

## 一、核心界面清单（11 个）

### 1.1 清单评估

包含原有 10 个 Form + 新增 SelfTattooForm：

| # | Form | 触发场景 | 状态 | 优先级 | 代码框架 |
|---|---|---|---|---|---|
| 1 | **MainMenuForm** | 游戏启动 / 返回大厅 | ✅ 代码完 | **P0** | 99 行 |
| 2 | **CharacterSelectForm** | 主菜单"开始" | ✅ 代码完 | **P0** | 47 行 |
| 3 | **CombatHUDForm** | 战斗中常驻 | ✅ 代码完 | **P0** | 268 行 + 读条子区块 |
| 4 | **SelfTattooForm** | 战斗中 Tab 键（**新**） | 🆕 待新建 | **P0** | ~150 行预估 |
| 5 | **PauseMenuForm** | 战斗中 ESC | ✅ 代码完 | **P1** | 136 行 |
| 6 | **TattooStudioForm** | 纹身师 NPC（现为纯附魔工） | ✅ 代码完 | **P1** | 133 行 |
| 7 | **TattooEnchantForm** | 纹身附魔选项 | ✅ 代码完 | **P1** | 48 行 |
| 8 | **ShopForm** | 商人 NPC | ✅ 代码完 | **P1** | 133 行 |
| 9 | **ThreeChoiceForm** | 宝箱三选一事件 | ✅ 代码完 | **P1** | 60 行 |
| 10 | **RunResultForm** | 单局结束 | ✅ 代码完 | **P1** | 47 行 |
| 11 | **SettingsForm** | 主菜单/暂停菜单入口 | 🟡 进行中（10-settings-form） | **P2** | 321 行 |

### 1.2 SelfTattooForm 新增说明

**触发方式**：玩家在战斗中任意地点按 `Tab` 键

**GDD 依据**：`项目知识库/GDD-v2/systems/01-纹身构筑系统.md` §2.8 / §5.1 / §5.2

**界面需求**：
- 部位选择区（6 部位高亮）：头 / 胸 / 腰 / 左臂 / 右臂 / 腿
- 颜料库存（按数量显示，0 灰显）：常见（红黄绿蓝）/ 稀有（紫金）/ 传说（白）
- 图案选择（解锁状态显示）：当前有 2 个初始图案（直线 + 圆环），后续 Boss/宝箱 解锁更多
- 预览区：实时显示选中部位 + 颜色 + 图案的效果
- 风险提示：本部位读条时长（3-8s 根据部位不同）+ 中断惩罚说明
- 开始/取消按钮：开始后发 `RequestSelfTattooEvent`，UIModule 自动关闭该 Form

**读条 UI（子区块）**：
- 位置：不单独开 Form，在 CombatHUDForm 的条件显示逻辑中实现
- 效果：角色脚下圆环 + 屏幕中央进度条
- 关闭条件：读条完成或被中断（受击、移动、按 ESC）

---

## 二、视觉补齐工作分层

### 2.1 完整 5 阶段流程

**阶段 1：需求设计**（已完成）
- `art/requirements.md` 三表（页面清单 / 复用组件清单 / 组件状态表）

**阶段 2：效果图设计**（art-ui）
→ 撰写 11 个 Form 的效果图提示词，包含：
  - SelfTattooForm：部位高亮 UI + 颜料库存列表 + 图案解锁态 + 预览窗
  - 其他 10 个 Form：参考既有风格 + 10-settings-form 的 mockups 对齐

**阶段 3：效果图生成**（主对话 → codex-image-gen）
→ 按分批顺序生成，每个 Form 最多 3 轮重试

**分批顺序**（按重要性排序）：
1. **批次 A（P0 核心，最高频）**：
   - MainMenuForm
   - CharacterSelectForm
   - CombatHUDForm
   - SelfTattooForm
   
2. **批次 B（P1 每局必经）**：
   - PauseMenuForm
   - RunResultForm
   
3. **批次 C（P1 条件触发）**：
   - TattooStudioForm
   - TattooEnchantForm
   - ShopForm
   - ThreeChoiceForm

4. **SettingsForm（P2，独立 change）**：
   - 由 10-settings-form 独立处理，本 change 仅验证链路

**阶段 4：Prefab 视觉调整**（client-unity）
→ 按效果图微调现有 Prefab（间距 / 字号 / 配色 / Sprite），**不改 RectTransform 层级**
→ SelfTattooForm：新建 Prefab + 脚本

**阶段 5：联调验收**（client-unity + qa-engineer）
→ 见下方§三

---

## 三、联调验证清单

### 3.1 端到端流程验证

跑通一局完整游戏循环：

```
主菜单
  ↓ [点击"开始" or 测试快捷键]
角色选择
  ↓ [选择角色 + 确认]
战斗 HUD
  ├─ [Tab] SelfTattooForm（选部位 / 颜料 / 图案 → 读条 → 落实）
  ├─ [E] TattooStudioForm（NPC 附魔）
  ├─ [E] ShopForm（商人买东西）
  ├─ [宝箱事件] ThreeChoiceForm
  ├─ [收集敌人掉落的颜料 / 金币]
  └─ [ESC] PauseMenuForm（暂停）
      └─ [继续 or 返回主菜单 or Settings]
战斗结束（死亡 or 撤离）
  ↓
结算页（RunResultForm）
  ├─ [查看本局成绩]
  └─ [返回主菜单 or 重新开始]
主菜单
  ↓ (循环结束)
```

### 3.2 具体验证点

| 检查项 | 验证方法 | 备注 |
|---|---|---|
| **Form 触发链路** | 每个 Form 对应的业务事件（如 `NPCInteractStartEvent`）正确从 Module 发出，UIModule 能正确接住并打开 Form | 若某个事件始终不发出，记录到 `tests/bugs.md` 转给对应 Module owner |
| **Form 关闭链路** | ESC/B 键统一逻辑关闭；`ThreeChoiceForm` 3s 防误触锁；`TattooEnchantForm` 读条期间按钮锁定 | 验证 InputModule 与 UIModule 的事件对接 |
| **Sort Order 层级** | HUD 常驻层（0）/ 覆盖层（10）/ 系统层（20）/ 全屏层（30）正确遮挡 | 如 PauseMenuForm 被 CombatHUDForm 盖住 = 错，需修复 Sort Order |
| **跨 Form 状态同步** | ShopForm 购买后，金币数在 CombatHUDForm 同步刷新；TattooStudioForm 附魔后，装备栏实时更新 | 验证 EventBus 事件链路是否正确 |
| **异常路径** | 战斗中突然死亡时，所有覆盖层（NPC、Shop、Three Choice）自动关闭，RunResultForm 无遮挡 | 验证 `PlayerDeadEvent` 的强制关闭逻辑 |
| **SelfTattooForm 读条** | Tab 打开 → 选部位/颜料/图案 → 点开始 → 读条条 → 自动关闭；按 ESC 中断读条 | 验证 `TattooModule.StartSelfTattoo` / `RequestSelfTattooEvent` / `TattooFinishedEvent` 的完整链路 |
| **设置菜单链路** | MainMenuForm 与 PauseMenuForm 都能打开 SettingsForm；关闭后返回到调用 Form | 跨 Form 导航状态管理，涉及 `UIModule.ShowFormStack` 的栈管理 |

### 3.3 Bug 修复原则

**只修这类问题**：
- Form 显示异常（如被其他层盖住、Sort Order 错误）
- Form 交互异常（如按钮不响应、ESC 不关闭）
- 事件链路不通（如 NPC 交互事件发不出、UIModule 接不到）
- 跨 Form 状态不同步（如金币数不刷新）

**这类问题转给对应 owner**（记录到 `tests/bugs.md`）：
- 数值异常（如怪物伤害不对、金币掉落数量错误）
- 玩法异常（如 Boss 不出现、宝箱概率不对）
- 美术资源缺失（如 Sprite 贴不上、动画不播放）
- 音效缺失（如 NPC 对话音没声）

---

## 四、SelfTattooForm 代码框架预期

### 4.1 UI Form 脚手架

```csharp
// Assets/Scripts/Modules/UIModule/SelfTattooUIForm.cs
public class SelfTattooUIForm : IUIForm
{
    [SerializeField] private Button[] _bodyPartButtons; // 6 部位按钮
    [SerializeField] private GridLayoutGroup _colorGrid; // 颜料库存网格
    [SerializeField] private Button[] _patternButtons; // 图案选择
    [SerializeField] private Image _previewImage; // 预览
    [SerializeField] private Text _timeHintText; // 读条时长提示
    [SerializeField] private Button _startButton; // 开始按钮
    
    private int _selectedBodyPart = -1;
    private ColorType _selectedColor;
    private TattooPattern _selectedPattern;
    
    public void OnBodyPartSelected(int bodyPartIndex)
    {
        _selectedBodyPart = bodyPartIndex;
        UpdatePreview();
        // 更新时长提示：3-8s 根据部位
    }
    
    public void OnStartButtonClicked()
    {
        if (_selectedBodyPart < 0) return;
        // Publish RequestSelfTattooEvent
        var @event = new RequestSelfTattooEvent
        {
            BodyPart = (BodyPartType)_selectedBodyPart,
            Color = _selectedColor,
            Pattern = _selectedPattern
        };
        EventBus.Publish(@event);
        // Form 自动关闭由 TattooModule 发 TattooFinishedEvent 触发
    }
}
```

### 4.2 读条 UI 子区块（CombatHUDForm 内）

```csharp
// 在 CombatHUDForm 中添加
[SerializeField] private Transform _tattooProgressGroup; // 容纳圆环 + 进度条
private ProgressBar _readingProgressBar;
private bool _isShowingProgress = false;

// 订阅事件
private void OnSelfTattooStarted(RequestSelfTattooEvent @event)
{
    _isShowingProgress = true;
    _tattooProgressGroup.SetActive(true);
    // 开启读条动画（3-8s）
    StartCoroutine(PlayProgressBar(@event.DurationSeconds));
}

private void OnTattooFinished(TattooFinishedEvent @event)
{
    _isShowingProgress = false;
    _tattooProgressGroup.SetActive(false);
}
```

---

## 五、与其他 change 的关系

| Change | 状态 | 本 Change 关系 |
|---|---|---|
| 06-v21-implementation | ✅ 交付 | Form 代码框架基础，本 change 沿用 |
| 10-settings-form | 🟡 进行中 | 独立进行效果图 → Prefab 流程；本 change 仅验证 SettingsForm 的打开/关闭链路 |
| 12-core-ui-screens | 作为前置评估 | 本 change 是其延伸，新增 SelfTattooForm + 扩大范围至完整联调 |

---

## 六、时间预期与资源分配

| 阶段 | 主要 Agent | 工作日 | 并行度 |
|---|---|---|---|
| 阶段 2（效果图设计） | art-ui | 1 day | 串行 |
| 阶段 3A（核心 Form 出图） | codex-image-gen | 1 day | Form 级并行，每个 3 轮上限 |
| 阶段 3B/C（条件 Form 出图） | codex-image-gen | 0.5 day | 继续并行 |
| 阶段 4（Prefab 微调 + SelfTattooForm 新建） | client-unity | 1.5 day | 并行 11 个 Form |
| 阶段 5（联调验收） | client-unity + qa-engineer | 1.5 day | Fan-Out + WhenAll |
| **总计** | — | **≤5 day** | — |

