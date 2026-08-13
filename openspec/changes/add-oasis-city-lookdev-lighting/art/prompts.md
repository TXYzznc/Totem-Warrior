# Engine Capture Briefs

These are deterministic Unity capture briefs, not generative-image prompts. Validation PNGs must be rendered by OasisCity in Unity URP.

## WarmCinematic

Render OasisCity as a warm cinematic desert oasis: golden sun and sand highlights, subtly cooler shaded architecture, filmic highlight roll-off, restrained glow on genuinely bright surfaces, and a mild center emphasis. Preserve believable material colors and avoid orange clipping.

## NeutralRealistic

Render OasisCity under neutral natural daylight: balanced white point, restrained contrast and saturation, clean architectural readability, minimal bloom and vignette, and no decorative lens artifacts. Use this look as the material and lightmap reference baseline.

## BoldStylized

Render OasisCity with deliberate stylization: stronger warm/cool color separation, richer saturation, deeper but readable shadows, pronounced highlight energy, and a visible yet controlled vignette/bloom treatment. Avoid crushed blacks, neon sand, and chromatic aberration.

## Capture matrix

For each brief, capture the exact four cameras listed in `requirements.md` at 2560x1440 after the corresponding baked LightingDataAsset and VolumeProfile are active and validated.
