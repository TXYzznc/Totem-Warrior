"""Reproject Oasis City building UV0 at consistent real-world material scales.

This keeps the structural mesh/object hierarchy intact. Repetition happens inside
UV0, so Unity receives no extra tile GameObjects or Renderers.
"""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from pathlib import Path

import bpy
from mathutils import Vector


# Metres covered by one complete 0-1 texture repeat. The source images contain
# multiple pavers/boards per repeat; these values produce plausible module sizes.
METRES_PER_REPEAT = {
    "SANDSTONE": 3.2,
    "TILE": 2.4,
    "WOOD": 2.4,
    "PLASTER": 3.0,
    "METAL": 1.5,
}


def parse_args():
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--only", default="")
    parser.add_argument("--report")
    return parser.parse_args(values)


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for collection in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for item in list(collection):
            if item.users == 0:
                collection.remove(item)


def discover(root: Path, only: str):
    for folder in sorted(root.iterdir()):
        match = re.match(r"^(BF-\d{2})_(.+)$", folder.name)
        if not folder.is_dir() or not match:
            continue
        asset_id, display_name = match.groups()
        if only and asset_id != only:
            continue
        models = sorted((folder / "Export" / "Models").glob("*.fbx"))
        manifest_path = folder / "Export" / "export_manifest.json"
        if len(models) == 1 and manifest_path.exists():
            yield asset_id, display_name, models[0], manifest_path


def material_lookup(manifest: dict) -> dict[str, str]:
    result = {}
    for mapping in manifest.get("materials", []):
        texture_set = mapping.get("texture_set", "PLASTER")
        for object_name in mapping.get("objects", []):
            result[object_name] = texture_set
    return result


def bounds(objects):
    points = [obj.matrix_world @ Vector(corner) for obj in objects for corner in obj.bound_box]
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def uv_range(obj):
    layer = obj.data.uv_layers.active
    if layer is None or not layer.data:
        return None
    us = [loop.uv.x for loop in layer.data]
    vs = [loop.uv.y for loop in layer.data]
    return [min(us), min(vs), max(us), max(vs)]


def project_uv(obj, texture_set: str):
    if obj.data.users > 1:
        obj.data = obj.data.copy()
    mesh = obj.data
    layer = mesh.uv_layers.active or mesh.uv_layers.new(name="UVMap")
    scale = METRES_PER_REPEAT.get(texture_set, METRES_PER_REPEAT["PLASTER"])
    normal_matrix = obj.matrix_world.to_3x3().inverted().transposed()

    for polygon in mesh.polygons:
        normal = (normal_matrix @ polygon.normal).normalized()
        dominant = max(range(3), key=lambda axis: abs(normal[axis]))
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index]
            point = obj.matrix_world @ vertex.co
            if dominant == 0:  # X-facing: horizontal Z, vertical Y in imported Y-up FBX.
                u, v = point.z / scale, point.y / scale
            elif dominant == 1:  # Horizontal floor/roof: XZ projection.
                u, v = point.x / scale, point.z / scale
            else:  # Z-facing: horizontal X, vertical Y.
                u, v = point.x / scale, point.y / scale
            # Keep opposing faces consistently oriented without changing density.
            if normal[dominant] < 0:
                u = -u
            layer.data[loop_index].uv = (u, v)
    layer.name = "UVMap"
    mesh.uv_layers.active = layer
    mesh.update()


def export_fbx(objects, output_path: Path):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        use_tspace=True,
        use_custom_props=False,
        add_leaf_bones=False,
        bake_anim=False,
        path_mode="AUTO",
        embed_textures=False,
    )


def process(asset_id, display_name, model_path, manifest_path, output_root):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(model_path), use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    lookup = material_lookup(manifest)
    before_min, before_max = bounds(meshes)
    before_polygons = sum(len(obj.data.polygons) for obj in meshes)
    before_uv = {obj.name: uv_range(obj) for obj in meshes}
    by_material = {}
    for obj in meshes:
        texture_set = lookup.get(obj.name, "PLASTER")
        project_uv(obj, texture_set)
        by_material[texture_set] = by_material.get(texture_set, 0) + 1

    output_path = output_root / asset_id / f"{asset_id}_{display_name}.fbx"
    export_fbx(meshes, output_path)
    after_min, after_max = bounds(meshes)
    after_polygons = sum(len(obj.data.polygons) for obj in meshes)
    after_uv = {obj.name: uv_range(obj) for obj in meshes}
    expanded = sum(
        1
        for name, value in after_uv.items()
        if value and before_uv.get(name) and max(value[2] - value[0], value[3] - value[1]) > 1.05
    )
    return {
        "asset_id": asset_id,
        "name": display_name,
        "source": str(model_path),
        "output": str(output_path),
        "objects": len(meshes),
        "polygons_before": before_polygons,
        "polygons_after": after_polygons,
        "bounds_before": [list(before_min), list(before_max)],
        "bounds_after": [list(after_min), list(after_max)],
        "uv_expanded_objects": expanded,
        "objects_by_texture_set": by_material,
        "metres_per_repeat": METRES_PER_REPEAT,
        "bytes": output_path.stat().st_size,
    }


def main():
    args = parse_args()
    root = Path(args.root).resolve()
    output_root = Path(args.output_root).resolve()
    results = []
    for index, asset in enumerate(discover(root, args.only), 1):
        print(f"[RETILE] {index} {asset[0]}", flush=True)
        results.append(process(*asset, output_root))
    if not results:
        raise RuntimeError("No matching building assets")
    report = {
        "blender_version": bpy.app.version_string,
        "strategy": "continuous structural meshes with metre-scaled UV0; no extra Unity tile GameObjects",
        "results": results,
    }
    report_path = Path(args.report).resolve() if args.report else output_root / "retile_report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(results), "report": str(report_path)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
