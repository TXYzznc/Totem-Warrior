#!/usr/bin/env python3
"""One-shot migration utility for importing an explicitly supplied legacy table source."""

from __future__ import annotations

import argparse
import json
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_OUTPUT = REPO_ROOT / "GameData" / "AIData" / "DataTables" / "Business"

BOT_PROFILE_FIELDS: list[dict[str, str]] = [
    {"name": "BotId", "type": "int", "desc": "Primary profile id"},
    {"name": "Type", "type": "string", "desc": "Smart or Light"},
    {"name": "DisplayName", "type": "string", "desc": "Runtime/debug display name"},
    {"name": "RethinkInterval", "type": "float", "desc": "Build planner rethink interval in seconds"},
    {"name": "AttackCooldown", "type": "float", "desc": "Minimum normal attack cooldown"},
    {"name": "VisionRadius", "type": "float", "desc": "Target search radius"},
    {"name": "AggroRadius", "type": "float", "desc": "Preferred combat/chase radius"},
    {"name": "DodgeReactionMs", "type": "int", "desc": "Threat reaction latency in milliseconds"},
    {"name": "Confidence", "type": "float", "desc": "Decision confidence 0..1"},
    {"name": "PreferredPreset", "type": "int", "desc": "BotBuildPreset.PresetId"},
    {"name": "LootGreedFactor", "type": "float", "desc": "Death chest/resource greed 0..2"},
    {"name": "SelfTattooBoldness", "type": "float", "desc": "Smart self-tattoo boldness 0..1"},
    {"name": "EnchantGreed", "type": "float", "desc": "Enchant/shop upgrade preference 0..1"},
    {"name": "Personality", "type": "string", "desc": "Smart AI personality"},
    {"name": "TargetPlayerWeight", "type": "float", "desc": "Score weight for the real player"},
    {"name": "TargetHumanoidAiWeight", "type": "float", "desc": "Score weight for Smart/Light AI targets"},
    {"name": "TargetBossWeight", "type": "float", "desc": "Score weight for active Boss target"},
    {"name": "TargetResourceWeight", "type": "float", "desc": "Score weight for resource seeking"},
    {"name": "ReadingTargetWeight", "type": "float", "desc": "Bonus weight against self-tattoo reading targets"},
    {"name": "ShopPreference", "type": "float", "desc": "Shop and upgrade preference"},
    {"name": "RiskTolerance", "type": "float", "desc": "Risk tolerance 0..1"},
]


def resolve_personality_weights(personality: str) -> dict[str, float]:
    if personality == "PlayerPriority":
        return {
            "TargetPlayerWeight": 1.1,
            "TargetHumanoidAiWeight": 1.1,
            "TargetBossWeight": 0.2,
            "TargetResourceWeight": 0.4,
            "ReadingTargetWeight": 1.5,
            "ShopPreference": 0.4,
            "RiskTolerance": 0.85,
        }
    if personality == "Conservative":
        return {
            "TargetPlayerWeight": 0.45,
            "TargetHumanoidAiWeight": 0.35,
            "TargetBossWeight": 0.2,
            "TargetResourceWeight": 0.25,
            "ReadingTargetWeight": 0.8,
            "ShopPreference": 0.4,
            "RiskTolerance": 0.35,
        }
    if personality == "ResourceAcquisition":
        return {
            "TargetPlayerWeight": 0.35,
            "TargetHumanoidAiWeight": 0.25,
            "TargetBossWeight": 0.2,
            "TargetResourceWeight": 1.6,
            "ReadingTargetWeight": 0.8,
            "ShopPreference": 1.2,
            "RiskTolerance": 0.55,
        }
    if personality == "BossPriority":
        return {
            "TargetPlayerWeight": 0.25,
            "TargetHumanoidAiWeight": 0.25,
            "TargetBossWeight": 2.0,
            "TargetResourceWeight": 0.45,
            "ReadingTargetWeight": 0.8,
            "ShopPreference": 0.4,
            "RiskTolerance": 0.7,
        }
    if personality == "Aggressive":
        return {
            "TargetPlayerWeight": 0.9,
            "TargetHumanoidAiWeight": 0.9,
            "TargetBossWeight": 0.2,
            "TargetResourceWeight": 0.4,
            "ReadingTargetWeight": 0.8,
            "ShopPreference": 0.4,
            "RiskTolerance": 0.85,
        }

    return {
        "TargetPlayerWeight": 0.9,
        "TargetHumanoidAiWeight": 0.9,
        "TargetBossWeight": 0.2,
        "TargetResourceWeight": 0.4,
        "ReadingTargetWeight": 0.8,
        "ShopPreference": 0.4,
        "RiskTolerance": 0.6,
    }


