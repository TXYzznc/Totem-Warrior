# AI 友好型项目 — 项目指南（模板版）

> Unity 2022.3.62f3 + GF_X runtime 的 **AI 协作模板**。
>
> 核心配置：**20 人虚拟开发团队** + **113 个本地 Claude skills** + **8 个 MCP 工具（默认启 4，按需启 4）** + **openspec 一站式工作流** + **决策门槛 hook**。

---

## 当前 GF_X 覆盖层（优先级最高）

本项目已经从旧轻量框架迁入 GF_X。后续开发、测试、重构和 Agent 路由必须先遵守本节；本文件后面的旧模板段落如果与本节冲突，以本节为准。

- Unity 版本：`2022.3.62f3`
- 如果任何旧文档、skill 参考或外部链接写着 Unity 6 / 6000.3，它们只能作为通用思路参考；落地代码、包版本、API 用法必须按 `2022.3.62f3` 校验。
- 当前启动场景：`Assets/Game/Scene/Launch.unity`
- 当前业务运行时代码：`Assets/Game/Scripts`
- 旧业务代码证据：`LegacyProjectArchive`，不得重新挂回启动或运行流程
- 旧美术资源可复用：`Assets/Resources/Prefab`、`Assets/Resources/Sprite` 等只作为资源来源，加载和生命周期必须走 GF_X runtime 服务
- AI 可编辑业务配置源：`GameData/AIData/DataTables/Business/*.json`
- 策划可读业务配置表：`GameData/DataTables/Business/*.xlsx`
- 运行配置产物：`GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`（由 Business AI DataTable 生成）
- 运行资源索引：`GameData/AIData/GameplayCatalogs/totem_runtime_assets.json`
- 旧业务 DataTable 证据：`LegacyProjectArchive/Assets/Resources/DataTable`

禁止在新工作中恢复或依赖旧运行宿主：`GameApp`、`ModuleRunner`、`EventBus`、`UIModule`、`DataTableModule`、`SaveModule`。不要新建 `Assets/Resources/DataTable`，不要为新玩法运行旧 `DataTableGenerator`。当前业务配置工作流为：AI 修改 `GameData/AIData/DataTables/Business/*.json` → 逆向生成 `GameData/DataTables/Business/*.xlsx` → 生成/检查 `GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json` → 跑 GF_X 诊断。

Unity 诊断优先使用：

```text
python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8092
```

人工在 Unity 菜单中复跑时可用 `Game Framework/GameTools/Diagnostics/Run All`；AI 自动验证不要再通过通用 `editor_execute_menu` 路由这个菜单。

## 一、AI 行为准则（每次任务必须严格执行）

> 以臆猜接口为耻，以认真查询为荣。
> 以模糊执行为耻，以寻求确认为荣。
> 以臆想业务为耻，以人类确认为荣。
> 以创造接口为耻，以复用现有为荣。
> 以跳过验证为耻，以主动测试为荣。
> 以破坏架构为耻，以遵循规范为荣。
> 以假装理解为耻，以诚实无知为荣。
> 以盲目修改为耻，以谨慎重构为荣。

- 始终用**中文**回答。
- 回复尽量简洁，不要加无关的客套话。
- 优先用简单方案，不要过度工程。
- 涉及项目代码和业务开发时，必须先遵循本 CLAUDE.md 与 [conventions.md](./conventions.md)。
- 查找具体系统/问题文档时，先查阅[项目知识库索引 INDEX.md](../项目知识库（AI自行维护）/INDEX.md)。
- 所有按键输入必须走 `TotemInputService` / `ITotemInputProvider`。

---

## 二、虚拟开发团队 — 路由规则（20 人）

主对话作为 **orchestrator**，**不亲自做专家活**。轻量任务（读文件/解释代码）可自己处理。

