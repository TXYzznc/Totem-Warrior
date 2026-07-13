# 移除 Assets/Resources 并统一迁入 Assets/Game：任务上下文

> 状态：仅完成盘点与范围确认，**尚未移动、删除或修改任何资源/配置/代码**。
>
> 用途：将本文完整交给后续执行窗口，作为迁移任务的起始上下文。

## 目标

彻底删除 `Assets/Resources` 文件夹。此前位于其中的项目资源、PCG 配置和框架专用资源都迁入 `Assets/Game`，并将所有相关加载机制与配置改为新位置可用的实现。

## 已确认的目录映射

| 原位置 | 目标位置 | 状态 |
| --- | --- | --- |
| `Assets/Resources/Sprite` | `Assets/Game/Sprite` | 已确认 |
| `Assets/Resources/PCG` | `Assets/Game/Config/PCG` | 已确认 |
| `Assets/Resources/Sprite/PCG` | `Assets/Game/Sprite/PCG` | 已确认 |
| 其余 `Assets/Resources/<一级目录>` | `Assets/Game/<一级目录>` | 已确认，保留原一级目录名 |
| `Assets/Resources/Prefab/UI` | 删除 | 已确认；运行时使用 `Assets/Game/Prefabs/UI` |
| 框架专用资源（AppSettings、Obfuz 密钥、AOT 元数据 DLL） | `Assets/Game` 下对应目录 | 已确认；必须先替换 `Resources.Load` |

`Resources` 的其余已发现一级目录包括：`Anim`、`Animation`、`Audio`、`Effect`、`Font`、`Material`、`Model`、`Obfuz`、`PCG`、`Prefab`、`Sprite`、`Texture`。

## 已确认的 UI Prefab 结论

`Assets/Resources/Prefab/UI` 与 `Assets/Game/Prefabs/UI` 各有同名的 12 个 UI Prefab：

`CharacterSelect`、`CombatHUD`、`MainMenu`、`PauseMenu`、`RunResult`、`SelfTattoo`、`Settings`、`Shop`、`StartupSelect`、`TattooEnchant`、`TattooStudio`、`ThreeChoice`。

- 运行时 UI 路径由 `UtilityBuiltin.AssetsPath.GetUIFormPath` 生成，固定为 `Assets/Game/Prefabs/UI/{名称}.prefab`。
- 证据：[UtilityBuiltin.cs](../../Assets/Game/ScriptsBuiltin/Runtime/Extension/UtilityBuiltin.cs) 的 `GetUIFormPath`。
- `Assets/Resources/Prefab/UI` 中 12 个 Prefab 的 GUID 在活动工程文件中均未发现静态引用。
- 两组同名 Prefab 的文件内容并非 SHA-256 完全相同；应以 `Assets/Game/Prefabs/UI` 版为唯一保留源，不要用旧 Resources 版覆盖它。

## 当前资源盘点事实

- 美术相关文件：`Assets/Resources` 约 1638 个，`Assets/Game` 约 29 个（按图片、Prefab、材质、动画、字体、音频等扩展名统计）。
- 两个目录间：SHA-256 完全相同文件为 0；同名文件跨目录组为 0（UI Prefab 例外是文件名相同但内容不同，盘点脚本的扩展名/路径统计未按该组输出同名结论）。
- 问题本质是目录职责分散与加载机制混用，不是大规模二进制重复。
- 已生成逐项美术核对清单：
  - [Markdown](./art-asset-review.md)
  - [CSV](./art-asset-review.csv)

## 必须迁移/改造的依赖

### 1. 运行时资产目录

[GameData/AIData/GameplayCatalogs/totem_runtime_assets.json](../../GameData/AIData/GameplayCatalogs/totem_runtime_assets.json) 当前共有 59 条资源记录，其中：

- 50 条 `activeAssetPath` 指向 `Assets/Resources/...`；
- 9 条已指向 `Assets/Game/...`。

迁移 Sprite 后，必须更新这 50 条路径及其 `legacySourcePath`（如已无遗留路径则清空或改为新路径，具体语义需结合验证器确认）。涉及角色头像、角色选择框、宝箱、地图贴图、武器、技能、临时特效、纹身部位和纹身图案。

加载入口：[TotemAssetService.cs](../../Assets/Game/Scripts/Runtime/Services/TotemAssetService.cs)。目前该服务在编辑器侧通过 `AssetDatabase` 按 `activeAssetPath` 加载；迁移后要验证 Player 构建侧的实际资源加载路径，不能只验证 Editor。

### 2. PCG

当前 PCG 目录配置位于：

- `Assets/Resources/PCG/TerrainVisualCatalog.json`
- `Assets/Resources/PCG/WorldObjectCatalog.json`
- `Assets/Resources/PCG/ZoneRuleCatalog.json`
- `Assets/Resources/PCG/TerrainTileSetCatalog.json`
- `Assets/Resources/PCG/TerrainMaskOverlayCatalog.json`

目标位置：`Assets/Game/Config/PCG`。

PCG Sprite（包括地形切片、对象、POI、路线）目标位置：`Assets/Game/Sprite/PCG`。

