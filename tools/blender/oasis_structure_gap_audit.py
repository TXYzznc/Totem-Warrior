"""Read-only structural seam audit for Oasis City building FBXs."""

from __future__ import annotations

import argparse
import json
import re
import sys
from collections import Counter
from pathlib import Path

import bpy
from mathutils import Vector


ROOF_TOKENS = ("CEILING", "ROOF", "CANOPY")
NEAR_WALL_TOKENS = ("_WALL_N_", "_WALL_W_", "_W_F1_N_", "_W_F2_N_", "_W_F1_W_", "_W_F2_W_")


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--only", default="")
    return parser.parse_args(argv)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points))),
        Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points))),
    )


def interval_overlap(a0: float, a1: float, b0: float, b1: float) -> float:
    return min(a1, b1) - max(a0, b0)


def interval_gap(a0: float, a1: float, b0: float, b1: float) -> float:
    if a1 < b0:
        return b0 - a1
    if b1 < a0:
        return a0 - b1
    return 0.0


def manifest_object_names(manifest: dict, material_name: str) -> set[str]:
    for entry in manifest.get("materials", []):
        if entry.get("name") == material_name:
            return set(entry.get("objects", []))
    return set()


def audit_asset(asset_id: str, fbx: Path, manifest_path: Path) -> dict:
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(fbx), use_custom_normals=True)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    floor_names = manifest_object_names(manifest, "MAT_BF_FLOOR")
    meshes = {obj.name: obj for obj in bpy.context.scene.objects if obj.type == "MESH"}
    mapped_names = {
        name
        for entry in manifest.get("materials", [])
        for name in entry.get("objects", [])
    }
    unmapped_mesh_names = sorted(set(meshes) - mapped_names)
    unmapped_mesh_materials = {
        name: [slot.material.name for slot in meshes[name].material_slots if slot.material]
        for name in unmapped_mesh_names
    }
    floors = []
    false_hidden = []
    for name in sorted(floor_names):
        obj = meshes.get(name)
        if obj is None:
            continue
        bmin, bmax = bounds(obj)
        record = {
            "name": name,
            "min": [round(v, 6) for v in bmin],
            "max": [round(v, 6) for v in bmax],
            "bottom_z": bmin.z,
            "top_z": bmax.z,
        }
        floors.append(record)
        upper = name.upper()
        current_rule_hides = any(token in upper for token in ROOF_TOKENS) or any(token in upper for token in NEAR_WALL_TOKENS)
        if current_rule_hides:
            false_hidden.append(record)

    first_level_walls = []
    first_level_ceilings = []
    second_level_walls = []
    for name, obj in meshes.items():
        upper = name.upper()
        bmin, bmax = bounds(obj)
        record = {"name": name, "bottom_z": round(bmin.z, 6), "top_z": round(bmax.z, 6)}
        if "L1_WALL" in upper or "_W_F1_" in upper:
            first_level_walls.append(record)
        if "L1_CEILING_ROOF" in upper:
            first_level_ceilings.append(record)
        if "L2_WALL" in upper or "_W_F2_" in upper:
            second_level_walls.append(record)

    narrow_gaps = []
    coplanar_contacts = 0
    level_mismatches = []
    for index, first in enumerate(floors):
        for second in floors[index + 1 :]:
            if abs(first["top_z"] - second["top_z"]) > 0.08:
                continue
            ax0, ay0, _ = first["min"]
            ax1, ay1, _ = first["max"]
            bx0, by0, _ = second["min"]
            bx1, by1, _ = second["max"]
            xgap = interval_gap(ax0, ax1, bx0, bx1)
            ygap = interval_gap(ay0, ay1, by0, by1)
            xover = interval_overlap(ax0, ax1, bx0, bx1)
            yover = interval_overlap(ay0, ay1, by0, by1)
            candidate_gap = None
            axis = None
            if yover > 0.1 and xgap <= 0.05:
                candidate_gap, axis = xgap, "X"
            elif xover > 0.1 and ygap <= 0.05:
                candidate_gap, axis = ygap, "Y"
            if candidate_gap is None:
                continue
            if candidate_gap <= 1e-5:
                coplanar_contacts += 1
            else:
                narrow_gaps.append({
                    "a": first["name"], "b": second["name"], "axis": axis,
                    "gap_m": round(candidate_gap, 6),
                    "top_delta_m": round(abs(first["top_z"] - second["top_z"]), 6),
                })
            top_delta = abs(first["top_z"] - second["top_z"])
            if candidate_gap <= 0.01 and top_delta > 0.005:
                level_mismatches.append({
                    "a": first["name"], "b": second["name"],
                    "top_delta_m": round(top_delta, 6),
                })

    top_levels = Counter(round(item["top_z"], 3) for item in floors)
    wall_top_levels = Counter(round(item["top_z"], 3) for item in first_level_walls)
    ceiling_bottom_levels = Counter(round(item["bottom_z"], 3) for item in first_level_ceilings)
    ceiling_top_levels = Counter(round(item["top_z"], 3) for item in first_level_ceilings)
    second_wall_bottom_levels = Counter(round(item["bottom_z"], 3) for item in second_level_walls)
    hidden_floor_bottom_levels = Counter(round(item["bottom_z"], 3) for item in false_hidden)
    return {
        "asset_id": asset_id,
        "fbx": str(fbx),
        "mesh_count": len(meshes),
        "unmapped_material_mesh_count": len(unmapped_mesh_names),
        "unmapped_material_mesh_names": unmapped_mesh_names,
        "unmapped_material_mesh_materials": unmapped_mesh_materials,
        "floor_manifest_count": len(floor_names),
        "floor_mesh_count": len(floors),
        "floor_top_levels": dict(sorted(top_levels.items())),
        "first_level_wall_top_levels": dict(sorted(wall_top_levels.items())),
        "first_level_ceiling_bottom_levels": dict(sorted(ceiling_bottom_levels.items())),
        "first_level_ceiling_top_levels": dict(sorted(ceiling_top_levels.items())),
        "second_level_wall_bottom_levels": dict(sorted(second_wall_bottom_levels.items())),
        "false_hidden_floor_bottom_levels": dict(sorted(hidden_floor_bottom_levels.items())),
        "cutaway_false_hidden_floor_count": len(false_hidden),
        "cutaway_false_hidden_floor_names": [item["name"] for item in false_hidden],
        "narrow_floor_gap_count": len(narrow_gaps),
        "narrow_floor_gaps": narrow_gaps,
        "coplanar_floor_contacts": coplanar_contacts,
        "floor_level_mismatch_count": len(level_mismatches),
        "floor_level_mismatches": level_mismatches,
    }


