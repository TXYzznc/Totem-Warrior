# Migration Inventory: GF_X framework migration phase 1

## 1. Source and target

- Source framework: `C:\Users\WIN10\Desktop\GF_X-master`
- Target project: `D:\unity\UnityProject\GameDesinger_2\Totem-Warrior`
- Source Unity version: `2022.3.62f3`
- Target Unity version: `2022.3.62f3c1`

The Unity versions are close enough that the first migration risk is dependency and startup-chain integration, not Unity major-version incompatibility.

## 2. Current target state

Target project currently does not have these GF_X target roots:

```text
Assets/Game
GameData
Assets/Plugins
```

Current target already has:

```text
Assets/Scripts/Game.asmdef
Assets/Scripts/Modules/DataTable/Editor/Game.Editor.asmdef
Assets/Demigiant/DOTween
Assets/Demigiant/DOTweenPro
Packages/manifest.json
Packages/packages-lock.json
```

`git status` shows many existing modified and untracked files before this migration. These are treated as user/project changes and must not be reverted.

## 3. GF_X directories

GF_X main framework/data roots:

```text
Assets/Game
GameData
Assets/Plugins
Assets/Resources
Assets/HybridCLRData
```

GF_X `Assets/Game` contains:

```text
Audio
Config
DataTable
Examples
Font
HotfixDlls
Language
Materials
Prefabs
Scene
ScriptableAssets
Scripts
ScriptsBuiltin
Shader
```

GF_X script count under `Assets/Game`: 230 `*.cs` files.

## 4. Dependency comparison

### Must use from GF_X

- UniTask: use GF_X `Assets/Plugins/UniTask`.
- DOTween: use GF_X `Assets/Plugins/DOTween`.

The target project's `com.cysharp.unitask` package entry and `Assets/Demigiant` DOTween/DOTweenPro assets should be removed from Unity's compile path. Current business code uses many `UniTask` APIs and ordinary `DG.Tweening` APIs, but the scan did not find gameplay code that directly depends on DOTweenPro-only APIs.

### Must keep or reuse from target/GF_X-compatible packages

- URP: both projects use `14.0.12`.
- Unity Test Framework: both projects use `1.1.33`.
- Timeline and UGUI already exist in target.

### Already present through target lock file

`Packages/packages-lock.json` already contains:

- `com.unity.nuget.newtonsoft-json` at `3.2.1`
- `com.unity.collections` at `1.2.4`
- `com.unity.mathematics` at `1.2.6`

These are present transitively, but direct manifest dependencies may still be needed if GF_X asmdefs rely on them explicitly.

### Missing from target and referenced by GF_X

- `com.code-philosophy.hybridclr`
- `com.code-philosophy.obfuz`
- `com.code-philosophy.obfuz4hybridclr`
- `com.unity.cinemachine`
- `com.unity.jobs` or an equivalent source for the `Unity.Jobs` asmdef reference

GF_X runtime/editor scripts reference HybridCLR and Obfuz in:

- `Assets/Game/ScriptsBuiltin/Runtime/Procedures/LoadHotfixDllProcedure.cs`
- `Assets/Game/ScriptsBuiltin/Runtime/Obfuz/GeneratedEncryptionVirtualMachine.cs`
- `Assets/Game/ScriptsBuiltin/Editor/HybridCLRExtensionTool.cs`
- `Assets/Game/ScriptsBuiltin/Editor/EditorTools/AppBuildEditor.cs`
- `Assets/Game/ScriptsBuiltin/Editor/EditorTools/StripLinkConfigTool.cs`
- `Assets/Game/ScriptsBuiltin/Editor/EditorTools/StripLinkConfigEditor.cs`

## 5. GF_X plugin import classification

### Import candidates

- `Assets/Plugins/UnityGameFramework`
- `Assets/Plugins/ZString`
- `Assets/Plugins/ForEditor/EPPlus.6.2`
- `Assets/Plugins/Protobuf`
- `Assets/Plugins/UniTask`
- `Assets/Plugins/DOTween`
- `Assets/Plugins/WebGL`
- `Assets/Plugins/Android`

### Exclude or adapt

- Target `Packages/manifest.json` entry `com.cysharp.unitask`: remove, because GF_X UniTask is the chosen source.
- Target `Assets/Demigiant`: remove from Unity compile path, because GF_X DOTween is the chosen source.
- Target `Assets/Scripts/Game.asmdef`: adapt `DOTween.Modules` to GF_X `DOTween.Extension`, and remove `DOTweenPro.dll` from precompiled references unless a DOTweenPro usage is found later.

### Needs package confirmation before full compile

- HybridCLR and Obfuz packages.
- Cinemachine package.
- Jobs package or verified assembly source.

## 6. GF_X asmdef observations

GF_X `Assets/Game/ScriptsBuiltin/Runtime/Builtin.Runtime.asmdef` references:

```text
Unity.TextMeshPro
UnityGameFramework.Runtime
ZString
GameFramework
UniTask
Obfuz.Runtime
```

GF_X `Assets/Game/Scripts/Hotfix.asmdef` references:

```text
Unity.TextMeshPro
Cinemachine
UnityGameFramework.Runtime
Unity.RenderPipelines.Universal.Runtime
Builtin.Runtime
UniTask
GameFramework
DOTween.Extension
Unity.Jobs
Unity.Collections
Unity.Mathematics
Obfuz.Runtime
```

This means full GF_X runtime import cannot be treated as a pure file copy. It either needs package updates or a temporary compile-gating strategy.

## 7. Safe first import scope

The first actual import can safely create isolated target roots and copy non-conflicting content after package decision:

```text
Assets/Game
GameData
Docs/GF_X or docs imported from GF_X
```

However, if copied scripts are allowed to compile immediately, package and plugin dependencies must be resolved in the same step. Otherwise the import should use a temporary compile-gating strategy and be clearly marked as not yet active.

## 8. Protected files requiring confirmation

Before modifying any of these, stop and ask the user:

```text
Packages/manifest.json
ProjectSettings/EditorBuildSettings.asset
Assets/Scripts/Core/GameApp.cs
Assets/Scripts/Core/ModuleRunner.cs
Assets/Scripts/Core/EventBus.cs
Assets/Scripts/Core/IGameModule.cs
Assets/Scripts/Core/GameTickDriver.cs
Assets/Scripts/Modules/Input/InputModule.cs
Assets/Scripts/Modules/DataTable/DataTableModule.cs
Assets/Scripts/Modules/UI/UIModule.cs
Assets/Scripts/Modules/Scene/SceneModule.cs
```

## 9. Recommended next implementation step

User confirmed dependency authority: use GF_X UniTask and GF_X DOTween, removing the current framework's versions.

Next implementation step: modify `Packages/manifest.json`, import GF_X dependency directories, remove target duplicate dependency paths from Unity's compile path, and adapt target asmdef references.

Proposed direct additions:

```json
"com.code-philosophy.hybridclr": "https://gitee.com/focus-creative-games/hybridclr_unity.git",
"com.code-philosophy.obfuz": "https://gitee.com/focus-creative-games/obfuz.git",
"com.code-philosophy.obfuz4hybridclr": "https://gitee.com/focus-creative-games/obfuz4hybridclr.git",
"com.unity.cinemachine": "2.10.3"
```

Proposed direct removal:

```json
"com.cysharp.unitask": "file:../OutPackages/UniTask/src/UniTask/Assets/Plugins/UniTask"
```

Potential addition after compile check:

```json
"com.unity.jobs": "0.70.0-preview.7"
```

`com.unity.collections` and `com.unity.mathematics` already appear in the target lock file, so they should be verified before adding direct dependencies.
