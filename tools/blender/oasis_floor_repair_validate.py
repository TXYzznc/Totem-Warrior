"""Round-trip validation for staged Oasis City floor-repaired FBXs."""

from __future__ import annotations

import argparse
import bmesh
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector


TOLERANCE = 1e-5


def parse_args() -> argparse.Namespace:
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--repair-report", required=True)
    parser.add_argument("--output", required=True)
    return parser.parse_args(values)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def bounds(obj: bpy.types.Object) -> tuple[list[float], list[float]]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        [min(point[axis] for point in points) for axis in range(3)],
        [max(point[axis] for point in points) for axis in range(3)],
    )


def inspect(path: Path) -> dict:
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(path), use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    signatures = {}
    invalid_uv = []
    for obj in meshes:
        minimum, maximum = bounds(obj)
        signatures[obj.name] = {
            "bounds": minimum + maximum,
            "polygons": len(obj.data.polygons),
            "vertices": len(obj.data.vertices),
        }
        layer = obj.data.uv_layers.active
        values = [] if layer is None else [(loop.uv.x, loop.uv.y) for loop in layer.data]
        if not values or not all(math.isfinite(value) for pair in values for value in pair):
            invalid_uv.append(obj.name)
    all_points = [value for signature in signatures.values() for value in (signature["bounds"][:3], signature["bounds"][3:])]
    scene_min = [min(point[axis] for point in all_points) for axis in range(3)]
    scene_max = [max(point[axis] for point in all_points) for axis in range(3)]
    return {
        "objects": meshes,
        "signatures": signatures,
        "scene_bounds": scene_min + scene_max,
        "invalid_uv": invalid_uv,
        "polygons": sum(len(obj.data.polygons) for obj in meshes),
        "vertices": sum(len(obj.data.vertices) for obj in meshes),
    }


def max_delta(left: list[float], right: list[float]) -> float:
    return max(abs(a - b) for a, b in zip(left, right))


def validate_floor_mesh(obj: bpy.types.Object) -> dict:
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    non_manifold_edges = sum(1 for edge in bm.edges if not edge.is_manifold)
    ngons = sum(1 for face in bm.faces if len(face.verts) > 4)
    signed_volume = bm.calc_volume(signed=True)
    bm.free()
    layer = obj.data.uv_layers.active
    uv_span = 0.0
    if layer and layer.data:
        us = [loop.uv.x for loop in layer.data]
        vs = [loop.uv.y for loop in layer.data]
        uv_span = max(max(us) - min(us), max(vs) - min(vs))
    return {
        "name": obj.name,
        "non_manifold_edges": non_manifold_edges,
        "ngons": ngons,
        "signed_volume": signed_volume,
        "uv_span": uv_span,
        "success": non_manifold_edges == 0 and ngons == 0 and signed_volume > 0 and uv_span > 0,
    }


def main() -> None:
    args = parse_args()
    repair = json.loads(Path(args.repair_report).read_text(encoding="utf-8"))
    results = []
    for index, item in enumerate(repair["results"], 1):
        print(f"[FLOOR-VALIDATE] {index}/{len(repair['results'])} {item['asset_id']}", flush=True)
        manifest = json.loads(Path(item["manifest"]).read_text(encoding="utf-8-sig"))
        original_floor_names = set()
        for mapping in manifest.get("materials", []):
            if mapping.get("name") == "MAT_BF_FLOOR":
                original_floor_names.update(mapping.get("objects", []))
        source = inspect(Path(item["source"]))
        source_non_floor = {
            name: signature for name, signature in source["signatures"].items()
            if name not in original_floor_names
        }
        staged = inspect(Path(item["output"]))
        staged_floor_names = set(item["floor_output_names"])
        staged_non_floor = {
            name: signature for name, signature in staged["signatures"].items()
            if name not in staged_floor_names
        }
        names_equal = source_non_floor.keys() == staged_non_floor.keys()
        non_floor_bounds_delta = 0.0
        non_floor_topology_equal = names_equal
        if names_equal:
            non_floor_bounds_delta = max(
                max_delta(source_non_floor[name]["bounds"], staged_non_floor[name]["bounds"])
                for name in source_non_floor
            )
            non_floor_topology_equal = all(
                source_non_floor[name]["polygons"] == staged_non_floor[name]["polygons"]
                and source_non_floor[name]["vertices"] == staged_non_floor[name]["vertices"]
                for name in source_non_floor
            )
        floor_objects = [obj for obj in staged["objects"] if obj.name in staged_floor_names]
        floor_checks = [validate_floor_mesh(obj) for obj in floor_objects]
        bounds_delta = max_delta(source["scene_bounds"], staged["scene_bounds"])
        success = (
            names_equal
            and non_floor_topology_equal
            and non_floor_bounds_delta <= TOLERANCE
            and bounds_delta <= TOLERANCE
            and len(floor_objects) == len(staged_floor_names)
            and all(check["success"] for check in floor_checks)
            and not staged["invalid_uv"]
            and item["overlay_objects_removed"] > 0
            and item["floor_objects_after"] < item["floor_objects_before"]
        )
        results.append({
            "asset_id": item["asset_id"],
            "success": success,
            "scene_bounds_delta_m": bounds_delta,
            "non_floor_bounds_delta_m": non_floor_bounds_delta,
            "non_floor_names_equal": names_equal,
            "non_floor_topology_equal": non_floor_topology_equal,
            "source_objects": len(source["objects"]),
            "staged_objects": len(staged["objects"]),
            "source_polygons": source["polygons"],
            "staged_polygons": staged["polygons"],
            "source_vertices": source["vertices"],
            "staged_vertices": staged["vertices"],
            "floor_checks": floor_checks,
            "invalid_uv_objects": staged["invalid_uv"],
        })
    report = {"success": all(item["success"] for item in results), "results": results}
    output = Path(args.output).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": report["success"], "count": len(results), "output": str(output)}, ensure_ascii=False), flush=True)
    if not report["success"]:
        raise SystemExit(2)


if __name__ == "__main__":
    main()
