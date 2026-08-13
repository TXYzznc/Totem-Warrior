## Why

OasisCity currently has only one review camera, which makes it slow to compare lighting, composition, draw distance, and material rendering from representative city locations. The Unity development toolbox also lacks a unified view of all cameras and their scene-level grouping.

## What Changes

- Add a camera-group panel to the existing Unity development toolbox.
- List all cameras in loaded scenes, including inactive objects, with their group, transform, projection, clipping, depth, target display, and enabled state.
- Use the camera's parent under a `CameraGroups` hierarchy as the persisted group model, with Undo-supported regrouping.
- Provide separate actions for aligning Scene View to a camera and temporarily switching Game View to a camera.
- Provide a per-camera inline Game View switch so review does not require selecting the camera first.
- Cache filtered scene/group sections and group popup data to avoid rebuilding LINQ enumerations on every IMGUI event.
- Restore camera enabled states after a temporary Game View switch ends or the panel is disabled.
- Generate multiple representative review-camera groups and viewpoints in OasisCity, including building close-ups.
- Add editor tests for camera discovery, grouping, temporary switching, and restoration.

## Capabilities

### New Capabilities

- `scene-camera-review-tool`: Editor-only camera discovery, hierarchy grouping, data inspection, Scene View preview, Game View temporary switching, and OasisCity review viewpoints.

### Modified Capabilities

None.

## Impact

- `Assets/Game/ScriptsBuiltin/Editor/MigratedToolbox/`: new toolbox panel and editor-only tests.
- `Assets/Game/Editor/OasisCityMapBuilder/Editor/OasisCityMapBuilder.cs`: generated OasisCity review-camera hierarchy.
- `Assets/Game/Scene/OasisCity.unity`: regenerated or patched review cameras.
- No runtime assembly, input path, package dependency, or public gameplay API changes.
