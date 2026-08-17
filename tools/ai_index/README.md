# AI 项目索引工具

`build_ai_manifests.py` 只生成可重复构建的机器清单，不生成策划、Wiki、功能规格或源码目录说明。

游戏策划的唯一标准信息源始终是 `Docs/GameDesign/目录.md`。这些 JSON 仅描述工程当前实际存在的代码、配置、美术和测试，不能覆盖策划结论。

## 生成

```powershell
python tools/ai_index/build_ai_manifests.py
```

输出目录为 `GameData/AIData/ProjectManifests/`：

- `modules.json`：`Assets/Game/Scripts` 的当前脚本模块。
- `datatables.json`：Business AI DataTable 的当前表结构和行数。
- `assets.json`：`Assets/Game` 中当前存在的美术资源。现存项一律标记为已确认；策划需要但不存在的资源表示尚未制作。
- `tests.json`：当前 Unity 测试与 GF_X 诊断场景。

## 校验

```powershell
python tools/ai_index/build_ai_manifests.py --check
```

`--check` 仅检查生成物是否与目录事实一致，不判断玩法或实现是否符合策划。旧知识库、历史 OpenSpec 和归档美术需求都不得作为活动输入。
