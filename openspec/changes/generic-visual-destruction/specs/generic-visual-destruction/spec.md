## ADDED Requirements

### Requirement: Static prop opt-in configuration
The system SHALL provide a reusable component that can be attached to a small static prop with one or more opaque Renderers. The component MUST expose a destruction material category, supported Renderer references, effect profile, and terminal visibility behavior. It MUST NOT require a per-prop broken mesh, debris prefab, damage-state texture, Rigidbody, or Collider.

#### Scenario: Configure a ceramic storage jar
- **WHEN** an author attaches the component to `INT-PROP-009` and selects the ceramic category
- **THEN** the jar can use the shared ceramic visual-destruction profile without assigning a broken-version asset.

#### Scenario: Missing supported renderer
- **WHEN** the component is configured without a supported opaque Renderer
- **THEN** it MUST report a configuration warning and MUST NOT attempt to play the Shader destruction path.

### Requirement: Explicit visual destruction trigger
The component SHALL expose an explicit API that accepts a world-space hit position, optional normalized strength, and an optional completion notification. The component MUST NOT read keyboard, mouse, or input actions directly. A destruction playback MUST ignore repeated trigger requests until its terminal state has been reset by its owner.

#### Scenario: Trigger from an attack hit
- **WHEN** a gameplay caller supplies a world-space hit position for an intact prop
- **THEN** the component MUST begin one destruction playback centered on that hit position.

#### Scenario: Repeated trigger during playback
- **WHEN** the component receives another trigger before the current playback reaches its terminal state
- **THEN** it MUST NOT start an additional concurrent playback or duplicate its VFX emission.

### Requirement: Shared Shader-driven body breakup
The system SHALL render the source opaque Renderer with a URP-compatible visual-destruction effect that uses shared noise and per-instance parameters to create a time-bounded breakup. The effect MUST combine a hit-position-centered reveal or clipping progression with bounded vertex displacement, and MUST preserve the original lit appearance before the terminal breakup phase.

#### Scenario: Ceramic jar visual breakup
- **WHEN** a ceramic prop begins full-quality playback
- **THEN** the body MUST visibly break up outward from the supplied hit position before the original Renderer reaches its configured terminal visibility state.

#### Scenario: Low-density mesh
- **WHEN** the source mesh has too few vertices for a readable displacement pattern
- **THEN** the effect MUST remain valid through its clipping and edge presentation without requiring additional mesh data.

### Requirement: Shared material-category VFX
The system SHALL provide shared visual-destruction VFX profiles for ceramic, wood, stone, and metal. Each profile MUST use reusable debris and dust resources and MUST NOT require geometry sampled from the destroyed prop. Ceramic VFX MUST include a light ceramic-dust or shard presentation and MAY include a shared glaze-color variation.

#### Scenario: Ceramic VFX emission
- **WHEN** a ceramic prop receives a full-quality destruction trigger
- **THEN** the system MUST emit the shared ceramic debris and dust profile at the trigger position.

#### Scenario: Reuse across different ceramic props
- **WHEN** two ceramic props with different meshes are destroyed
- **THEN** both MUST use the same shared ceramic VFX resources rather than requiring mesh-specific debris assets.

### Requirement: Global quality budget and fallback
The system SHALL enforce a configurable global budget for concurrent visual-destruction effects. It MUST select one of full, simplified, or no-VFX playback before emission. Simplified playback MUST preserve a visible body-breakup result while reducing VFX cost; no-VFX playback MUST finish without spawning debris or dust.

#### Scenario: Full budget available
- **WHEN** a trigger occurs while a full-quality budget token is available
- **THEN** the system MUST play Shader breakup, category debris, and dust.

#### Scenario: Simplified budget available
- **WHEN** full-quality budget is exhausted and a simplified token is available
- **THEN** the system MUST play Shader breakup with reduced VFX according to the selected profile.

#### Scenario: Budget exhausted
- **WHEN** all visual-destruction budget tokens are exhausted
- **THEN** the system MUST complete using the no-VFX fallback and MUST NOT allocate new debris or dust instances.

### Requirement: Renderer and object lifecycle isolation
The visual-destruction component SHALL not destroy its GameObject. At playback completion it MUST apply its configured terminal behavior, limited to hiding its managed Renderers, disabling its GameObject, or notifying its owner. It MUST release any acquired global budget token and reusable VFX resources.

#### Scenario: Owner-managed pooling
- **WHEN** a component is configured to notify its owner on completion
- **THEN** it MUST invoke the completion notification without destroying the GameObject, allowing the owner to return it to an existing pool.

#### Scenario: Renderer hide terminal behavior
- **WHEN** a component is configured to hide its Renderers on completion
- **THEN** it MUST hide only its managed Renderers and release its active budget token.

### Requirement: Unsupported renderer fallback
The system SHALL treat transparent materials, Skinned Mesh Renderers, Terrain, and renderers whose materials do not support the visual-destruction Shader as unsupported in the first release. Unsupported inputs MUST use a safe terminal fallback without material instantiation or rendering errors.

#### Scenario: Unsupported transparent prop
- **WHEN** an author triggers a component on a transparent Renderer
- **THEN** the system MUST skip the Shader breakup path and complete through its configured safe fallback.

#### Scenario: Mixed renderer object
- **WHEN** a configured object contains both supported and unsupported Renderers
- **THEN** the system MUST apply Shader breakup only to supported Renderers and use safe fallback behavior for unsupported Renderers.
