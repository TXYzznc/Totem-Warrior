# GF_X tool migration manifest

> Source: `C:/Users/WIN10/Desktop/GF_X-master`
>
> Target: `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior`
>
> This manifest records the confirmed and executed GF_X tool migration. It also keeps the original risk notes as audit evidence.

## User-confirmed direction

- GF_X tool folders shown by the user should be brought into the current project before business rewrite continues.
- Tools should keep the same relative paths where possible.
- Hardcoded absolute paths must be removed or rewritten for the current project.
- Old business code must not be kept in the clean GF_X runtime workspace.

## Confirmed decisions

- Old `Assets/Scripts` is archived outside `Assets` at `LegacyProjectArchive/Assets/Scripts`.
- Old resources stay in place and are managed through `项目知识库（AI自行维护）/wiki/manifests/art_assets.json`.
- Migrate only `AB` / `CompressImageTool` / GF_X `Docs` / `GameData` / `Tools` / `Packages`. GF_X `Docs` are normalized into `项目知识库（AI自行维护）/wiki`; the project root should not keep an active `Docs` directory.
- Do not migrate generated `.csproj` / `.sln` files.
- Existing target directories must be merged, not blindly overwritten.
- Acceptance for the first UI slice remains: `Launch.unity -> GF_X Preload -> Workspace -> MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`, with no old `GameApp/ModuleRunner/EventBus/UIModule/DataTableModule` runtime dependency.
- Old tests and legacy editor helpers that depend on the old runtime are also archived outside `Assets`:
  - `LegacyProjectArchive/Assets/Tests`
  - `LegacyProjectArchive/Assets/Editor/Playtest`
  - `LegacyProjectArchive/Assets/Editor/Character`

## Directory comparison

| Directory | GF_X files before migration | Current files before migration | Final state | Action |
|---|---:|---:|---|---|
| `AB` | 1 | 0 | Present | Copied directory; placeholder only. |
| `CompressImageTool` | 2 | 0 | Present | Copied directory; placeholder only. |
| GF_X `Docs` | 1 | 1 | Normalized into `项目知识库（AI自行维护）/wiki` | Keep the stable guide in the knowledge wiki; no active root `Docs` directory. |
| `GameData` | 35 | 40 | Mostly already migrated; GF_X-only files are old reports | Keep current; do not copy old GF_X report files unless needed for audit. |
| `Packages` | 2 | 2 | Current project package files retained | No overwrite; current project already includes UnitySkills and project-specific packages. |
| `Tools` | 26 | 1198 | GF_X tool files merged into current `tools` directory | Copied GF_X `Tools/*`; Windows treats `Tools` and `tools` as the same path. |

## GF_X-only tool files

`AB`:

- `AB/AssetBundle打包目录.txt`

`CompressImageTool`:

- `CompressImageTool/ImgBackupDir/图片备份目录.txt`
- `CompressImageTool/ImgCompressedDir/图片压缩目录.txt`

`Tools`:

- `Tools/CompressImageTools/pngquant_mac/COPYRIGHT`
- `Tools/CompressImageTools/pngquant_mac/README.md`
- `Tools/CompressImageTools/pngquant_mac/pngquant`
- `Tools/CompressImageTools/pngquant_mac/pngquant-compat`
- `Tools/CompressImageTools/pngquant_win/COPYRIGHT`
- `Tools/CompressImageTools/pngquant_win/Drag PNG here to reduce palette automatically.bat`
- `Tools/CompressImageTools/pngquant_win/Drag PNG here to reduce palette to 256.bat`
- `Tools/CompressImageTools/pngquant_win/README.txt`
- `Tools/CompressImageTools/pngquant_win/pngquant.exe`
- `Tools/FontMinify/CharSets_ScanFromProject.txt`
- `Tools/FontMinify/CharacterSetsBase.txt`
- `Tools/Jenkins/BuildApp.bat`
- `Tools/Jenkins/BuildAppConfig.json`
- `Tools/Jenkins/BuildResource.bat`
- `Tools/Jenkins/BuildResourceConfig.json`
- `Tools/Jenkins/CreateBuildAppConfig.bat`
- `Tools/Jenkins/CreateBuildResourceConfig.bat`
- `Tools/Jenkins/GitPullLatestProject.bat`
- `Tools/Jenkins/Run_Jenkins.bat`
- `Tools/Jenkins/jenkins.war`
- `Tools/Jenkins/jobs/Build App/config.xml`
- `Tools/Jenkins/jobs/Build Resource/config.xml`
- `Tools/LocalizationStringScanner/LocalizationCodeScanner`
- `Tools/LocalizationStringScanner/LocalizationCodeScanner.exe`
- `Tools/LocalizationStringScanner/扫描代码中国际化语言工具.txt`
- `Tools/PSD2UGUI/ExportPsdForUGUI.jsx`

## Hardcoded path risks found in GF_X source

These were found in GF_X source and were rewritten in active migrated files:

- `Tools/Jenkins/BuildResourceConfig.json`
  - old `ResourceOutputDir`: `D:/GF_X/AB`
  - new `ResourceOutputDir`: `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior/AB`
- `Tools/Jenkins/BuildAppConfig.json`
  - old `ResourceOutputDir`: `D:/Workspace/OpenSource/GF_X/AB`
  - new `ResourceOutputDir`: `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior/AB`
