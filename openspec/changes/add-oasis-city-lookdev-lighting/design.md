## Context

OasisCity runs on Unity 2022.3.62f3c1 with URP 14.0.12. The active `URP-Balanced` asset has HDR enabled, 1x MSAA, 50 m shadow distance, no required depth/opaque textures, and one active downsampled SSAO Renderer Feature. A separate `URP-HighFidelity` asset already provides 4x MSAA, 4096 shadow maps, and 150 m shadow distance.

The scene has 14 deterministic review cameras but no Volume, VolumeProfile, LightingSettings, LightingDataAsset, lightmaps, Light Probes, or Reflection Probes. It has one realtime directional light. About 787 generated environment objects carry static flags that exclude Contribute GI, and all 55 OasisCity FBX model importers currently disable secondary UV generation.

The requested workflow is editor-only and promotional-capture oriented. It must compare three distinct looks, each with fully baked lighting and its own post-processing, from four representative cameras. Preview bakes must precede final-quality bakes.

## Goals / Non-Goals

**Goals:**

- Produce `WarmCinematic`, `NeutralRealistic`, and `BoldStylized` looks with independent baked lighting and Volume profiles.
- Switch a look's VolumeProfile, LightingDataAsset, environment, and source-light configuration atomically from the existing camera toolbox.
- Bake all three looks sequentially with progress, cancellation, failure recovery, and deterministic asset paths.
- Prepare geometry, UV2, probes, and lightmap density for a large outdoor city without relying on realtime lights.
- Capture 12 engine-authentic 2560x1440 PNG comparisons from four fixed review cameras.
- Preserve the user's current quality level, lighting data, camera states, scene dirtiness where possible, and active look when temporary operations end.

**Non-Goals:**

- PPv2, `PostProcessLayer`, `OnRenderImage`, custom URP Render Features, or custom full-screen shaders.
- Runtime look selection, runtime UI, InputModule changes, day/night blending, or gameplay-facing lighting scenarios.
- Unity 6 APV Lighting Scenarios; this project remains on Unity 2022.3.
- AI-generated or AI-retouched validation images.
- Baking every decorative placeholder or navigation/debug object.

## Decisions

### Decision: one editor catalog with three deterministic preset assets

Create an editor-only `OasisLookDevCatalog` ScriptableObject containing exactly three `OasisLookPreset` entries. Each entry references:

- stable look ID and display name;
- VolumeProfile;
- preview and final LightingSettings;
- baked LightingDataAsset after a successful bake;
- baked sun rotation/color/intensity and ambient/fog/sky settings;
- output directory and bake/capture validation metadata.

The catalog is configuration, not a database. Generated bake state and progress live in an editor-only `ScriptableSingleton` under `Library`, preventing transient job data from polluting source control.

Alternatives rejected:

- Three scene copies would duplicate the large OasisCity hierarchy and drift from the map builder.
- A runtime MonoBehaviour would broaden scope and make look-development state ship with gameplay.
- Name-based asset discovery is brittle and cannot prove that a LightingDataAsset matches its profile.

### Decision: temporary, atomic editor look switching

The camera-group panel gains a `LookDev / 烘焙` section with three explicit look buttons, active status, restore, bake, cancel, and capture controls. Selecting a look performs one editor transaction:

1. snapshot the active render-pipeline/quality context, LightingDataAsset, sun/environment values, and any temporary Volume;
2. select `URP-HighFidelity` for the preview session without editing the shared asset;
3. apply the preset's baked sun/environment values;
4. assign the preset LightingDataAsset;
5. create or reuse a hidden editor-only global Volume and assign the preset VolumeProfile;
6. repaint Scene/Game views.

Restore, panel disable, assembly reload, scene save, and play-mode transition restore the snapshot. Switching between looks retains the original snapshot, matching the existing reversible Game View camera-switch behavior.

### Decision: three independent fully baked datasets

