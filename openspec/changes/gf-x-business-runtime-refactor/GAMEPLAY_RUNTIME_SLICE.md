# Gameplay Runtime Slice

Date: 2026-07-07

## Scope

This slice starts the GF_X-native rewrite after the first UI chain:

```text
MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD -> gameplay runtime
```

The old runtime remains evidence only. The new path does not mount or call the old `GameApp`,
`ModuleRunner`, `EventBus`, `UIModule`, or `DataTableModule`.

## Old Effects Used As Evidence

- `MapGenModule`
  - deterministic 150m map placeholder as early migration evidence; the active
    runtime contract was later replaced by the fixed 400m / 40m room-footprint
    map spec recorded in this document and in `T13an`
  - 4 functional rooms: spawn, tattoo studio, merchant, boss room
  - initial shrink-zone center in the middle third of the map
  - runtime-generated simple geometry
- `SpawnerModule`
  - 1 player actor
  - 20 smart AI actors
  - 29 light AI actors
  - 1 boss actor
  - player and enemies originally used character prefabs with primitive fallback
- `InputModule`
  - all keyboard/mouse input is centralized
  - movement: WASD/arrow keys, normalized diagonal movement
  - attack: mouse left, held attack threshold 0.4s
  - skill: E
  - dodge: Space
  - interact: F
  - self tattoo: Tab
  - pause/back: Escape
- `CameraModule`
  - orthographic 2.5D camera
  - 55 degree tilt
  - orthographic size 9
  - late tick follow with map-bound clamping
- `CombatModule`
  - tick-driven controller loop
  - movement, attack, charged attack, skill, dodge
  - target selection skips dead targets

## GF_X-Native Implementation Added

- Runtime tick contracts:
  - `ITotemRuntimeTickService`
  - `ITotemRuntimeLateTickService`
- Runtime services:
  - `TotemMapService`
  - `TotemActorService`
  - `TotemEconomyService`
  - `TotemStatusService`
  - `TotemTattooService`
  - `TotemWeaponService`
  - `TotemChestService`
  - `TotemSkillService`
  - `TotemZoneService`
  - `TotemBossService`
  - `TotemAIService`
  - `TotemNpcService`
  - `TotemChoiceService`
  - `TotemInteractionService`
  - `TotemCameraService`
  - `TotemVfxService`
  - `TotemCombatService`
  - upgraded `TotemInputService`
- Models:
  - `TotemMapSnapshot`
  - `TotemRoomInfo`
  - `TotemMapAnchor`
  - `TotemActorSpawnInfo`
  - `TotemActorModel`
  - `TotemInputSnapshot`
  - `TotemCombatSnapshot`
  - `TotemTattooDefinition`
  - `TotemWeaponDefinition`
  - `TotemSkillDefinition`
  - `TotemStatusInstance`
  - `TotemZonePhase`
  - `TotemBossPhase`
  - `TotemAIActorState`
  - `TotemAISnapshot`
  - `TotemNpcModel`
  - `TotemChoiceOption`
  - `TotemInteractionSnapshot`

## Current Behavior

Entering `CombatHud` now triggers:

```text
TotemGameFlowService.CombatHud
  -> TotemMapService.GenerateMap(seed:1, themeId:1) with functional terrain and map anchors
  -> TotemActorService.SpawnActors(...) from player/Boss/EnemySpawn anchors
  -> TotemEconomyService registers actor inventories
  -> TotemTattooService applies startup color/pattern selection
  -> TotemWeaponService equips startup weapon
  -> TotemWeaponService spawns MapResource weapon pickups from Resource anchors
  -> TotemChestService spawns 2 common chests and 2 rare chests
  -> TotemSkillService equips two default skill slots
  -> TotemZoneService activates shrink-zone timing
  -> TotemBossService activates phase tracking
  -> TotemAIService builds 20 smart AI and 29 light AI controller states
  -> TotemNpcService spawns 3 tattooists and 2 merchants
  -> TotemInteractionService detects nearby NPCs and opens GF_X-native NPC overlays on F
  -> TotemCameraService.ActivateCombatCamera()
  -> TotemVfxService provides temporary attack/skill VFX markers
  -> TotemCombatService starts ticking
  -> TotemUIService opens CombatHUD
```

`TotemMapSnapshot.AnchorPlacements` is now the shared spawn/interactable placement contract for
the first-round runtime. The map produces seed-stable anchors for player spawn, Boss spawn,
merchant, tattooist, common/rare chests, EnemySpawn sockets, map weapon resources and map choice
events. Player/Boss actors, the 49 Smart/Light AI actor spawn groups, runtime chests, NPC catalog
models, map-resource weapon pickups, event-anchor three-choice rolls and the player map-event
interaction focus consume those anchors before using fallbacks. Diagnostics verify same-seed
determinism, different-seed variation, walkable anchor positions, EnemySpawn grouped counts
`inner=14`, `mid=17`, `outer=18`, and the first consumer bindings.

The runtime creates simple GF_X-owned primitive visuals under:

```text
[TotemMap]
[TotemActors]
```

These are placeholders for lifecycle and behavior validation. They are not the final art-resource
loading path.

## Gameplay Catalog Source

The current gameplay tuning source is:

```text
GameData/AIData/GameplayCatalogs/totem_gameplay_catalog.json
```

`TotemDataService` loads this JSON first and exposes it as `GameplayCatalog`. Runtime services now
prefer this catalog for weapon, skill, tattoo, shrink-zone, boss, NPC/shop, three-choice and AI
tuning data. If the file is missing or invalid, the services keep a code default fallback so the
workspace can still compile and boot while diagnostics report the source problem.

