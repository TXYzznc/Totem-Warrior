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
    normalized = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", name)
    return re.sub(r"[^a-z0-9_]+", ".", normalized.lower()).strip(".")


def resource_active_asset_path(resource_type: str, load_path: str) -> str:
    if load_path.startswith("Assets/"):
        return load_path
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
        ("BaseHP", "baseHP", floating),
        ("HPCurveK", "hpCurveK", floating),
        ("BaseDamage", "baseDamage", floating),
        ("DamageCurveK", "damageCurveK", floating),
        ("MoveSpeed", "moveSpeed", floating),
        ("AttackRange", "attackRange", floating),
        ("DetectRange", "detectRange", floating),
        ("SkillIds", "skillIds", text),
        ("LootTableId", "lootTableId", text),
        ("GuaranteedLootIds", "guaranteedLootIds", text),
        ("ElitePaintDropRare", "elitePaintDropRare", integer),
        ("XPReward", "xpReward", integer),
        ("CoinReward", "coinReward", text),
        ("PoolIds", "poolIds", text),
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


def build_catalog(input_dir: Path, seed_path: Path) -> dict[str, Any]:
    catalog = json.loads(seed_path.read_text(encoding="utf-8-sig"), object_pairs_hook=OrderedDict)
    catalog["source"] = "GameData/AIData/DataTables/Business"
    for spec in TABLE_SPECS:
        apply_table(catalog, input_dir, spec)
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
