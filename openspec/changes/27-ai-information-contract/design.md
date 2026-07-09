# Design — AI 信息契约层

## 1. 设计原则

1. Git 仓库是唯一可信来源，MCP / 向量库 / 记忆服务只做索引增强。
2. 不搬动现有目录，避免破坏 Unity 引用和已有工作流。
3. 机器入口优先：AI 先读 `PROJECT_MAP.md` 和 `ACTIVE_CONTEXT.md`，再进入具体模块。
4. 每个模块给一张 `MODULE.md` 卡片，避免 AI 每次从全项目搜索开始。
5. manifest 可重复生成，减少手工维护负担。

## 2. 信息链路

```text
AGENTS.md
→ 项目知识库（AI自行维护）/INDEX.md
→ PROJECT_MAP.md
→ ACTIVE_CONTEXT.md
→ manifests/*.json
→ Assets/Scripts/Modules/<Module>/MODULE.md
→ GDD / openspec / DataTable / Resources / Tests
```

## 3. 文件职责

- `PROJECT_MAP.md`：给 AI 的项目总地图，解释策划、程序、美术、测试、变更记录如何互相追踪。
- `ACTIVE_CONTEXT.md`：当前活跃 change、禁改区、任务前检查清单。
- `manifests/modules.json`：模块结构、脚本数量、关联文档、配置表和资源。
- `manifests/datatables.json`：配置表字段、行数、生成的 C# 类型、注册表一致性。
- `manifests/assets.json`：Resources 下资源分类和计数。
- `manifests/tests.json`：测试入口、playtest 报告、openspec active/archived 状态。
- `MODULE.md`：模块级 AI 读取卡。

## 4. 取舍

| 方案 | 优点 | 缺点 | 决策 |
|---|---|---|---|
| 手写所有索引 | 表达最精确 | 容易过期 | 只保留少量人工入口 |
| 全部交给 MCP / 知识图谱 | 查询强 | 同步和可信度复杂 | 后续增强，不作为 source of truth |
| 生成 manifest + 少量人工说明 | 稳、可版本管理、可校验 | 需要维护生成器 | 采用 |

## 5. 验证

- 运行 `python tools/ai_index/build_ai_manifests.py` 生成索引。
- 运行 `python tools/ai_index/build_ai_manifests.py --check` 验证索引与当前项目一致。
- 检查 `health.json` 中 warnings，作为后续清理项。
