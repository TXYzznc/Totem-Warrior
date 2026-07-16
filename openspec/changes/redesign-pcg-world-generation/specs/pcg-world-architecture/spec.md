## ADDED Requirements

### Requirement: Deterministic World Plan output
The system SHALL generate an immutable World Plan from a normalized theme identifier, seed, map dimensions, and versioned generation profiles. The World Plan SHALL contain macro terrain cells, region and density metadata, event anchors, visual placement requests, capability metadata, and a reproducibility hash. The same normalized input and profile versions MUST produce the same World Plan and hash.

#### Scenario: Repeating a seed
- **WHEN** a theme is generated twice with the same seed, dimensions, and profile versions
- **THEN** both World Plans contain identical terrain, anchors, visual requests, capability metadata, and reproducibility hash

### Requirement: Dual-scale spatial representation
The system SHALL represent terrain and regional composition on a macro grid and SHALL represent anchors, visual placements, and future gameplay areas in world coordinates. A micro-scale record MUST retain the macro cell and region from which it was sampled.

#### Scenario: Multiple events in one terrain cell
- **WHEN** two valid event candidates are sampled from the same macro terrain cell
- **THEN** the World Plan records distinct world coordinates for both candidates while retaining their shared terrain and region metadata

### Requirement: Ordered isolated random streams
The system SHALL execute generation in the order base/reservation, terrain features, region-density fields, event layout, visual placement, capability aggregation, and hashing. Each stage MUST use a named deterministic sub-seed derived from the request seed and MUST NOT use Unity global random state.

#### Scenario: Adding visual variation
- **WHEN** a visual-placement-only random choice is added to a profile
- **THEN** terrain feature and event layout output for the same seed remains unchanged

### Requirement: Current traversal policy
The current World Plan capability activation profile SHALL enable only visual output. Every macro terrain cell and every emitted micro-scale anchor position MUST be reported as traversable while collision, movement modifier, hazard, interaction, and occlusion activation are disabled.

#### Scenario: Water visual terrain
- **WHEN** a generated macro cell has a water-like terrain identifier
- **THEN** the World Plan reports that cell as traversable and emits no collision or movement-modifier activation

### Requirement: Generation diagnostics and preview data
The system SHALL expose generation diagnostics containing seed, profile versions, terrain area counts, feature counts, emitted event counts, visual instance counts, and reproducibility hash. The diagnostics MUST be available without reading rendered Tilemap state.

#### Scenario: Inspecting a generated seed
- **WHEN** a developer requests diagnostics for a completed World Plan
- **THEN** the system returns the World Plan metadata and counts required to identify the seed and configuration that produced it