| 任务类型 | Agent | Tier |
|---|---|---|
| 项目计划、PRD、排期、风险、竞品 | [`producer`](./agents/producer.md) | lead |
| 核心玩法 vision、GDD、MDA、留存哲学 | [`gd-lead`](./agents/gd-lead.md) | lead (opus) |
| 具体公式、数值表、loot 表、状态机规格 | [`gd-system`](./agents/gd-system.md) | system |
| 关卡布局、节奏、encounter、puzzle、引导 | [`level-designer`](./agents/level-designer.md) | system |
| 美术风格统筹、art bible、风格审稿 | [`art-director`](./agents/art-director.md) | lead (opus) |
| HUD / 菜单 / icon 设计 | [`art-ui`](./agents/art-ui.md) | impl |
| 字体选型、排版、CJK | [`art-font`](./agents/art-font.md) | impl |
| 特效设计、粒子配方（美术侧） | [`art-vfx`](./agents/art-vfx.md) | impl |
| 立绘、sprite、像素美术 | [`art-2d`](./agents/art-2d.md) | impl |
| 3D 模型、UV、贴图、Blender | [`art-3d`](./agents/art-3d.md) | impl |
| 动画、骨骼、Mecanim、Timeline | [`art-anim`](./agents/art-anim.md) | impl |
| 客户端架构、设计模式、性能预算 | [`client-lead`](./agents/client-lead.md) | lead (opus) |
| Unity C# 实现、UI 接入、存档、输入、DataTable | [`client-unity`](./agents/client-unity.md) | impl |
| Shader、URP/HDRP、后处理、TA 工具 | [`client-ta`](./agents/client-ta.md) | impl |
| 服务端架构、协议、匹配、反作弊 | [`net-lead`](./agents/net-lead.md) | lead (opus) |
| API、JWT、Redis、消息队列实现 | [`net-backend`](./agents/net-backend.md) | impl |
| DB schema、索引、迁移、查询优化 | [`net-db`](./agents/net-db.md) | system |
| 测试策略、UTF、bug、crash、playtest | [`qa-engineer`](./agents/qa-engineer.md) | impl |
| CI/CD、Unity 构建、发版、签名 | [`devops-engineer`](./agents/devops-engineer.md) | impl |
| Editor 扩展、内部工具、新建 skill | [`tools-engineer`](./agents/tools-engineer.md) | impl |

**默认行为**：匹配以上任一类，**先 delegate 给对应 agent**。

> 历史上的 4 个项目原生 agent（code-reviewer / bug-tracer / datatable-helper / ui-scaffold）已在重构 v1.0 中砍掉：code-reviewer 走通用 SKILL；bug-tracer 并入 `qa-engineer` + `client-lead`；datatable-helper 写入 `client-unity` 的 system prompt；ui-scaffold 拆为 `art-ui`（设计）+ `client-unity`（接入）。

---

## 三、Agent 兜底机制（escalate_to: main）

每个 agent 在 system prompt 中显式声明，出现以下情形之一时 **立即停止并交回主对话**：

1. **白名单外 SKILL**：需要调用 `frontmatter.skills` 之外的 SKILL
2. **跨职能决策**：任务涉及多个 agent 领域
3. **MCP / 外部权限不足**：缺凭据、缺工具、缺文件权限
4. **职责边界外**：任务实质不属于本 agent 职位
5. **多轮收敛失败**：3 轮内无法给出可行方案或反复回退
6. **意图模糊**：用户原始 prompt 含糊
7. **决策门槛触发**：检测到大型决策关键词（见 §五）时，应由主对话发起 grill-me + openspec

主对话因 tier 最高、SKILL 全开放，是最终兜底执行者。

详细的 agent ↔ SKILL 映射 + 兜底规则总览见 [SKILL_MATRIX.md](./SKILL_MATRIX.md)。

---

## 四、多 Agent 协作 5 模式

详见 [AGENTS.md](./AGENTS.md)。

> **关键**：若一个需求拆为多个**互相引用**的子模块（共享事件 / 跨模块 `GetModule<T>` / 共享数据结构），**必须**先走「骨架先行」——单 Agent 生成公共骨架（事件总表 + 模块空壳 + 基础设施 + 入口注册），裁定接口分歧后再并行填充。

---

## 五、决策门槛（两阶段 FSM）

> **设计目标**：前期一次性人在场把需求挖透，后续全自动跑 —— 配合 Auto Mode / Loop 长时间无人值守执行。Auto Mode 的「少打断」前提是「目标已对齐」，grill-me 是**对齐目标**的工具，不算违反 Auto Mode。

### 触发关键词（settings.json 中实装）

```
设计 / 架构 / 重构 / 大改 / 重写 / GDD / PRD / 系统 / 范式 / 方案 / 思路
```

### 阶段 A：需求挖掘（人在场，必须阻塞）

触发关键词后**必须**先调用 `grill-me`（或 `grill-with-docs`）多轮反问，**直到以下 5 条全部明确**才能退出：

- [ ] 核心目标一句话能说清楚（做什么、为什么）
- [ ] 关键决策点有 A/B 比较并明确选了哪个、为什么
- [ ] 不做什么（边界）已明确
- [ ] 验收标准已明确（怎么算完成）
- [ ] 关键约束已明确（性能/兼容/时间）

**任何一条没挖透都不能退出 grill** —— 这是整个流程**唯一**的人在场卡点。

### 阶段 B：自动执行（不再打断用户）

阶段 A 退出后，**主对话先做「任务规模评估」**，再选择对应路径执行，**不再请求用户审批**：

