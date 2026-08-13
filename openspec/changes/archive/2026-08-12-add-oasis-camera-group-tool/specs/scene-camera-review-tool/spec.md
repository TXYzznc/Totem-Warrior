## ADDED Requirements

### Requirement: loaded-scene camera inventory
The Unity development toolbox SHALL expose an editor-only camera panel that lists every Camera belonging to a valid loaded scene, including cameras on inactive GameObjects, and SHALL display its scene, hierarchy group, transform, projection, clipping planes, depth, target display, and enabled state.

#### Scenario: inactive camera is visible
- **WHEN** a loaded scene contains an inactive GameObject with a Camera component
- **THEN** the camera SHALL appear in the inventory with its inactive and disabled state visible

#### Scenario: non-scene camera is excluded
- **WHEN** Unity has preview or persistent editor Camera objects that do not belong to a valid loaded scene
- **THEN** those objects SHALL not appear in the inventory

### Requirement: hierarchy-backed camera grouping
The panel SHALL derive a camera's group from its parent beneath a scene-local `CameraGroups` hierarchy and SHALL allow the user to move a camera between groups with Unity Undo support.

#### Scenario: camera is regrouped
- **WHEN** the user selects a destination group for a camera
- **THEN** the camera Transform SHALL be reparented beneath that group and the scene SHALL be marked dirty
- **AND** the operation SHALL be reversible with Unity Undo

#### Scenario: camera has no camera-group ancestor
- **WHEN** a camera is not beneath a `CameraGroups` hierarchy
- **THEN** the panel SHALL show it in an explicit ungrouped category

### Requirement: separate Scene View preview
The panel SHALL provide an explicit Scene View preview action that aligns the active Scene View with the selected camera without changing any Camera enabled state.

#### Scenario: preview perspective camera
- **WHEN** the user invokes Scene View preview for a perspective camera
- **THEN** the Scene View SHALL use the camera position, orientation, and field-of-view-equivalent size
- **AND** all scene Camera enabled states SHALL remain unchanged

#### Scenario: preview orthographic camera
- **WHEN** the user invokes Scene View preview for an orthographic camera
- **THEN** the Scene View SHALL enter orthographic mode and use the camera's orthographic size

### Requirement: reversible Game View camera switch
The panel SHALL provide an explicit temporary Game View switch that enables only the selected scene camera, retains the original enabled-state snapshot across repeated switches, and exposes restoration.

#### Scenario: switch directly from camera inventory
- **WHEN** the user invokes the inline Game View switch on any listed camera row
- **THEN** the target camera SHALL become the temporary Game View camera without requiring prior row selection
- **AND** the active row action SHALL visibly indicate the current temporary camera

#### Scenario: switch Game View camera
- **WHEN** the user invokes Game View switch for a listed camera
- **THEN** that camera SHALL be enabled and every other listed scene camera SHALL be disabled
- **AND** the panel SHALL indicate that a temporary switch session is active

#### Scenario: restore original camera states
- **WHEN** the user invokes restore or the panel is disabled or scripts are about to reload
- **THEN** every snapshotted camera that still exists SHALL return to its original enabled state

### Requirement: allocation-conscious camera inventory rendering
The panel SHALL cache filtered scene/group sections and selected-scene group options instead of rebuilding LINQ groupings on every IMGUI layout or repaint event.

#### Scenario: repaint unchanged inventory
- **WHEN** Unity redraws the panel without a hierarchy or search change
- **THEN** the panel SHALL reuse its cached list sections and group option array

### Requirement: OasisCity representative review cameras
The OasisCity builder SHALL generate scene-persisted review-camera groups covering city overview, district composition, water, boundary rendering, and representative building close-ups, with exactly one default active review camera.

#### Scenario: rebuild OasisCity review hierarchy
- **WHEN** the complete OasisCity map is rebuilt
- **THEN** `99_Review/CameraGroups` SHALL contain purpose-named groups and multiple deterministic review cameras
- **AND** the `建筑特写` group SHALL contain deterministic viewpoints targeting multiple named building types
- **AND** exactly one generated review camera SHALL be enabled and tagged `MainCamera`
