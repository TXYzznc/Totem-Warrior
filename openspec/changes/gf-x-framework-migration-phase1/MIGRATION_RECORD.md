# Migration Record: GF_X framework migration phase 1

## 1. Completed file migration

Source:

```text
C:\Users\WIN10\Desktop\GF_X-master
```

Target:

```text
D:\unity\UnityProject\GameDesinger_2\Totem-Warrior
```

Copied:

```text
Assets/Game
Assets/Plugins
Assets/HybridCLRData
Assets/Resources/AppSettings.asset
Assets/Resources/DOTweenSettings.asset
Assets/Resources/Newtonsoft.Json-for-Unity.Converters.asset
Assets/Resources/Obfuz
Assets/link.xml
Assets/URP.asset
Assets/URP_Renderer.asset
GameData
项目知识库（AI自行维护）/wiki/AI_DIAGNOSTICS_GUIDE.md
```

Note: `Assets/UniversalRenderPipelineGlobalSettings.asset` existed in the target project and was restored to the target project's original content after an accidental overwrite during copying.

Filtered while copying `GameData`:

```text
GameData/AIData/Reports
GameData/Diagnostics/Reports
GameData/AIData/Backups
```

The report directories were recreated empty in the target project so new reports have a stable destination without carrying old absolute-path report files.

## 2. Dependency authority decision

User confirmed:

```text
UniTask and DOTween use GF_X versions.
Remove the current framework's versions.
```

Applied:

- Imported GF_X `Assets/Plugins/UniTask`.
- Imported GF_X `Assets/Plugins/DOTween`.
- Removed `com.cysharp.unitask` from `Packages/manifest.json`.
- Added GF_X-required package entries for HybridCLR, Obfuz, Cinemachine, Jobs, Collections, Newtonsoft and Newtonsoft converters.
- Changed `Assets/Scripts/Game.asmdef` from `DOTween.Modules` to `DOTween.Extension`.
- Removed `DOTweenPro.dll`, `UniTask.Addressables`, and `UniTask.DOTween` from `Assets/Scripts/Game.asmdef`.

Package lock note:

```text
Unity Package Manager refreshed Packages/packages-lock.json successfully.
The old root com.cysharp.unitask package entry is no longer present in Packages/manifest.json or Packages/packages-lock.json.
GF_X HybridCLR, Obfuz, Obfuz4HybridCLR, Cinemachine, Jobs, Collections, Newtonsoft and Newtonsoft converters are locked/resolved.
```

`Assets/Plugins/UniTask/package.json` still declares `com.cysharp.unitask`, but this is the embedded GF_X plugin package metadata and is not a root project package dependency.

## 3. Legacy Demigiant handling

Intended action:

```text
Move Assets/Demigiant to D:\unity\UnityProject\GameDesinger_2\Totem-Warrior_MigrationBackups\gf-x-framework-migration-phase1_20260707
```

Observed result:

```text
Move-Item failed with Access denied because the current project is open in Unity and the directory or DLLs are locked.
Second non-destructive move attempt also failed with Access denied while Unity was still open.
```

Backup result:

```text
Copied Assets/Demigiant to D:\unity\UnityProject\GameDesinger_2\Totem-Warrior_MigrationBackups\gf-x-framework-migration-phase1_20260707\Demigiant
```

Temporary compile-path removal:

- Disabled legacy Demigiant DLL importers by setting their `.meta` `enabled` flag to `0`.
- Added unmet `GF_X_DISABLE_LEGACY_DOTWEEN` define constraints to legacy DOTween/DOTweenPro asmdefs.

Files temporarily disabled:

```text
Assets/Demigiant/DemiLib/Core/DemiLib.dll.meta
Assets/Demigiant/DemiLib/Core/Editor/DemiEditor.dll.meta
Assets/Demigiant/DOTween/DOTween.dll.meta
Assets/Demigiant/DOTween/Editor/DOTweenEditor.dll.meta
Assets/Demigiant/DOTween/Modules/DOTween.Modules.asmdef
Assets/Demigiant/DOTweenPro/DOTweenPro.dll.meta
Assets/Demigiant/DOTweenPro/DOTweenPro.Scripts.asmdef
Assets/Demigiant/DOTweenPro/Editor/DOTweenProEditor.dll.meta
Assets/Demigiant/DOTweenPro/Editor/DOTweenPro.EditorScripts.asmdef
```

Final cleanup:

After Unity released file locks, `Assets/Demigiant` and `Assets/Demigiant.meta` were removed from the Unity project. The backup copy remains outside the project:

```text
D:\unity\UnityProject\GameDesinger_2\Totem-Warrior_MigrationBackups\gf-x-framework-migration-phase1_20260707\Demigiant
```

GF_X `Assets/Plugins/DOTween` is now the active DOTween authority.

## 4. Resource conflict cleanup

Unity reported GUID conflicts for the duplicated TextMesh Pro sample resources copied under:

```text
Assets/Game/Font/TextMesh Pro
```

The current project already owns top-level `Assets/TextMesh Pro`, so the duplicated GF_X copy was moved out of the project to:

```text
D:\unity\UnityProject\GameDesinger_2\Totem-Warrior_MigrationBackups\gf-x-framework-migration-phase1_20260707\Assets_Game_Font_TextMeshPro_duplicate
```

`Assets/Game/Font/Common` was kept.

## 5. Startup authority

User confirmed the migrated project should start through the GF_X launch scene.

Applied:

```text
ProjectSettings/EditorBuildSettings.asset -> Assets/Game/Scene/Launch.unity
```

Removed from build/startup settings:

```text
Assets/Scenes/MainMenu.unity
Assets/Scenes/Launch.unity
Assets/Scenes/SampleScene.unity
```

`Assets/Scenes/SampleScene.unity` did not exist and was the last failing GF diagnostic item before this change.

## 6. Static usage notes

Scans found many existing business uses of `Cysharp.Threading.Tasks` and ordinary `DG.Tweening`.

Scans did not find gameplay/business scripts directly using DOTweenPro-only APIs such as `DOTweenAnimation`, `DOTweenPath`, or `DOTweenProShortcuts`.

Scans did not find gameplay/business scripts using Addressables or UniTask DOTween await extensions.

Static validation passed:

```text
Packages/manifest.json parses as valid JSON.
All Assets/**/*.asmdef files parse as valid JSON.
openspec validate gf-x-framework-migration-phase1 passes.
```

Unity validation completed:

```text
Unity batchmode compile/resource refresh passed:
Logs/gf_x_migration_batch_compile_gfx_launch.log

GF_X diagnostics passed:
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_135758.json
success=9 failure=0 warnings=0

AI DataTable read-only validation passed:
GameData/AIData/Reports/validate-data-tables-json_20260707_135846.json
success=5 failure=0 warnings=0
```

Unity logs still contain non-blocking Unity licensing client handshake noise and Mono thread abort messages during shutdown; command exit codes were `0`, script compilation succeeded, and generated GF_X reports were clean.

## 7. Pre-existing pollution found

Static scan found pre-existing generated result files with old absolute paths:

```text
Assets/Resources/Sprite/Character/.char17_*.result.json
```

These files point at `D:/unity/UnityProject/GameDesinger/...`. They were not introduced by the GF_X migration. They should be cleaned or moved later under a separate confirmation because they sit beside current business art resources.