当前 [PCGMapCatalogs.cs](../../Assets/Game/Scripts/Runtime/PCGMap/PCGMapCatalogs.cs) 第 294–297 行使用 `Resources.Load<TextAsset>` 加载前四类目录配置；[TotemMapService.cs](../../Assets/Game/Scripts/Runtime/Services/TotemMapService.cs) 仍使用 `Resources.Load<Sprite>` 与 `Resources.Load<Texture2D>`。

因此，移动文件本身不够：必须先/同步替换 PCG 的目录配置和贴图加载接口，使其能够从 `Assets/Game` 下的 GF 资源加载管线读取。

### 3. ResourceConfig 与纹身资源

[GameData/AIData/DataTables/Business/ResourceConfig.json](../../GameData/AIData/DataTables/Business/ResourceConfig.json) 有 14 条纹身资源映射：

- 部位：`Tattoo.Part.Head` 至 `Tattoo.Part.RightLeg`（6 条）；
- 图案：`Tattoo.Pattern.Line` 至 `Tattoo.Pattern.Beast`（8 条）。

现有 `LoadPath` 以 `Resources/Sprite` 为前提，例如 `Tattoo/Part/Head`。迁移到 `Assets/Game/Sprite/Tattoo` 后，数据表、生成的代码/文本表以及相应加载实现必须一起更新；不能只改 JSON。

### 4. 框架专用 Resources.Load

以下调用会因删除 `Assets/Resources` 立即失效，必须替换：

| 位置 | 当前加载内容 |
| --- | --- |
| `Assets/Game/ScriptsBuiltin/Runtime/ScriptableObject/AppSettings.cs` | `Resources.Load<AppSettings>("AppSettings")` |
| `Assets/Game/ScriptsBuiltin/Runtime/Procedures/LoadHotfixDllProcedure.cs` | `Resources.Load<TextAsset>("Obfuz/defaultStaticSecretKey")` |
| 同上 | `Resources.LoadAll<TextAsset>(ConstBuiltin.AOT_DLL_DIR)` |
| `Assets/Game/ScriptsBuiltin/Runtime/MigratedToolbox/ParticleEffectBatchPreview.cs` | 编辑器工具的 `Resources.LoadAll<GameObject>` |

这部分应改为 GF 项目的资源加载/构建管线，且放入 `Assets/Game` 下的明确目录。执行前必须先查清当前 GF `ResourceComponent`、资源组与构建规则的实际接口，禁止凭空假设 API。

## 已知诊断/工具引用

以下编辑器工具或诊断中仍存在旧路径，应在资源迁移后更新或移除旧迁移逻辑：

- `Assets/Game/ScriptsBuiltin/Editor/TotemFirstSlicePrefabMigrator.cs`（源路径为 `Assets/Resources/Prefab/UI`）
- `Assets/Game/ScriptsBuiltin/Editor/TotemRuntimeAssetMigrator.cs`
- `Assets/Game/ScriptsBuiltin/Editor/Diagnostics/BusinessRewriteInventoryDiagnosticScenario.cs`
- `Assets/Game/ScriptsBuiltin/Editor/Diagnostics/CleanWorkspaceDiagnosticScenario.cs`
- PCG、资源目录及数据表相关诊断。

注意区分：`GameData/Diagnostics/Reports` 中出现的旧路径只是历史诊断报告，不应作为运行时引用修改目标；报告可在新诊断通过后重新生成。

## 推荐实施顺序

1. 创建 OpenSpec change，并先记录迁移前清单、目标目录映射和回滚点。
2. 查清并确定 GF 资源加载/构建管线的可用 API、资源组与构建规则。
3. 先替换框架专用 `Resources.Load`、PCG 目录与 PCG Sprite/Texture 的加载接口，并为新接口补验证。
4. 移动 Sprite、PCG、其余 Resources 一级目录下的资源，务必保留 Unity `.meta` 文件以维持 GUID。
5. 更新 `totem_runtime_assets.json`、PCG JSON 目录、ResourceConfig 及相关生成表/代码、编辑器迁移器和诊断。
6. 删除 `Assets/Resources/Prefab/UI`（保留 `Assets/Game/Prefabs/UI`）。
7. 删除最后的 `Assets/Resources` 文件夹。
8. 验证并生成新报告。

## 待确认的验收标准

此前建议但尚未获得用户明确确认的完成条件：

1. Unity 编译通过；
2. GF_X 全量诊断通过；
3. 启动、UI、PCG 地图生成的冒烟验证通过；
4. 项目内不再存在 `Assets/Resources` 目录，也不存在活动代码/配置对它的引用；
5. Player 构建侧验证资源实际可加载，不能仅依赖 Editor 的 `AssetDatabase`。

全量诊断优先命令：

```powershell
python .claude/skills/unity-skills/scripts/unity_skills.py totem_diagnostics_run_all --port 8092
```

## 约束

- Unity 版本固定为 2022.3.62f3；实现和包/API 必须按此版本验证。
- 不要改动 `Assets/Game/ScriptsBuiltin` 的框架核心，除非迁移确实需要且已有回滚与验证方案。
- Unity 资产移动必须连同 `.meta` 执行，以保持 GUID。
- 不要把历史 `LegacyProjectArchive` 当作活动依赖来源。
- 资源迁移属于大范围架构/配置改造；开始前应建立 OpenSpec change，而不是直接批量移动文件。