The catalog also carries migrated `WeaponConfig`, `ProjectileConfig`, `WeaponTraitConfig`,
`WeaponDropConfig` and `ChestConfig` rows. Weapon core data keeps the old `WeaponId` primary keys,
projectile references, normal/charged trait references, prefab paths, ammo counts, rarity, aim
spread and startup/active/recovery frame timing. Weapon cooldown is derived from the old
`(BaseStartup + BaseActive + BaseRecovery) / 60` rule and stored explicitly so AI edits can be
validated without re-reading the old xlsx flow. Projectile rows preserve `bullet_pistol` and
`arrow_bow`, including speed, max range, piercing flag, VFX path and pool size. Weapon trait rows
preserve the 10 old trait ids and effect types. Current trait effect application is still a
production-tuning follow-up; this slice makes the references and active trait metadata visible to
runtime diagnostics first.

Weapon drop rows are exposed as `weaponDrops`. Chest reward rows are exposed as `chestRewards` and
keep the old grouped `ChestId` probability shape; diagnostics require each chest id group to sum to
100.

The catalog now also promotes the old `BotConfig` and `BotBuildPreset` evidence into structured,
AI-friendly JSON:

```text
botProfiles     -> 20 Smart profiles + 3 Light profiles
botBuildPresets -> 7 Smart build presets with direct JSON arrays for tendency, parts and tattoo seq
```

`TotemAIService` assigns those profiles and presets to all 20 Smart AI and 29 Light AI. Smart AI
uses profile confidence, reaction delay, attack cooldown, vision radius, behavior macro and
self-tattoo boldness to drive combat and build-planning behavior. Light AI keeps the old lightweight
contract of wandering and counterattacking during a short damaged window, but now reads vision and
attack cadence from its profile instead of a shared hard-coded value.

The confirmed Smart AI split is 5 Aggressive, 3 Conservative, 4 ResourceAcquisition,
4 BossPriority and 4 PlayerPriority profiles. Runtime diagnostics prove these are behavior
differences, not only labels: Aggressive chases a 16m visible humanoid while Conservative refuses
the same far chase, ResourceAcquisition consumes map-resource pickups and merchant offers,
BossPriority pursues the active Boss even while a nearby death chest exists, and PlayerPriority
targets the nearest humanoid candidate without forcing the real player above a closer AI.

The catalog now also carries old `EnemyConfig` and `BossPhaseConfig` evidence:

```text
enemies    -> common Light, common Elite, AI_RUINS Boss body stats and loot metadata
bossPhases -> 3 AI_RUINS Boss phases with BossId, skills, VFX/BGM cues and death recipe
```

`TotemActorService` keeps the first-round roster contract of 50 actors: 1 player, 20 Smart AI,
29 Light AI and 1 Boss. Light AI bind to `enemy_common_light_01`, Smart AI bind to
`enemy_common_elite_01`, and the Boss binds to `enemy_ai_ruins_boss_01`. The Smart/Light roster
now consumes the map's EnemySpawn anchors before using fallback rings: `inner` carries 14 actors,
`mid` carries 17 actors and `outer` carries 18 actors. This keeps behavior type, enemy body type
and map placement separate: Bot profiles decide the AI brain, EnemyConfig supplies HP/body stats,
and map anchors decide first-round placement. EnemyConfig still supplies body damage, move speed,
attack range, detect range, loot table ids, guaranteed loot ids, coin reward range and pool ids.
`TotemAIService` consumes those body stats as movement/range/damage fallbacks, and
`TotemBossService` exposes the migrated phase skill ids, VFX/BGM cues and `recipe_ai_ruins_boss`
death reward without mounting old Boss modules.

Smart AI build planning now writes into `TotemTattooService` actor-scoped runtime state instead of
only incrementing an AI-side counter. Player/startup tattoos remain on the global player path, while
each AI actor can own an independent self-tattoo read bar, equipped tattoo list and tattoo effect
log. Tattoo trigger resolution is source-based: an AI without actor-scoped tattoos cannot trigger
player/global tattoos by accident.

AI combat now routes through the same GF_X services as the player combat path. On combat activation,
Smart and Light AI receive the old-evidence default Bot loadout (`pistol_basic` plus the two default
skill slots). AI attacks call `TotemWeaponService.FireWeapon`, AI skills call
`TotemSkillService.TryCastSlot`, and both paths emit source-scoped tattoo triggers plus temporary
VFX markers. Ranged weapon fire results now expose the projectile definition and active trait
definition used by the attack, so diagnostics can trace `pistol_basic -> bullet_pistol ->
trait_pierce` without depending on old `WeaponModule` events. This keeps Bot behavior out of the
old `CombatModule`/`WeaponModule` while preserving the important old effect that Bot attacks are
weapon-driven rather than raw placeholder damage.

Smart AI also restores the old reading-prey pressure from `SmartBotPlayerController`: when a visible
target is currently self-tattooing, Smart AI prefers that target over a closer non-reading decoy and
may attack it inside profile `AggroRadius`. The new implementation reads
`TotemTattooService.IsSelfTattooInProgress(actor)` and keeps the behavior inside GF_X-native AI.

ResourceAcquisition Smart AI now consumes the map-resource layer directly. When its
`TargetResourceWeight` reaches the resource-chase threshold, `TotemAIService` searches active
`MapResource` weapon pickups from `TotemWeaponService`, scores them by resource weight, distance
and whether they offer a new weapon, moves toward the pickup world position, and claims it through
`TotemWeaponService.TryPickupWeapon` inside pickup range. The AI decision record exposes
`PickupInstanceId`, `PickupWeaponId` and `PickupSource`, so diagnostics can prove the cause and
effect chain from map resource anchor -> pickup spawn -> AI chase -> pickup consumed.

This runtime catalog is now generated from the 28 Business AI DataTable manifests. The
planner-readable Business xlsx files are synchronized from those JSON manifests for review, while
final generated txt/bytes/code export remains a later build-pipeline step rather than a gameplay
runtime dependency.

## Runtime Asset Catalog Source

The first-pass runtime visual source is:

```text
GameData/AIData/GameplayCatalogs/totem_runtime_assets.json
```

This catalog maps GF_X-native runtime keys to old visual evidence and active GF_X asset paths:

