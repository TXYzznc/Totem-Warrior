## ADDED Requirements

### Requirement: three deterministic OasisCity looks
The system SHALL define exactly three OasisCity editor look presets named `WarmCinematic`, `NeutralRealistic`, and `BoldStylized`, and each preset SHALL reference its own VolumeProfile, lighting configuration, LightingSettings, and baked LightingDataAsset.

#### Scenario: preset catalog is loaded
- **WHEN** the OasisCity look-development tool loads its catalog
- **THEN** all three required preset IDs SHALL resolve to distinct VolumeProfiles and distinct lighting configurations
- **AND** missing or duplicate references SHALL be reported as validation errors

### Requirement: modern URP post-processing
The system SHALL implement look processing with URP 14 VolumeProfiles and SHALL enable post-processing for all generated review cameras.

#### Scenario: look profile is inspected
- **WHEN** any of the three VolumeProfiles is inspected
- **THEN** it SHALL contain enabled Tonemapping, Color Adjustments, White Balance, Bloom, and Vignette overrides
- **AND** Motion Blur, Depth Of Field, Chromatic Aberration, Lens Distortion, and Panini Projection SHALL remain disabled for baseline comparisons

#### Scenario: neutral profile is activated
- **WHEN** `NeutralRealistic` is activated
- **THEN** its color and lens treatment SHALL remain more restrained than both other profiles

### Requirement: independent baked-only lighting
Each preset SHALL use a fully baked directional sun and SHALL have a distinct LightingDataAsset containing its baked lightmaps and probe data, without relying on realtime or mixed direct lighting.

#### Scenario: preset bake completes
- **WHEN** a preset bake completes successfully
- **THEN** its LightingDataAsset and linked bake outputs SHALL be stored in that preset's deterministic directory
- **AND** the preset catalog SHALL reference the generated LightingDataAsset
- **AND** no retained LightingDataAsset belonging to another preset SHALL be deleted or overwritten

#### Scenario: dynamic renderer receives lighting
- **WHEN** a non-lightmapped renderer is placed within the configured OasisCity probe volume
- **THEN** it SHALL receive baked lighting from Light Probes
- **AND** it SHALL not depend on realtime direct lighting or realtime shadows

### Requirement: reversible editor look switching
The camera-group toolbox SHALL expose explicit buttons for the three looks and SHALL atomically switch the active VolumeProfile, LightingDataAsset, baked sun/environment configuration, and temporary high-fidelity URP context.

#### Scenario: user switches looks
- **WHEN** the user clicks a look button with a valid completed bake
- **THEN** the corresponding VolumeProfile and LightingDataAsset SHALL become active in the editor
- **AND** the panel SHALL visibly identify the active look and bake tier
- **AND** Game View SHALL repaint without entering Play Mode

#### Scenario: temporary look session ends
- **WHEN** the user restores, closes the panel, saves the scene, enters Play Mode, or scripts reload
- **THEN** the original render-pipeline context, LightingDataAsset, sun/environment values, and temporary Volume state SHALL be restored

### Requirement: deterministic GI eligibility
The OasisCity build pipeline SHALL enable Contribute GI and lightmap receiving only for eligible rendered environment geometry, SHALL generate secondary UVs for imported and generated lightmapped meshes, and SHALL apply category-based lightmap density.

#### Scenario: OasisCity is rebuilt
- **WHEN** the complete OasisCity map is rebuilt
- **THEN** terrain, roads, walls, bridges, buildings, and eligible decorations SHALL retain deterministic GI flags and receive-GI settings
- **AND** cameras, lights, gameplay markers, navigation helpers, and placeholder-only objects SHALL remain excluded

#### Scenario: imported building mesh is prepared
- **WHEN** an OasisCity FBX used by a lightmapped building is imported
- **THEN** secondary lightmap UV generation SHALL be enabled
- **AND** the validation step SHALL reject missing lightmap UVs before baking

### Requirement: deterministic probes
The OasisCity builder SHALL create a bounded Light Probe layout and six baked Reflection Probes covering representative city regions.

#### Scenario: probe hierarchy is generated
- **WHEN** OasisCity review lighting objects are rebuilt
- **THEN** the scene SHALL contain approximately 400 to 700 Light Probe positions across three useful height layers
- **AND** it SHALL contain north, central, south, river, west-boundary, and east-boundary Reflection Probes

### Requirement: sequential preview and final bake workflow
The tool SHALL bake `WarmCinematic`, `NeutralRealistic`, and `BoldStylized` sequentially, SHALL expose progress/cancellation/resume state, and SHALL require successful preview validation before final baking.

#### Scenario: preview sequence starts
- **WHEN** the user starts preview baking
- **THEN** the tool SHALL process the three presets one at a time
- **AND** it SHALL show the current preset, bake progress, completed presets, backend, and output size

#### Scenario: bake is cancelled or interrupted
- **WHEN** the user cancels or the editor interrupts an active bake
- **THEN** automatic advancement SHALL stop
- **AND** completed preset outputs SHALL remain intact
- **AND** the tool SHALL offer resume from the first incomplete preset

#### Scenario: final bake is requested before preview approval
- **WHEN** any preview bake or comparison validation is incomplete
- **THEN** the final-bake action SHALL remain disabled unless the user explicitly invokes a documented override

### Requirement: engine-authentic comparison captures
The tool SHALL capture four fixed review cameras for each of the three looks at 2560x1440 and SHALL record the active profile and LightingDataAsset for every output.

#### Scenario: comparison capture completes
- **WHEN** all three preview or final datasets are valid and comparison capture runs
- **THEN** exactly 12 PNGs SHALL be created with deterministic look-and-camera filenames
- **AND** a comparison index SHALL record look ID, camera, VolumeProfile, LightingDataAsset, bake tier, resolution, and validation status
- **AND** every image SHALL come from Unity's real URP camera rendering without generative alteration

### Requirement: bake and render validation
The system SHALL validate compilation, scene integrity, GI eligibility, UV2 availability, lightmap/probe outputs, camera/profile pairings, and capture completeness before reporting success.

#### Scenario: validation detects an invalid bake
- **WHEN** a preset has missing linked lightmaps, missing probes, invalid UV2, black/error materials, or a mismatched capture manifest
- **THEN** that preset SHALL be marked invalid
- **AND** the final-bake or final-capture sequence SHALL not report completion
