## 1. Look configuration assets

- [x] 1.1 Add editor-only OasisLookDevCatalog/OasisLookPreset configuration types with validation and stable IDs.
- [x] 1.2 Create WarmCinematic, NeutralRealistic, and BoldStylized VolumeProfiles using only supported URP 14 overrides.
- [x] 1.3 Create preview/final LightingSettings assets and deterministic per-look bake output folders.
- [x] 1.4 Add automated tests for catalog completeness, distinct asset references, disabled baseline lens effects, and parameter ordering.

## 2. Baked-GI preparation

- [x] 2.1 Preview and apply secondary-UV importer changes to the 55 OasisCity FBX assets, then verify representative mesh/prefab integrity.
- [x] 2.2 Update the OasisCity builder to set Contribute GI, receive-GI, and category lightmap density while excluding non-rendering/tool objects.
- [x] 2.3 Generate secondary UVs for builder-created lightmapped Mesh assets before saving them.
- [x] 2.4 Generate the bounded three-layer Light Probe layout and six named baked Reflection Probes.
- [x] 2.5 Update review-camera generation so every camera supports URP post-processing without enabling runtime look switching.
- [ ] 2.6 Rebuild/save OasisCity and validate renderer counts, GI flags, UV2, probes, cameras, missing references, and Undo/rebuild determinism.

## 3. Look switching and bake orchestration

- [x] 3.1 Implement reversible editor look application for VolumeProfile, LightingDataAsset, baked sun/environment, and high-fidelity pipeline context.
- [x] 3.2 Extend the camera-group panel with three look buttons, active/bake-tier status, restore, sequential preview/final bake, cancel, resume, and capture controls.
- [x] 3.3 Implement persistent editor bake-job state and safe event cleanup across cancellation, scene save, play-mode transition, and assembly reload.
- [x] 3.4 Implement sequential on-demand BakeAsync processing with GPU-to-CPU fallback and no overwrite/deletion of retained look datasets.
- [x] 3.5 Move generated LightingData/lightmap/reflection outputs into deterministic per-look directories and update catalog references atomically.
- [ ] 3.6 Add EditMode tests for look switching/restoration, sequence order, cancellation/resume, final-bake gate, and retained asset safety.

## 4. Preview bake and comparison gate

- [ ] 4.1 Run WarmCinematic preview bake and validate lighting data, linked maps, probes, logs, disk size, and four representative cameras.
- [ ] 4.2 Run NeutralRealistic preview bake and validate lighting data, linked maps, probes, logs, disk size, and four representative cameras.
- [ ] 4.3 Run BoldStylized preview bake and validate lighting data, linked maps, probes, logs, disk size, and four representative cameras.
- [ ] 4.4 Capture the 12 preview PNGs at 2560x1440 and generate the comparison manifest/index.
- [ ] 4.5 Review captures for clipping, seams, UV overlap, black meshes, blotches, bloom haze, material readability, and water separation; record required tuning.
- [ ] 4.6 Pause for user approval of the preview comparison before any final bake.

## 5. Final bake and delivery

- [ ] 5.1 Apply approved profile/light/density tuning without invalidating unrelated completed datasets.
- [ ] 5.2 Run and validate the three final-quality bakes sequentially with cancellation and resume available.
- [ ] 5.3 Capture the 12 final PNGs and regenerate the comparison manifest/index with final asset GUIDs.
- [ ] 5.4 Run targeted EditMode tests, scene validation, compilation checks, disk-size report, and required GF_X diagnostics.
- [ ] 5.5 Verify implementation against proposal, design, specs, art requirements, and all completed OpenSpec tasks.