```text
actor.player      -> Assets/Game/Prefabs/Entity/Actors/Player.prefab
actor.smartAi     -> Assets/Game/Prefabs/Entity/Actors/SmartAI.prefab
actor.lightAi     -> Assets/Game/Prefabs/Entity/Actors/LightAI.prefab
actor.boss        -> Assets/Game/Prefabs/Entity/Actors/Boss.prefab
npc.tattooist     -> Assets/Game/Prefabs/Entity/Actors/NpcTattooist.prefab
npc.merchant      -> Assets/Game/Prefabs/Entity/Actors/NpcMerchant.prefab
chest.chest_common -> Assets/Resources/Sprite/Items/item_chest_common.png
chest.chest_rare   -> Assets/Resources/Sprite/Items/item_chest_rare.png
map.floor.ruins   -> Assets/Resources/Sprite/Environments/env_floor_ruins.png
map.floor.metal   -> Assets/Resources/Sprite/Environments/env_floor_metal.png
map.floor.blood   -> Assets/Resources/Sprite/Environments/env_floor_blood_rock.png
map.wall.ruins    -> Assets/Resources/Sprite/Environments/env_wall_ruins.png
map.wall.metal    -> Assets/Resources/Sprite/Environments/env_wall_metal.png
map.wall.blood    -> Assets/Resources/Sprite/Environments/env_wall_blood.png
weapon.*          -> Assets/Resources/Sprite/Weapons/*.png
skill.*           -> Assets/Resources/Sprite/Skills/*.png
effect.attack.hit -> temporary attack-hit VFX marker sprite
effect.projectile.* -> projectile-specific VFX marker sprites derived from ProjectileConfig
effect.skill.*    -> temporary skill/Boss VFX marker sprites
```

`TotemRuntimeAssetMigrator` copies the old visual prefabs from `Assets/Resources/Prefab/Character`
into `Assets/Game/Prefabs/Entity/Actors` and strips all old MonoBehaviour components. The resulting
active prefabs keep SpriteRenderer/Animator visual data but no old `GameApp`-era behaviour scripts.

`TotemAssetService` loads this catalog and lets actor/NPC spawning prefer indexed active prefabs in
the Editor. Ordinary chests resolve indexed sprite views through the same service. If the indexed
asset is missing, runtime spawning falls back to primitive objects and diagnostics report the
missing path. This is the first resource-lifecycle rewrite step; final GF_X resource async loading
and full art replacement remain pending.

The catalog now also includes first-pass map environment textures, startup weapon sprites, and skill
sprites. `TotemAssetService` can load Texture2D/Sprite entries and create runtime materials from
catalog texture keys. `TotemMapService` still owns the gameplay map geometry and the current
hand-authored functional terrain grid, but its ground, room floors, and boundary walls now resolve
visuals through the catalog-backed material path before falling back to a tint-only material. The
active terrain layer is deterministic `100x100` at `4m` per cell and exposes `Ground`, `Slow`,
`Blocked`, `Cover` and `Hazard`; `TotemActorService.MoveActor` consumes it for slow movement and
blocked-cell rejection, the actor terrain-effect tick applies `TerrainHazard` damage on Hazard
cells through the normal damage pipeline, and source-based combat damage against Cover targets is
mitigated outside melee range while environment/status damage is left untouched.

The first UI flow uses the same catalog path for visible icon binding. `TotemStartupSelectForm`
loads startup weapon card icons from `weapon.<weaponId>`, and `TotemCombatHUDForm` loads the
selected weapon plus the default player skill icons from `TotemAssetService`. This keeps old art
reuse visible without reintroducing the old `Resources.Load` lifecycle.

`TotemVfxService` uses the same catalog for temporary combat markers. Attack hits spawn
`effect.attack.hit`, player skill bursts spawn `effect.skill.burst`, and the active BossPhaseConfig
beam skill `skill_beam` maps to `effect.boss.bolt`. Projectile trail signals now resolve
`bullet_pistol` to `effect.projectile.bullet_pistol` and `arrow_bow` to
`effect.projectile.arrow_bow`, so ProjectileConfig visual identity is preserved even while the
current art is a temporary marker. These are deliberately marked as temporary sprite markers because
the legacy `Effect`/projectile prefab folders do not currently provide final VFX assets.

`TotemCombatHUDForm` now refreshes runtime state at a low frequency through a coroutine. It displays
live player HP, Boss HP visibility/fill, selected weapon/ammo, skill cooldown, shrink-zone phase,
zone radius/damage, alive enemy count, and the current NPC interaction prompt. `TotemInteractionService`
does not auto-purchase or auto-apply rewards. Pressing interact near a merchant opens the GF_X-native
`Shop` overlay. Pressing interact near a tattooist rolls a deterministic `tattoo_<npcId>` three-choice
snapshot and opens the GF_X-native `TattooStudio` overlay; the player can then open `ThreeChoice` and
apply exactly one option from that UI. Pressing interact near a map event anchor rolls that anchor's
payload event through `TotemChoiceService.RollAnchorChoice` and opens the GF_X-native `ThreeChoice`
overlay directly. The interaction snapshot records `hasMapEvent`, `mapEventAnchorId`, `mapEventId`,
prompt, choice event id and choice count so diagnostics can trace map anchor -> player focus ->
three-choice UI.

The active GF_X UI table now includes:

```text
UIViews.MainMenu        = 1
UIViews.CharacterSelect = 2
UIViews.StartupSelect   = 3
UIViews.CombatHUD       = 4
UIViews.Shop            = 5
UIViews.ThreeChoice     = 6
UIViews.TattooStudio    = 7
UIViews.PauseMenu       = 8
UIViews.RunResult       = 9
UIViews.Settings        = 10
UIViews.SelfTattoo      = 11
UIViews.TattooEnchant   = 12
```

`TotemFirstSlicePrefabMigrator` prepares all twelve UI prefabs under `Assets/Game/Prefabs/UI`. The
Shop, ThreeChoice, TattooStudio, PauseMenu, RunResult, Settings, SelfTattoo and TattooEnchant prefabs
reuse the old visual shells as evidence but strip old callbacks and run new GF_X-native form scripts.
Each overlay self-builds a lightweight runtime panel when opened so it does not depend on old prefab
child names or old UI scripts.

