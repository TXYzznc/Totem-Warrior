"""Read-only audit of real-world texture scale in Oasis City building FBXs."""

from __future__ import annotations

import argparse
import json
import re
import statistics
import sys
from pathlib import Path

import bpy


def arguments():
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(values)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)


def classify(name: str, texture_set: str, dimensions: tuple[float, float, float]) -> str:
    upper = name.upper()
    if "CEILING" in upper or "ROOF" in upper or "CANOPY" in upper:
        return "roof_ceiling"
    if "_WALL_" in upper or re.search(r"_PART_\d+_", upper):
        return "wall_structure"
    if "_ST_" in upper or "STAIR" in upper or "STEP" in upper or "LAND" in upper:
        return "stairs_platforms"
    if "FLOOR" in upper or texture_set == "SANDSTONE":
        return "walkable_floor"
    if texture_set == "WOOD":
        return "wood_trim_openings"
    if texture_set == "TILE":
        return "feature_tile"
    if texture_set == "METAL":
        return "metalwork"
    if texture_set == "PLASTER":
        return "plaster_other"
    return "other"


def uv_extent(obj) -> tuple[float, float] | None:
    layer = obj.data.uv_layers.active
    if layer is None or not layer.data:
        return None
    us = [loop.uv.x for loop in layer.data]
    vs = [loop.uv.y for loop in layer.data]
    return max(us) - min(us), max(vs) - min(vs)


def material_lookup(manifest: dict) -> dict[str, str]:
    lookup = {}
    for entry in manifest.get("materials", []):
        for name in entry.get("objects", []):
            lookup[name] = entry.get("texture_set", "UNKNOWN")
    return lookup


def summarize(records: list[dict]) -> dict:
    groups = {}
    for record in records:
        groups.setdefault(record["category"], []).append(record)
    result = {}
    for category, items in groups.items():
        with_uv = [item for item in items if item["uv_span"] is not None]
        suspicious = [item for item in with_uv if item["max_dimension_m"] >= 2.0 and max(item["uv_span"]) <= 1.05]
        result[category] = {
            "objects": len(items),
            "with_uv": len(with_uv),
            "suspicious_stretched_or_single_fit": len(suspicious),
            "median_max_dimension_m": round(statistics.median(item["max_dimension_m"] for item in items), 4),
            "median_uv_max_span": round(statistics.median(max(item["uv_span"]) for item in with_uv), 4) if with_uv else None,
        }
    return result


def main():
    args = arguments()
    root = Path(args.root).resolve()
    output = Path(args.output).resolve()
    assets = []
    all_records = []
    for folder in sorted(root.iterdir()):
        match = re.match(r"^(BF-\d{2})_(.+)$", folder.name)
        if not folder.is_dir() or not match:
            continue
        models = list((folder / "Export" / "Models").glob("*.fbx"))
        manifest_path = folder / "Export" / "export_manifest.json"
        if len(models) != 1 or not manifest_path.exists():
            continue
        asset_id, display_name = match.groups()
        clear_scene()
        bpy.ops.import_scene.fbx(filepath=str(models[0]))
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        lookup = material_lookup(manifest)
        records = []
        for obj in bpy.context.scene.objects:
            if obj.type != "MESH":
                continue
            dimensions = tuple(round(float(value), 5) for value in obj.dimensions)
            uv = uv_extent(obj)
            texture_set = lookup.get(obj.name, "UNKNOWN")
            record = {
                "asset_id": asset_id,
                "object": obj.name,
                "texture_set": texture_set,
                "category": classify(obj.name, texture_set, dimensions),
                "dimensions_m": dimensions,
                "max_dimension_m": max(dimensions),
                "uv_span": tuple(round(value, 5) for value in uv) if uv else None,
                "polygons": len(obj.data.polygons),
            }
            records.append(record)
            all_records.append(record)
        assets.append({"asset_id": asset_id, "name": display_name, "objects": len(records), "categories": summarize(records)})

    suspicious = [record for record in all_records if record["uv_span"] is not None and record["max_dimension_m"] >= 2.0 and max(record["uv_span"]) <= 1.05]
    suspicious.sort(key=lambda record: record["max_dimension_m"], reverse=True)
    report = {
        "assets": assets,
        "global_categories": summarize(all_records),
        "suspicious_objects": len(suspicious),
        "largest_suspicious_examples": suspicious[:120],
        "criterion": "max world dimension >= 2m while the complete mesh UV range fits inside approximately one 0-1 tile",
    }
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"output": str(output), "assets": len(assets), "objects": len(all_records), "suspicious": len(suspicious)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
