# Totem Warrior

Unity 2022.3.62f3 项目，当前运行时基于 GF_X。

## 当前入口

- 协作和变更规则：[AGENTS.md](AGENTS.md)
- 当前上下文：[CONTEXT.md](CONTEXT.md)
- 启动场景：`Assets/Game/Scene/Launch.unity`
- 业务代码：`Assets/Game/Scripts/`
- 业务配置：`GameData/AIData/DataTables/Business/`
- 运行时目录与资源索引：`GameData/AIData/GameplayCatalogs/`
- OpenSpec：`openspec/changes/`

已归档的变更仅可用于追溯，不能替代当前代码和活跃 OpenSpec。

## 常用验证

```powershell
python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8092
python tools/ai_index/build_ai_manifests.py --check
```

生成的测试报告、助手输出、导出包和临时文件均不应提交；规则见 `.gitignore`。