Pause, result, settings, self-tattoo and enchantment are now GF_X-native runtime paths:

- Escape in combat first closes an escape-enabled overlay, then opens `PauseMenu`.
- `PauseMenu` pauses time while open and can resume, open Settings, or return to MainMenu.
- `Settings` edits a preview snapshot through `TotemSettingsService`, then commits or rolls back.
  `Preview`, `Commit` and `Rollback` are no-op guards until `BeginEdit` has created an edit snapshot.
  Commit persists `totem_settings.json` under `Application.persistentDataPath` with a temp file and
  `.bak` backup. Invalid or missing settings files fail cleanly and keep runtime defaults.
- Tab in combat toggles `SelfTattoo`; reading has explicit progress and completion state in
  `TotemTattooService`.
- Smart AI self-tattoo uses the same service lifecycle as player self-tattoo, but stores read bars
  and equipped tattoos on the source actor.
- `TattooEnchant` applies a minor enchant to the currently equipped tattoo set.
- Invalid tattoo equip requests are rejected without mutating equipped/effect state, and `Clear()` resets
  equipped tattoos, effect logs, pending triggers, active enchant affixes and actor-scoped tattoo state.
- `TotemCombatService` emits a `TotemRunResultSnapshot` and opens `RunResult` when the player is
  defeated or all enemies are cleared.
- `TotemRunStatsService` records cumulative run statistics in a GF_X-native save file instead of
  using old `SaveModule` events. The saved file is `totem_run_stats.json` under
  `Application.persistentDataPath`, written through the same temp-file plus `.bak` pattern used by
  settings persistence. The first persisted scope intentionally matches the old proven statistics:
  total runs, total kills and total play time, with added win/loss and best-run summaries for the
  new result screen.

Shop and three-choice rewards now apply to runtime state instead of stopping at UI display:

- `TotemNpcService.TryPurchase` returns a `TotemShopPurchaseResult` with price, stock, reward type
  and reward summary.
- Shop offers in `totem_gameplay_catalog.json` now declare `rewardType`, `rewardId`,
  `rewardAmount` and `rewardSlot`. Item-id group inference remains only as an old-data fallback.
- Reward type `Ink` adds ink, `WeaponUpgrade` upgrades/replaces the mapped weapon, and `SkillCore`
  refreshes a mapped skill slot.
- `TotemChoiceService.ApplyChoiceEffect` routes CoinReward, StatusCleanse, WeaponUpgrade and
  TattooBonus into Economy, Status, Weapon and Tattoo services.
- Unknown shop reward types fail explicitly so bad table data is visible in diagnostics.

Weapon drops and pickups now have a GF_X-native lifecycle:

- `totem_gameplay_catalog.json` carries the 15 migrated `WeaponDropConfig` rows as `weaponDrops`.
  Each row keeps `dropId`, `weaponId`, `dropSource`, `weight`, `minRoomIndex` and `maxRoomIndex`.
- `TotemWeaponService` owns the runtime drop catalog, weighted source selection, active pickup
  instances, pickup counters, and pickup marker visuals loaded through `TotemAssetService`.
- Smart AI deaths are treated as the current GF_X equivalent of old `Elite` drops, because the new
  actor model does not yet expose old enemy tier data. Light AI do not auto-drop weapons in this
  slice.
- `TotemInteractionService` exposes nearby weapon pickups through the same F-interact HUD prompt
  channel as death chests, ordinary chests and NPCs. Focus priority is death chest, then weapon
  pickup, then ordinary chest, then NPC.
- Picking up a weapon routes through `TotemWeaponService.TryUpgrade`: same-weapon pickups upgrade,
  max-level duplicates convert 50 base gold cost into 25 coins through `TotemEconomyService`, and
  different weapons keep the existing upgrade/replacement semantics already used by shop rewards.

Ordinary map chests now have a GF_X-native lifecycle:

- `totem_gameplay_catalog.json` carries the migrated 6-row `ChestConfig` evidence as `chestRewards`.
  `chest_common` preserves Weapon 45%, Gold 40 amount 40, Potion 15 amount 1. `chest_rare`
  preserves Weapon 60%, Gold 30 amount 80, Potion 10 amount 2.
- `TotemChestService` spawns 4 runtime chests on combat entry: 2 common chests and 2 rare chests
  placed near the functional rooms.
- Opening a chest selects a weighted reward by `ChestId`. Weapon rewards route into
  `TotemWeaponService` Chest-source drops, Gold rewards route into `TotemEconomyService.AddCoins`,
  and Potion rewards heal the opener by 25 HP per reward stack through `TotemActorModel.Heal`.
- Opened chests stay in the runtime list but are no longer interactable, so diagnostics can inspect
  opened counts and last reward type without relying on old `ChestInteractTrigger`.

Shrink-zone runtime damage is now event-visible and allocation-safe:

- `totem_gameplay_catalog.json` carries the migrated 3-row `ZoneShrinkConfig` evidence as
  `zonePhases`: phase 0 starts at 0s for 180s and shrinks to radius 65 with 2 damage; phase 1
  starts at 180s for 360s and shrinks to radius 35 with 6 damage; phase 2 starts at 540s for 360s
  and shrinks to radius 5 with 18 damage. Offset modes preserve `None`, `Drift` and `Fixed`.
- `TotemZoneService.Tick` computes the current phase/radius directly instead of allocating a
  `TotemZoneSnapshot` every frame.
- Out-zone damage routes through `TotemActorService.ApplyDamage`, so `DamageApplied` observers,
  kill state, and killed actor object deactivation stay consistent with combat damage.
- `TotemZoneSnapshot` reports affected actor count, killed actor count, last tick damage and total
  accumulated zone damage for AI-readable diagnostics.

Status and tattoo runtime damage follow the same route:

- `TotemStatusService` ticks and `TotemTattooService` triggers use `TotemActorService.ApplyDamage`
  when running inside `TotemGameRuntime`.
