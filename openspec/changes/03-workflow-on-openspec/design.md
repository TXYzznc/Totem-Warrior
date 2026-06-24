# Design — 03-workflow-on-openspec

> 工作流沉淀到 openspec change 单一目录树的设计决策。

---

## 一、新目录约定

每个 openspec change 是一个**功能的全生命周期容器**。目录子项按 Phase 编排：

| 子项 | Phase | Owner | 必须？ |
|---|---|---|---|
| `proposal.md` | 2 (需求) | producer + lead | 是（openspec 原生） |
| `design.md` | 2/3 (设计) | lead | 是（openspec 原生） |
| `tasks.md` | 3 (执行) | impl agents | 是（openspec 原生） |
| `specs/<能力>/spec.md` | 2 (规范) | lead | 是（openspec 原生） |
| `brainstorm.md` | 1 (策划讨论) | 主对话 + 用户 | 可选（首次提出功能时建议） |
| `CONTRACT.md` | 2 (多模块契约) | lead | 仅多模块时 |
| `art/` | 2 (美术) | art-director + ai-art SKILL | 仅含美术需求时 |
| `tests/` | 4 (测试) | qa-engineer | 是（除非纯文档变更） |

## 二、art/ 子目录约定（ai-art SKILL 新落盘点）

```
art/
├─ requirements.md          # 美术需求分析（含状态头部：美术素材状态 / 处理日期 / 输出目录 / 生成记录）
├─ prompts.md               # 提示词
└─ raw/                     # AI 出图源图（单模块直接平铺）
   ├─ <资源名>.png 或 <序号>-<资源名>.png
   ├─ <资源名>_v01.png       # 多候选
   └─ 生成记录.md             # 每张图的资源名、提示词摘要、输出文件名、生成状态、后续处理建议
```

**多模块**：

```
art/raw/
└─ <子模块名>/
   ├─ <资源名>.png
   └─ 生成记录.md
```

**ai-art SKILL 怎么找当前 change**：

1. 优先：`openspec status` 输出找 active change（同一时刻只有 1 个 active 才合法）
2. 次选：从用户上下文（最近提到的 change-name）推测
3. 最次：询问用户「目标 change 是哪个？」

**禁止**：旧路径 `工作/1.美术/...`、`工作/工作区/0.AI绘图输出文件（未处理）/...`

## 三、tests/ 子目录约定（qa-engineer 新落盘点）

```
tests/
├─ plan.md                  # 测试策略 + 场景 + 是否自动化
├─ results.md               # 测试执行结果
└─ bugs.md                  # 发现的 bug 清单（每条含: 编号 / 描述 / 复现 / 严重度 / 状态 / 修复 commit）
```

**测试代码**（NUnit/UTF）**不**放 openspec，按 Unity 项目结构走 `Assets/Tests/EditMode/...` 或 `Assets/Tests/PlayMode/...`。tests/plan.md 引用代码路径。

## 四、被否定的备选方案

| 备选 | 否定理由 |
|---|---|
| **提案 B：极简化「工作/」只留 1.策划 + 1.美术 + 工作区** | 仍然双路径，agent 流与人工流并存；用户已删除整个「工作/」 |
| **art/raw/ 走 git LFS** | 单人原型阶段过度工程；要团队协作再迁，git filter-branch 可追溯迁移 |
| **保留 `_v1/_v2` 版本号** | 与 openspec「change 中心」心智模型冲突；迭代开新 change 更直观 |
| **切图中间产物保留 `art/cut/`** | 切图是一次性 source → Asset 步骤，中间产物无价值 |
| **brainstorm.md 落到 `项目知识库（AI自行维护）/raw/`** | 策划讨论与功能强耦合，跟功能一起归档最合理 |
| **多模块 CONTRACT.md 内联到 design.md** | 多模块时 design.md 会爆炸；单文件契约更清晰 |

## 五、验收（自动化）

```bash
# 1. 旧路径残留检查
grep -ri "工作/" .claude/ 项目知识库（AI自行维护）/ 2>/dev/null
# 期望：0 行（除引用本次 change 文档的元说明外）

# 2. ai-art SKILL 新路径就位
grep -q "openspec/changes/<change-name>/art/" .claude/skills/ai-art/SKILL.md

# 3. CLAUDE.md §六 简化
grep -E "Phase 1️⃣|Phase 2️⃣|Phase 3️⃣|Phase 4️⃣|Phase 5️⃣" .claude/CLAUDE.md | wc -l
# 期望：≤ 3（保留对原 5 Phase 的历史性引用 ≤ 3 处）
```

## 六、对照表（旧 → 新）

| 原 `工作/` 路径 | 新 `openspec/changes/<name>/` 路径 |
|---|---|
| `1.策划/NN.功能名/策划案.md` | `brainstorm.md`（不强制） |
| `2.需求列表/NN.功能名/需求.md` | `proposal.md`（openspec 原生） |
| `3.正在处理的任务/NN.功能名/proposal.md` | 同 proposal.md |
| `3.正在处理的任务/NN.功能名/design.md` | `design.md`（原生） |
| `3.正在处理的任务/NN.功能名/tasks.md` | `tasks.md`（原生） |
| `3.正在处理的任务/NN.功能名/README.md`（全局契约） | `CONTRACT.md` |
| `1.美术/NN.功能名/美术需求分析.md` | `art/requirements.md` |
| `1.美术/NN.功能名/提示词.md` | `art/prompts.md` |
| `工作区/0.AI绘图输出文件（未处理）/需求名/` | `art/raw/` |
| `工作区/1.切图完成/` | **退役**（切图直接进 Assets/Resources/） |
| `3.测试/NN.功能名/测试方案.md` | `tests/plan.md` |
| `3.测试/NN.功能名/测试结果.md` | `tests/results.md` |
| `3.测试/NN.功能名/bug报告.md` | `tests/bugs.md` |
| `4.已归档任务/NN.功能名_vX/` | `openspec/changes/archive/<name>/`（openspec archive 自动） |
