# pcg-map-runtime Specification

## Purpose
TBD - created by archiving change 28-pcg-map-runtime-integration. Update Purpose after archive.
## Requirements
### Requirement: PCG map generation backend

`TotemMapService.BuildLayout(seed, themeId)` MUST prefer the PCG generator when PCG catalogs are available.

#### Scenario: BuildLayout creates a PCG snapshot

- WHEN `BuildLayout(1, 1)` is called
- THEN the returned snapshot MUST have `IsPcgGenerated == true`
- AND `PcgWidth == 64`
- AND `PcgHeight == 64`
- AND `PcgContentHash != 0`
- AND existing fields `Rooms`, `AnchorPlacements`, `TerrainGrid`, `InitialZoneCenter` MUST remain populated.

### Requirement: Existing gameplay consumers remain compatible

PCG integration MUST NOT require Actor, NPC, Chest, Weapon, Event, Zone, Camera or UI systems to consume a second map model.

#### Scenario: Map anchors remain stable

- WHEN a PCG map is generated
- THEN the existing 16 anchor contract MUST remain available
- AND anchor positions MUST be walkable according to `TotemMapService.QueryTerrain`.

### Requirement: Runtime PCG presentation

Entering `CombatHud` MUST create a visible PCG map under `[TotemMap]`.

#### Scenario: CombatHud renders PCG map

- WHEN startup is confirmed and flow enters `CombatHud`
- THEN `[TotemMap]` MUST exist
- AND the runtime snapshot MUST report at least one ground sprite per PCG cell
- AND `pcgMissingSpriteCount` MUST be `0` for required ground/object resources.

