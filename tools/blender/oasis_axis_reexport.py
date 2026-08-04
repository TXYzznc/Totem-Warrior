"""Re-export Oasis City FBX collections with transforms baked for Unity Y-up."""

import argparse
import json
import math
import os
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--collection", action="append", default=[])
    parser.add_argument("--all-buildings", action="store_true")
    parser.add_argument("--all-decorations", action="store_true")
    parser.add_argument("--report-only", action="store_true")
    parser.add_argument("--report")
    return parser.parse_args(argv)


def collection_bounds(objects):
    points = []
    for obj in objects:
        if obj.type != "MESH":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        return None
    minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
    maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
    return {
        "min": [round(v, 6) for v in minimum],
        "max": [round(v, 6) for v in maximum],
        "size_blender_xyz": [round(v, 6) for v in maximum - minimum],
    }


def mesh_object_bounds(objects):
    results = []
    for obj in sorted((item for item in objects if item.type == "MESH"), key=lambda item: item.name):
        points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
        minimum = Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points)))
        maximum = Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points)))
        results.append(
            {
                "name": obj.name,
                "center_blender_xyz": [round(v, 6) for v in (minimum + maximum) * 0.5],
                "size_blender_xyz": [round(v, 6) for v in maximum - minimum],
            }
        )
    return results


def export_collection(collection, output_root, report_only=False):
    objects = [obj for obj in collection.all_objects if obj.type in {"EMPTY", "MESH"}]
    if not objects:
        raise RuntimeError(f"Collection has no exportable objects: {collection.name}")

    mesh_objects = [obj for obj in objects if obj.type == "MESH"]
    output_path = output_root / f"{collection.name}.fbx"
    if not report_only:
        # Blender's baked -Z/Y FBX conversion imports into Unity as (-X, Z, -Y).
        # The project contract is Layout(X,Z,Y), so pre-rotate the authoring data
        # by 180 degrees around Blender Z to cancel that horizontal inversion.
        rotation = Matrix.Rotation(math.pi, 4, "Z")
        for obj in mesh_objects:
            world = obj.matrix_world.copy()
            mesh = obj.data.copy() if obj.data.users > 1 else obj.data
            obj.data = mesh
            for vertex in mesh.vertices:
                vertex.co = rotation @ (world @ vertex.co)
            obj.parent = None
            obj.matrix_world = Matrix.Identity(4)
        bpy.ops.object.select_all(action="DESELECT")
        for obj in mesh_objects:
            obj.select_set(True)
        bpy.context.view_layer.objects.active = mesh_objects[0] if mesh_objects else objects[0]
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
    return {
        "collection": collection.name,
        "objects": len(objects),
        "mesh_objects": len(mesh_objects),
        "meshes_with_uv": sum(1 for obj in mesh_objects if len(obj.data.uv_layers) > 0),
        "meshes_without_uv": sum(1 for obj in mesh_objects if len(obj.data.uv_layers) == 0),
        "bounds": collection_bounds(objects),
        "mesh_bounds": mesh_object_bounds(objects),
        "output": str(output_path),
        "bytes": output_path.stat().st_size if output_path.exists() else 0,
        "axis_forward": "-Z",
        "axis_up": "Y",
        "bake_space_transform": True,
        "pre_export_rotation_z_degrees": 180,
        "unity_mapping": "Unity(X,Y,Z)=Layout(X,Z,Y)",
    }


def main():
    args = parse_args()
    names = list(args.collection)
    if args.all_buildings:
        names.extend(collection.name for collection in bpy.data.collections["10_BUILDINGS"].children)
    if args.all_decorations:
        names.extend(collection.name for collection in bpy.data.collections["20_DECOR"].children)
    names = list(dict.fromkeys(names))
    if not names:
        raise RuntimeError("No collections selected for export.")

    output_root = Path(args.output_root).resolve()
    results = []
    for name in names:
        collection = bpy.data.collections.get(name)
        if collection is None:
            raise KeyError(f"Collection not found: {name}")
        results.append(export_collection(collection, output_root, args.report_only))
        print(f"{'INSPECTED' if args.report_only else 'EXPORTED'} {name}", flush=True)

    report = {
        "blender_version": bpy.app.version_string,
        "source_blend": bpy.data.filepath,
        "output_root": str(output_root),
        "results": results,
    }
    report_path = Path(args.report).resolve() if args.report else output_root / "axis_reexport_report.json"
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(results), "report": str(report_path)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
