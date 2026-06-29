# Tasks — 13-ui-screens-complete

## 执行计划（3-5 工作日，并行 Agent 编排）

### Phase A：需求准备（1 day）

#### Task A1：编写三表需求文档
- **负责**：主对话 (orchestrator)
- **产出**：`art/requirements.md`（页面清单 / 复用组件清单 / 组件状态表）
- **依赖**：无
- **验收**：三表齐全，art-ui 可直接用于提示词撰写

#### Task A2：SelfTattooForm 界面需求
- **负责**：主对话
- **产出**：`art/requirements.md` 中新增 SelfTattooForm 章节
- **依赖**：A1
- **验收**：界面流程清晰（部位 → 颜料 → 图案 → 预览 → 开始/取消）

---

### Phase B：视觉设计 & 生成（2-2.5 day）

#### Task B1：效果图提示词编写（art-ui）
- **负责**：**art-ui Agent**
- **产出**：`art/prompts.md`（11 个 Form 各 1 条提示词）
- **依赖**：A1 / A2
- **工作内容**：
  - 设计 11 个 Form 的效果图提示词
  - 风格对齐 10-settings-form mockups
  - 包含 SelfTattooForm 的部位高亮、颜料库存、图案解锁态等关键 UI 元素
- **验收**：提示词完整，每条 ≥ 100 字，描述明确

#### Task B2：核心 Form 效果图生成（批次 A，codex-image-gen）
- **负责**：**主对话** → `codex-image-gen` SKILL
- **产出**：`art/mockups/<FormName>.png`（MainMenu / CharacterSelect / CombatHUD / SelfTattoo）
- **依赖**：B1
- **工作内容**：
  - 调用 codex-image-gen，每个 Form 最多 3 轮重试
  - 按优先级顺序：MainMenu → CharacterSelect → CombatHUD → SelfTattoo
  - 每轮重试调整提示词（若用户/系统提示效果不符）
  - 3 轮仍不通过则停下通知用户决策
- **验收**：4 张 mockups 品质达成（清晰度、信息层级、风格一致）
- **时间**：1 day

#### Task B3：条件 Form 效果图生成（批次 B/C，codex-image-gen）
- **负责**：主对话 → codex-image-gen
- **产出**：`art/mockups/<FormName>.png`（PauseMenu / RunResult / TattooStudio / TattooEnchant / Shop / ThreeChoice）
- **依赖**：B2 完成 + 用户确认批次 A mockups
- **工作内容**：继续按分批顺序生成，同样 3 轮重试上限
- **验收**：6 张 mockups 齐全
- **时间**：0.5 day

#### Task B4：生成记录
- **负责**：主对话
- **产出**：`art/生成记录.md`（记录每个 Form 的重试情况、最终选用版本）
- **依赖**：B2 / B3
- **验收**：记录完整，便于后续追溯

---

### Phase C：Prefab 制作 & 代码（1.5-2 day）

#### Task C1：SelfTattooForm Prefab 新建 + 脚本
- **负责**：**client-unity Agent**
- **产出**：
  - `Assets/Resources/Prefab/UI/SelfTattooForm.prefab`（新建）
  - `Assets/Scripts/Modules/UIModule/SelfTattooUIForm.cs`（新建脚本，参考 TattooStudioForm 框架）
- **依赖**：B2（SelfTattoo mockup 确认）
- **工作内容**：
  - 按 mockup 搭建层级（Canvas → Panel → [BodyParts / Colors / Patterns / Preview / Buttons]）
  - 实现 UI Form 脚手架（部位选择、颜料更新、图案预览、开始/取消逻辑）
  - **关键**：发 `RequestSelfTattooEvent` 时，监听 `TattooFinishedEvent` 自动关闭 Form
- **验收**：
  - Prefab 层级与 mockup 一致
  - 代码编译通过，无 TODO
  - 与 TattooModule 事件链路对接正确

#### Task C2：读条 UI 子区块（CombatHUDForm 扩展）
- **负责**：client-unity
- **产出**：
  - `Assets/Resources/Prefab/UI/CombatHUDForm.prefab`（修改，新增读条区块）
  - `Assets/Scripts/Modules/UIModule/CombatHUDForm.cs`（扩展，新增读条逻辑）
- **依赖**：设计阶段确认读条 UI 位置 / 大小
- **工作内容**：
  - 在 CombatHUDForm 中新增 ProgressGroup（角色脚下圆环 + 屏幕中央进度条）
  - 订阅 `RequestSelfTattooEvent` 显示读条，订阅 `TattooFinishedEvent` 隐藏读条
  - **不改** CombatHUD 其他逻辑（HP 条、能量条等）
