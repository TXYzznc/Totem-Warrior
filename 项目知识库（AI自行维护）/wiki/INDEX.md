# 项目知识库索引

> 当前项目以用户最新口头确认和 GF_X runtime 实现为准。旧 GDD、旧模块文档、历史 openspec 只作为需求演进和行为证据，不能覆盖当前实现。

## 1. 目录入口

| 入口 | 状态 | 用途 |
|---|---|---|
| [README.md](README.md) | active | 知识库目录规则 |
| [PROJECT_MAP.md](PROJECT_MAP.md) | generated | 项目结构、模块、配置、资源、测试的总览 |
| [ACTIVE_CONTEXT.md](ACTIVE_CONTEXT.md) | generated | 当前 active change、禁改区、任务前检查 |
| [manifests/](manifests/) | generated | 机器可读索引：配置、资源、功能切片、诊断反查 |
| [项目总结/PROJECT_STATUS_AND_TODO.md](项目总结/PROJECT_STATUS_AND_TODO.md) | active | 当前项目状态、结构、待做清单 |

## 2. 当前稳定知识

| 文档 | 状态 | 说明 |
|---|---|---|
| [AI_DIAGNOSTICS_GUIDE.md](AI_DIAGNOSTICS_GUIDE.md) | active | GF_X 诊断、测试点、日志规范 |
| [UI结构先行规范.md](UI结构先行规范.md) | active | UI 制作 v3 结构先行流程 |
| [SKILL路由统一.md](SKILL路由统一.md) | active | agent/skill 路由规则历史与当前约束 |
| [工作流迁移.md](工作流迁移.md) | active | openspec 工作流迁移说明 |
| [Codex批量出图协议.md](Codex批量出图协议.md) | active | 批量出图和资源生成约束 |
| [unity-skills-CJK编码修复.md](unity-skills-CJK编码修复.md) | active | Windows 中文编码问题修复记录 |
| [Tattoo系统重构.md](Tattoo系统重构.md) | historical-reference | 旧框架时期 Tattoo 重构记录，只作为历史证据 |

## 3. 已降权历史资料

这些资料仍有需求演进价值，但不能直接当作当前开发事实。

| 文档/目录 | 状态 | 降权原因 |
|---|---|---|
| [历史资料/GDD-v2/](历史资料/GDD-v2/) | historical-reference | GDD v2.1 是早期完整设计，后续大量内容已被用户口头确认和 GF_X 重构修正 |
| [历史资料/初版GDD-2026-06/](历史资料/初版GDD-2026-06/) | historical-reference | 初版 GDD 是更早期草案，只作为立项和需求来源证据 |
| [初版GDD设计文档.md](初版GDD设计文档.md) | historical-reference | 初版 GDD 的整理稿，不代表当前最终设计 |
| [UI先定表规范.md](UI先定表规范.md) | superseded | 已被 [UI结构先行规范.md](UI结构先行规范.md) 取代 |
| [设计草稿/2.5D俯视角PCG地图实现思路.md](设计草稿/2.5D俯视角PCG地图实现思路.md) | draft | 设计思路尚未正式拆成当前 GF_X 实现任务 |

## 4. 当前机器索引

| 索引 | 用途 |
|---|---|
| [manifests/datatables.json](manifests/datatables.json) | 配置表索引 |
| [manifests/art_assets.json](manifests/art_assets.json) | 美术/音频/Prefab/动画等资源索引 |
| [manifests/feature_slices.json](manifests/feature_slices.json) | 功能切片到配置、服务、UI、资源、诊断的反链 |
| [manifests/diagnostic_triage.json](manifests/diagnostic_triage.json) | 诊断失败时的定位索引 |
| [manifests/modules.json](manifests/modules.json) | 旧模块证据卡索引 |
| [manifests/tests.json](manifests/tests.json) | 测试和 openspec 状态索引 |
| [manifests/health.json](manifests/health.json) | 索引健康状态 |

## 5. 使用顺序

1. 先读 [README.md](README.md)，确认目录规则。
2. 再读 [ACTIVE_CONTEXT.md](ACTIVE_CONTEXT.md)，确认当前上下文和禁改区。
3. 做功能或架构判断时读 [PROJECT_MAP.md](PROJECT_MAP.md)。
4. 涉及配置、资源、诊断时查 [manifests/](manifests/)。
5. 需要历史原因时再读 `历史资料/`，并以“历史证据”身份使用。
6. 大功能或重构仍必须走 `openspec`。

## 6. 维护要求

- `raw/` 和 `outputs/` 中的内容处理完后必须移动到 `wiki/`。
- 每次新增、移动、删除 wiki 文档后必须更新本索引。
- 过时文档必须使用 `historical-reference`、`superseded` 或 `draft` 标记。
- 生成索引不手改，统一运行 `python tools\ai_index\build_ai_manifests.py`。
- 当前运行框架是 GF_X；旧 `Assets/Scripts` 只作为 `LegacyProjectArchive` 证据。
