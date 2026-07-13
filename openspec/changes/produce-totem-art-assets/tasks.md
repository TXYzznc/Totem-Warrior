## 1. Production setup

- [x] 1.1 Finalize `art/requirements.md`, `art/prompts.md`, raw output directories, and generation record template from the approved production brief.
- [x] 1.2 Create a non-destructive chroma-key removal and alpha validation pass for project-bound character and NPC images.
- [ ] 1.3 Prepare reference sheets for existing tattooist and merchant assets.

## 2. Character and Boss preproduction

- [x] 2.1 Generate and review the `actor_common` concept illustration with six unobstructed tattoo placement zones and no baked tattoos.
- [x] 2.2 Generate and review `actor_common` front, back, left, and right turnarounds using the approved concept as reference.
- [x] 2.3 Generate and review the AI ruins warden Boss concept illustration and four turnarounds.
- [x] 2.4 Generate `player_2` and `player_3` placeholder half-body portraits.

## 3. Character animation production

- [x] 3.1 Produce and cut `actor_common_m02` idle, walk, attack, and death animations for down, up, left, and right directions.
- [x] 3.2 Produce and cut Boss idle, walk, attack, and death animations for down, up, left, and right directions.
- [x] 3.3 Validate each cut frame for dimensions, alpha, consistent subject, frame count, and shared foot pivot.

## 4. NPC static production

- [x] 4.1 Produce a full-body static tattooist Sprite consistent with the existing purple-tattoo portrait reference.
- [x] 4.2 Produce a full-body static merchant Sprite consistent with the existing warm-copper coin portrait reference.
- [x] 4.3 Remove chroma keys, validate alpha edges, and prepare NPC world-Sprite imports.

## 6. Unity integration

- [x] 6.1 Import final actor, Boss, NPC, and portrait assets into their non-legacy runtime directories.
- [x] 6.2 Create animation clips and AnimatorControllers with `Direction`, `IsMoving`, `AttackTrigger`, `Die`, and `Dead` parameters; bind Player and Boss prefabs.
- [x] 6.2.a Import and bind the shared `ActorCommonM02` controller to Player, SmartAI, and LightAI; see `art/integration/actor_common_m02_integration.md`.
- [x] 6.3 Replace actor full-Sprite tint with player / SmartAI / LightAI foot-ring indicators; verified by `gf-diagnostics-run-all_20260713_165702.json`.
- [x] 6.4 Update runtime asset catalog, required keys, asset index, and generation record with final paths.
- [x] 6.4.a Update shared participant catalog entries to neutral M02 visuals and rebuild the AI art-asset index.

## 7. Verification

- [x] 7.1 Verify no new asset is placed in a deleted legacy art directory.
- [x] 7.2 Verify actor and NPC prefabs have valid visual references, and Player/Boss have valid AnimatorControllers.
- [x] 7.3 Run GF_X diagnostics; record remaining non-art failures separately in `art/integration/validation_2026-07-13.md`.