Every preset uses a Baked directional sun; there is no realtime or mixed direct-light contribution. Static receivers get direct and indirect lighting from lightmaps. Non-lightmapped/dynamic renderers receive baked lighting through Light Probes but do not cast realtime shadows. This is acceptable because the target is editor promotional capture rather than gameplay lighting.

Unity 2022.3 exposes `Lightmapping.lightingDataAsset` and `Lightmapping.BakeAsync`, so one scene can store three independent LightingDataAssets and switch them in the editor. The sequential baker applies one preset, assigns its LightingSettings, detaches the previous LightingDataAsset without deleting it, starts an on-demand bake, moves the resulting LightingDataAsset and linked lightmaps/reflection cubemaps into that preset's deterministic directory, records references, validates, then advances to the next preset.

The baker MUST NOT call `ClearLightingDataAsset` on a retained preset because that can delete linked bake assets. It uses asset moves that preserve GUIDs and explicit reference reassignment.

### Decision: two-stage bake quality

Each preset has preview and final LightingSettings:

| Setting | Preview | Final |
|---|---:|---:|
| Lightmapper | Progressive GPU, CPU fallback | Progressive GPU, CPU fallback |
| Lightmap max size | 2048 | 4096 |
| Lightmap resolution | 2 texels/unit baseline | 5 texels/unit baseline |
| Direct samples | 32 | 128 |
| Indirect samples | 128 | 512 |
| Environment samples | 64 | 256 |
| Bounces | 2 | 4 |
| Directional mode | Directional | Directional |
| Ambient occlusion | Enabled, restrained | Enabled, restrained |
| Compression | Normal | High quality/normal platform-safe compression |

Per-renderer `scaleInLightmap` controls atlas pressure: hero buildings and four capture neighborhoods receive higher density; terrain, distant walls, roads, repeated buildings, and large low-frequency surfaces receive lower density. Final bake is unavailable until all three preview bakes and the 12 preview captures pass validation or the user explicitly overrides the gate.

### Decision: prepare UV2 and Contribute GI through the builder/import pipeline

- Enable `generateSecondaryUV` for the 55 OasisCity FBX importers and reimport once.
- Add Contribute GI to the generated environment static flags for terrain, roads, walls, bridges, buildings, and eligible decorations.
- Exclude review cameras, lights, gameplay/navigation markers, placeholder-only objects, and non-rendering helpers.
- Generate secondary UVs for builder-created Mesh assets before saving them.
- Configure renderer receive-GI and scale-in-lightmap values deterministically from category/name so rebuilding OasisCity preserves bake eligibility.

This is preferred over hand-editing the scene because the map is builder-owned.

### Decision: bounded probes for a large outdoor scene

Create a sparse road/district Light Probe lattice rather than a dense map-wide grid. Initial target is approximately 400–700 probes across three vertical layers, pruned from solid geometry and outside the navigable city envelope. Create six baked Reflection Probes covering north, central, south, river, west boundary, and east boundary regions at 256 resolution with overlap/blend.

Probe creation is deterministic and builder-owned. The bake validation rejects missing tetrahedralization, probes inside geometry above a threshold, or absent reflection results.

### Decision: restrained built-in URP effects only

All profiles use built-in URP Volume overrides. Motion Blur, Depth Of Field, Chromatic Aberration, Lens Distortion, and Panini Projection remain disabled for the comparison baseline because they hide geometry/lightmap defects or vary undesirably across cameras.

Initial tuning targets:

| Parameter | WarmCinematic | NeutralRealistic | BoldStylized |
|---|---:|---:|---:|
| Tonemapping | ACES | ACES | ACES |
| Post exposure | +0.15 | 0.00 | +0.05 |
| Contrast | +12 | +4 | +24 |
| Saturation | +6 | 0 | +18 |
| White balance temperature | +18 | 0 | +8 |
| White balance tint | -3 | 0 | -5 |
| Bloom intensity | 0.45 | 0.12 | 0.80 |
| Bloom threshold | 1.10 | 1.20 | 0.90 |
| Vignette intensity | 0.18 | 0.06 | 0.28 |