- **验收**：
  - 读条动画播放正确（3-8s 时长与部位对应）
  - ESC 中断时进度条停止
  - 完成后自动隐藏

#### Task C3：其他 10 个 Form Prefab 视觉微调（并行）
- **负责**：client-unity（并行处理 10 个 Form）
- **产出**：
  - 修改 `Assets/Resources/Prefab/UI/MainMenuForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/CharacterSelectForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/CombatHUDForm.prefab`（基础部分）
  - 修改 `Assets/Resources/Prefab/UI/PauseMenuForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/TattooStudioForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/TattooEnchantForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/ShopForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/ThreeChoiceForm.prefab`
  - 修改 `Assets/Resources/Prefab/UI/RunResultForm.prefab`
  - SettingsForm：仅验证链路，不改视觉（由 10-settings-form 独立处理）
- **依赖**：B2 / B3 mockups 确认
- **工作内容**：
  - 按 mockup 调整间距、字号、配色、Sprite
  - **禁止改** RectTransform 层级结构和脚本逻辑
  - 使用 Unity Inspector 逐项对比 mockup 调整
- **验收**：
  - 每个 Form Prefab 在 PlayMode 下视觉与 mockup ≥ 90% 一致
  - 编译无错，无 warning

#### Task C4：代码补齐（业务逻辑缺口）
- **负责**：client-unity
- **触发条件**：联调时若发现"Form 打不开"、"事件链路不通"等阻断性 bug
- **工作内容**：
  - 检查事件发送端（Module）是否正确发 Publish
  - 检查 UIModule 订阅逻辑是否正确
  - 检查 Form 脚本中的事件处理是否有错
- **不补齐**：数值问题、玩法问题（转给对应 owner，记录到 `tests/bugs.md`）
- **验收**：所有 Form 能通过事件链路正确打开/关闭

---

### Phase D：端到端联调验收（1-1.5 day）

#### Task D1：完整流程验证（client-unity + qa-engineer）
- **负责**：**client-unity + qa-engineer Agent**（Fan-Out 并行）
- **产出**：`tests/checklist.md`（验证项目逐项标记 Pass / Fail）
- **依赖**：C1 / C2 / C3 / C4 完成
- **工作内容**：
  - 跑通一局完整游戏流程（主菜单 → 角色选 → 战斗 → NPC/自纹身 → 暂停 → 结算 → 返回）
  - 逐项验证§三中的检查表（Form 触发链路、关闭逻辑、Sort Order、跨 Form 同步等）
  - 记录每一步的 Pass / Fail / Bug 描述
- **验收**：
  - 无阻断性 bug（Form 无法打开、ESC 无法关闭、致命错误等）
  - 所有 Form 视觉与 mockup ≥ 90% 一致
  - 流程完整可玩

#### Task D2：Bug 修复 & 迭代
- **负责**：client-unity（根据 D1 检查结果）
- **依赖**：D1 检查清单
- **工作内容**：
  - 修复 D1 中标记为 Fail 的阻断性 bug（Form 打不开、事件不响应、遮挡错误等）
  - **不修**：数值问题、美术资源缺失、音效缺失（记录转给 owner）
  - 修复后重复 D1 中失败的项目，直到 All Pass
- **验收**：所有检查项 Pass，一局游戏完整可玩

#### Task D3：Bug 记录与转移
- **负责**：qa-engineer
- **产出**：`tests/bugs.md`
- **工作内容**：
  - 记录非阻断性 bug（如 Sprite 贴不上、音效缺失、数值异常）
  - 标记 bug 类型、优先级、转移给的 owner（如美术、数值设计等）
  - 示例：
    ```
    ## 非阻断性 bug 清单
    
    | Bug | 类型 | 优先级 | Owner | 描述 |
    |---|---|---|---|---|
    | Boss 血条显示异常 | UI 资源 | P2 | art-ui | Boss 进场时 HP 条闪烁 |
    | 商人对话音缺失 | 音效 | P3 | art-audio | NPC 交互时应有对白音 |
    | 颜料掉落数量不对 | 数值 | P1 | gd-system | 应掉 3-5 颜料，实际掉 1 颜料 |
    ```
- **验收**：所有非阻断性 bug 记录完整，转移方向明确

---

### Phase E：收尾（0.5 day）