#### B0. 任务规模评估（必做，主对话自决）

按下表判断是否走 openspec。**任一「走 openspec」信号命中即走 openspec；否则走轻量路径**。判断结果在回复里用一行说明（例："任务规模评估：轻量任务，跳过 openspec"），不必征求用户同意。

| 信号 | 走 openspec | 轻量路径（跳过 openspec） |
|---|---|---|
| 涉及模块数 | 跨 2+ 模块 / 新增模块 | 单模块内部修改 |
| 公共契约 | 新事件 / 改公共 API / 改 DataTable schema | 不动公共接口 |
| 美术素材 | 需要 ai-art 出图（要落 `art/` 目录） | 不涉及美术生成 |
| 实施粒度 | 子任务 ≥ 3 / 需要分阶段 | 1-2 步内完成 |
| 决策密度 | 阶段 A 留下多个需 design.md 沉淀的 trade-off | 阶段 A 共识已经足够覆盖 |
| 测试规模 | 需要 qa-engineer 写测试计划 / 多场景 E2E | 单元测试或无测试足够 |
| 影响范围 | 框架核心 / 多人协作契约 | 局部 bug fix / 局部参数调整 / 局部重构 |

> **判断原则**：宁可漏走也不要硬上 openspec。openspec 的成本是 proposal/design/tasks/specs 至少 4 个文件 + 归档同步索引；轻量任务硬走 openspec 会浪费 token 与时间。

#### B1. openspec 路径（命中任一信号）

1. `openspec new change <NN-功能名>` → 写 proposal/design/tasks/specs
2. 按 tasks.md 顺序实现（client-unity / art-director 等 agent 落地）
3. 中途遇到模糊点：**优先按阶段 A 的共识自决**，写日志/spec 备注
4. 完成后 `openspec archive-change <NN-name>` + 同步更新 [项目知识库（AI自行维护）/INDEX.md](../项目知识库（AI自行维护）/INDEX.md)

#### B2. 轻量路径（跳过 openspec）

1. 直接按阶段 A 共识落地代码
2. 中途遇到模糊点：**优先按阶段 A 的共识自决**
3. 完成后简短回复变更摘要即可，**不必创建 openspec change，也不必更新 INDEX.md**
4. 如果实现过程中发现规模超预期（触发任一「走 openspec」信号）→ 当场升级到 B1 路径，补建 openspec change

### 例外打断条件（阶段 B 仅以下情况可中断用户）

只有遇到**真正不可自决**的问题才能打断：

- 与阶段 A 共识**直接冲突**（grill 说了 A，实现发现必须做 B）
- 引入**不可逆变更**（删除/重命名公共 API、迁移数据、改动他人正在用的契约）
- 触及**项目宪法级**文件（`.claude/` / `openspec/` / `Assets/Game/ScriptsBuiltin/` GF_X 框架核心）

其他所有模糊点（命名、内部实现选型、测试粒度、日志格式等）一律自决。

**阶段 A 未挖透直接给方案视为违规**。lead/system agent 在此规则下应立即停止并交回主对话。

---

## 六、工作流系统（一站式 openspec change）

> **适用范围**：仅适用于 §五「B0 任务规模评估」命中「走 openspec」信号的中大型任务。轻量任务走 B2 路径，**不创建 openspec change**。

中大型功能 = 一个 `openspec/changes/<NN-name>/` 目录，承载全生命周期 artifact。

```
策划讨论（brainstorm.md） → openspec 全程（proposal/design/tasks/specs+art+tests） → openspec archive-change 自动归档
```

### 目录约定

```
openspec/changes/<NN-name>/
├─ .openspec.yaml
├─ proposal.md / design.md / tasks.md / specs/<能力>/spec.md   ← openspec 原生
├─ brainstorm.md         ← Phase 1: 策划讨论沉淀（首次提出功能时建议）
├─ CONTRACT.md           ← 多模块全局契约（仅多模块时创建）
├─ art/                  ← 美术（ai-art SKILL 落盘点）
│  ├─ requirements.md    ←   美术需求分析
│  ├─ prompts.md         ←   提示词
│  └─ raw/               ←   AI 出图源图 + 生成记录.md
└─ tests/                ← 测试（qa-engineer 落盘点）
   ├─ plan.md
   ├─ results.md
   └─ bugs.md
```

