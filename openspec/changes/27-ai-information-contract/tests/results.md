# Results — AI 信息契约层

已执行：

```powershell
python tools/ai_index/build_ai_manifests.py
python tools/ai_index/build_ai_manifests.py --check
```

结果：

- `build_ai_manifests.py` 生成 55 个 AI 信息索引文件。
- `--check` 通过：AI manifest 已是最新。
- 生成 24 个模块 `MODULE.md` 与对应 Unity `.meta`。
- `health.json` 当前为 `warning`，非阻塞。

已知 warning：

1. 部分配置表使用业务主键而不是 `Id:int`；重新生成 DataTable 代码前需确认生成器支持这些主键。
2. `openspec/changes/25-camera-2p5d-system` 缺少 proposal/design/tasks，疑似归档残留 active 目录。