def build_bot_profile_row(
    bot_id: int,
    profile_type: str,
    display_name: str,
    personality: str,
    preferred_preset: int,
    vision_radius: float,
    aggro_radius: float,
    dodge_reaction_ms: int,
    confidence: float,
    loot_greed_factor: float,
    self_tattoo_boldness: float,
    enchant_greed: float,
    rethink_interval: float = 20.0,
    attack_cooldown: float = 0.7,
) -> dict[str, Any]:
    row: dict[str, Any] = {
        "BotId": bot_id,
        "Type": profile_type,
        "DisplayName": display_name,
        "RethinkInterval": rethink_interval,
        "AttackCooldown": attack_cooldown,
        "VisionRadius": vision_radius,
        "AggroRadius": aggro_radius,
        "DodgeReactionMs": dodge_reaction_ms,
        "Confidence": confidence,
        "PreferredPreset": preferred_preset,
        "LootGreedFactor": loot_greed_factor,
        "SelfTattooBoldness": self_tattoo_boldness,
        "EnchantGreed": enchant_greed,
        "Personality": personality,
    }
    row.update(resolve_personality_weights(personality))
    return row


def build_bot_profile_rows() -> list[dict[str, Any]]:
    return [
        build_bot_profile_row(1, "Smart", "Smart Aggressive Fire", "Aggressive", 1, 20.0, 18.0, 210, 0.88, 0.95, 0.62, 0.65),
        build_bot_profile_row(2, "Smart", "Smart Aggressive Lightning", "Aggressive", 2, 20.0, 18.0, 220, 0.86, 1.0, 0.58, 0.6),
        build_bot_profile_row(3, "Smart", "Smart Aggressive Mutation", "Aggressive", 5, 20.0, 18.0, 240, 0.82, 0.9, 0.64, 0.58),
        build_bot_profile_row(4, "Smart", "Smart Aggressive Pure", "Aggressive", 7, 20.0, 18.0, 215, 0.87, 0.85, 0.6, 0.62),
        build_bot_profile_row(5, "Smart", "Smart Aggressive Flanker", "Aggressive", 1, 21.0, 18.0, 230, 0.84, 1.05, 0.55, 0.7),
        build_bot_profile_row(6, "Smart", "Smart Conservative Nature", "Conservative", 3, 22.0, 15.0, 320, 0.72, 0.45, 0.35, 0.35, attack_cooldown=0.85),
        build_bot_profile_row(7, "Smart", "Smart Conservative Frost", "Conservative", 4, 22.0, 14.0, 300, 0.75, 0.4, 0.3, 0.4, attack_cooldown=0.85),
        build_bot_profile_row(8, "Smart", "Smart Conservative Holy", "Conservative", 6, 24.0, 16.0, 310, 0.78, 0.5, 0.38, 0.45, attack_cooldown=0.9),
        build_bot_profile_row(9, "Smart", "Smart Resource Lightning", "ResourceAcquisition", 2, 21.0, 16.0, 260, 0.78, 1.65, 0.48, 0.75, attack_cooldown=0.75),
        build_bot_profile_row(10, "Smart", "Smart Resource Nature", "ResourceAcquisition", 3, 21.0, 16.0, 280, 0.74, 1.75, 0.44, 0.8, attack_cooldown=0.8),
        build_bot_profile_row(11, "Smart", "Smart Resource Mutation", "ResourceAcquisition", 5, 22.0, 17.0, 270, 0.76, 1.85, 0.5, 0.85, attack_cooldown=0.75),
        build_bot_profile_row(12, "Smart", "Smart Resource Holy", "ResourceAcquisition", 6, 22.0, 17.0, 265, 0.79, 1.7, 0.46, 0.9, attack_cooldown=0.8),
        build_bot_profile_row(13, "Smart", "Smart Boss Fire", "BossPriority", 1, 24.0, 18.0, 250, 0.82, 0.55, 0.5, 0.6, attack_cooldown=0.75),
        build_bot_profile_row(14, "Smart", "Smart Boss Frost", "BossPriority", 4, 24.0, 18.0, 255, 0.8, 0.5, 0.5, 0.62, attack_cooldown=0.8),
        build_bot_profile_row(15, "Smart", "Smart Boss Pure", "BossPriority", 7, 25.0, 19.0, 245, 0.84, 0.45, 0.52, 0.65, attack_cooldown=0.75),
        build_bot_profile_row(16, "Smart", "Smart Boss Mutation", "BossPriority", 5, 24.0, 18.0, 265, 0.78, 0.6, 0.48, 0.58, attack_cooldown=0.8),
        build_bot_profile_row(17, "Smart", "Smart Player Lightning", "PlayerPriority", 2, 22.0, 20.0, 220, 0.86, 0.7, 0.55, 0.65),
        build_bot_profile_row(18, "Smart", "Smart Player Pure", "PlayerPriority", 7, 22.0, 20.0, 225, 0.88, 0.65, 0.58, 0.62),
        build_bot_profile_row(19, "Smart", "Smart Player Fire", "PlayerPriority", 1, 22.0, 20.0, 215, 0.87, 0.68, 0.56, 0.66),
        build_bot_profile_row(20, "Smart", "Smart Player Mutation", "PlayerPriority", 5, 23.0, 20.0, 235, 0.83, 0.72, 0.54, 0.68),
        build_bot_profile_row(101, "Light", "Light Scout A", "Hybrid", 1, 14.0, 12.0, 350, 0.45, 0.3, 0.0, 0.0, rethink_interval=45.0, attack_cooldown=1.0),
        build_bot_profile_row(102, "Light", "Light Scout B", "Hybrid", 2, 14.0, 12.0, 380, 0.4, 0.3, 0.0, 0.0, rethink_interval=45.0, attack_cooldown=1.0),
        build_bot_profile_row(103, "Light", "Light Scout C", "Hybrid", 3, 14.0, 12.0, 400, 0.4, 0.3, 0.0, 0.0, rethink_interval=45.0, attack_cooldown=1.1),
    ]


