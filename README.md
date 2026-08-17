# Totem Warrior

Unity 2022.3.62f3 项目，当前运行时基于 GF_X。

## 当前入口

- 协作和变更规则：[AGENTS.md](AGENTS.md)
- 当前上下文：[CONTEXT.md](CONTEXT.md)
- **游戏策划唯一标准信息源**：[Docs/GameDesign/目录.md](Docs/GameDesign/目录.md)
- 启动场景：`Assets/Game/Scene/Launch.unity`
- 业务代码：`Assets/Game/Scripts/`
- 业务配置：`GameData/AIData/DataTables/Business/`
- 运行时目录与资源索引：`GameData/AIData/GameplayCatalogs/`
- OpenSpec：`openspec/changes/`

OpenSpec 只定义实现变更和验收合同；配置、代码与测试只提供运行证据。它们以及历史资料都不得在冲突时覆盖 `Docs/GameDesign/`。已归档变更仅可用于追溯。

`Assets/Game` 中现存美术资源均视为已确认并已导入；策划需要但工程中不存在的资源视为尚未制作。

## 常用验证

```powershell
python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8090
python tools/ai_index/build_ai_manifests.py --check
```

## 编辑器 Play Mode 热重载

项目内置本地 UPM 形式的 Fast Script Reload（FSR），用于在 Unity Editor 的 Play Mode 中迭代已有 C# 方法体而不退出运行会话。首次使用、日常流程、回调、限制与排查见 [FSR 开发指南](Docs/Development/FastScriptReload.md)。FSR 不用于已发布 Player 的热更新，也不替代 HybridCLR。

生成的测试报告、助手输出、导出包和临时文件均不应提交；规则见 `.gitignore`。
