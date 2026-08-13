# 项目知识库规则

本文件是 `项目知识库（AI自行维护）` 的目录规则。知识库根目录只能保留三个子文件夹：

```text
项目知识库（AI自行维护）/
  raw/
  outputs/
  wiki/
```

根目录不得再放普通 Markdown、JSON、manifest 或临时文件。稳定入口统一放在 `wiki` 内。

## 目录职责

| 目录 | 职责 | 生命周期 |
|---|---|---|
| `raw/` | 外部输入的原始资料暂存区，例如用户给的 GDD、截图、参考资料、导入文档 | 只暂存；被索引和整理后必须移入 `wiki/历史资料/` 或对应 wiki 分类 |
| `outputs/` | AI 生成文档的暂存区，例如阶段总结、分析报告、设计草稿 | 只暂存；被用户接受或完成整理后必须移入 `wiki/` 并更新 `wiki/INDEX.md` |
| `wiki/` | 当前项目可长期读取的知识层，包含稳定规范、历史资料、生成索引、阶段总结 | 长期维护；过时内容必须显式标记或清理 |

## 当前固定入口

| 文件/目录 | 作用 |
|---|---|
| `wiki/INDEX.md` | 人类和 AI 的知识库总索引 |
| `wiki/PROJECT_MAP.md` | 脚本生成的项目地图 |
| `wiki/ACTIVE_CONTEXT.md` | 脚本生成的当前上下文 |
| `wiki/manifests/*.json` | 脚本生成的机器索引 |
| `wiki/历史资料/` | 已降权的历史 GDD、旧草稿、迁移前资料 |
| `wiki/项目总结/` | 阶段总结、交接文档 |
| `wiki/设计草稿/` | 尚未正式落地的设计思路 |

## 与 codebase-memory / openspec 的分工

| 系统 | 负责内容 | 不负责内容 |
|---|---|---|
| `codebase-memory` | 代码结构、调用关系、架构查询 | 设计结论、人工总结、阶段复盘 |
| `openspec` | 大功能和重构的 proposal/design/tasks/spec/验收记录 | 日常知识库、当前项目事实总览 |
| `项目知识库（AI自行维护）` | 人类可读的项目知识、历史资料、阶段总结、资源/配置/诊断索引入口 | 重复扫描代码、绕过 openspec 做大型决策 |

## 过时内容规则

1. 与当前 GF_X runtime 冲突的旧文档必须标记为历史资料。
2. GDD v2.1 和初版 GDD 只作为需求演进证据，不能覆盖用户口头确认、当前 GF_X 实现和 active openspec。
3. 提到旧 `Assets/Scripts`、`GameApp`、`ModuleRunner`、`EventBus`、旧 `InputModule`、旧 `DataTableModule` 的内容，只能当迁移前证据。
4. 如果文档仍有参考价值但不再代表当前事实，保留并加“历史/已降权”标记。
5. 如果文档没有参考价值且会误导当前开发，应删除或合并到更稳定的 wiki 条目。

## 处理流程

```text
外部输入 -> raw/ -> 整理/索引 -> wiki/历史资料 或 wiki/<分类>/ -> 更新 wiki/INDEX.md
AI 输出 -> outputs/ -> 用户接受/整理 -> wiki/<分类>/ -> 更新 wiki/INDEX.md
资源/代码/配置变化 -> build_ai_manifests.py -> wiki/manifests + PROJECT_MAP + ACTIVE_CONTEXT
```

## 生成脚本

运行：

```powershell
python tools\ai_index\build_ai_manifests.py
```

会更新：

- `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md`
- `项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md`
- `项目知识库（AI自行维护）/wiki/manifests/*.json`
- `Assets/Game/Scripts/`

校验：

```powershell
python tools\ai_index\build_ai_manifests.py --check
```

## 默认约束

- 不要在知识库根目录创建文件。
- 不要让 `raw/` 和 `outputs/` 长期堆积已处理文件。
- 不要直接手改 `wiki/manifests/*.json`，应改生成源或生成脚本。
- 更新知识库结构后，必须更新 `wiki/INDEX.md`。
- 大型功能和重构仍先走 `openspec`，知识库只沉淀最终可读结论。
