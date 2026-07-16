## ADDED Requirements

### Requirement: Theme terrain profiles define all terrain behavior inputs
The system SHALL load a terrain profile for each theme that defines exactly four active terrain identifiers, their visual roles, feature recipes, area budgets, adjacency preferences, density rules, event affinities, and capability metadata. The core generator MUST interpret these records generically and MUST NOT branch on a concrete theme identifier to create terrain.

#### Scenario: Adding a fourth theme profile
- **WHEN** a new valid theme terrain profile is registered using supported feature recipes
- **THEN** the generator can produce its World Plan without adding a theme-specific generation branch

### Requirement: Terrain features support varied natural forms
The terrain profile system SHALL support deterministic Blob, Ribbon, Chain, Scatter, and Fringe feature recipes with configurable count, area, scale, curvature, noise, spacing, placement preference, adjacency preference, and overwrite priority. Recipes MAY combine in one terrain profile.

#### Scenario: Mixed water forms
- **WHEN** a terrain profile configures Blob, Scatter, and Ribbon recipes for its water-like terrain
- **THEN** a generated map can contain pools, small patches, and curved channels for that terrain without requiring a single fixed line feature

### Requirement: Four terrain types have guaranteed representation
For every valid theme profile, generation SHALL enforce configured minimum and maximum area budgets for each of its four terrain identifiers. If a profile cannot satisfy required minimum areas within map bounds, generation MUST fail with a diagnostic that identifies the unsatisfied terrain and constraint.

#### Scenario: Underrepresented accent terrain
- **WHEN** initial feature placement leaves an active terrain below its configured minimum area
- **THEN** the generator continues constrained placement or reports an explicit generation failure instead of silently omitting that terrain

### Requirement: Three theme terrain identities
The baseline profiles SHALL define the following terrain identities: AI Ruins uses ruins_floor, ruins_metal, ruins_growth, and ruins_coolant; Alien Hive uses hive_chitin, hive_membrane, hive_resin, and hive_acid; Virus Swamp uses swamp_grass, swamp_mud, swamp_corruption, and swamp_water. Each identity MUST have a distinct visual role and future capability metadata.

#### Scenario: Loading a baseline theme
- **WHEN** a baseline theme profile is loaded
- **THEN** it exposes four distinct terrain identities with non-empty visual-role and capability metadata

### Requirement: Water is a configurable terrain instead of a global special case
The generator SHALL treat water-like terrain only through the same terrain profile and feature recipe interfaces as other terrain. It MUST NOT impose a global single-ribbon, linear, blocked, or edge-overlay rule on water-like terrain.

#### Scenario: Nonlinear swamp water
- **WHEN** the Virus Swamp profile is generated with a seed that selects multiple configured water features
- **THEN** the resulting water cells follow the selected blobs, chains, scatters, or ribbons and do not depend on a mandatory vertical line rule
