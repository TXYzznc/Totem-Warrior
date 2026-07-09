# Baseline: GF_X business runtime refactor

## 1. Current answer

原 `Assets`，尤其是 `Assets/Scripts` 的业务内容，**尚未全面重构到 GF_X**。

已完成的是 GF_X 框架、工具、诊断和 BuildSettings 启动入口迁移。当前默认 Unity 启动已经指向：

```text
Assets/Game/Scene/Launch.unity
```

但旧业务仍主要由以下入口驱动：

```text
Assets/Scripts/Core/GameApp.cs
Assets/Scripts/Core/ModuleRunner.cs
Assets/Scripts/Core/EventBus.cs
Assets/Scripts/Modules/DataTable/DataTableModule.cs
Assets/Scripts/Modules/UI/UIModule.cs
```

GF_X `WorkspaceProcedure` 当前仍是干净工作区占位，不会自动启动旧业务主菜单、战斗、UI、Bot 或玩法模块。

## 2. Implemented old business effects

当前旧业务已实现的效果包括：

- 主菜单、角色选择、启动选择、设置、暂停、结算、HUD、商店、纹身、三选一等 UGUI Form。
- 28 张业务 JSON 配置表和生成的强类型 C# 表。
- 玩家输入语义：WASD、鼠标左键、E、Space、Tab、Esc、F、F12。
- 2.5D 地图、相机、玩家/AI/Bot 生成、武器拾取/升级、技能、战斗、纹身构筑、状态效果、经济、NPC、事件、VFX、音频桥和存档。
- EditMode / PlayMode 测试文件与 playtest driver。

这些效果需要在 GF_X 化过程中保留，不能通过删除旧功能来换取框架统一。

## 3. Structural gaps

| Gap | Evidence | Required direction |
|---|---|---|
| GF_X 启动不进业务 | `WorkspaceProcedure` 只打印“进入干净工作区流程” | 新增 Totem business Procedure/runtime |
| 双生命周期 | 旧 `GameApp` 手动注册 20+ 模块 | 逐步迁到 GF_X Procedure/GF Event/服务 |
| 双 DataTable | GF_X 只加载 Core xlsx 表；旧业务加载 `Resources/DataTable/*.json` | 建立迁移 manifest 与 facade，逐模块迁表 |
| 双 UI | 旧 UIModule 加载 `Resources/Prefab/UI`；GF_X 有 GF.UI/UITable | 首屏 UI 链优先接 GF_X 生命周期 |
| 资源硬编码 | 大量 `Resources.Load("Prefab/...")`、`Resources.Load("Sprite/...")` | 迁到 GF_X Resource/DataTable 配置 |
| 测试入口不稳 | `uloop` CLI 不存在；batchmode 不能和打开的 Unity 并发 | 记录限制，优先补可复跑验证入口 |

## 4. Baseline verification

Environment:

```text
UnitySkills: http://localhost:8092/
Unity: 2022.3.62f3c1
Editor state: isPlaying=false, isCompiling=false
```

Passed:

```text
UnitySkills health:
ok=true

UnitySkills asset_refresh:
success=true

GF_X diagnostics:
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_143656.json
success=11 failure=0 warnings=0
BusinessRuntime.oldRuntimeHostHits=0
BusinessRuntime.pendingDecisionCount=9
BusinessRuntime.legacyDataTableCount=28
BusinessRuntime.legacyUIPrefabCount=12

Active editor scene:
Assets/Game/Scene/Launch.unity

AI DataTable read-only validation:
GameData/AIData/Reports/validate-data-tables-json_20260707_141035.json
success=5 failure=0 warnings=0

Unity Console after diagnostics:
Project diagnostic failures=0
Project diagnostic warnings=0

Note:
After switching scenes through the editor menu, Unity emitted one UIElements MissingReferenceException
inside editor UI rendering. It is recorded separately because the GF_X diagnostic report itself passed
without project failures or warnings.

OpenSpec:
openspec validate gf-x-business-runtime-refactor --strict
valid
```

Limited / not completed:

```text
uloop run-tests:
uloop CLI is not installed or not on PATH.

Unity batchmode -runTests:
failed to start because the same project is already open in the Unity Editor.

UnitySkills package_list:
does not list com.unity.test-framework as installed, despite Packages/manifest.json containing com.unity.test-framework.
```

This means UTF is not yet a reliable automated gate in the current live-editor setup. GF_X diagnostics and UnitySkills Console checks are the current non-UI validation baseline until the test runner path is fixed.

## 5. First implementation slice

The next implementation slice should establish:

```text
WorkspaceProcedure -> TotemGameProcedure -> TotemGameRuntime
```

with diagnostics proving that GF_X startup reaches a business runtime. The runtime must be GF_X-native: old `GameApp` / `ModuleRunner` / `EventBus` can be read as requirement evidence, but must not be mounted as a runtime host.

Current neutral progress:

```text
Assets/Game/Scripts/Runtime/TotemGameRuntime.cs
Assets/Game/Scripts/Runtime/Services/ITotemRuntimeService.cs
Assets/Game/Scripts/Runtime/Services/TotemRuntimeServiceBase.cs
Assets/Game/Scripts/Runtime/Services/TotemGameFlowService.cs
Assets/Game/Scripts/Runtime/Services/TotemInputService.cs
Assets/Game/Scripts/Runtime/Services/TotemDataService.cs
Assets/Game/Scripts/Runtime/Services/TotemUIService.cs
```

These files compile and provide a GF_X-native service skeleton, but concrete service behavior and startup activation are intentionally pending user decisions in `DECISIONS_NEEDED.md`.

Additional guardrail:

```text
Assets/Game/ScriptsBuiltin/Editor/Diagnostics/BusinessRewriteInventoryDiagnosticScenario.cs
```

This diagnostic keeps the rewrite scope visible while decisions are pending. It does not decide implementation priority.

## 6. 2026-07-07 cleanup update

Active Unity compile path cleanup is now part of the baseline:

```text
Assets/Scripts                            -> LegacyProjectArchive/Assets/Scripts
Assets/Tests                              -> LegacyProjectArchive/Assets/Tests
Assets/Editor/Playtest                    -> LegacyProjectArchive/Assets/Editor/Playtest
Assets/Editor/Character                   -> LegacyProjectArchive/Assets/Editor/Character
Assets/Game/ScriptsBuiltin/Editor         -> Builtin.Editor explicit editor assembly
```

Current verification:

```text
Unity Console after asset_refresh:
errors=0
warnings=0

GF_X diagnostics:
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_151300.json
success=11
failure=0
warnings=0

BusinessRewriteInventory:
toolMigrationManifest=True
archivedLegacyScriptCount=174
archivedLegacyTestCount=11
archivedLegacyEditorToolCount=4
```

The remaining UnitySkills warning is external-port related (`8090/8091` occupied by other Unity instances), not a project compile or GF_X diagnostic failure.

Note: `Game Framework/Show Toolbars` is wrapped in `UNITY_6000_3_OR_NEWER` in GF_X. It is not an expected menu in this Unity `2022.3.62f3` project.