def build_bot_config_payload(source_file: Path) -> dict[str, Any]:
    return {
        "table": "BotConfig",
        "fields": BOT_PROFILE_FIELDS,
        "rows": build_bot_profile_rows(),
        "source": str(source_file.relative_to(REPO_ROOT)).replace("\\", "/"),
        "migrationNote": "BotConfig is intentionally upgraded from the legacy 10 sample rows to the confirmed 20 Smart + 3 Light GF_X profile set.",
    }


def normalize_cell(value: Any) -> str:
    if value is None:
        return ""
    if isinstance(value, bool):
        return "true" if value else "false"
    if isinstance(value, (list, dict)):
        return json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    text = str(value)
    return text.replace("\r", "").replace("\n", "").replace("\t", " ")


def normalize_type(value: Any) -> str:
    text = normalize_cell(value).strip()
    if not text:
        return "string"
    return text


def is_int_text(value: Any) -> bool:
    text = normalize_cell(value).strip()
    if not text:
        return False
    try:
        int(text)
    except ValueError:
        return False
    return True


def choose_gf_ids(fields: list[dict[str, Any]], rows: list[dict[str, Any]]) -> list[int]:
    if not fields:
        return list(range(1, len(rows) + 1))

    first_name = normalize_cell(fields[0].get("name"))
    first_type = normalize_type(fields[0].get("type")).lower()
    values = [row.get(first_name) for row in rows]
    text_values = [normalize_cell(value).strip() for value in values]
    if first_type == "int" and all(is_int_text(value) for value in values):
        int_values = [int(value) for value in text_values]
        if len(set(int_values)) == len(int_values):
            return int_values

    return list(range(1, len(rows) + 1))


