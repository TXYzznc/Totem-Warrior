## 1. Baseline and contracts

- [x] 1.1 Capture the current dirty-worktree baseline, latest diagnostics, runtime service order and all `TotemActorKind`/`IsEnemy`/`aliveEnemyCount` consumers without reverting existing startup-protection work.
- [x] 1.2 Add the shared Combatant, Participant, Enemy, lifecycle, relationship-decision, event and snapshot contracts required by the design.
- [x] 1.3 Implement the pure `TotemCombatRelationshipService` matrix with structured reason codes and EditMode diagnostics for readiness protection, the 60-second Participant combat grace period and enemy friendly-fire rules.

## 2. Participant domain migration

- [x] 2.1 Refactor `TotemActorService` into participant-only roster, spawn, movement, health and lifecycle ownership for 1 Human + 20 SmartBot + 29 LightBot.
- [x] 2.2 Remove EnemyConfig/EnemyTier/Boss fields from participant spawn and model paths; replace `TotemActorKind` control semantics with `TotemParticipantControllerKind`.
- [x] 2.3 Refactor `TotemAIService` to control SmartBot and LightBot participants only while preserving the five confirmed Smart AI personalities and equal participant permissions.
- [x] 2.4 Implement Reserved/Loading/Protected/Active/Eliminated/Disconnected readiness, local Ready provider, 5-second protection cancellation through InputModule and configurable 90-second timeout.
- [x] 2.5 Implement the global 60-second Participant-vs-Participant combat grace period; NPC enemies may still damage Active Participants during this window, and Participant combat becomes valid at 60 seconds.

## 3. Enemy data and generated catalogs

- [x] 3.1 Extend EnemyConfig and BossPhaseConfig and add EnemyAbilityConfig, EncounterSpawnConfig and EnemyLootConfig Business JSON/xlsx/C# DataTable schemas.
- [x] 3.2 Author the 15 confirmed enemy definitions, reusable abilities, three Boss phase sets, theme/common pools, encounter schedules and tier loot tables.
- [x] 3.3 Extend gameplay catalog generation, normalization, fallback data and foreign-key validation for all new enemy data.
- [x] 3.4 Synchronize AI JSON to xlsx without backup copies, regenerate DataTable C# and runtime catalog products, and prove JSON/xlsx/catalog freshness.
- [x] 3.5 Add all enemy RuntimeAssetKey entries to `TotemRuntimeAssetCatalog` and provide explicit Theme/Tier fallback assets for missing final art.

## 4. Native Enemy runtime

- [x] 4.1 Implement `TotemEnemyService` for enemy registry, spawn/despawn, health, damage integration, death idempotence and independent snapshots.
- [x] 4.2 Implement `TotemEnemyControllerBase` and legal FSM transitions with complete GFTrace state causality.
- [x] 4.3 Implement fixed-capacity threat tracking, equal Human/SmartBot/LightBot target selection, 1.25 switch hysteresis, leash and group alert.
- [x] 4.4 Implement reusable Melee, Projectile, Charge, Leap, Beam, ConeSweep, AreaPulse, HazardZone, Shield, Summon, Regenerate, DeathBurst and PhaseTransition abilities.
- [x] 4.5 Implement Light, Elite and Boss controller policies plus the unique split/summon lifecycle behavior needed by the three Bosses.
- [x] 4.6 Implement nearest-active-participant Hot/Warm/Cold LOD, no-progress path fallback, path caching, per-frame path budget and zero-allocation Tick loops.
- [x] 4.7 Bind all 15 enemy definitions to their configured behavior/abilities and verify each has observably distinct runtime behavior.

## 5. PCG encounter lifecycle

- [x] 5.1 Upgrade EnemySpawn anchors into deterministic encounter anchors while keeping PCG free of GameObject instantiation side effects.
- [x] 5.2 Implement pure deterministic SpawnPlan construction from map, theme, encounter config, enemy pools and seed.
- [x] 5.3 Enforce walkability, minimum participant distance, same-wave spacing, theme filtering and deterministic relocation/rejection.
- [x] 5.4 Implement world-clock waves: initial Light population, capped Light replenishment, finite Elite spawns from 240 seconds and one Boss at 600 seconds.
- [x] 5.5 Preserve 400m world scale and spawn distances in both quick diagnostic PCG and full PCG modes.

## 6. Combat and consumer integration

- [x] 6.1 Route direct, charged, skill, tattoo, status, weapon-trait, projectile, terrain and zone damage through the shared relationship policy.
- [x] 6.2 Replace player-only Enemy/Boss attack paths with Enemy abilities targeting any valid active participant.
- [x] 6.3 Replace `IsEnemy`, SmartAI-as-Elite and Boss-as-Actor consumers in Weapon, Tattoo, Status, Skill, Economy, Audio, VFX, Camera and Interaction services.
- [x] 6.4 Replace win-on-enemy-clear with last-participant-standing resolution, winner identity and correct local victory/defeat results.
- [x] 6.5 Update CombatHUD and runtime snapshots to display alive participants, monster pressure and Boss state as independent values.

## 7. Enemy loot and progression

- [x] 7.1 Implement `TotemEnemyLootService` and public `TotemLootPickupModel` generation independent from participant death chests.
- [x] 7.2 Implement guaranteed and weighted Light/Elite/Boss loot, deterministic rolls and equal pickup permissions for Human/SmartBot/LightBot.
- [x] 7.3 Move Boss rewards from victory-time claims to immediate death-time world drops.
- [x] 7.4 Persist newly picked Boss recipes through `TotemMetaProgressService` and convert duplicates into two configured high-tier paints.
- [x] 7.5 Preserve participant death-chest inheritance and prove enemy deaths never use that formula.

## 8. Diagnostics and regression repair

- [x] 8.1 Add pure diagnostics for domain counts, relationship matrix, readiness, timeout, threat switching, FSM legality, ability timing and victory.
- [x] 8.2 Add data diagnostics for 15 enemy definitions, all foreign keys, three themes, Boss phases, loot guarantees and runtime asset fallback evidence.
- [x] 8.3 Add integration diagnostics for deterministic SpawnPlan, spawn safety, wave lifecycle, enemy combat, public loot and recipe persistence.
- [x] 8.4 Update all existing diagnostics that depend on `TotemActorKind`, `IsEnemy`, `aliveEnemyCount`, SmartAI-as-Elite, Boss Actor or enemy-clear victory semantics.
- [x] 8.5 Add a quick-PCG PlayMode combat smoke and a full-PCG final smoke covering Ready, Light, Elite, Boss phases, loot and participant winner resolution.

## 9. Verification and documentation closure

- [x] 9.1 Compile Unity 2022.3.62f3 with zero C# errors and inspect Console for new warnings/exceptions.
- [x] 9.2 Run GF_X `totem_diagnostics_run_all` on port 8092 and resolve every failure or warning introduced by this change.
- [x] 9.3 Run quick and full PlayMode smoke, verify scene residue cleanup and ensure no persistent runtime-generated objects remain in Launch.unity.
- [x] 9.4 Measure Enemy AI LOD/path counters and prove zero per-frame managed allocation in the tested combat loop.
- [x] 9.5 Update OpenSpec completion evidence, project summary, feature/asset manifests and knowledge-base index without reviving stale GDD claims.
- [x] 9.6 Run strict OpenSpec validation and a requirement-by-requirement completion audit before marking the change complete.
