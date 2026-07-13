#!/usr/bin/env python3
"""Build Totem gameplay catalog JSON from GF_X Business AI DataTable manifests."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import OrderedDict
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Callable


ROOT = Path(__file__).resolve().parents[2]
DEFAULT_INPUT = ROOT / "GameData" / "AIData" / "DataTables" / "Business"
DEFAULT_OUTPUT = ROOT / "GameData" / "AIData" / "GameplayCatalogs" / "totem_gameplay_catalog.json"


def text(value: Any) -> str:
    return "" if value is None else str(value)


def integer(value: Any) -> int:
    raw = text(value).strip()
    if not raw:
        return 0
    return int(float(raw))


def floating(value: Any) -> float:
    raw = text(value).strip()
    return 0.0 if not raw else float(raw)


def boolean(value: Any) -> bool:
    return text(value).strip().lower() in {"true", "1", "yes", "y"}


def json_cell(value: Any) -> Any:
    raw = text(value).strip()
    if not raw:
        return []
    return json.loads(raw)


def resource_asset_key(name: str) -> str:
    return re.sub(r"[^a-z0-9]+", ".", name.lower()).strip(".")


def resource_active_asset_path(resource_type: str, load_path: str) -> str:
    if resource_type.lower() == "sprite":
        return f"Assets/Resources/Sprite/{load_path}.png"
    return f"Assets/Resources/{resource_type}/{load_path}"


def weapon_cooldown(row: dict[str, str]) -> float:
    frames = integer(row.get("BaseStartup")) + integer(row.get("BaseActive")) + integer(row.get("BaseRecovery"))
    return 0.35 if frames <= 0 else round(frames / 60.0, 6)


FieldParser = Callable[[Any], Any]


@dataclass(frozen=True)
class TableSpec:
    table_name: str
    catalog_key: str
    source_key: str
    target_key: str
    fields: tuple[tuple[str, str, FieldParser], ...]
    preserve_extra_seed_rows: bool = False


def pascal_to_camel(name: str) -> str:
    return name[:1].lower() + name[1:] if name else name


def simple_fields(*names: str) -> tuple[tuple[str, str, FieldParser], ...]:
    result: list[tuple[str, str, FieldParser]] = []
    for name in names:
        result.append((name, pascal_to_camel(name), text))
    return tuple(result)


TABLE_SPECS: tuple[TableSpec, ...] = (
    TableSpec("ItemConfig", "items", "ItemId", "itemId", (
        ("ItemId", "itemId", integer),
        ("ItemType", "itemType", text),
        ("SubType", "subType", text),
        ("Tier", "tier", integer),
        ("DisplayName", "displayName", text),
        ("Rarity", "rarity", text),
        ("MaxStack", "maxStack", integer),
        ("BasePrice", "basePrice", integer),
        ("SellRatio", "sellRatio", floating),
    )),
    TableSpec("ResourceConfig", "resources", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("Type", "resourceType", text),
        ("LoadPath", "loadPath", text),
    )),
    TableSpec("WeaponConfig", "weapons", "WeaponId", "weaponId", (
        ("WeaponId", "weaponId", text),
        ("Name", "displayName", text),
        ("Class", "className", text),
        ("BaseDamage", "baseDamage", floating),
        ("AttackSpeed", "attackSpeed", floating),
        ("Range", "range", floating),
        ("ChargedMul", "chargedMul", floating),
        ("ProjectileId", "projectileId", text),
        ("Rarity", "rarity", integer),
        ("MaxAmmo", "maxAmmo", integer),
        ("BaseStartup", "baseStartup", integer),
        ("BaseActive", "baseActive", integer),
        ("BaseRecovery", "baseRecovery", integer),
        ("RequiresCharge", "requiresCharge", boolean),
        ("AimSpreadHalfDeg", "aimSpreadHalfDeg", floating),
        ("NormalTraitId", "normalTraitId", text),
        ("ChargedTraitId", "chargedTraitId", text),
        ("WeaponPrefabPath", "weaponPrefabPath", text),
    )),
    TableSpec("ProjectileConfig", "projectiles", "ProjectileId", "projectileId", (
        ("ProjectileId", "projectileId", text),
        ("Speed", "speed", floating),
        ("MaxRange", "maxRange", floating),
        ("Piercing", "piercing", boolean),
        ("AoeRadius", "aoeRadius", floating),
        ("VisualPrefabPath", "visualPrefabPath", text),
        ("PoolSize", "poolSize", integer),
    )),
    TableSpec("WeaponTraitConfig", "weaponTraits", "TraitId", "traitId", (
        ("TraitId", "traitId", text),
        ("Name", "displayName", text),
        ("Description", "description", text),
        ("EffectType", "effectType", text),
        ("EffectParam1", "effectParam1", floating),
        ("EffectParam2", "effectParam2", floating),
    )),
    TableSpec("WeaponDropConfig", "weaponDrops", "DropId", "dropId", (
        ("DropId", "dropId", text),
        ("WeaponId", "weaponId", text),
        ("DropSource", "dropSource", text),
        ("Weight", "weight", integer),
        ("MinRoomIndex", "minRoomIndex", integer),
        ("MaxRoomIndex", "maxRoomIndex", integer),
    )),
    TableSpec("ChestConfig", "chestRewards", "ChestId", "chestId", (
        ("ChestId", "chestId", text),
        ("RewardType", "rewardType", text),
        ("RewardId", "rewardId", text),
        ("RewardAmount", "rewardAmount", integer),
        ("Probability", "probability", integer),
    )),
    TableSpec("MapTemplateConfig", "mapTemplates", "Id", "id", (
        ("Id", "id", integer),
        ("ThemeName", "themeName", text),
        ("MapSize", "mapSize", floating),
        ("MinRoomSize", "minRoomSize", floating),
        ("BspMaxDepth", "bspMaxDepth", integer),
        ("TerrainPoolId", "terrainPoolId", integer),
        ("PrefabPath", "prefabPath", text),
        ("HudAccentColor", "hudAccentColor", text),
        ("DominantColor", "dominantColor", text),
    )),
    TableSpec("TattooPartConfig", "tattooParts", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("TriggerEvent", "triggerEvent", text),
        ("ScaleStat", "scaleStat", text),
        ("SymmetryGroup", "symmetryGroup", text),
        ("ScaleFactor", "scaleFactor", floating),
        ("PassiveDimension", "passiveDimension", text),
    )),
    TableSpec("TattooColorConfig", "tattooColors", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("Element", "element", text),
        ("ColorMultiplier", "multiplier", floating),
    )),
    TableSpec("TattooElementConfig", "tattooElements", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("BaseMultiplier", "baseMultiplier", floating),
        ("Param1", "param1", floating),
        ("Param2", "param2", floating),
        ("Param3", "param3", floating),
    )),
    TableSpec("TattooPatternConfig", "tattooPatterns", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("Shape", "shape", text),
        ("PatternMultiplier", "multiplier", floating),
    )),
    TableSpec("TattooShapeConfig", "tattooShapes", "Id", "id", (
        ("Id", "id", integer),
        ("Name", "name", text),
        ("Param1", "param1", floating),
        ("Param2", "param2", floating),
        ("Param3", "param3", floating),
    )),
    TableSpec("TattooReadingTimeConfig", "tattooReadingTimes", "PartId", "partId", (
        ("PartId", "partId", integer),
        ("PartName", "partName", text),
        ("DurationSec", "durationSec", floating),
    )),
    TableSpec("TattooEnchantAffixConfig", "tattooEnchantAffixes", "Id", "id", (
        ("Id", "id", integer),
        ("PartId", "partId", integer),
        ("ColorTier", "colorTier", text),
        ("AffixType", "affixType", text),
        ("StatKey", "statKey", text),
        ("Value", "value", floating),
        ("ConditionKey", "conditionKey", text),
        ("ConditionVal", "conditionVal", floating),
        ("DisplayText", "displayText", text),
        ("Weight", "weight", floating),
    )),
    TableSpec("TattooEnchantRecipeConfig", "tattooEnchantRecipes", "Id", "id", (
        ("Id", "id", integer),
        ("ColorTier", "colorTier", text),
        ("CoinCost", "coinCost", integer),
        ("RarePigmentCost", "rarePigmentCost", integer),
        ("MaxAffixPerSlot", "maxAffixPerSlot", integer),
    )),
    TableSpec("SkillConfig", "skills", "SkillId", "skillId", (
        ("SkillId", "skillId", text),
        ("Name", "displayName", text),
        ("ChargeModel", "chargeModel", integer),
        ("Cooldown", "cooldown", floating),
        ("MaxCharges", "maxCharges", integer),
        ("ChargeRegenTime", "chargeRegenTime", floating),
        ("HoldDuration", "holdDuration", floating),
        ("OverchargeWindow", "overchargeWindow", floating),
        ("StartupFrames", "startupFrames", integer),
        ("ActiveFrames", "activeFrames", integer),
        ("RecoveryFrames", "recoveryFrames", integer),
        ("DamageMul", "damageMul", floating),
        ("HitShape", "hitShape", text),
        ("HitRadius", "hitRadius", floating),
        ("Element", "element", text),
        ("CancelableByDodge", "cancelableByDodge", boolean),
        ("ItemId", "itemId", integer),
    ), preserve_extra_seed_rows=True),
    TableSpec("EnemyConfig", "enemies", "EnemyId", "enemyId", (
        ("EnemyId", "enemyId", text),
        ("DisplayName", "displayName", text),
        ("ThemeId", "themeId", text),
        ("Tier", "tier", text),
        ("RuntimeAssetKey", "runtimeAssetKey", text),
        ("FallbackRuntimeAssetKey", "fallbackRuntimeAssetKey", text),
        ("BehaviorProfileId", "behaviorProfileId", text),
        ("AbilityIds", "abilityIds", text),
        ("BaseHP", "baseHP", floating),
        ("HPCurveK", "hpCurveK", floating),
        ("BaseDamage", "baseDamage", floating),
        ("DamageCurveK", "damageCurveK", floating),
        ("MoveSpeed", "moveSpeed", floating),
        ("AttackRange", "attackRange", floating),
        ("DetectRange", "detectRange", floating),
        ("LeashRange", "leashRange", floating),
        ("SkillIds", "skillIds", text),
        ("LootTableId", "lootTableId", text),
        ("GuaranteedLootIds", "guaranteedLootIds", text),
        ("SpawnCost", "spawnCost", integer),
        ("ElitePaintDropRare", "elitePaintDropRare", integer),
        ("XPReward", "xpReward", integer),
        ("CoinReward", "coinReward", text),
        ("PoolIds", "poolIds", text),
    )),
    TableSpec("EnemyAbilityConfig", "enemyAbilities", "AbilityId", "abilityId", (
        ("AbilityId", "abilityId", text),
        ("AbilityType", "abilityType", text),
        ("Range", "range", floating),
        ("Radius", "radius", floating),
        ("Cooldown", "cooldown", floating),
        ("Windup", "windup", floating),
        ("Active", "active", floating),
        ("Recovery", "recovery", floating),
        ("DamageMultiplier", "damageMultiplier", floating),
        ("StatusId", "statusId", text),
        ("StatusChance", "statusChance", floating),
        ("SummonEnemyId", "summonEnemyId", text),
        ("SummonCount", "summonCount", integer),
        ("VfxId", "vfxId", text),
        ("AudioCueId", "audioCueId", text),
        ("ParametersJson", "parametersJson", text),
    )),
    TableSpec("EncounterSpawnConfig", "encounterSpawns", "EncounterId", "encounterId", (
        ("EncounterId", "encounterId", text),
        ("ThemeId", "themeId", text),
        ("ZoneRoles", "zoneRoles", text),
        ("EnemyPoolIds", "enemyPoolIds", text),
        ("StartTime", "startTime", floating),
        ("EndTime", "endTime", floating),
        ("InitialCount", "initialCount", integer),
        ("ActiveCap", "activeCap", integer),
        ("TotalCap", "totalCap", integer),
        ("WaveMin", "waveMin", integer),
        ("WaveMax", "waveMax", integer),
        ("WaveInterval", "waveInterval", floating),
        ("MinParticipantDistance", "minParticipantDistance", floating),
        ("MinSpacing", "minSpacing", floating),
        ("Weight", "weight", integer),
        ("Unique", "unique", boolean),
    )),
    TableSpec("EnemyLootConfig", "enemyLoot", "LootEntryId", "lootEntryId", (
        ("LootEntryId", "lootEntryId", text),
        ("LootTableId", "lootTableId", text),
        ("ItemId", "itemId", text),
        ("RewardType", "rewardType", text),
        ("MinCount", "minCount", integer),
        ("MaxCount", "maxCount", integer),
        ("Weight", "weight", integer),
        ("Guaranteed", "guaranteed", boolean),
        ("TierFilter", "tierFilter", text),
        ("ThemeId", "themeId", text),
    )),
    TableSpec("ZoneShrinkConfig", "zonePhases", "Id", "id", (
        ("Id", "id", integer),
        ("PhaseName", "phaseName", text),
        ("StartTime", "startTime", floating),
        ("Duration", "duration", floating),
        ("TargetRadius", "targetRadius", floating),
        ("OutZoneDamage", "outZoneDamage", floating),
        ("CenterOffsetMode", "centerOffsetMode", text),
    )),
    TableSpec("BossPhaseConfig", "bossPhases", "PhaseIndex", "phaseIndex", (
        ("BossId", "bossId", text),
        ("PhaseIndex", "phaseIndex", integer),
        ("HPThreshold", "hpThreshold", floating),
        ("AbilityIds", "abilityIds", text),
        ("NewSkillIds", "skillIds", text),
        ("EnrageMultiplier", "enrageMultiplier", floating),
        ("PhaseVFXId", "phaseVFXId", text),
        ("PhaseBGMCueId", "phaseBGMCueId", text),
        ("DeathPatternRecipeId", "deathPatternRecipeId", text),
    )),
    TableSpec("NPCConfig", "npcs", "Id", "configId", (
        ("Id", "configId", integer),
        ("NPCId", "npcId", text),
        ("Type", "type", text),
        ("MapTheme", "mapTheme", text),
        ("ShopStockTable", "shopStockTable", text),
        ("InteractRadius", "interactRadius", floating),
        ("ThemePriceMul", "themePriceMultiplier", floating),
        ("GuardRadius", "guardRadius", floating),
        ("ServiceCooldown", "serviceCooldown", floating),
        ("GuardSpawnId", "guardSpawnId", text),
        ("GuardCount1", "guardCount1", integer),
        ("GuardCount2", "guardCount2", integer),
    )),
    TableSpec("ShopStockConfig", "shopStocks", "Id", "id", (
        ("Id", "id", integer),
        ("TableId", "tableId", text),
        ("ItemId", "itemId", integer),
        ("Category", "category", text),
        ("Weight", "weight", floating),
        ("MinCount", "minCount", integer),
        ("MaxCount", "maxCount", integer),
        ("BasePrice", "basePrice", integer),
        ("SellRatio", "sellRatio", floating),
    )),
    TableSpec("MerchantConfig", "merchantSlots", "Id", "id", (
        ("SlotIndex", "slotIndex", integer),
        ("WeaponId", "weaponId", text),
        ("GoldCost", "goldCost", integer),
        ("RefreshWeight", "refreshWeight", integer),
    )),
    TableSpec("EventConfig", "events", "EventId", "eventId", (
        ("EventId", "eventId", text),
        ("EventType", "eventType", text),
        ("DisplayName", "displayName", text),
        ("TriggerCondition", "triggerCondition", text),
        ("BaseRewardCoin", "baseRewardCoin", integer),
        ("RewardPoolId", "rewardPoolId", text),
        ("TimeoutSec", "timeoutSec", floating),
        ("CurseDebuffId", "curseDebuffId", text),
        ("WeightBase", "weightBase", integer),
        ("IsRepeatAllowed", "isRepeatAllowed", boolean),
    )),
    TableSpec("ThreeChoiceOptionConfig", "choiceOptions", "OptionId", "optionId", (
        ("OptionId", "optionId", text),
        ("OptionType", "optionType", text),
        ("DisplayName", "displayName", text),
        ("DescKey", "descKey", text),
        ("ContentRef", "contentRef", text),
        ("SkillSlot", "skillSlot", integer),
        ("ValueInt", "valueInt", integer),
        ("WeightBase", "weightBase", integer),
        ("WeightBuildBonus", "weightBuildBonus", text),
        ("MinRunElapsedSec", "minRunElapsedSec", floating),
        ("IsUnique", "isUnique", boolean),
    )),
    TableSpec("BotConfig", "botProfiles", "BotId", "botId", (
        ("BotId", "botId", integer),
        ("Type", "type", text),
        ("DisplayName", "displayName", text),
        ("RethinkInterval", "rethinkInterval", floating),
        ("AttackCooldown", "attackCooldown", floating),
        ("VisionRadius", "visionRadius", floating),
        ("AggroRadius", "aggroRadius", floating),
        ("DodgeReactionMs", "dodgeReactionMs", integer),
        ("Confidence", "confidence", floating),
        ("PreferredPreset", "preferredPreset", integer),
        ("LootGreedFactor", "lootGreedFactor", floating),
        ("SelfTattooBoldness", "selfTattooBoldness", floating),
        ("EnchantGreed", "enchantGreed", floating),
        ("Personality", "personality", text),
        ("TargetPlayerWeight", "targetPlayerWeight", floating),
        ("TargetHumanoidAiWeight", "targetHumanoidAiWeight", floating),
        ("TargetBossWeight", "targetBossWeight", floating),
        ("TargetResourceWeight", "targetResourceWeight", floating),
        ("ReadingTargetWeight", "readingTargetWeight", floating),
        ("ShopPreference", "shopPreference", floating),
        ("RiskTolerance", "riskTolerance", floating),
    )),
    TableSpec("BotBuildPreset", "botBuildPresets", "PresetId", "presetId", (
        ("PresetId", "presetId", integer),
        ("Name", "name", text),
        ("Tendency", "tendency", json_cell),
        ("PreferredParts", "preferredParts", json_cell),
        ("RecommendedSeq", "recommendedSeq", json_cell),
        ("EarlyGameWeapon", "earlyGameWeapon", integer),
        ("BehaviorMacro", "behaviorMacro", text),
        ("PreferredSkillQ", "preferredSkillQ", integer),
        ("PreferredSkillE", "preferredSkillE", integer),
        ("TargetEnchantAffixes", "targetEnchantAffixes", json_cell),
    )),
)


def load_manifest_rows(input_dir: Path, table_name: str) -> list[dict[str, str]]:
    path = input_dir / f"{table_name}.json"
    payload = json.loads(path.read_text(encoding="utf-8-sig"), object_pairs_hook=OrderedDict)
    return [dict(row.get("values") or {}) for row in payload.get("rows") or []]


def seed_by_key(rows: list[dict[str, Any]], key: str) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for row in rows:
        result[str(row.get(key, ""))] = row
    return result


def transform_row(spec: TableSpec, values: dict[str, str], seed: dict[str, Any] | None) -> dict[str, Any]:
    row: dict[str, Any] = dict(seed or {})
    for source, target, parser in spec.fields:
        row[target] = parser(values.get(source))

    if spec.table_name == "ResourceConfig":
        name = text(values.get("Name"))
        resource_type = text(values.get("Type"))
        load_path = text(values.get("LoadPath"))
        row["assetKey"] = resource_asset_key(name)
        row["activeAssetPath"] = resource_active_asset_path(resource_type, load_path)
    elif spec.table_name == "WeaponConfig":
        row["cooldown"] = weapon_cooldown(values)
    elif spec.table_name == "NPCConfig":
        npc_type = text(values.get("Type"))
        row.setdefault("offers", [])
        if not row.get("roomType"):
            row["roomType"] = "Merchant" if npc_type.lower() == "merchant" else "TattooStudio"
        row.setdefault("offsetX", 0.0)
        row.setdefault("offsetY", 0.0)
        row.setdefault("offsetZ", 0.0)

    return row


def apply_table(catalog: dict[str, Any], input_dir: Path, spec: TableSpec) -> None:
    source_rows = load_manifest_rows(input_dir, spec.table_name)
    existing_rows = catalog.get(spec.catalog_key) or []
    existing = seed_by_key(existing_rows, spec.target_key)
    generated: list[dict[str, Any]] = []
    used_keys: set[str] = set()

    for values in source_rows:
        source_id = str(values.get(spec.source_key, ""))
        seed = existing.get(source_id)
        generated.append(transform_row(spec, values, seed))
        used_keys.add(source_id)

    if spec.preserve_extra_seed_rows:
        for row in existing_rows:
            row_key = str(row.get(spec.target_key, ""))
            if row_key not in used_keys:
                generated.append(row)

    catalog[spec.catalog_key] = generated


def compute_source_fingerprint(input_dir: Path) -> tuple[int, str]:
    sha = hashlib.sha256()
    files = sorted(input_dir.glob("*.json"), key=lambda item: item.name)
    for file_name in files:
        sha.update(file_name.name.encode("utf-8"))
        sha.update(b"\0")
        sha.update(file_name.read_bytes())
        sha.update(b"\0")
    return len(files), sha.hexdigest()


def attach_generation_info(catalog: dict[str, Any], input_dir: Path) -> OrderedDict[str, Any]:
    file_count, source_hash = compute_source_fingerprint(input_dir)
    generation = OrderedDict(
        (
            ("generatedBy", "tools/ai_index/build_gameplay_catalog_from_business_tables.py"),
            ("sourceRoot", "GameData/AIData/DataTables/Business"),
            ("sourceFileCount", file_count),
            ("sourceContentHash", source_hash),
        )
    )

    updated: OrderedDict[str, Any] = OrderedDict()
    inserted = False
    for key, value in catalog.items():
        if key == "generation":
            continue
        updated[key] = value
        if key == "source":
            updated["generation"] = generation
            inserted = True

    if not inserted:
        updated["generation"] = generation
    return updated


def split_ids(value: Any) -> list[str]:
    return [part.strip() for part in text(value).split(",") if part.strip()]


def validate_enemy_domain(catalog: dict[str, Any]) -> None:
    errors: list[str] = []

    def require(condition: bool, message: str) -> None:
        if not condition:
            errors.append(message)

    enemies = catalog.get("enemies") or []
    abilities = catalog.get("enemyAbilities") or []
    phases = catalog.get("bossPhases") or []
    encounters = catalog.get("encounterSpawns") or []
    loot_rows = catalog.get("enemyLoot") or []
    items = {str(row.get("itemId")) for row in catalog.get("items") or []}
    weapons = {text(row.get("weaponId")) for row in catalog.get("weapons") or []}
    audio_cues = {text(row.get("cueId")) for row in catalog.get("audioCues") or []}

    expected_tiers = {
        "enemy_common_hunter": "Light",
        "enemy_common_shooter": "Light",
        "enemy_common_guardian": "Elite",
        "enemy_ai_servo": "Light",
        "enemy_ai_arc_drone": "Light",
        "enemy_ai_manager": "Elite",
        "boss_ai_core_zero": "Boss",
        "enemy_alien_crawler": "Light",
        "enemy_alien_spitter": "Light",
        "enemy_alien_guard": "Elite",
        "boss_alien_hive_mother": "Boss",
        "enemy_virus_mutant": "Light",
        "enemy_virus_spore_carrier": "Light",
        "enemy_virus_spore_host": "Elite",
        "boss_virus_terminus": "Boss",
    }
    enemy_by_id = {text(row.get("enemyId")): row for row in enemies}
    require(len(enemies) == 15, f"EnemyConfig must contain 15 rows, got {len(enemies)}.")
    require(set(enemy_by_id) == set(expected_tiers), "EnemyConfig ids do not match the 15 confirmed design ids.")
    for enemy_id, tier in expected_tiers.items():
        row = enemy_by_id.get(enemy_id) or {}
        require(row.get("tier") == tier, f"{enemy_id} must use tier {tier}.")

    ability_by_id = {text(row.get("abilityId")): row for row in abilities}
    require(len(ability_by_id) == len(abilities), "EnemyAbilityConfig ability ids must be unique and non-empty.")
    expected_ability_types = {
        "Melee", "Projectile", "Charge", "Leap", "Beam", "ConeSweep", "AreaPulse",
        "HazardZone", "Shield", "Summon", "Regenerate", "DeathBurst", "PhaseTransition",
    }
    actual_ability_types = {text(row.get("abilityType")) for row in abilities}
    require(expected_ability_types <= actual_ability_types, "EnemyAbilityConfig must cover all 13 reusable ability types.")
    for ability_id, row in ability_by_id.items():
        require(bool(ability_id), "EnemyAbilityConfig has an empty AbilityId.")
        require(row.get("cooldown", 0) >= 0 and row.get("windup", 0) >= 0 and row.get("active", 0) >= 0 and row.get("recovery", 0) >= 0,
                f"{ability_id} has a negative timing value.")
        require(0 <= row.get("statusChance", 0) <= 1, f"{ability_id} StatusChance must be in 0..1.")
        summon_id = text(row.get("summonEnemyId"))
        if row.get("abilityType") == "Summon":
            require(summon_id in enemy_by_id and row.get("summonCount", 0) > 0, f"{ability_id} has an invalid SummonEnemyId/SummonCount.")
        elif summon_id:
            require(False, f"{ability_id} declares SummonEnemyId but is not a Summon ability.")
        cue_id = text(row.get("audioCueId"))
        if cue_id:
            require(cue_id in audio_cues, f"{ability_id} references missing AudioCueId {cue_id}.")
        try:
            json.loads(text(row.get("parametersJson")) or "{}")
        except json.JSONDecodeError as exception:
            errors.append(f"{ability_id} ParametersJson is invalid: {exception}.")

    pools: set[str] = set()
    loot_tables = {text(row.get("lootTableId")) for row in loot_rows}
    loot_by_id = {text(row.get("lootEntryId")): row for row in loot_rows}
    require(len(loot_by_id) == len(loot_rows), "EnemyLootConfig entry ids must be unique and non-empty.")
    for enemy_id, row in enemy_by_id.items():
        for ability_id in split_ids(row.get("abilityIds")):
            require(ability_id in ability_by_id, f"{enemy_id} references missing AbilityId {ability_id}.")
        require(bool(text(row.get("behaviorProfileId"))), f"{enemy_id} BehaviorProfileId is empty.")
        require(bool(text(row.get("runtimeAssetKey"))), f"{enemy_id} RuntimeAssetKey is empty.")
        require(bool(text(row.get("fallbackRuntimeAssetKey"))), f"{enemy_id} FallbackRuntimeAssetKey is empty.")
        require(row.get("leashRange", 0) >= row.get("detectRange", 0), f"{enemy_id} LeashRange must be >= DetectRange.")
        require(row.get("spawnCost", 0) > 0, f"{enemy_id} SpawnCost must be positive.")
        table_id = text(row.get("lootTableId"))
        require(table_id in loot_tables, f"{enemy_id} references missing LootTableId {table_id}.")
        for loot_id in split_ids(row.get("guaranteedLootIds")):
            loot = loot_by_id.get(loot_id)
            require(loot is not None, f"{enemy_id} references missing GuaranteedLootId {loot_id}.")
            if loot is not None:
                require(bool(loot.get("guaranteed")) and loot.get("lootTableId") == table_id,
                        f"{enemy_id} GuaranteedLootId {loot_id} must be guaranteed and belong to {table_id}.")
        pools.update(split_ids(row.get("poolIds")))

    recipe_ids = {text(row.get("deathPatternRecipeId")) for row in phases if text(row.get("deathPatternRecipeId"))}
    for loot_id, row in loot_by_id.items():
        require(bool(loot_id) and bool(text(row.get("lootTableId"))), "EnemyLootConfig ids must be non-empty.")
        require(row.get("minCount", 0) > 0 and row.get("maxCount", 0) >= row.get("minCount", 0), f"{loot_id} has an invalid count range.")
        require(bool(row.get("guaranteed")) or row.get("weight", 0) > 0, f"{loot_id} must be guaranteed or have positive weight.")
        reward_type = text(row.get("rewardType"))
        item_id = text(row.get("itemId"))
        if reward_type == "Weapon":
            require(item_id in weapons, f"{loot_id} references missing WeaponConfig id {item_id}.")
        elif reward_type == "Recipe":
            require(item_id in recipe_ids, f"{loot_id} references missing Boss recipe id {item_id}.")
        else:
            require(item_id in items, f"{loot_id} references missing ItemConfig id {item_id}.")

    expected_themes = {"ai_ruins", "alien_hive", "virus_swamp"}
    require(len(encounters) == 9, f"EncounterSpawnConfig must contain 9 schedules, got {len(encounters)}.")
    for row in encounters:
        encounter_id = text(row.get("encounterId"))
        require(text(row.get("themeId")) in expected_themes, f"{encounter_id} has an invalid ThemeId.")
        require(bool(text(row.get("zoneRoles"))), f"{encounter_id} ZoneRoles is empty.")
        for pool_id in split_ids(row.get("enemyPoolIds")):
            require(pool_id in pools, f"{encounter_id} references missing enemy pool {pool_id}.")
        require(row.get("initialCount", 0) <= row.get("activeCap", 0) <= row.get("totalCap", 0), f"{encounter_id} count caps are invalid.")
        require(row.get("waveMin", 0) <= row.get("waveMax", 0), f"{encounter_id} wave range is invalid.")
        require(row.get("minParticipantDistance", 0) > 0 and row.get("minSpacing", 0) > 0, f"{encounter_id} spawn safety distances must be positive.")

    phases_by_boss: dict[str, list[dict[str, Any]]] = {}
    for row in phases:
        phases_by_boss.setdefault(text(row.get("bossId")), []).append(row)
    expected_bosses = {enemy_id for enemy_id, tier in expected_tiers.items() if tier == "Boss"}
    require(set(phases_by_boss) == expected_bosses, "BossPhaseConfig must cover exactly the three confirmed bosses.")
    for boss_id, boss_phases in phases_by_boss.items():
        boss_phases.sort(key=lambda row: row.get("phaseIndex", 0))
        require([row.get("phaseIndex") for row in boss_phases] == [1, 2, 3], f"{boss_id} must have phases 1, 2, 3.")
        require([row.get("hpThreshold") for row in boss_phases] == [1.0, 0.6, 0.3], f"{boss_id} thresholds must be 1.0, 0.6, 0.3.")
        require(bool(text(boss_phases[-1].get("deathPatternRecipeId"))), f"{boss_id} phase 3 recipe is empty.")
        for row in boss_phases:
            for ability_id in split_ids(row.get("abilityIds")):
                require(ability_id in ability_by_id, f"{boss_id} phase {row.get('phaseIndex')} references missing AbilityId {ability_id}.")
            require(text(row.get("phaseBGMCueId")) in audio_cues, f"{boss_id} phase {row.get('phaseIndex')} references a missing BGM cue.")

    if errors:
        raise ValueError("Enemy domain catalog validation failed:\n- " + "\n- ".join(errors))


def build_catalog(input_dir: Path, seed_path: Path) -> dict[str, Any]:
    catalog = json.loads(seed_path.read_text(encoding="utf-8-sig"), object_pairs_hook=OrderedDict)
    catalog["source"] = "GameData/AIData/DataTables/Business"
    for spec in TABLE_SPECS:
        apply_table(catalog, input_dir, spec)
    validate_enemy_domain(catalog)
    return attach_generation_info(catalog, input_dir)


def write_or_check(catalog: dict[str, Any], output: Path, check: bool) -> int:
    text_out = json.dumps(catalog, ensure_ascii=False, indent=2) + "\n"
    current = output.read_text(encoding="utf-8-sig") if output.exists() else ""
    if current != text_out:
        if check:
            print(f"Would update {output.relative_to(ROOT)}")
            return 1
        output.parent.mkdir(parents=True, exist_ok=True)
        output.write_text(text_out, encoding="utf-8", newline="\n")
        print(f"Wrote {output.relative_to(ROOT)}")
    elif not check:
        print(f"Already up to date: {output.relative_to(ROOT)}")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, default=DEFAULT_INPUT)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--seed", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    input_dir = args.input if args.input.is_absolute() else ROOT / args.input
    output = args.output if args.output.is_absolute() else ROOT / args.output
    seed = args.seed if args.seed.is_absolute() else ROOT / args.seed
    catalog = build_catalog(input_dir, seed)
    return write_or_check(catalog, output, args.check)


if __name__ == "__main__":
    raise SystemExit(main())