- `TotemStatusService` preserves the old status edge contracts: same-name refresh keeps max DPS,
  extends to max duration, ignores invalid apply/clear inputs, and keeps clear-all expiry counters visible.
- DamageApplied events, killed flags and inactive GameObject state stay consistent across combat,
  AI, shrink-zone, status and tattoo damage.
- `TotemActorService.DamageResolved` emits `TotemDamageRecord` with sequence, source, target,
  amount, killed flag, reason and target HP after damage. This keeps old subscribers compatible
  while giving diagnostics a direct cause chain.
- `TotemActorService.ApplyDamage` keeps the old damage receiver edge contract: zero/negative
  damage is ignored, excess damage clamps HP to 0, and later damage against a dead actor does not
  emit another damage/death chain.
- Standalone pure-service tests still use a local fallback when no actor service exists.

Death chest economy now has a GF_X-native lifecycle:

- `TotemEconomyService` listens to `TotemActorService.DamageResolved` killed records.
- Killed actors with transferable inventory create one pending `TotemDeathChestSnapshot`.
- The dead actor immediately loses the chested portion: half coins, half ink, half recipe copies,
  and all equipment in the current simplified inventory model.
- `TryLootDeathChest` transfers the snapshot to the looter and removes the pending chest, so it
  cannot be looted twice.
- `TotemInteractionService` exposes nearby pending death chests through the same HUD prompt channel
  as NPCs and uses the existing F-interact path to loot into the player inventory.
- `TotemAIService` also reads pending death chests through `TotemEconomyService` without copying
  snapshots every tick. Smart AI with high `LootGreedFactor` can enter `Loot`, chase a chest and
  loot it through the same economy transfer path. Low-greed Light AI only considers chests already
  inside the close pickup radius, so normal combat/wander pacing is not pulled apart by far loot.

## Verified Test Points

`TotemGameplayRuntimeDiagnosticScenario` verifies:

- required service/model types resolve
- map size is 400
- map has the 4 required functional rooms
- initial zone center is inside the middle third
- actor roster has:
  - 1 player
  - 20 smart AI
  - 29 light AI
  - 1 boss
  - 51 spawn entries including boss
- combat lifecycle visuals use indexed runtime assets in the normal path:
  - 51 actor visuals with 0 primitive actor fallbacks
  - 3 map-resource weapon pickup visuals with 0 primitive pickup fallbacks
  - 4 weapon pickup visuals after spawning `hammer_heavy` with 0 primitive pickup fallbacks
- combat lifecycle VFX sprite requests are observable: `effect.attack.hit` is requested once and
  missing count stays 0 in the normal path
- diagonal movement input is normalized
- `TotemInputService` reads through an injectable `ITotemInputProvider` boundary; diagnostics verify
  provider-driven attack press/hold, movement and gameplay/UI button snapshots
- full-lock targeting selects the closest alive enemy
- cone targeting prefers centered targets
- dead targets are skipped
- damage can kill targets

`TotemExtendedGameplayDiagnosticScenario` verifies:

- tattoo catalog has exactly 336 combinations: 6 parts x 7 colors x 8 patterns
- tattoo trigger mapping includes RightArm -> AttackHitEvent and LeftArm -> SkillCastEvent
- weapon catalog includes the 5 startup weapons
- migrated weapon core rows preserve old damage/cooldown/projectile/trait contracts:
  - `knife_basic` damage 18 and cooldown 10/60 seconds
  - `pistol_basic` damage 22, projectile `bullet_pistol`, normal trait `trait_pierce`
  - `bow_charge` requires charge, charged damage 105, projectile `arrow_bow`, charged trait
    `trait_chain`
- weapon upgrade math matches the old verified formula:
  - level 2: damage x1.2, range +0.5, cooldown x0.9
  - level 3: damage x1.44, range +1.0, cooldown x0.81
  - max-level duplicate converts 50 gold cost into 25 gold
- default/direct runtime upgrade guards verify `knife_basic` starts at level 1, direct
  upgrades reach levels 2 and 3, max-level duplicate conversion keeps level 3 and returns 25 gold,
  and the knife/pistol fire paths still report damage 18/16
- migrated weapon drop catalog has 15 rows split into 5 Elite, 5 Chest and 5 Merchant rows
- runtime weapon service exposes 2 projectile definitions and 10 weapon trait definitions
- weighted weapon-drop selection works for Elite and Chest sources with room gates preserved
- a nearby weapon pickup takes F-interact focus, shows the pickup prompt, upgrades the player weapon
  and clears the active pickup
- a max-level duplicate weapon pickup converts into 25 coins while keeping weapon level 3
- Smart AI death spawns an Elite weapon pickup through the GF_X `DamageResolved` path
- migrated chest reward catalog has 6 rows split across `chest_common` and `chest_rare`
- chest reward probabilities sum to 100 per `ChestId`
- direct chest opening routes Gold to player coins, Potion to actor healing, and Weapon to
  Chest-source weapon pickup spawning
- ordinary chest interaction focuses a nearby unopened chest, emits a HUD prompt, opens through
  F-interact, records `lastInteraction`, and removes that chest from future focus
- skill slot 0 can equip, cast, and enter cooldown
- status effects tick every 0.5s, merge same-name DPS/duration by max, and clear correctly
- status and tattoo runtime damage both publish `DamageApplied`, report killed state, and deactivate
  killed target GameObjects through `TotemActorService`
- detailed damage records expose sequence/source/target/reason for status and tattoo damage
- shrink-zone has 3 phases and resolves phase 0/1/2 by elapsed time
- shrink-zone runtime damage affects outside-zone player, AI and Boss actors through
  `TotemActorService.ApplyDamage`, while inside-zone actors remain safe
- shrink-zone snapshots expose affected/killed actor counts and accumulated damage
- boss has 3 HP phases, 0.8s transition window, 4s skill cooldown, and phase-3 death recipe
- boss death reward is claimed once by `TotemBossService`, applied by `TotemCombatService` on
  victory, unlocked into the player inventory by `TotemEconomyService`, and surfaced in RunResult