| 节点 | 主导 agent | 自动行为 |
|---|---|---|
| **1. 策划讨论** | 主对话 + 用户 | 多轮澄清需求，沉淀 `brainstorm.md`（非必须，但建议） |
| **2. openspec 拆解** | producer / gd-lead / lead 群 | 调 `grill-me` 决策门槛 → `openspec new change <NN-name>` 落地 proposal/design/tasks/specs |
| **3. 实现** | client-lead 给架构、client-unity / net-backend 等落地、art-director 调 ai-art 出图 | 按 tasks.md 推进，资源直接写到对应子目录 |
| **4. 测试** | qa-engineer | 测试方案/结果/bug 写入 `tests/` 子目录；测试代码进 `Assets/Tests/` |
| **5. 归档** | 主对话 / 用户 | `openspec archive-change <NN-name>` 自动移到 `openspec/changes/archive/`，子目录 `art/`、`tests/` 等随行 |

### 美术素材生成意图

详见 [.claude/skills/ai-art/SKILL.md](./skills/ai-art/SKILL.md) 的「美术素材实现流程」。核心规则：

1. 主对话识别意图后，**先定位当前 active openspec change**（`openspec status` / 用户上下文 / 询问用户）
2. **⚠️ UI 类型前置**：若 change 含 UI 类型素材，`art/prefab-layout.md` 必须先由 art-ui 用 `unity-rect-transform` SKILL 产出并经用户确认；缺 layout → 阻塞并交回主对话按 §六 UI 子流程 v3 阶段 1 重新走，未确认不得进出图。ai-art 只承担阶段 2（写 prompts.md）+ 阶段 3（生 mockups）
3. 读取 `openspec/changes/<change-name>/art/prompts.md`（提示词已由 art-ui 从 prefab-layout.md 反哺画布长宽与组件占比写好）
4. 调绘图模型逐项生图，输出到 `openspec/changes/<change-name>/art/raw/` 或 `art/mockups/`
5. 同目录写 `生成记录.md`；更新 `art/prompts.md` 头部状态字段为「已处理」
6. 无可用绘图模型时明确阻塞，不能假装已生成

### 角色帧动画生产约束（强制）

需要美术资源时，主对话可按当前已确定的项目美术风格直接 fan-out 给 art-2d / art-anim / codex-art-gen / frame-ronin 等子 agent 或工具；资源生成过程中不用反复等待用户确认，只有工具不可用、缺少必要参考图或触及不可逆工程变更时才交回主对话。

角色帧动画必须按**单角色连续批处理**执行，禁止把多个角色混在同一画布、同一输出目录或同一切图/命名批次里：

1. 每个角色单独建批次目录：`openspec/changes/<change>/art/raw/characters/<character_id>/`，并传入该角色参考图、动作列表、风格约束和目标尺寸。
2. 同一角色的所有目标动作连续生成；按精细度选择「每张画布一个动画」或「每张画布最多两个动画」，但画布内仍只能包含同一个角色。
3. 精度基线固定为：每个动作 4 个方向，每个方向 4 帧；方向枚举使用 `down` / `up` / `left` / `right`。
4. 生成后在同一批次内统一做抠图、切图、去背景、边界检查、重命名和导入设置检查，避免和其它角色动画资源污染。
5. 单帧命名优先使用 `{character_id}_{action}_{direction}_{frame:00}.png`，例如 `hero_idle_down_00.png`；整张源画布和切分清单必须保留在同一角色目录下。
6. 入库后同步更新资源索引 / runtime asset catalog / 相关 usage 说明；角色动画资源不得绕过 `TotemAssetService` 或直接硬编码路径。

### UI 制作子流程（强制时序，v3 — 2026-07-01 结构先行重构）

> 适用于任何新建 / 重做的 UI 界面（HUD / 菜单 / 弹窗 / 表单 / 设置 / 商店 / 任务面板等）。**主对话作为 orchestrator 按下列 6 阶段顺序编排**，禁止跳阶段。**简单弹窗也走完整 6 阶段，无豁免**（历史归档 UI 不回溯）。

```
1.结构设计           2.效果图设计         3.效果图生成    4.素材拆分            5.拼装实现         6.联调微调
(art-ui +           (art-ui 从 layout   (codex-        (ui-asset-splitting,  (client-unity      (client-unity
 unity-rect-        提取画布/占比→       image-gen)     多张 mockup           单线：unity-       +用户对比
 transform SKILL,   写 prompts.md;                     Fan-Out 并行拆分,     skills MCP 建       效果图迭代)
 产出 prefab-       状态每态独立)                      每态独立生素材)       Prefab + 贴素材
 layout.md)                                                                 + 写 UIForm)
```

