# authored-oasis-city-runtime Specification

## Purpose
TBD - created by archiving change rebuild-six-player-pvpve-first-playable. Update Purpose after archive.
## Requirements
### Requirement: OasisCity is the authoritative gameplay scene

The runtime SHALL load `OasisCity` additively before entering CombatHUD or spawning participants. The empty `TotemGame` scene and runtime PCG generation SHALL NOT be authoritative gameplay-map sources.

#### Scenario: Start a local first-playable match

- **WHEN** the player confirms a local match from the main menu
- **THEN** OasisCity is loaded and made active before the map snapshot and six participants are created
- **AND** no PCG terrain, room, prop, or tilemap visual is generated over the authored scene

### Requirement: Gameplay placement uses explicit authored anchors

OasisCity SHALL expose typed authored anchors for player spawns, map resources, and extraction. The scene SHALL provide at least six player spawn anchors, enough enabled map-resource anchors for the configured first-playable population, and at least three extraction anchors. IDs SHALL be unique inside each anchor type, and one anchor type SHALL NOT substitute for another.

#### Scenario: Deterministic legal-anchor selection

- **WHEN** a match is built with the same seed, scene, round, and resource configuration
- **THEN** player, resource, and extraction placements are selected from their matching enabled anchor sets with the same result
- **AND** no placement is synthesized from a PCG world plan or an arbitrary fallback coordinate

### Requirement: Runtime map state follows authored world bounds

The map snapshot SHALL expose the authored OasisCity world bounds and initial zone center. Movement clamping, zone initialization, minimap projection, camera framing, resource placement, and extraction placement SHALL consume the same bounds contract rather than assuming a square map whose minimum coordinate is zero.

#### Scenario: Use centered rectangular scene coordinates

- **WHEN** OasisCity contains negative coordinates or unequal X/Z extents
- **THEN** all map consumers project and clamp against the authored minimum and maximum bounds
- **AND** the initial zone is centered inside those bounds

### Requirement: Gameplay scene teardown is repeatable

The runtime SHALL restore the persistent Launch scene as active, unload OasisCity, and clear runtime map instances when leaving a match for the main menu.

#### Scenario: Return and start another match

- **WHEN** a completed or cancelled local match returns to the main menu and another match starts
- **THEN** exactly one OasisCity scene is loaded for the new match
- **AND** no authored anchor, generated pickup, extraction marker, or participant from the previous match remains

### Requirement: Legacy PCG assets and code have zero active references

After migration, production code, tests, diagnostics, scenes, prefabs, and active configuration SHALL have no references to `Assets/Resources/PCG`, `PCGMap`, `TotemPcgRuntimeProfile`, or `PcgMapData`.

#### Scenario: Validate PCG retirement

- **WHEN** the project is compiled and the retirement audit runs
- **THEN** the PCG resource directory, runtime PCG module, empty TotemGame scene, and PCG-only tests are absent
- **AND** the OasisCity local-match PlayMode smoke and GF_X diagnostics pass without PCG fallback warnings

