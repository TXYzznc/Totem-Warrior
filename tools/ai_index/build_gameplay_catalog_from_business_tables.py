#!/usr/bin/env python3
"""Build Totem gameplay catalog JSON from GF_X Business AI DataTable manifests."""

from __future__ import annotations

import argparse
import hashlib
import json
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
    TableSpec("MapTemplateConfig", "mapTemplates", "Id", "id", (
        ("Id", "id", integer),
        ("ThemeName", "themeName", text),
        ("MapSize", "mapSize", floating),
        ("MinRoomSize", "minRoomSize", floating),
        ("PrefabPath", "prefabPath", text),
        ("HudAccentColor", "hudAccentColor", text),
        ("DominantColor", "dominantColor", text),
    )),
    TableSpec("MapResourcePickupConfig", "mapResourcePickups", "PickupId", "pickupId", (
        ("PickupId", "pickupId", text),
        ("Category", "category", text),
        ("ResourceId", "resourceId", text),
        ("Element", "element", text),
        ("MinAmount", "minAmount", integer),
        ("MaxAmount", "maxAmount", integer),
        ("Weight", "weight", integer),
        ("MinRound", "minRound", integer),
        ("MaxRound", "maxRound", integer),
        ("AssetKey", "assetKey", text),
        ("Enabled", "enabled", boolean),
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
        ("Personality", "personality", text),
        ("TargetPlayerWeight", "targetPlayerWeight", floating),
        ("TargetHumanoidAiWeight", "targetHumanoidAiWeight", floating),
        ("TargetResourceWeight", "targetResourceWeight", floating),
        ("RiskTolerance", "riskTolerance", floating),
    )),
    TableSpec("BotBuildPreset", "botBuildPresets", "PresetId", "presetId", (
        ("PresetId", "presetId", integer),
        ("Name", "name", text),
        ("BehaviorMacro", "behaviorMacro", text),
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

    if spec.table_name == "WeaponConfig":
        row["cooldown"] = weapon_cooldown(values)

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


def build_catalog(input_dir: Path, seed_path: Path) -> dict[str, Any]:
    catalog = json.loads(seed_path.read_text(encoding="utf-8-sig"), object_pairs_hook=OrderedDict)
    for retired_key in (
        "enemies", "enemyAbilities", "encounterSpawns", "enemyLoot", "bossPhases", "chestRewards",
        "weaponDrops", "skills", "npcs", "shopStocks", "merchantSlots", "events", "choiceOptions",
        "tattooParts", "tattooColors", "tattooElements", "tattooPatterns", "tattooShapes", "items",
        "tattooReadingTimes", "tattooEnchantAffixes", "tattooEnchantRecipes",
        "weapons", "projectiles", "weaponTraits", "resources",
    ):
        catalog.pop(retired_key, None)
    catalog["source"] = "GameData/AIData/DataTables/Business"
    for spec in TABLE_SPECS:
        apply_table(catalog, input_dir, spec)
    for row in catalog.get("mapTemplates", []):
        row.pop("bspMaxDepth", None)
        row.pop("terrainPoolId", None)
    catalog["botProfiles"] = [
        row for row in catalog.get("botProfiles", [])
        if str(row.get("personality", "")).lower() != "bosspriority"
    ]
    for row in catalog["botProfiles"]:
        for retired_key in (
            "targetBossWeight", "selfTattooBoldness", "enchantGreed",
            "readingTargetWeight", "shopPreference",
        ):
            row.pop(retired_key, None)
    catalog["audioCues"] = [
        row for row in catalog.get("audioCues", [])
        if str(row.get("legacySource", "")).lower() != "bossphaseconfig.phasebgmcueid"
        and str(row.get("cueId", "")) not in {
            "sfx_hit_melee", "sfx_hit_special", "sfx_hit_default", "sfx_skill_cast"
        }
    ]
    for cue in catalog.get("audioCues", []):
        if cue.get("cueId") == "bgm_in_game":
            cue["usage"] = "Combat BGM."
        elif cue.get("cueId") == "sfx_kill":
            cue["usage"] = "Opponent eliminated."
    envelope = catalog.get("aiTuning")
    if isinstance(envelope, dict):
        for retired_key in ("bossAttackRange", "bossMoveSpeed", "bossAttackCooldown", "bossDamage"):
            envelope.pop(retired_key, None)
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
