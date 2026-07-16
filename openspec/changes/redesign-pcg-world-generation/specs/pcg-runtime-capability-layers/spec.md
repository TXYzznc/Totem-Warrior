## ADDED Requirements

### Requirement: Elements carry composable capability metadata
The system SHALL allow terrain cells, visual elements, and event anchors to carry zero or more capability records from Visual, Collision, MovementModifier, Occlusion, Hazard, Interaction, and EventAffinity. Capability records MUST contain a capability kind, enabled state, tags, and optional parameter reference.

#### Scenario: Terrain with future movement metadata
- **WHEN** a terrain profile marks a terrain as a future movement-modifier candidate
- **THEN** its World Plan records MovementModifier metadata without requiring the modifier to be active

### Requirement: Activation is controlled by theme configuration
The runtime SHALL activate capabilities only when the loaded theme activation profile explicitly enables their capability kind. Capability metadata alone MUST NOT create colliders, interactions, hazards, movement effects, or occlusion behavior.

#### Scenario: Disabled collision metadata
- **WHEN** an element has Collision metadata but the active theme profile disables Collision
- **THEN** the runtime creates no blocking collider from that element

### Requirement: Visual-only baseline behavior
The baseline activation profile for AI Ruins, Alien Hive, and Virus Swamp SHALL enable Visual capability and SHALL disable Collision, MovementModifier, Occlusion, Hazard, Interaction, and gameplay effects. This baseline MUST apply equally to all four terrain identities in each theme.

#### Scenario: Baseline map load
- **WHEN** a baseline theme World Plan is loaded
- **THEN** its terrain and elements render visually while no gameplay-effect capability is activated

### Requirement: Event and visual systems consume semantic roles
The runtime SHALL resolve event visuals and object placement using theme, terrain/region tags, event type, and visual role. It MUST support multiple instances of the same event type and MUST NOT require an exact legacy anchor ID to find every visual shell.

#### Scenario: Multiple merchant anchors
- **WHEN** a World Plan contains several merchant anchors for one theme
- **THEN** each anchor can resolve a themed merchant visual candidate through its shared event type or visual role

### Requirement: Legacy capability and visual paths are removed
The implementation SHALL remove unused BSP generation parameters, unused ZoneRuleCatalog loading and data, terrain edge-matching, transition-mask placement, and water-edge-base placement paths once the World Plan adapter is active. No active generation path may depend on those removed concepts.

#### Scenario: New generator execution
- **WHEN** the World Plan generator produces a map
- **THEN** its generation path does not read BSP depth, ZoneRuleCatalog, edge-matching, transition-mask, or water-edge-base data