- economy death chest follows half-retention for coins/ink/recipes and full equipment retention
- runtime death chest lifecycle creates one pending chest on killed inventory actors, applies the
  dead-actor penalty, transfers loot to the looter, and blocks duplicate loot
- death chest interaction focuses a nearby pending chest, emits a HUD prompt, transfers loot through
  F-interact, records `lastInteraction`, and clears focus after loot
- NPC contract has 5 NPCs: 3 tattooists and 2 merchants
- each merchant has at least 3 shop offers
- three-choice event produces exactly 3 options
- merchant and tattooist interaction prompts are deterministic
- merchant/tattooist interaction event ids map to `shop_<npcId>` / `tattoo_<npcId>`
- interaction-triggered choice snapshots still produce exactly 3 options
- interaction stable seed is deterministic for repeatable NPC choice snapshots

`TotemAIRuntimeDiagnosticScenario` verifies:

- AI state build produces exactly 49 controller states
- 20 smart AI start in `Chase`
- 29 light AI start in `Wander`
- all 49 AI states receive profile data from `totem_gameplay_catalog.json`
- Smart AI profiles cycle across 7 build presets
- Light AI profiles cycle across 3 lightweight profiles
- the first Smart build plan resolves to `Part4/Color1/Pattern2`
- Smart self-tattoo planning obeys the old safety-threshold rule derived from `SelfTattooBoldness`
- a temporary non-UI runtime confirms Smart AI receives `pistol_basic` and default skill charges
- Smart AI attack routing consumes pistol ammo, damages the player, triggers actor-scoped attack
  tattoos and spawns VFX
- Smart AI skill routing uses `TotemSkillService`, triggers actor-scoped skill tattoos and increases
  the AI skill-use counter
- Smart AI prioritizes a farther self-tattooing prey over a nearer non-reading decoy and damages the
  reading prey through the weapon-routed attack path
- Smart AI with default high loot greed consumes a nearby pending death chest through
  `TotemEconomyService.TryLootDeathChest`, receives the inventory transfer, and increments AI loot
  counters
- low-greed Light AI leaves a far pending death chest untouched instead of chasing across the map
- initial actor rings are inside the hot LOD bucket
- hot/cold LOD boundary resolves at 20m
- decision intervals match the old behavior evidence:
  - Smart hot: every tick
  - Smart cold: 0.5s
  - Light hot: 0.2s
  - Light cold: 2s
- `TotemActorService.DamageApplied` reports damage and killed state for AI reaction windows

`TotemGameplayCatalogDiagnosticScenario` verifies:

- gameplay catalog JSON exists and parses through `TotemDataService`
- catalog schema passes required counts:
  - 31 migrated item metadata rows
  - 14 migrated tattoo resource rows
  - 5 startup weapons
  - 2 projectile rows
  - 10 weapon trait rows
  - 15 weapon drop rows
  - 6 chest reward rows with per-`ChestId` probability sum 100
  - 14 skill rows: 8 migrated old `SkillConfig` rows, 2 SkillAcquire targets and 4 BossPhaseConfig skills
  - 6 tattoo parts, 7 colors, 7 elements, 8 patterns, 8 shapes, 336 generated combinations
  - 3 zone phases
  - 3 boss phases
  - 5 NPCs: 3 tattooists and 2 merchants
  - 9 migrated merchant slot rows
  - 11 migrated three-choice option rows
- catalog-generated contracts preserve item, resource, weapon, tattoo, skill, boss, NPC/shop and AI tuning values
- catalog-generated item/resource contracts preserve:
  - 21 ink bottle rows across 7 colors x 3 tiers
  - Coin, RecipeShard, RecipeFull, Equipment and Antidote item types
  - White Premium ink rarity/price, Legendary equipment price and Detox antidote subtype
  - 6 tattoo part sprites and 8 tattoo pattern sprites with explicit asset keys and active paths
- catalog-generated weapon contracts preserve old frame-derived cooldown, projectile references,
  trait references and charged multiplier values
- catalog-generated projectile/trait contracts preserve `bullet_pistol`, `arrow_bow`,
  `trait_multishot` and `trait_pull`
- catalog-generated weapon drop contracts preserve 5 Elite, 5 Chest and 5 Merchant source rows
- catalog-generated chest reward contracts preserve common Gold 40 and rare Potion x2 rewards
- catalog-generated skill contracts preserve:
  - Fireball cooldown 6s, 8-frame startup, `DamageMul = 2.5`, circle radius 3
  - Chain Lightning charge model with 3 charges and 7s charge regeneration
  - Stealth hold-release model with 1.5s hold duration and 0.8s overcharge window
- catalog-generated enemy and boss contracts preserve:
  - 3 migrated `EnemyConfig` rows: Light, Elite and AI_RUINS Boss
  - Smart AI roster binding to `enemy_common_elite_01` and Light AI roster binding to
    `enemy_common_light_01`
  - Boss HP 800, body damage 35, `loot_boss_ai_ruins`, `pool_boss` and `80-120` coin range
  - 3 migrated `BossPhaseConfig` rows with phase skill ids, VFX/BGM cues and
    `recipe_ai_ruins_boss`
- catalog-generated tattoo enchant contracts preserve:
  - 6 self-tattoo reading-time rows: Head/Torso 8s, arms 5s, legs 3s
  - 24 affix rows across Common/Rare/Legendary tiers, including `DistanceGt8m` and `AfterDodge`
  - 3 enchant recipes with Common 200, Rare 350 and Legendary 500 coin costs
- catalog-generated tattoo core contracts preserve:
  - 6 migrated `TattooPartConfig` rows with ScaleStat, SymmetryGroup and ScaleFactor metadata
  - 7 migrated `TattooColorConfig` rows with old table multiplier `1.0`
  - 7 migrated `TattooElementConfig` rows, including Fire DPS/duration and Pure bonus params
  - 8 migrated `TattooPatternConfig` rows with old table multiplier `1.0`
  - 8 migrated `TattooShapeConfig` rows, including AOEBurst max-target and ProbBurst seed params
  - runtime tattoo definitions carry element/shape params and use ShapeConfig for diagnostic hit counts