| 阶段 | 主导 | 产出物 | 通过条件 |
|---|---|---|---|
| **1. 结构设计**🔄 | art-ui（用 `unity-rect-transform` SKILL） | `openspec/changes/<change>/art/prefab-layout.md`（含全局约定 + 每页节点树 + RectTransform 数据 + 状态清单 + 跨页复用组件） | layout 完整、用户确认所有页面结构；缺失即阻塞 |
| **2. 效果图设计** | art-ui | `art/prompts.md`：每页一条效果图提示词，**开头必带「结构约束」段落**（画布尺寸 + 各节点占比，直接从 layout 提取） | 用户确认提示词；含结构约束段 |
| **3. 效果图生成** | 主对话 → `codex-image-gen` | `art/mockups/<PageName>.png` + 同目录 `生成记录.md` | 用户确认效果图（**3 轮重试上限**：每轮调整提示词；3 轮仍不满意 → 阻塞通知用户人工介入） |
| **4. 素材拆分** | 主对话 fan-out 子 Agent → `ui-asset-splitting` | `art/raw/<PageName>/`（背景 1 张 + 组件/状态变体若干张，layout 每个 states 独立成图；一张画布装不下拆多张 batch）→ 搬进 `Assets/Resources/Sprite/UI/<PageName>/` | 每页拆分清单与 layout 节点数一致；`UISpriteImportProcessor` 自动设好导入参数（抽查 1 张 `.meta` 确认 `textureType: 8`） |
| **5. 拼装实现**🔄 | client-unity（单线，用 `unity-rect-transform` SKILL 读 layout） | Prefab 文件（unity-skills MCP 自动建，按 layout 节点树建层级 + 贴入阶段 4 素材 + 设 RectTransform 数据）+ UIForm 脚本 | Prefab 层级与 `prefab-layout.md` 一致；UIForm 编译通过 |
| **6. 联调微调** | client-unity + 用户 | 运行时截图 vs 效果图对比 + 偏差修复 | 运行时与效果图视觉一致（间距 / 字号 / 配色） |

#### 强制约束

1. **不许跳阶段**：`prefab-layout.md` 未确认 → 禁止进入阶段 2；效果图未生成/未确认 → 禁止进入阶段 4；素材未拆分入库 → 禁止进入阶段 5
2. **layout 是唯一 source of truth**：阶段 2 的提示词、阶段 4 的拆分清单、阶段 5 的 Prefab 层级，**全部**从 `prefab-layout.md` 读取，禁止任一阶段自行推断结构
3. **效果图位置固定**：`openspec/changes/<change-name>/art/mockups/<PageName>.png`，**与 `raw/`（拆分素材）严格分目录**
4. **阶段 4 多页并行**：N 张已确认 mockup 互不依赖时，主对话直接 fan-out N 个 Agent 各自跑 `ui-asset-splitting`，不串行
5. **状态每态独立生成**：layout 中每个含 `states: [normal, pressed, disabled, ...]` 的节点，每态在阶段 3/4 各出一张，禁止一张图里画多态
6. **阶段 4 素材必须透明背景 + 禁止裁 mockup**：组件素材只能由 Codex 绿幕重生成或程序化 PIL 生成（四角 alpha 必须 = 0），**严禁从已确认的 mockup 上直接裁矩形**（会带面板底色变成不透明方块）；普通文字（标签/数值/按钮文案/键名）走 Prefab 里 TMP_Text 独立节点，只有特殊艺术字才作为图片素材。"MVP 简化"只能砍状态变体数量，绝不能砍"透明重生成"这个生产方式。详见 [skills/ui-asset-splitting/SKILL.md](./skills/ui-asset-splitting/SKILL.md) §一铁律 + §3.4 alpha 硬检查（2026-07-01 SettingsForm v2 踩坑固化）
7. **画布不够就加新画布**：一张 1920×1080 mockup 装不下时拆 `<Page>_part1.png` / `<Page>_part2.png`；1024×1024 绿幕组件画布装不下时拆 `_merged/batch_1.png` / `_merged/batch_2.png`
8. **导入设置不手动改**：`Assets/Resources/Sprite/UI/` 下贴图由 `Assets/Editor/UISpriteImportProcessor.cs` 自动设置 Texture Type 等参数，禁止在 Inspector 里手动调（改了也会在下次 reimport 被覆盖，应改脚本而非改单个贴图）
9. **Prefab 优先 MCP 自动建**：阶段 5 由 client-unity 调用 `unity-skills` MCP 按 layout 建层级 + 贴入阶段 4 素材；**MCP 不可用** → 回退到通知用户在 Unity Editor 手动搭。**调用 unity-skills 时若参数含 CJK / Emoji（节点名、按钮文本、说明文字等），必须用 `--stdin-json` 模式**，详见 [skills/unity-skills/SKILL.md](./skills/unity-skills/SKILL.md) 「中文 / CJK 参数调用约定（强制）」
10. **效果图重试上限 3 轮**：codex-image-gen 调用失败或用户不满意 → 调整提示词/加参考图重试，**累计 3 轮仍未通过即停下来交回用户决定**（手动找参考 / 跳过本页 / 重新设计），禁止无限重试
11. **联调以效果图为准绳**：阶段 6 必须把运行时截图与 mockups 并排对比，列偏差清单后再迭代；client-unity 不许凭感觉调

