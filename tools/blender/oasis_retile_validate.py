"""Round-trip validation for staged real-world UV building FBXs."""

from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


def args():
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--retile-report", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(values)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def inspect(path: Path):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(path), use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    expanded = 0
    invalid_uv = []
    for obj in meshes:
        layer = obj.data.uv_layers.active
        if layer is None:
            invalid_uv.append(obj.name)
            continue
        values = [(loop.uv.x, loop.uv.y) for loop in layer.data]
        if not values or not all(math.isfinite(v) for pair in values for v in pair):
            invalid_uv.append(obj.name)
            continue
        span = max(max(u for u, _ in values) - min(u for u, _ in values), max(v for _, v in values) - min(v for _, v in values))
        if span > 1.05:
            expanded += 1
    return {
        "names": sorted(obj.name for obj in meshes),
        "objects": len(meshes),
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "bounds_min": list(minimum),
        "bounds_max": list(maximum),
        "expanded_uv_objects": expanded,
        "invalid_uv_objects": invalid_uv,
    }


def max_delta(left, right):
    return max(abs(a - b) for a, b in zip(left, right))


def main():
    parsed = args()
    retile = json.loads(Path(parsed.retile_report).read_text(encoding="utf-8"))
    results = []
    for index, item in enumerate(retile["results"], 1):
        print(f"[VALIDATE] {index}/{len(retile['results'])} {item['asset_id']}", flush=True)
        source = inspect(Path(item["source"]))
        staged = inspect(Path(item["output"]))
        bounds_delta = max(
            max_delta(source["bounds_min"], staged["bounds_min"]),
            max_delta(source["bounds_max"], staged["bounds_max"]),
        )
        success = (
            source["names"] == staged["names"]
            and source["polygons"] == staged["polygons"]
            and bounds_delta <= 0.00001
            and staged["expanded_uv_objects"] > 0
            and not staged["invalid_uv_objects"]
        )
        results.append({
            "asset_id": item["asset_id"],
            "success": success,
            "bounds_delta_m": bounds_delta,
            "source_objects": source["objects"],
            "staged_objects": staged["objects"],
            "source_polygons": source["polygons"],
            "staged_polygons": staged["polygons"],
            "expanded_uv_objects": staged["expanded_uv_objects"],
            "invalid_uv_objects": staged["invalid_uv_objects"],
        })
    report = {"success": all(item["success"] for item in results), "results": results}
    output = Path(parsed.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": report["success"], "count": len(results), "output": str(output)}, ensure_ascii=False))
    if not report["success"]:
        raise SystemExit(2)


if __name__ == "__main__":
    main()
