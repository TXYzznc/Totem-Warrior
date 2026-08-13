# CONTRACT — AI 信息契约层

## 1. 唯一入口

AI 接手项目级任务时，读取顺序必须是：

```text
AGENTS.md
项目知识库（AI自行维护）/wiki/INDEX.md
项目知识库（AI自行维护）/wiki/PROJECT_MAP.md
项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md
```

## 2. 生成物

以下文件由 `tools/ai_index/build_ai_manifests.py` 生成，可以重复生成：

- `项目知识库（AI自行维护）/wiki/PROJECT_MAP.md`
- `项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md`
- `项目知识库（AI自行维护）/wiki/manifests/*.json`
- `LegacyProjectArchive/Assets/Scripts/Modules/*/MODULE.md`

## 3. 手工维护边界

- GDD、wiki、openspec 仍由对应工作流维护，不由 manifest 脚本改写正文。
- `项目知识库（AI自行维护）/raw/` 是外部输入暂存区，脚本不得写入；处理完成的资料应进入 `wiki/`。
- `.claude/` 是语义源，`.codex/` 是镜像/适配层，本 change 不修改二者。

## 4. 校验口径

`--check` 只校验 AI 信息索引是否需要重新生成，不负责判断业务实现正确性。业务正确性仍由 Unity 测试、playtest、openspec spec 验证。