#### v2 → v3 变更要点（2026-07-01）

- **删「三表」**（页面清单 / 复用组件清单 / 组件状态表）→ 换成 **单文件 `prefab-layout.md`**（含 RectTransform 数据），一份文档同时喂养阶段 2/4/5
- **阶段 1 主导** producer/gd-system → **art-ui**（结构设计属于 UI 美术职责，用 `unity-rect-transform` SKILL 产出）
- **阶段 5 取消标注稿**（原 art-ui ∥ client-unity Fan-Out）→ **单线 client-unity**（layout 已含 RectTransform，标注稿变成冗余中间层）
- **新增 `unity-rect-transform` SKILL**（art-ui + client-unity 共享）：UGUI anchor / pivot / sizeDelta / anchoredPosition / preserveAspect / Canvas Scaler 完整词典 + `prefab-layout.md` 模板
- **无豁免**：简单弹窗也走完整 6 阶段；历史归档 UI 不回溯改造

#### Agent 编排速查

```
主对话
 ├─ 阶段 1：delegate art-ui（用 unity-rect-transform SKILL 产出 prefab-layout.md）
 │          用户确认
 ├─ 阶段 2：delegate art-ui（读 prefab-layout.md → 反哺写 prompts.md 结构约束段）
 │          用户确认
 ├─ 阶段 3：直接调 codex-image-gen SKILL（生 mockups + 重试循环）
 │          用户确认
 ├─ 阶段 4：fan-out（N 张 mockup → N 个 Agent，各自调 ui-asset-splitting，读 layout 每态独立）
 │          await WhenAll
 ├─ 阶段 5：delegate client-unity（unity-skills MCP 按 layout 建 prefab + 贴素材 + 写脚本）
 └─ 阶段 6：delegate client-unity 比对效果图，迭代到一致
```

---

## 七、技术栈

- **框架核心**：GF_X runtime / Procedure / GameTools / Diagnostics，当前入口为 `Assets/Game/Scene/Launch.unity`
- **业务代码**：新代码只放 `Assets/Game/Scripts`，旧 `Assets/Scripts` 代码已归档到 `LegacyProjectArchive`
- **UniTask / DOTween**：以 GF_X 迁入版本为准
- **AI 可编辑业务配置**：`GameData/AIData/DataTables/Business/*.json`
- **策划可读业务配置**：`GameData/DataTables/Business/*.xlsx`
- **运行配置产物**：`GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json`
- **运行资源索引**：`GameData/AIData/GameplayCatalogs/totem_runtime_assets.json`
- **旧 DataTable**：仅作为 `LegacyProjectArchive/Assets/Resources/DataTable` 下的需求证据，不作为新运行时配置源
- **开发热重载**：Unity Domain Reload + Enter Play Mode Options

---

## 八、项目环境与工具栈

- **平台**：Unity 2022.3.62f3
- **OS**：Windows 10，shell 用 **bash**（不是 PowerShell）—— 路径用 `/`
- **Python 环境**：`.venv/`（frame-ronin MCP），见 [setup.md](../setup.md) 与 [requirements.txt](../requirements.txt)
- **凭据**：`.env`（从 [`.env.example`](../.env.example) 复制并填值，已加 .gitignore）

### MCP 服务清单（[.mcp.json](../.mcp.json) + [.codex/config.toml](../.codex/config.toml)）

| MCP | 默认状态 | 说明 |
|---|---|---|
| codebase-memory | 🟢 常驻 | 代码结构索引（优先于 Read+Grep） |
| codex-art-gen | 🟢 常驻 | 美术出图主入口（codex exec 调度） |
| playwright | 🟡 高频 | Web E2E 测试 |
| blender | ⚪ 按需 | 3D 资产生成与脚本（art-3d 触发时手动启） |
| godot | ⚪ 按需 | 跨引擎参考（极少用） |
| frame-ronin | ⚪ 按需 | 帧/精灵/像素美术（特定批次手动启） |
| atlassian | ⚪ 按需 | Jira / Confluence（外部协作时手动启） |

> 启用清单见 [.claude/settings.local.json](./settings.local.json) 的 `enabledMcpjsonServers`。`skill4agent` 已移除；SKILL 直接从 `.claude/skills/` 读取。**按需 MCP** 不在默认启用列表里，使用时手动加入 → 重启会话生效。理由：每个 MCP 启动都注册 schema 占 token，低频 MCP 常驻浪费上下文。