- catalog-generated shop contracts preserve:
  - 15 migrated `ShopStockConfig` rows
  - `general_shop` 10 rows and `alien_shop` 5 rows
  - merchant `shopStockTable` metadata
  - 9 migrated `MerchantConfig` slot rows, 3 weighted candidates per slot
  - deterministic merchant weapon-slot offers appended to shop stock offers
  - Antidote/Remover as `StatusCleanse` and RareInk as ink rewards
- catalog-generated map contracts preserve:
  - 3 current `MapTemplateConfig` themes: AI_RUINS, ALIEN_HIVE and VIRUS_SWAMP
  - 400m fixed-map size, 40m min-room placeholder footprint, BSP depth 4 and deterministic 100x100 functional terrain grids
  - terrain pool ids and HUD/dominant color metadata
- catalog-generated event contracts preserve:
  - 6 migrated `EventConfig` rows
  - 2 choice events with 20s timeout
  - combat reward metadata and curse debuff/repeat metadata
- catalog-generated three-choice contracts preserve:
  - 11 migrated `ThreeChoiceOptionConfig` rows
  - tattoo/pattern recipe unlock rows, weapon upgrade row, skill refresh/acquire rows, coin/heal rows
  - weight, build-bonus JSON, min-run-time, unique flag, skill slot and value metadata
  - initial rolls filter delayed options and select 3 options without replacement
- catalog-generated AI contracts expose:
  - 10 bot profiles
  - 7 bot build presets
  - Rush and Camp behavior macros

`TotemFirstSliceUIDiagnosticScenario` verifies:

- `UIViews` defines MainMenu, CharacterSelect, StartupSelect, CombatHUD, Shop, ThreeChoice,
  TattooStudio, PauseMenu, RunResult, Settings, SelfTattoo and TattooEnchant
- `UITable` contains all twelve active UI rows
- all twelve active UI prefabs exist under `Assets/Game/Prefabs/UI`
- all twelve active UI prefabs have 0 missing scripts and 0 old persistent button callbacks
- `CombatHUD` has `WeaponIcon`, `SkillSlotE`, and `SkillSlotQ` image placeholders
- all 5 startup weapon icons load through `TotemAssetService`
- player skill icons `skill.skill_fireball_01` and `skill.skill_stealth_01` load through
  `TotemAssetService`
- CombatHUD status text formats weapon ammo, skill cooldown, enemy count, zone status, and NPC prompts
- Shop, TattooStudio and ThreeChoice overlay text formatters preserve NPC, inventory, offer and choice state
- PauseMenu, RunResult, Settings, SelfTattoo and TattooEnchant formatters preserve pause status,
  result title, audio/quality settings, reading state, equipped tattoo state and enchant count

`TotemExtendedGameplayDiagnosticScenario` now also verifies:

- `TotemSettingsService` preview formatting
- `TotemSettingsService` idle `Preview` / `Commit` / `Rollback` no-op guards
- `TotemSettingsService` temp-file save/load roundtrip and invalid JSON failure reporting
- `TotemRunStatsService` cumulative win/loss/kills/time aggregation
- `TotemRunStatsService` temp-file save/load roundtrip and invalid JSON failure reporting
- `TotemEconomyService` exposes 31 migrated item definitions, sell values and configured-item ink routing
- self-tattoo start, pending snapshot, duration tick, completion and equipped-count state
- old-table tattoo multipliers and shape hit-count params: RightArm Red Line magnitude 1.0 and
  AOEBurst hit count 5 from `TattooShapeConfig`
- actor-scoped self-tattoo start, pending snapshot, duration tick, completion and equipped-count state
- self-tattoo reading durations use migrated catalog values rather than temporary service constants
- map generation exposes migrated theme metadata through `TotemMapSnapshot`
- `TotemChoiceService` exposes runtime event catalog data and weighted event selection by type
- actor-scoped trigger isolation: a Smart AI with its own RightArm tattoo can trigger it, another
  Smart AI without actor tattoos cannot borrow player/global tattoos
- player/global tattoo trigger behavior still works through the existing public player API
- minor tattoo enchant count and last-affix snapshot fields for id, type, tier, stat, value and recipe cost
- run-result snapshot fields for win/loss, reason and kill count
- Boss reward runtime: phase 3 transition, all-enemy victory, one-shot recipe claim, inventory
  unlock count, and RunResult summary exposure
- shop reward routing:
  - Red Ink adds ink
  - Knife Upgrade raises weapon level
  - Skill Core refreshes cooldown and charges
  - Antidote clears active statuses through `TotemStatusService`
- gameplay catalog shop reward metadata:
  - merchant offers must expose explicit reward types
  - Bow Upgrade must expose `rewardId = bow_charge`
  - Fireball Skill Core must expose `rewardId = skill_fireball_01`
- migrated SkillConfig runtime contracts:
  - Fireball equips and casts from `TotemSkillService`
  - Fireball damage resolves as `knife_basic.BaseDamage * DamageMul = 45`
  - Chain Lightning consumes one of three charges and regenerates it after 7s
  - Stealth uses the hold-release timing contract and exposes a 2.3s hold/overcharge gate
- three-choice reward routing:
  - CoinReward adds coins
  - Heal restores player HP
  - RecipeUnlock unlocks a recipe id through `TotemEconomyService`
  - SkillRefresh clears the target skill cooldown
  - StatusCleanse clears statuses
  - WeaponUpgrade raises the equipped weapon level
  - TattooBonus applies an enchant
