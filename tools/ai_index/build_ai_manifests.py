#!/usr/bin/env python3
"""Generate small, factual project manifests without creating a second design authority."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path
from typing import Any


ROOT = Path(__file__).resolve().parents[2]
OUTPUT_DIR = ROOT / "GameData/AIData/ProjectManifests"
SCRIPT_ROOT = ROOT / "Assets/Game/Scripts"
TABLE_ROOT = ROOT / "GameData/AIData/DataTables/Business"
TEST_ROOT = ROOT / "Assets/Game/Tests"
ASSET_ROOT = ROOT / "Assets/Game"

ART_EXTENSIONS = {
    ".anim", ".controller", ".fbx", ".jpeg", ".jpg", ".mat", ".mp3",
    ".obj", ".ogg", ".otf", ".png", ".prefab", ".psd", ".shader",
    ".shadergraph", ".tga", ".tif", ".tiff", ".ttf", ".wav",
}
ART_PATH_SEGMENTS = {
    "Audio", "Font", "Material", "Materials", "Models", "Shader", "Shaders",
    "Prefab", "Prefabs", "Sprite", "Sprites", "VFX",
}


def relative(path: Path) -> str:
    return path.relative_to(ROOT).as_posix()


def json_text(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, indent=2) + "\n"


def read_json(path: Path) -> dict[str, Any]:
    try:
        value = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, UnicodeDecodeError, json.JSONDecodeError) as exc:
        return {"readError": str(exc)}
    return value if isinstance(value, dict) else {"readError": "Root value is not an object."}


def build_modules() -> dict[str, Any]:
    modules: list[dict[str, Any]] = []
    if SCRIPT_ROOT.exists():
        for directory in sorted((item for item in SCRIPT_ROOT.iterdir() if item.is_dir()), key=lambda item: item.name.lower()):
            scripts = sorted(relative(path) for path in directory.rglob("*.cs"))
            if scripts:
                modules.append({
                    "name": directory.name,
                    "path": relative(directory),
                    "scriptCount": len(scripts),
                    "scripts": scripts,
                })
    return {
        "schemaVersion": 1,
        "generatedBy": relative(Path(__file__).resolve()),
        "designAuthority": "Docs/GameDesign/目录.md",
        "count": len(modules),
        "modules": modules,
    }


def build_datatables() -> dict[str, Any]:
    tables: list[dict[str, Any]] = []
    for path in sorted(TABLE_ROOT.glob("*.json"), key=lambda item: item.name.lower()):
        source = read_json(path)
        rows = source.get("rows", [])
        columns = source.get("columns", [])
        tables.append({
            "tableName": source.get("tableName", path.stem),
            "path": relative(path),
            "sourceExcel": source.get("sourceExcel", ""),
            "rowCount": len(rows) if isinstance(rows, list) else 0,
            "columns": [
                column.get("name", "")
                for column in columns
                if isinstance(column, dict) and column.get("name")
            ] if isinstance(columns, list) else [],
            **({"readError": source["readError"]} if "readError" in source else {}),
        })
    return {
        "schemaVersion": 1,
        "generatedBy": relative(Path(__file__).resolve()),
        "sourceRoot": relative(TABLE_ROOT),
        "count": len(tables),
        "tables": tables,
    }


def is_art_asset(path: Path) -> bool:
    if path.suffix.lower() not in ART_EXTENSIONS:
        return False
    relative_parts = path.relative_to(ASSET_ROOT).parts
    return bool(set(relative_parts) & ART_PATH_SEGMENTS)


def build_assets() -> dict[str, Any]:
    assets = [
        {
            "path": relative(path),
            "extension": path.suffix.lower(),
            "approved": True,
        }
        for path in sorted(ASSET_ROOT.rglob("*"), key=lambda item: relative(item).lower())
        if path.is_file() and is_art_asset(path)
    ]
    return {
        "schemaVersion": 1,
        "generatedBy": relative(Path(__file__).resolve()),
        "sourceRoot": relative(ASSET_ROOT),
        "assetPolicy": "Every existing art asset under Assets/Game is approved and imported; absent required art has not been made.",
        "count": len(assets),
        "assets": assets,
    }


def build_tests() -> dict[str, Any]:
    tests = sorted(relative(path) for path in TEST_ROOT.rglob("*.cs")) if TEST_ROOT.exists() else []
    diagnostics_root = SCRIPT_ROOT / "Editor/Diagnostics"
    diagnostics = sorted(relative(path) for path in diagnostics_root.rglob("*.cs")) if diagnostics_root.exists() else []
    return {
        "schemaVersion": 1,
        "generatedBy": relative(Path(__file__).resolve()),
        "testCount": len(tests),
        "tests": tests,
        "diagnosticScenarioCount": len(diagnostics),
        "diagnosticScenarios": diagnostics,
    }


def expected_outputs() -> dict[Path, str]:
    return {
        OUTPUT_DIR / "modules.json": json_text(build_modules()),
        OUTPUT_DIR / "datatables.json": json_text(build_datatables()),
        OUTPUT_DIR / "assets.json": json_text(build_assets()),
        OUTPUT_DIR / "tests.json": json_text(build_tests()),
    }


def check_outputs(outputs: dict[Path, str]) -> int:
    stale = [relative(path) for path, content in outputs.items() if not path.exists() or path.read_text(encoding="utf-8-sig") != content]
    unexpected = sorted(
        relative(path)
        for path in OUTPUT_DIR.glob("*.json")
        if path not in outputs
    ) if OUTPUT_DIR.exists() else []
    if stale or unexpected:
        print("AI project manifests are stale.")
        for path in stale:
            print(f"- update: {path}")
        for path in unexpected:
            print(f"- remove obsolete: {path}")
        return 1
    print("AI project manifests are current.")
    return 0


def write_outputs(outputs: dict[Path, str]) -> None:
    OUTPUT_DIR.mkdir(parents=True, exist_ok=True)
    for path in OUTPUT_DIR.glob("*.json"):
        if path not in outputs:
            path.unlink()
    for path, content in outputs.items():
        path.write_text(content, encoding="utf-8", newline="\n")
        print(f"Wrote {relative(path)}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check generated manifests without writing files.")
    args = parser.parse_args()
    outputs = expected_outputs()
    if args.check:
        return check_outputs(outputs)
    write_outputs(outputs)
    return 0


if __name__ == "__main__":
    sys.exit(main())
