# 当前项目上下文

本文件只保留入口，不承载实施历史或设计草稿。

## 当前事实来源

- 项目协作与工作流：[AGENTS.md](AGENTS.md)
- 当前 GF_X 工程入口：[README.md](README.md)
- 活跃变更、禁改区和任务前检查：[项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md](项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md)
- 工程结构与机器索引：[项目知识库（AI自行维护）/wiki/PROJECT_MAP.md](项目知识库（AI自行维护）/wiki/PROJECT_MAP.md)
- 当前项目状态：[项目知识库（AI自行维护）/wiki/项目总结/PROJECT_STATUS_AND_TODO.md](项目知识库（AI自行维护）/wiki/项目总结/PROJECT_STATUS_AND_TODO.md)

## 使用规则

1. 以 `Assets/Game/Scripts`、`GameData/AIData` 和当前 OpenSpec 为运行事实；已移除的旧 `Assets/Scripts` 不再作为资料来源。
2. 不要用已归档 OpenSpec、历史 GDD 或旧重构报告覆盖当前实现。
3. 需要刷新索引时运行 `python tools/ai_index/build_ai_manifests.py`，不要手改 `wiki/manifests/`。

最后更新：2026-08-12