def build_columns(fields: list[dict[str, Any]]) -> list[dict[str, Any]]:
    columns: list[dict[str, Any]] = [
        {
            "index": 1,
            "key": "Note1",
            "name": "#",
            "type": "#",
            "comment": "#",
            "role": "comment",
        },
        {
            "index": 2,
            "key": "Id",
            "name": "Id",
            "type": "int",
            "comment": "GF_X numeric row id. Original business key is preserved as a data column when its name is not Id.",
            "role": "id",
        },
        {
            "index": 3,
            "key": "Note3",
            "name": "",
            "type": "",
            "comment": "Note",
            "role": "comment",
        },
    ]

    next_index = 4
    for field in fields:
        name = normalize_cell(field.get("name")).strip()
        if not name:
            continue
        if name == "Id":
            continue

        columns.append(
            {
                "index": next_index,
                "key": name,
                "name": name,
                "type": normalize_type(field.get("type")),
                "comment": normalize_cell(field.get("desc")),
                "role": "data",
            }
        )
        next_index += 1

    return columns


def build_manifest(source_file: Path, existing_manifest: dict[str, Any] | None = None) -> dict[str, Any]:
    if source_file.name == "BotConfig.json":
        payload = build_bot_config_payload(source_file)
    else:
        payload = json.loads(source_file.read_text(encoding="utf-8-sig"))
    table_name = normalize_cell(payload.get("table") or source_file.stem)
    fields = payload.get("fields") or []
    rows = payload.get("rows") or []
    gf_ids = choose_gf_ids(fields, rows)
    columns = build_columns(fields)
    generated_at = (
        existing_manifest.get("generatedAtUtc")
        if existing_manifest
        else datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
    )
    source_excel_last_write_utc = (
        existing_manifest.get("sourceExcelLastWriteUtc")
        if existing_manifest
        else ""
    )

    manifest_rows: list[dict[str, Any]] = []
    for offset, row in enumerate(rows):
        values: dict[str, str] = {"Id": str(gf_ids[offset])}
        notes: dict[str, str] = {}
        cells = ["", str(gf_ids[offset]), ""]

        for column in columns[3:]:
            value = normalize_cell(row.get(column["name"]))
            values[column["key"]] = value
            cells.append(value)

        manifest_rows.append(
            {
                "row": offset + 5,
                "enabled": True,
                "comment": None,
                "values": values,
                "notes": notes,
                "_cells": cells,
            }
        )

    return {
        "schemaVersion": 1,
        "kind": "GF_X.DataTable.AI",
        "tableName": table_name,
        "relativePath": f"Business/{table_name}",
        "sourceExcel": f"GameData/DataTables/Business/{table_name}.xlsx",
        "sourceExcelLastWriteUtc": source_excel_last_write_utc or "",
        "generatedAtUtc": generated_at,
        "tableComment": (
            f"Business table imported from an explicit migration source ({source_file.name}). "
            "GF_X Id is numeric; original business keys are preserved as data columns."
        ),
        "columns": columns,
        "rows": manifest_rows,
    }


def generate(source_dir: Path, output_dir: Path, check: bool) -> int:
    source_files = sorted(source_dir.glob("*.json"))
    if not source_files:
        raise FileNotFoundError(f"No source table json files found: {source_dir}")

    generated: dict[Path, tuple[dict[str, Any], str]] = {}
    for source_file in source_files:
        target = output_dir / f"{source_file.stem}.json"
        existing_manifest = None
        if target.exists():
            existing_manifest = json.loads(target.read_text(encoding="utf-8"))

        manifest = build_manifest(source_file, existing_manifest)
        target = output_dir / f"{manifest['tableName']}.json"
        text = json.dumps(manifest, ensure_ascii=False, indent=2) + "\n"
        generated[target] = (manifest, text)

    changed = []
    for target, (manifest, text) in generated.items():
        current_manifest = None
        if target.exists():
            current_manifest = json.loads(target.read_text(encoding="utf-8"))
        if current_manifest != manifest:
            changed.append(target)

    if check:
        for target in changed:
            print(f"Would update {target.relative_to(REPO_ROOT)}")
        return 1 if changed else 0

    output_dir.mkdir(parents=True, exist_ok=True)
    for target, (_, text) in generated.items():
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(text, encoding="utf-8", newline="\n")
        print(f"Wrote {target.relative_to(REPO_ROOT)}")

    print(f"Generated {len(generated)} business AI DataTable manifests.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()

    source = args.source if args.source.is_absolute() else REPO_ROOT / args.source
    output = args.output if args.output.is_absolute() else REPO_ROOT / args.output
    return generate(source, output, args.check)


if __name__ == "__main__":
    raise SystemExit(main())