#### Task E1：生成记录补齐
- **负责**：主对话
- **产出**：
  - 更新 `art/生成记录.md`（补充 Prefab 调整情况、联调结果）
  - 更新 `art/requirements.md` 头部状态字段为"已完成"
- **依赖**：D1 / D2 / D3 完成
- **验收**：记录完整，便于后续版本回溯

#### Task E2：Commit 提交
- **负责**：主对话
- **产出**：Git commit（包含本 change 所有产物）
- **依赖**：E1
- **工作内容**：
  - `git add openspec/changes/13-ui-screens-complete/`
  - `git add Assets/Resources/Prefab/UI/*.prefab` （11 个 Form）
  - `git add Assets/Scripts/Modules/UIModule/*.cs` （SelfTattooUIForm + 改动）
  - `git commit -m "13-ui-screens-complete: 11 个 Form 视觉补齐 + SelfTattooForm 新建 + 端到端联调"`
- **验收**：Commit 成功，CI 通过

#### Task E3：更新项目知识库索引
- **负责**：主对话
- **产出**：
  - 更新 `项目知识库（AI自行维护）/INDEX.md`（新增 13-ui-screens-complete 条目）
  - 更新 `openspec/changes/13-ui-screens-complete/.openspec.yaml` 状态为 "completed"
- **依赖**：E2
- **验收**：索引更新完毕，该 change 可归档

---

## 并行编排（Agent 协作模式）

### 关键时序点

```
Phase A（1 day）
  ↓
Phase B-Task B1（art-ui，0.5 day）
  ↓ [B1 完成后]
Phase B-Task B2（codex-image-gen 批次 A，0.75 day）
  ↓ [B2 完成 + 用户确认后]
Phase B-Task B3（codex-image-gen 批次 B/C，0.5 day）
  ↓ [B2/B3 完成]
Phase C（并行）
  ├─ C1（client-unity，SelfTattooForm，0.75 day）
  ├─ C2（client-unity，读条 UI，0.5 day）
  ├─ C3（client-unity，10 个 Form 微调，1 day）
  └─ [等待 C1/C2/C3 全部完成]
Phase C-Task C4（client-unity，缺口补齐，按需）
  ↓
Phase D（Fan-Out 并行）
  ├─ D1（client-unity + qa-engineer，完整流程验证，0.75 day）
  └─ D2/D3（根据 D1 结果，迭代修复，≤0.75 day）
  ↓
Phase E（0.5 day）
```

### Agent 分工

| Agent | 任务 | 工作量 |
|---|---|---|
| **art-ui** | B1（效果图提示词） | 0.5 day |
| **codex-image-gen** | B2/B3（效果图生成） | 1.25 day（4 轮试图） |
| **client-unity** | C1/C2/C3/C4/D2 | 2.5+ day（视联调结果） |
| **qa-engineer** | D1/D3（验证 + bug 记录） | 0.75+ day |
| **主对话 (orchestrator)** | A/E + codex-image-gen 调度 | 1.5 day |

### 同步点

1. **B1 完成**：art-ui 提交 `art/prompts.md` → 主对话 review → codex-image-gen 开始 B2
2. **B2 完成**：4 张核心 Form mockups 生成 → 用户确认 → 主对话 trigger codex-image-gen B3 + client-unity 启动 C
3. **B3 完成**：6 张条件 Form mockups 齐全 → client-unity 可开始 C3
4. **C1/C2/C3 完成**：client-unity 通知 qa-engineer 启动 D1（完整流程验证）
5. **D1 完成**：qa-engineer 提交检查清单 → client-unity 根据结果修复 D2 → 循环直到 All Pass
6. **D2/D3 完成**：主对话 E1/E2/E3 收尾

---

## 验收门槛（DoD）

整个 Phase 完成 = 所有任务均标记 ✓：

- ✓ Task A1/A2：三表完整
- ✓ Task B1：提示词完成
- ✓ Task B2/B3：11 张 mockups 生成（可能有部分 3 轮重试上限停下）
- ✓ Task B4：生成记录完整
- ✓ Task C1：SelfTattooForm Prefab + 脚本完成，与 TattooModule 事件对接正确
- ✓ Task C2：读条 UI 子区块可正常显示/隐藏
- ✓ Task C3：10 个 Form 视觉与 mockup ≥ 90% 一致
- ✓ Task C4（按需）：业务逻辑阻断性 bug 修复
- ✓ Task D1/D2：一局游戏完整可玩，无阻断性 bug，验证清单 All Pass
- ✓ Task D3：非阻断性 bug 记录转移完成
- ✓ Task E1/E2/E3：收尾与归档完成