`BoldStylized` additionally uses built-in split toning or shadows/midtones/highlights for warm highlights and restrained cool shadows. Parameters are starting points, not immutable art values; adjustments require regenerating the comparison index but do not require rebaking unless sun/environment inputs change.

### Decision: lighting directions remain visually distinct

- `WarmCinematic`: golden baked sun, warm sand bounce, subtly cool shaded ambient, mild atmospheric fog.
- `NeutralRealistic`: neutral daylight, conservative ambient gradient, minimal color cast; canonical material/lightmap diagnostic look.
- `BoldStylized`: stronger warm sun/cool ambient separation, deeper readable shadows, more saturated atmosphere.

Changing sun rotation, color, intensity, environment lighting, sky material, or emissive bake inputs invalidates only that preset's LightingDataAsset. Changing only Volume parameters invalidates captures but not the bake.

### Decision: capture matrix and validation manifest

The panel captures these cameras for every look:

- `CAM_Overview_SouthEast`
- `CAM_District_Central`
- `CAM_Building_Tower_BF01`
- `CAM_River_Bridge03`

Output is 12 PNGs at 2560x1440, named `{look_id}_{camera_name}_2560x1440.png`, plus a JSON/Markdown index containing asset GUIDs, bake tier, capture size, active camera, profile, LightingDataAsset, and validation results. Capture uses Unity URP rendering; no image-generation tool participates.

## Risks / Trade-offs

- [55 model reimports can alter mesh data or take significant time] → preview importer diffs, batch once, verify renderer/mesh counts and representative prefabs before scene rebuild.
- [Full-city lightmaps can consume large disk/memory] → category density policy, preview bake gate, per-preset size report, and cancellation threshold before final bake.
- [UV overlap, seams, or black meshes] → secondary-UV validation and four-camera preview review before final quality.
- [Baked-only lighting gives dynamic objects no realtime shadows] → use Light Probes for illumination and document the promotional-capture boundary.
- [Switching LightingDataAsset can dirty or corrupt the scene reference] → snapshot/restore, Undo-compatible assignments where possible, deterministic asset directories, and never delete retained linked assets.
- [Bake interrupted by editor/domain reload] → persist job state, stop automatic advancement, retain completed presets, and offer Resume from the next incomplete preset.
- [Progressive GPU unsupported or out of memory] → fall back to Progressive CPU and report the backend used in the manifest.
- [Three looks multiply bake duration] → mandatory preview tier and explicit final-bake gate.
- [SSAO double-darkens baked AO] → lower/disable screen-space SSAO per profile after preview inspection; baked AO remains the baseline.
- [HighFidelity pipeline selection leaks outside the tool] → snapshot and restore the previous quality/render pipeline on every exit path.

## Migration Plan

1. Add editor catalog/preset assets, three VolumeProfiles, preview/final LightingSettings, and deterministic output directories.
2. Extend the builder/import pipeline for UV2, Contribute GI, density, baked sun variants, Light Probes, Reflection Probes, and post-processing-capable review cameras.
3. Reimport representative models, validate, then batch-reimport all 55 OasisCity FBX assets.
4. Rebuild/save OasisCity and validate object, renderer, camera, probe, and GI eligibility counts.
5. Extend the camera panel with reversible look switching and async sequential bake state.
6. Run three preview bakes sequentially, validate generated assets, and capture the 12-image preview matrix.
7. Stop for visual review. After approval, run three final bakes and regenerate the 12 final captures.
8. Roll back by restoring importer flags, builder flags, scene changes, and removing only the dedicated generated look-development assets; never clear unrelated project lighting data.

## Open Questions

None. The user selected four representative cameras, three independent fully baked looks, editor-tool integration, and preview-before-final processing.
