## Why

OasisCity currently renders through URP 14 without a Volume profile, assigned LightingSettings, generated lightmaps, or a repeatable look-development workflow. High-quality promotional captures require deterministic post-processing, baked lighting, and a safe way to compare multiple visual directions from representative cameras.

## What Changes

- Add three OasisCity-specific look presets: warm cinematic oasis, neutral realistic, and strongly stylized.
- Give every preset its own URP VolumeProfile, fully baked sun/environment configuration, LightingSettings, and LightingData/Lightmap result.
- Extend the camera-group toolbox with preset switching, sequential preview/final bake controls, cancellation, progress, validation, and capture actions.
- Enable post-processing on generated review cameras and switch both VolumeProfile and LightingDataAsset without entering Play Mode.
- Prepare eligible OasisCity geometry for baked GI by enabling Contribute GI and secondary lightmap UVs, with explicit exclusions and density controls for oversized surfaces.
- Add baked Light Probes and Reflection Probes so non-lightmapped renderers and reflective materials receive stable scene lighting.
- Generate a 4-camera by 3-look, 2560x1440 comparison set from the real Unity render output.
- Keep all changes editor-only and OasisCity-scoped; do not add runtime input, runtime look switching, PPv2, or `OnRenderImage` code.

## Capabilities

### New Capabilities

- `oasis-city-lookdev-lighting`: Scene-scoped URP look presets, independent baked-lighting datasets, editor switching/baking workflow, and deterministic comparison captures for OasisCity.

### Modified Capabilities

None.

## Impact

- `Assets/Game/Editor/OasisCityMapBuilder/`: generated cameras, baked-GI eligibility, lights, probes, and look-development setup.
- `Assets/Game/ScriptsBuiltin/Editor/MigratedToolbox/`: camera-tool look selection and bake/capture workflow.
- `Assets/Game/Models/Environment/OasisCity/**/*.fbx.meta`: secondary UV generation for lightmapped meshes.
- `Assets/Game/Scene/OasisCity.unity` and a dedicated generated look-development asset directory: VolumeProfiles, LightingSettings, LightingDataAssets, lightmaps, probes, and comparison captures.
- `Assets/Settings/URP-HighFidelity.asset` is selected only through a reversible editor preview path; shared URP assets are not destructively retuned.
- Depends on the unarchived `add-oasis-camera-group-tool` change for the camera panel and 14 review viewpoints.
- No runtime API, InputModule, gameplay scene flow, package dependency, or AI-generated art asset changes.
