"""Normalize exported Oasis City decoration FBXs to their documented metre dimensions."""

import argparse
import json
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


TARGETS = {
    "DE01": (1.0, 1.0, 0.08),
    "DE02": (0.3, 2.0, 0.02),
    "DE031": (2.4, 0.9, 0.15),
    "DE032": (2.4, 0.9, 0.15),
    "DE04": (0.35, 3.0, 0.18),
    "DE051": (2.5, 2.5, 0.3),
    "DE052": (2.5, 2.5, 0.3),
    "DE06": (1.2, 0.6, 0.55),
    "DE07": (1.4, 1.4, 3.5),
    "DE08": (0.5, 0.5, 1.2),
    "DE09": (1.5, 1.0, 0.8),
    "DE10": (3.0, 4.0, 0.15),
    "DE111": (1.2, 0.05, 2.0),
    "DE112": (1.2, 0.05, 2.0),
    "DE12": (0.5, 0.05, 2.2),
}


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-root", required=True)
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--report")
    return parser.parse_args(argv)


def base_key(key):
    return key.replace("-", "").split("_v", 1)[0]


def bounds(meshes):
    points = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return minimum, maximum


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)


def normalize_one(source_path, output_path, key, target):
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(source_path), use_custom_normals=True)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError(f"No mesh imported: {source_path}")

    minimum, maximum = bounds(meshes)
    size = maximum - minimum
    if min(size) <= 0:
        raise RuntimeError(f"Degenerate bounds for {key}: {tuple(size)}")

    # Documented dimensions are X width, Y depth and Z height in authoring space.
    # Unity mapping is (X, Z, Y), so Blender targets remain (width, depth, height).
    scale = Vector((target[0] / size.x, target[1] / size.y, target[2] / size.z))
    center_x = (minimum.x + maximum.x) * 0.5
    center_y = (minimum.y + maximum.y) * 0.5

    for obj in meshes:
        world = obj.matrix_world.copy()
        mesh = obj.data.copy() if obj.data.users > 1 else obj.data
        obj.data = mesh
        for vertex in mesh.vertices:
            point = world @ vertex.co
            vertex.co = Vector(
                (
                    (point.x - center_x) * scale.x,
                    (point.y - center_y) * scale.y,
                    (point.z - minimum.z) * scale.z,
                )
            )
        obj.parent = None
        obj.matrix_world = Matrix.Identity(4)

    # Cancel the horizontal 180-degree inversion introduced by Blender's baked
    # -Z/Y FBX conversion so Unity receives the documented Layout(X,Z,Y) mapping.
    for obj in meshes:
        for vertex in obj.data.vertices:
            vertex.co.x *= -1.0
            vertex.co.y *= -1.0

    for obj in list(bpy.context.scene.objects):
        if obj.type != "MESH":
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in meshes:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
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
    final_min, final_max = bounds(meshes)
    return {
        "key": key,
        "source": str(source_path),
        "output": str(output_path),
        "source_size_blender_xyz": [round(v, 6) for v in size],
        "target_size_blender_xyz": list(target),
        "result_size_blender_xyz": [round(v, 6) for v in final_max - final_min],
        "result_min_blender_xyz": [round(v, 6) for v in final_min],
        "bytes": output_path.stat().st_size,
        "axis_forward": "-Z",
        "axis_up": "Y",
        "bake_space_transform": True,
        "pre_export_rotation_z_degrees": 180,
        "unity_mapping": "Unity(X,Y,Z)=Layout(X,Z,Y)",
    }


def main():
    args = parse_args()
    source_root = Path(args.source_root).resolve()
    output_root = Path(args.output_root).resolve()
    results = []
    for manifest_path in sorted(source_root.rglob("export_manifest.json")):
        manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
        key = manifest.get("key")
        if not key or base_key(key) not in TARGETS:
            continue
        model_entry = next((entry for entry in manifest.get("files", []) if entry["path"].lower().endswith(".fbx")), None)
        if model_entry is None:
            raise RuntimeError(f"FBX entry missing: {manifest_path}")
        source_path = manifest_path.parent / model_entry["path"]
        normalized_key = key.replace("-", "")
        output_path = output_root / f"{normalized_key}.fbx"
        results.append(normalize_one(source_path, output_path, normalized_key, TARGETS[base_key(key)]))
        print(f"NORMALIZED {key}", flush=True)

    report = {
        "blender_version": bpy.app.version_string,
        "source_root": str(source_root),
        "output_root": str(output_root),
        "results": results,
    }
    report_path = Path(args.report).resolve() if args.report else output_root / "decoration_normalize_report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(results), "report": str(report_path)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
