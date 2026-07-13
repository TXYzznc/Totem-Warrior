# Native Enemy Domain Baseline

Captured: 2026-07-10 (before implementation)

## Dirty worktree preserved

The following pre-existing startup-protection work is treated as user-owned input and MUST NOT be reverted:

- `Assets/Game/Scripts/Runtime/Services/TotemAIService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemActorService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemUIService.cs`
- `Assets/Game/Scripts/Runtime/TotemExtendedGameplayModels.cs`
- `Assets/Game/Scripts/Runtime/TotemGameplayModels.cs`
- `Assets/Game/Scripts/UI/TotemCombatHUDForm.cs`
- `Assets/Game/Scripts/UI/TotemStartupSelectForm.cs`
- `Assets/Game/Scripts/Editor/Diagnostics/TotemStartupProtectionDiagnosticScenario.cs` and meta files
- Diagnostics report cleanup/new report under `GameData/Diagnostics/Reports`

Historical baseline at change creation: the startup work exposed `BeginPlayerStartupProtection`, `TryReleasePlayerStartupProtection`, `CanEnemyTarget`, a 60-second participant damage guard, UI release calls and a dedicated diagnostic. The later user confirmation keeps the global 60-second Participant-vs-Participant guard: it is implemented in `TotemCombatRelationshipService` with `BlockedParticipantCombatGracePeriod`. Per-human Loading/Protected readiness remains independent and is absorbed into `ParticipantReadiness`.

## Diagnostics baseline

Latest report before this implementation:

- File: `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260710_112336.json`
- Created: `2026-07-10T03:22:39.3644156Z`
- Result: success=26, failure=2, warning=0, items=28

The two pre-existing failures are:

1. `GF_X Rewrite Inventory Contract`: completion-audit diagnostics report path is empty.
2. `Totem Gameplay Catalog`: generated source hash is stale (`expected=9e608f...`, catalog=`cd0772...`).

These failures predate the native Enemy implementation and remain required cleanup before final completion.

## Runtime service order

`TotemGameRuntime.RegisterDefaultServices` currently registers Map -> Actor -> Economy -> Status -> Tattoo -> Weapon -> Chest -> Skill -> Zone -> Boss -> AI -> Npc -> Choice -> Interaction -> Camera -> VFX -> Combat -> UI after foundation services. The migration must insert relationship/readiness before damage consumers and Enemy/Encounter/Loot before Combat outcome evaluation.

## Old semantic consumers

The initial source scan found active `TotemActorKind`, `IsEnemy`, `aliveEnemyCount`, `TotemEnemyTier` or Boss-Actor dependencies in 15 files:

- `Assets/Game/Scripts/Runtime/Services/TotemActorService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemAIService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemAudioService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemCombatService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemTattooService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemVfxService.cs`
- `Assets/Game/Scripts/Runtime/Services/TotemWeaponService.cs`
- `Assets/Game/Scripts/Runtime/TotemActorVisualHelper.cs`
- `Assets/Game/Scripts/Runtime/TotemExtendedGameplayModels.cs`
- `Assets/Game/Scripts/Runtime/TotemGameplayCatalog.cs`
- `Assets/Game/Scripts/Runtime/TotemGameplayModels.cs`
- `Assets/Game/Scripts/UI/TotemCombatHUDForm.cs`
- `Assets/Game/Scripts/UI/TotemPauseMenuForm.cs`
- `Assets/Game/Scripts/UI/TotemRunResultForm.cs`
- `Assets/Game/Scripts/Editor/Diagnostics/TotemStartupProtectionDiagnosticScenario.cs`

This list is the minimum migration inventory; later generated DataTable and diagnostic files may add consumers.