- data-source observability:
  - `TotemDataService` reports the loaded gameplay catalog path, content hash and fallback state
  - diagnostics fail if the active run silently uses `BuildDefault` while the JSON catalog should load
  - static/default helper paths in actor, AI, economy, choice, NPC, chest, skill, tattoo, weapon
    and map services route through `TotemDataService.LoadGameplayCatalogOrDefault`, so helper
    APIs prefer the AI-editable JSON catalog before the final in-code fallback
  - `TotemWeaponService` does not keep hidden static weapon/projectile/trait tables; helper APIs
    and runtime fallbacks rebuild those definitions from the gameplay catalog
  - `TotemZoneService` and `TotemBossService` do not keep hidden static phase tables; static
    helper APIs and runtime fallbacks rebuild shrink-zone/Boss phases from the gameplay catalog
  - Actor, Skill, Choice, Map and Tattoo services do not keep service-owned static gameplay
    catalog caches; static helper APIs rebuild data from the AI-editable gameplay catalog
- migrated ZoneShrinkConfig runtime contracts:
  - `TotemZoneService.GetPhaseAt` switches phases at 0s, 180s and 540s
  - `TotemZoneService.ComputeRadius` reaches radius 65 at 180s, 35 at 540s and 5 at 900s
  - runtime zone snapshots expose phase id 0 and out-zone damage 2 immediately after combat startup
  - phase 2 preserves out-zone damage 18 and offset mode `Fixed`

`TotemRuntimeAssetDiagnosticScenario` verifies:

- runtime asset catalog JSON exists and parses through `TotemAssetService`
- all required actor/NPC, chest, map, weapon, and skill runtime asset keys are present
- runtime asset catalog contains 39 entries: 6 prefabs, 6 textures, and 27 sprites
- active GF_X runtime prefabs exist under `Assets/Game/Prefabs/Entity/Actors`
- migrated prefabs have 0 missing scripts and 0 old MonoBehaviour components
- migrated prefabs retain visual SpriteRenderer components
- `TotemAssetService` can instantiate `actor.player` in the Editor
- map texture files exist, are non-empty, and load as `Texture2D`
- weapon, skill, chest and tattoo visual files exist, are non-empty, and load as `Sprite` or `Texture2D`
- `TotemAssetService` can load `map.floor.ruins`, `weapon.knife_basic`,
  `skill.skill_fireball_01`, `chest.chest_common`, `chest.chest_rare`, `tattoo.part.head`,
  `tattoo.pattern.line`, and create a material for `map.wall.ruins`
- `TotemAssetService` reports missing-entry and fallback-required counters; required diagnostic
  loads must keep instantiate fallback, visual fallback and visual missing-entry counts at 0
- `TotemAssetService` reports cache hit/miss/count/last-key observability for sprite, texture and
  prefab loads; repeated loads must hit cache, instantiation must return distinct GameObject
  instances from the cached prefab, and `ReloadRuntimeAssetCatalog` must clear cache counters

`TotemVfxRuntimeDiagnosticScenario` verifies:

- attack, skill burst, and boss bolt VFX keys resolve to catalog entries
- active Boss bolt VFX is owned by `skill_beam`, not the removed temporary Boss seed skill
- `effect.attack.hit`, `effect.skill.burst`, `effect.boss.bolt`,
  `effect.projectile.bullet_pistol` and `effect.projectile.arrow_bow` sprites load through
  `TotemAssetService`
- projectile trail signals spawn through projectile-specific VFX keys and record the last projectile id

Latest passing evidence:

```text
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_180121.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_184550.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_185358.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_185906.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_190358.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_191133.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_191619.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_192051.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_192605.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_192948.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_193650.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_194621.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_194810.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_200008.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_201304.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260707_233932.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_001720.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_002624.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_003146.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_003655.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_004853.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_005857.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_011117.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_011843.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_012430.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_013153.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_013628.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_013858.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_014211.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_014514.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_014937.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_015157.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_015829.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260708_020529.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_084258.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_084806.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_093855.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_094341.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_094902.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_100101.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_100325.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_104045.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_111413.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_114005.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_115350.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_115556.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_120848.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_121414.json
GameData/Diagnostics/Reports/gf-diagnostics-run-all_20260709_123030.json
success=27
failure=0
warning=0
```

## Closed Baseline And Accepted Later Work

- The current first-round target is closed: GF_X-native old-function coverage,
  non-UI automated diagnostics, clean workspace evidence, item-by-item audit
  traceability, stable PlayMode CombatHUD smoke, playable combat-loop smoke, and
  confirmed old-content production in the new Business data/runtime path are
  proved by the completion audit and latest diagnostics.
- The automated playable-stable combat-loop proxy is now covered by
  `TotemRuntimeCausalityDiagnosticScenario` `playableLoop.*` evidence:
  CombatHUD entry, movement `5.00m`, dodge `4.50m`, kill delta `1`, Smart AI
  decisions `11`, Smart AI attack `1`, player pressure health `74.0 -> 56.4`,
  final player health `34.4`, alive enemies `49`, elapsed `5.8s`, and actor
  cleanup `0`.
- `Tools/Playtest/Smoke/CombatHUD Input` now exists as the PlayMode entry for
  driving CombatHUD movement, attack, E/Q skill, dodge, interact, Tab, Escape
  and Return through `TotemInputService` / `ITotemInputProvider`.
  `TotemCombatHudInputSmokeTests.CombatHud_InputSmoke_UsesTotemInputService` is
  also discoverable as a PlayMode `Runnable` test and passed in PM-05 through a
  temporary UnitySkills `test_run_by_name` allowlist. Archived XML
  `tools/playtest/test-results/2026-07-09-1225-PM-05-combathud-input-playmode.xml`
  records `total=1`, `passed=1`, `failed=0`, `result=Passed`.
- Continue replacing accepted placeholder/primitive visual assets with final indexed production art, especially projectile/VFX, character animation and UI placeholder assets.
- Keep evolving resource loading from current catalog-driven runtime/editor cache toward the final build-player async backend when the production AB policy is ready.
- Expand smart/light AI from deterministic contract behavior into richer production behavior after the final first-round closure audit.
- Expand weapon, skill, tattoo, status, economy, NPC, shrink-zone and Boss systems from first-pass production tuning into final balance polish after the final first-round closure audit.