### codebase-memory MCP 使用准则

**优先**调用 `codebase-memory` 查询 `Assets/Game/Scripts/` 代码结构（函数定义、调用链、类型层级、跨文件引用）；**不要**用 Read + Grep 逐文件扫。

适用：
- "X 方法在哪里定义/被谁调用"
- "Foo 类的所有 public 接口"
- "TotemDataService 被哪些 runtime service 使用"
- 重构前的 impact 分析

不适用：读单个文件具体实现（用 Read）、改代码（用 Edit）。

### 工程工具（[tools/](../tools/)）

- `codebase-memory-mcp/` — codebase-memory MCP 二进制
- `ImageCompression_Tool/` — 通用图片压缩 CLI（PNG/JPEG/WEBP/TGA），仅作兜底用（外部/历史素材超规时压一遍）；正常生产由 [美术资源规范.md](./美术资源规范.md) 前置约束，详见 [image-compression SKILL](./skills/image-compression/SKILL.md)
- `ImageCut_Tool/` / `image-extender-main/` / `rembg-main/` — 图像处理

> tools/ 较大，已加 `.gitignore`。新机器按 [setup.md](../setup.md) 重建。

---

## 九、框架核心概念

### GF_X 运行入口

```
Assets/Game/Scene/Launch.unity
Assets/Game/Scripts
GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json
GameData/AIData/GameplayCatalogs/totem_runtime_assets.json
```

### 服务边界

- Procedure 负责流程切换，不复用旧 `GameApp` 启动宿主。
- Runtime service 负责玩法生命周期、数据读取、资源索引和诊断暴露。
- 输入必须经过 `TotemInputService` 的 provider 边界，业务脚本不得散落直接输入读取。
- 资源加载必须经过 `TotemAssetService` / runtime asset catalog，旧 `Resources` 路径只作为资源来源。
- gameplay catalog 是当前运行配置源，不在 service 内部保存第二套隐藏静态配置。

### 初始化流程

1. Unity 打开 `Assets/Game/Scene/Launch.unity`
2. GF_X 启动流程进入项目 Procedure / Workspace
3. 新业务 runtime service 加载 gameplay catalog 与 runtime asset catalog
4. 主菜单 → 角色选择 → 启动选择 → 战斗 HUD → 地图/玩家/输入/相机/战斗逐步接入
5. 通过 `totem_diagnostics_run_all` 生成可追踪诊断报告

---

## 十、SKILL 系统

- **总数**：113 个本地项目 skill，分组索引见 [skills/SKILLS_INDEX.md](./skills/SKILLS_INDEX.md)
- **Agent ↔ SKILL 白名单**：见 [SKILL_MATRIX.md](./SKILL_MATRIX.md)
- **大多数 skill 不进上下文**：仅在对应 agent 触发时按需读取 `SKILL.md` + `references/*.md`
- **找不到合适 skill 时**：用 `find-skills` 语义检索；仍找不到则 escalate_to: main 由主对话决定
- **写作规范**：新建 / 修改 SKILL 时遵守 [SKILL_MATRIX.md §六](./SKILL_MATRIX.md)（60-250 字符，带触发词，重叠时加 ❌ 不适用）

### 月度防腐机制

每月 1 号跑两个脚本看 SKILL/Agent/MCP 状态：

```bash
python tools/audit_skills.py        # description 长度审计（看是否回涨）
python tools/audit_skill_usage.py   # 使用频次 + 0 召回清单（看是否该淘汰）
```

- `tools/log_tool_usage.py`：PreToolUse hook 自动调用，记录每次 Skill / Agent / mcp__\* 调用到 `.claude/skills/_usage.log`（已加 .gitignore）
- 0 次召回的 SKILL → 候选淘汰；高频但有误召回的 → 改 description 加 ❌ 不适用
- 报告里出现极短 / 极长 / 重叠未划界 → 按 SKILL_MATRIX §六.3 checklist 处理 + 覆写 `_audit.json` 作为新基线

---

## 十一、命名规范

| 类型 | 规范 | 示例 |
|---|---|---|
| 运行时服务 | `Totem[Name]Service` | `TotemCombatService`, `TotemUIService` |
| 事件/记录 | `[描述]Event` / `[描述]Record` | `TotemDamageRecord`, `TotemRunResultSnapshot` |
| 事件处理方法 | `On[事件名]` | `OnCombatEnd`, `OnHPChanged` |
| UI 表单 | `[Name]UI` 或 `[Name]UIForm` | `GameUIForm` |
| 管理器（非模块） | `[Name]Manager` | `PlayerInputManager` |

---

## 十二、关键约束与陷阱

### 约束

