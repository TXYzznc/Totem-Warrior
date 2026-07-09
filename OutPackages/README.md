# OutPackages - external package cache

This directory is for local packages that are actively referenced by
`Packages/manifest.json`.

## Active packages

| Package | Source | Manifest path |
|---|---|---|
| Unity Skills | `OutPackages/Unity-Skills-main` | `file:../OutPackages/Unity-Skills-main/SkillsForUnity` |

## Archived packages

| Package | Archive path | Reason |
|---|---|---|
| UniTask upstream clone | `LegacyProjectArchive/OutPackages/UniTask` | The active project uses the GF_X embedded UniTask at `Assets/Plugins/UniTask`; keeping a second source clone under `OutPackages` polluted AI/code searches. |

## Rules

- `Packages/manifest.json` is the authority for active `OutPackages` entries.
- Do not add `com.cysharp.unitask` or `com.demigiant.dotween` as root Package
  Manager dependencies; the project uses the GF_X embedded plugin copies.
- Do not restore `OutPackages/UniTask` unless the dependency source strategy is
  intentionally changed and diagnostics are updated in the same change.
