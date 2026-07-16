## ADDED Requirements

### Requirement: Events use data-driven layout profiles
The system SHALL place event anchors from an Event Layout Profile containing event type, minimum and maximum count, priority, terrain and region affinity, world-space spacing, edge preference, grouping or exclusion rules, and visual role. The layout engine MUST support new event types through profile data without generator code branches.

#### Scenario: Adding an event type
- **WHEN** a profile adds a valid event type with a quota and terrain affinity
- **THEN** the layout engine emits anchors for that type without requiring a new fixed anchor implementation

### Requirement: Dynamic seeded anchor locations
The system SHALL derive anchor candidate locations from the completed World Plan and seed. Anchor locations MUST vary across eligible seeds while remaining deterministic for the same World Plan input. The system MUST NOT derive all event positions from fixed room centers or a fixed list of world coordinates.

#### Scenario: Comparing two seeds
- **WHEN** the same theme is generated with two different eligible seeds
- **THEN** at least one event-anchor location may differ while each result remains valid against its layout profile

### Requirement: Baseline event quotas
The baseline Event Layout Profiles SHALL request at least 10 active player-spawn candidates, 1 Boss anchor, 3 merchant anchors, 5 tattooist anchors, and 30 chest anchors per generated map. Player spawn selection MUST select one candidate from the active generated spawn set without regenerating the World Plan.

#### Scenario: Generating baseline event anchors
- **WHEN** a baseline theme World Plan is generated successfully
- **THEN** its event-anchor collection contains at least 10 player-spawn candidates, 1 Boss, 3 merchants, 5 tattooists, and 30 chests

### Requirement: Stable multi-instance anchor identities
The system SHALL assign stable IDs from event type and deterministic ordinal, such as `player.spawn.000` and `chest.012`. Consumers MUST be able to query all anchors by event type; a compatibility adapter MAY expose a designated primary anchor for legacy single-anchor consumers during migration.

#### Scenario: Querying multiple chests
- **WHEN** an event consumer requests anchors of type chest
- **THEN** it receives the complete generated chest-anchor collection rather than one fixed chest ID

### Requirement: Layout failure is explicit
If mandatory event quotas cannot be satisfied after deterministic candidate generation and permitted retries, the system MUST return a diagnostic containing the failed event type, satisfied count, candidate count, and blocking constraints. It MUST NOT silently emit an incomplete mandatory event set.

#### Scenario: Impossible spawn spacing
- **WHEN** a profile requires more player-spawn candidates than fit under its mandatory minimum spacing
- **THEN** generation reports an explicit player-spawn layout failure with constraint diagnostics