- 新运行时代码只写入 `Assets/Game/Scripts`
- 不得在启动/运行流程中恢复旧 `GameApp`、`ModuleRunner`、`EventBus`、`UIModule`、`DataTableModule`
- 不要新建 `Assets/Resources/DataTable`，旧表只保留在 `LegacyProjectArchive/Assets/Resources/DataTable`
- Business AI DataTables、gameplay catalog 与 runtime asset catalog 是当前运行配置入口；不要回退到旧 `Assets/Resources/DataTable`
- gameplay catalog 不允许在 service 内复制成隐藏静态缓存；需要静态辅助查询时从 `TotemDataService.LoadGameplayCatalogOrDefault` 读取
- 资源缺失和 fallback 必须计数并进入诊断报告
- 异步方法名必须以 `Async` 结尾，返回 `UniTask` 或 `UniTask<T>`
- 不要硬编码玩法数值，所有第一轮复现目标读 gameplay catalog
- 不要硬编码资源路径，所有可复用旧资源先写入 `totem_runtime_assets.json`
- 不在 Update 里做 GC alloc
- ScriptableObject 是配置不是数据库
- 任何引入新依赖前先问"标准库或现有依赖能做吗"
- 每个功能接入时同步设置测试点：输入、状态变化、资源加载、异常/fallback、关键 UI 流程
- 完成修改后优先跑 GF_X 诊断并检查 `GameData/Diagnostics/Reports/`

### 日志格式

```csharp
GFLogger.Error("TotemRuntime", $"Action=LoadCatalog Path={path} Exception={ex.GetType().Name} Msg=\"{ex.Message}\"");
GFLogger.Warn("TotemAssetService", $"Action=Fallback Key={key} MissingCount={missingCount}");
GFLogger.Info("TotemDiagnostics", $"Scenario={scenarioName} Success={success} Warnings={warningCount}");
```

### 常见陷阱

- `async void` 方法无法被 `await`——一律改为返回 `UniTask`
- UI 关闭时 DOTween 动画可能还在播放，需 `DOTween.Kill(target)` 或 `DOComplete`
- fallback 不能静默发生，必须能在诊断报告里看到原因和计数
- 旧资源可以复用，但旧生命周期、旧 UI 表单宿主、旧模块系统不能复用
- 修改配置时不要只改运行时对象，必须同步更新 `GameData/AIData/GameplayCatalogs/` 下的源 JSON
- Domain Reload 会导致静态字段归零，不要把运行时状态藏在静态单例里

---

## 十三、设计文档与索引

| 文档 | 内容 |
|---|---|
| [SKILL_MATRIX.md](./SKILL_MATRIX.md) | agent × skill 白名单 + 兜底规则 |
| [AGENTS.md](./AGENTS.md) | 多 Agent 协作 5 模式 |
| [conventions.md](./conventions.md) | 编码规范 |
| [资源配置规范.md](./资源配置规范.md) | ResourceModule + ResourceConfig |
| [美术资源规范.md](./美术资源规范.md) | 2D 美术生产侧前置约束：各视觉类别的 Max 尺寸 / 格式 / 文件大小预算（ai-art / codex-image-gen 写提示词时必读，生产即合规） |
| [skills/SKILLS_INDEX.md](./skills/SKILLS_INDEX.md) | 113 个本地项目 skill 分组索引 |
| [GAMEPLAY_RUNTIME_SLICE.md](../GAMEPLAY_RUNTIME_SLICE.md) | 当前 GF_X 业务运行时切片与诊断证据 |
| [openspec/changes/gf-x-business-runtime-refactor/](../openspec/changes/gf-x-business-runtime-refactor/) | 当前迁移与业务重写规格 |
| [01-框架核心设计概述.md](../AI友好型项目探讨/01-框架核心设计概述.md) | 旧框架历史资料，仅作对照 |
| [02-AI友好型日志规范.md](../AI友好型项目探讨/02-AI友好型日志规范.md) | 旧日志规范历史资料，仅作对照 |

---

## 十四、不要

- 不要绕过 agent 团队自己实现专家任务
- 不要把 skill 移到子目录 —— Claude Code 不递归扫描
- 不要在没有 `grill-me` / `grill-with-docs` 的情况下做大型设计决策（hook 会注入提醒）
- 不要直接改 `.codex/agents/` —— source of truth 是 `.claude/agents/`，跑 `tools/sync-agents.py` 同步；SKILL source of truth 是 `.claude/skills/`
- 不要把业务示例代码混入框架核心 —— 模板需要长期保持纯净

---

## 十五、压缩时保留

- 已修改的文件列表
- 当前 Phase 编号和完成状态
- 关键架构决策（如为什么选某个方案）
- 当前任务涉及的 Agent 与协作模式
