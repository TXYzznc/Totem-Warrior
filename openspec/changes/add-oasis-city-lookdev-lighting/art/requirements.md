# OasisCity LookDev Render Requirements

## Goal

Produce engine-authentic comparisons of three URP lighting and post-processing looks for high-quality editor screenshots and promotional review.

## Required looks

1. `WarmCinematic`: warm sand/sun highlights, restrained cool shadows, filmic contrast, controlled bloom.
2. `NeutralRealistic`: neutral daylight white balance, conservative contrast/saturation, minimal lens styling.
3. `BoldStylized`: stronger warm/cool separation, richer saturation and contrast, more visible but controlled bloom/vignette.

## Required cameras

- City overview: `CAM_Overview_SouthEast`
- District composition: `CAM_District_Central`
- Building close-up: `CAM_Building_Tower_BF01`
- Water/boundary: `CAM_River_Bridge03`

## Output

- 12 PNG files at 2560x1440: four cameras for each of three looks.
- Deterministic names: `{look_id}_{camera_name}_2560x1440.png`.
- A comparison index recording look ID, camera, profile, LightingDataAsset, resolution, capture timestamp, and validation result.
- Captures must come from Unity's actual URP camera rendering. Generative image tools must not alter or fabricate validation images.

## Acceptance checks

- No clipped highlight regions dominating the sky or emissive surfaces.
- No visible lightmap seams, UV overlap artifacts, black lightmapped meshes, or strong denoiser blotches.
- No unintended bloom haze, chromatic fringe, heavy vignette, motion blur, or global depth-of-field blur.
- Building façades retain readable material separation in all three looks.
- Water/bridge view retains reflection and depth separation.
- Every capture records the intended look/profile/lighting dataset pairing.
