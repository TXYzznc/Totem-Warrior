# Actor Common M02 Unity integration record

Date: 2026-07-13

## Source and validation

- The only input directory was `art/raw/characters/actor_common_m02/`.
- Independent validation found exactly 96 cut frames: Idle 16, Walk 24, Attack 24, Death 32.
- Every frame is 512 x 512 RGBA, has transparent corners, has no detected chroma-green residual, and shares a lowest visible alpha row at y=511.

## Runtime assets

- Sprite directory: `Assets/Game/Sprite/Actors/ActorCommonM02/` (96 Sprites, never a retired legacy art directory).
- Animation directory: `Assets/Game/Animation/Actors/ActorCommonM02/` (16 directional clips and `ActorCommonM02.controller`).
- Sprite import: PPU 512, custom bottom foot pivot `(0.5, 1/512)`, alpha transparency, clamp wrap, no mipmaps.
- Controller parameters are exactly the five parameters already driven by `TotemActorService`: `IsMoving`, `Direction`, `AttackTrigger`, `Die`, and `Dead`.
- `Player.prefab`, `SmartAI.prefab`, and `LightAI.prefab` have the same first Idle Sprite and the same shared controller. Their SpriteRenderer colour is white.

## Faction-indicator boundary

The M02 PNGs, clips, and prefabs do not bake in faction colours or a foot-ring. Runtime now creates a single reusable `TotemFactionRing` child per participant instance: Player / all selected-player keys blue `#34A6FF`, SmartAI red `#FF5940`, LightAI yellow `#FFC040`. The ring uses a cached runtime Sprite, stays one sorting order below the body, and is not created for Boss or NPC instances. The related runtime catalog entries remain neutral white so no body SpriteRenderer is faction-tinted.

## Unity verification

- UnitySkills instance: `http://localhost:8091/`.
- `Game/Totem/Art/Import Actor Common M02` completed successfully.
- `Game/Totem/Art/Validate Actor Common M02 Import` completed successfully.
- Follow-up Unity diagnosis reported no compiler errors and no console errors.

## Full-diagnostic follow-up

- The latest 8091 full report is `GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260713_165702.json`.
- It passed both `Totem Actor Visual Runtime` and `Totem Runtime Assets`: the independent helper test confirmed one reusable ring, red `#FF5940` reassignment, body/ring/shadow orders `9500/9499/9498`; the spawned runtime had 50 participant rings and no Boss ring.
- The runtime-asset scenario also instantiated Player / SmartAI / LightAI, Boss, tattooist, and merchant, proving neutral-white body renderers, the three required ring colours, and absence of rings on Boss/NPC. The report has five unrelated pre-existing workspace/migration failures; none concern faction rings, M02 art paths, prefab references, or compilation.