- `Tools/Jenkins/jobs/Build Resource/config.xml`
  - old default project path: `D:/Workspace/OpenSource/GF_X`
  - old default AB path: `D:/Workspace/OpenSource/GF_X/AB`
  - new defaults point at `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior` and its `AB` directory.
- `Tools/Jenkins/jobs/Build App/config.xml`
  - old default project path: `D:/Workspace/OpenSource/GF_X`
  - old default AB path: `D:/Workspace/OpenSource/GF_X/AB`
  - new defaults point at `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior` and its `AB` directory.
- `Assets/Plugins/UnityGameFramework/Configs/ResourceBuilder.xml`
  - old output directory: `D:\Workspace\OpenSource\GF_X\AB`
  - new output directory: `D:/unity/UnityProject/GameDesinger_2/Totem-Warrior/AB`
- `Tools/LocalizationStringScanner/扫描代码中国际化语言工具.txt`
  - old example command uses `D:\Workspace\GF_HybridCLR`
  - new example command uses relative paths from the scanner tool directory.

Historical reports under `GameData/AIData/Reports` and `GameData/Diagnostics/Reports` also contain absolute source paths. They should remain audit artifacts only, not copied as active configuration.

## Package merge notes

GF_X `Packages/manifest.json` differs from the current project. The current project already includes:

- `com.besty.unity-skills` from `file:../OutPackages/Unity-Skills-main/SkillsForUnity`
- Rider/VSCode IDE packages
- `com.unity.feature.2d`
- `com.unity.visualscripting`
- project-specific lock data

Therefore `Packages/manifest.json` and `Packages/packages-lock.json` should be merged intentionally. Do not overwrite them with GF_X versions.

## Dependency source notes

The active project uses GF_X-style embedded plugins instead of old root package
entries:

- UniTask authority: `Assets/Plugins/UniTask`
- DOTween authority: `Assets/Plugins/DOTween`
- DOTween settings: `Assets/Resources/DOTweenSettings.asset`

Guardrails:

- `Packages/manifest.json` and `Packages/packages-lock.json` must not contain
  `com.cysharp.unitask` or `com.demigiant.dotween`.
- `OutPackages/Unity-Skills-main/SkillsForUnity` must stay because
  `Packages/manifest.json` references it as the active Unity Skills local
  package.
- The old upstream `OutPackages/UniTask` clone is archived at
  `LegacyProjectArchive/OutPackages/UniTask`. It must not return under
  `OutPackages`, because the active source of truth is the embedded GF_X
  `Assets/Plugins/UniTask` copy.
- Old `Assets/Demigiant` DOTween/DOTweenPro must stay outside active Unity
  assets.
- `Assets/Plugins/UniTask/package.json` remains the embedded UniTask 2.5.10
  metadata from GF_X, not a root Package Manager dependency.
- `Assets/Plugins/UniTask/Runtime/External/DOTween/UniTask.DOTween.asmdef`
  intentionally references `DOTween.Extension`, because the migrated GF_X
  DOTween asmdef is named `DOTween.Extension`. The original `DOTween.Modules`
  reference from the source GF_X tree is stale for this project and would not
  match the active asmdef.
- `Assets/Plugins/UniTask/Runtime/External/Addressables/UniTask.Addressables.asmdef`
  stays guarded by `UNITASK_ADDRESSABLE_SUPPORT` while Addressables is not a
  root project package.

`DependencySourceDiagnosticScenario` enforces these rules in every full GF_X
diagnostic run. The latest verified report is
`gf-diagnostics-run-all_20260708_163725.json`, `success=24`,
`failure=0`, `warning=0`.

## Acceptance after migration

- `AB` and `CompressImageTool` exist at the top-level target relative paths.
- GF_X top-level `Tools` content exists without deleting current `tools` content.
- No active migrated tool config contains `D:/Workspace/OpenSource/GF_X`, `D:/GF_X`, `GF_X-master`, or `D:\Workspace\GF_HybridCLR`.
- Dependency source diagnostics confirm UniTask and DOTween come from the active
  GF_X embedded plugin paths, not old Package Manager/Demigiant entries.
- Unity compiles.
- GF_X diagnostics pass.
- `openspec validate gf-x-business-runtime-refactor --strict` passes.

## Active compile cleanup

During verification, Unity surfaced old compile-time dependencies outside `Assets/Scripts`:

- `Assets/Tests` referenced old `EventBus`, `ModuleRunner`, `Tattoo.*`, `Target`, and other old runtime types.
- `Assets/Editor/Playtest` referenced old `GameApp`, `ModuleRunner`, `EventBus`, `InputModule`, and old playtest menus.
- `Assets/Editor/Character` hardcoded `Player1/Player2/Player3/Boss1` and generated prefabs with old `EntityRef`.

These folders were moved to `LegacyProjectArchive/Assets/...` so the active Unity workspace can compile and launch from GF_X without old runtime pollution. Their contents remain available as rewrite evidence.

`Assets/Game/ScriptsBuiltin/Editor/Builtin.Editor.asmdef` was added so GF_X editor tools, diagnostics, AI DataTable tools, build tools, and resource tools compile into an explicit editor assembly after legacy default assemblies are removed.