def main() -> None:
    args = parse_args()
    root = Path(args.root).resolve()
    results = []
    for folder in sorted(root.iterdir()):
        match = re.match(r"^(BF-\d{2})_", folder.name)
        if not folder.is_dir() or not match:
            continue
        asset_id = match.group(1)
        if args.only and asset_id != args.only:
            continue
        models = list((folder / "Export" / "Models").glob("*.fbx"))
        manifest = folder / "Export" / "export_manifest.json"
        if len(models) != 1 or not manifest.exists():
            continue
        print(f"[GAP-AUDIT] {asset_id}", flush=True)
        results.append(audit_asset(asset_id, models[0], manifest))
    report = {
        "blender_version": bpy.app.version_string,
        "results": results,
        "summary": {
            "assets": len(results),
            "false_hidden_floors": sum(item["cutaway_false_hidden_floor_count"] for item in results),
            "narrow_floor_gaps": sum(item["narrow_floor_gap_count"] for item in results),
            "level_mismatches": sum(item["floor_level_mismatch_count"] for item in results),
            "unmapped_material_meshes": sum(item["unmapped_material_mesh_count"] for item in results),
        },
    }
    output = Path(args.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[GAP-AUDIT] complete: {output}", flush=True)


if __name__ == "__main__":
    main()
