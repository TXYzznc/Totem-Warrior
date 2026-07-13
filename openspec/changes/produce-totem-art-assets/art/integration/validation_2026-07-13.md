# 首批美术资源接入验证（2026-07-13）

## 已验证

- 运行时资源表 `GameData/AIData/GameplayCatalogs/totem_runtime_assets.json` 共 38 条 entry，24 个 active asset path 均存在；Player、SmartAI、LightAI、Boss、两名 NPC 与 `player_2` / `player_3` 肖像 key 均指向当前正式资源。
- `tools/ai_index/build_ai_manifests.py --check` 通过。生成的 `wiki/manifests/art_assets.json` 使用 Unity GUID 依赖反链记录 Prefab → AnimatorController → AnimationClip → Sprite 的间接用途；无 `Unclassified` 或 `classification_needed` 美术项。
- 四份 PCG catalog 共 1,354 条 `Assets/Game/Sprite/PCG/...` 引用均已解析；没有 `Assets/Resources/PCG/Terrain/` 的误创建补图依赖。
- `Totem First Slice UI` 诊断已通过：角色 1 没有肖像 key，角色 2 / 3 分别加载 `ui.character.portrait.2` / `ui.character.portrait.3`。
- `Totem Runtime Assets` 与 `Totem Actor Visual Runtime` 诊断通过；Player、SmartAI、LightAI 的中性角色主体配合运行时脚下阵营环，Boss 与 NPC 不创建阵营环。

## 诊断证据

全量诊断报告：`GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260713_182947.json`。

- 通过 24 项，失败 3 项，警告 6 项。
- 三项失败均为既有非美术工作区/迁移审计问题：完成审计缺少可引用的全绿 EditMode 报告、`Assets/Screenshots/black-screen-diagnosis.png` 未归档、知识库 `raw/` 与 `outputs/` 空目录契约未满足。
- 本轮资源、资源表、角色选择肖像、角色/阵营环和 PCG 路径相关诊断均通过。
