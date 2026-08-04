"""Repair redundant/fragmented Oasis City building floors in staged FBXs.

The source buildings use a structural slab plus a same-material 3 cm finish
overlay.  Upper slabs are also split into many touching boxes, leaving internal
faces that can produce AO seams after import.  This script removes the redundant
overlay and rebuilds each structural slab as one closed, welded grid mesh while
preserving openings and world-aligned UV density.
"""

from __future__ import annotations

import argparse
import json
import math
import re
import sys
from collections import Counter, defaultdict, deque
from pathlib import Path

import bpy
from mathutils import Vector


METRES_PER_REPEAT = 3.2
OVERLAY_HEIGHT = 0.03
EPSILON = 1e-5


def parse_args() -> argparse.Namespace:
    values = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--output-root", required=True)
    parser.add_argument("--report", required=True)
    parser.add_argument("--only", default="")
    return parser.parse_args(values)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.images):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def discover(root: Path, only: str):
    for folder in sorted(root.iterdir()):
        match = re.match(r"^(BF-\d{2})_(.+)$", folder.name)
        if not folder.is_dir() or not match:
            continue
        asset_id, display_name = match.groups()
        if only and asset_id != only:
            continue
        models = sorted((folder / "Export" / "Models").glob("*.fbx"))
        manifest = folder / "Export" / "export_manifest.json"
        if len(models) == 1 and manifest.exists():
            yield asset_id, display_name, models[0], manifest


def manifest_floor_names(manifest: dict) -> set[str]:
    for entry in manifest.get("materials", []):
        if entry.get("name") == "MAT_BF_FLOOR":
            return set(entry.get("objects", []))
    raise RuntimeError("MAT_BF_FLOOR mapping is missing")


def object_bounds(obj: bpy.types.Object) -> tuple[Vector, Vector]:
    points = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    return (
        Vector(tuple(min(point[axis] for point in points) for axis in range(3))),
        Vector(tuple(max(point[axis] for point in points) for axis in range(3))),
    )


def scene_bounds(objects: list[bpy.types.Object]) -> tuple[Vector, Vector]:
    bounds = [object_bounds(obj) for obj in objects]
    return (
        Vector(tuple(min(item[0][axis] for item in bounds) for axis in range(3))),
        Vector(tuple(max(item[1][axis] for item in bounds) for axis in range(3))),
    )


def assert_axis_aligned_box(obj: bpy.types.Object, minimum: Vector, maximum: Vector) -> None:
    for vertex in obj.data.vertices:
        point = obj.matrix_world @ vertex.co
        for axis in range(3):
            if min(abs(point[axis] - minimum[axis]), abs(point[axis] - maximum[axis])) > EPSILON:
                raise RuntimeError(f"{obj.name} is not an axis-aligned floor box")


def rounded(value: float) -> float:
    return round(float(value), 6)


def level_key(value: float) -> float:
    """Quantize authored floor levels while retaining sub-millimetre geometry."""
    return round(float(value), 3)


def find_vertical_axis(floors: list[bpy.types.Object]) -> int:
    candidates = []
    for obj in floors:
        minimum, maximum = object_bounds(obj)
        dimensions = [maximum[axis] - minimum[axis] for axis in range(3)]
        candidates.append(min(range(3), key=lambda axis: dimensions[axis]))
    axis, count = Counter(candidates).most_common(1)[0]
    # Narrow landing strips can be thinner horizontally than the slab itself;
    # use the dominant axis, but still require a strong consensus.
    if count / len(candidates) < 0.8:
        raise RuntimeError(f"Floor thickness axis is ambiguous: {Counter(candidates)}")
    return axis


def connected_components(cells: set[tuple[int, int, int]]) -> list[set[tuple[int, int, int]]]:
    remaining = set(cells)
    components = []
    while remaining:
        start = remaining.pop()
        component = {start}
        queue = deque([start])
        while queue:
            x, y, z = queue.popleft()
            for neighbour in (
                (x - 1, y, z), (x + 1, y, z),
                (x, y - 1, z), (x, y + 1, z),
                (x, y, z - 1), (x, y, z + 1),
            ):
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    component.add(neighbour)
                    queue.append(neighbour)
        components.append(component)
    return components


def occupied_grid(boxes: list[tuple[float, float, float, float, float, float]]):
    x_edges = sorted({rounded(value) for box in boxes for value in (box[0], box[1])})
    y_edges = sorted({rounded(value) for box in boxes for value in (box[2], box[3])})
    z_edges = sorted({rounded(value) for box in boxes for value in (box[4], box[5])})
    occupied = set()
    for x_index in range(len(x_edges) - 1):
        x_mid = (x_edges[x_index] + x_edges[x_index + 1]) * 0.5
        for y_index in range(len(y_edges) - 1):
            y_mid = (y_edges[y_index] + y_edges[y_index + 1]) * 0.5
            for z_index in range(len(z_edges) - 1):
                z_mid = (z_edges[z_index] + z_edges[z_index + 1]) * 0.5
                if any(
                    x0 - EPSILON <= x_mid <= x1 + EPSILON
                    and y0 - EPSILON <= y_mid <= y1 + EPSILON
                    and z0 - EPSILON <= z_mid <= z1 + EPSILON
                    for x0, x1, y0, y1, z0, z1 in boxes
                ):
                    occupied.add((x_index, y_index, z_index))
    return x_edges, y_edges, z_edges, occupied


def build_union_mesh(name: str, source_objects: list[bpy.types.Object], vertical_axis: int) -> bpy.types.Object:
    if vertical_axis != 2:
        raise RuntimeError(f"{name}: expected Blender Z-up floors, got axis {vertical_axis}")
    source_bounds = [object_bounds(obj) for obj in source_objects]
    top = rounded(source_bounds[0][1][vertical_axis])
    if any(abs(item[1][vertical_axis] - top) > EPSILON for item in source_bounds):
        raise RuntimeError(f"{name} contains mixed top levels")
    boxes = [
        (rounded(item[0].x), rounded(item[1].x),
         rounded(item[0].y), rounded(item[1].y),
         rounded(item[0].z), rounded(item[1].z))
        for item in source_bounds
    ]
    x_edges, y_edges, z_edges, occupied = occupied_grid(boxes)
    if not occupied:
        raise RuntimeError(f"{name} produced no occupied floor cells")

    vertices: list[tuple[float, float, float]] = []
    faces: list[tuple[int, ...]] = []
    for component in connected_components(occupied):
        vertex_indices: dict[tuple[float, float, float], int] = {}

        def vertex(x: float, y: float, z: float) -> int:
            key = (rounded(x), rounded(y), rounded(z))
            if key not in vertex_indices:
                vertex_indices[key] = len(vertices)
                vertices.append(key)
            return vertex_indices[key]

        for x_index, y_index, z_index in sorted(component):
            x0, x1 = x_edges[x_index], x_edges[x_index + 1]
            y0, y1 = y_edges[y_index], y_edges[y_index + 1]
            z0, z1 = z_edges[z_index], z_edges[z_index + 1]
            boundaries = (
                ((x_index - 1, y_index, z_index), (vertex(x0, y0, z0), vertex(x0, y0, z1), vertex(x0, y1, z1), vertex(x0, y1, z0))),
                ((x_index + 1, y_index, z_index), (vertex(x1, y1, z0), vertex(x1, y1, z1), vertex(x1, y0, z1), vertex(x1, y0, z0))),
                ((x_index, y_index - 1, z_index), (vertex(x1, y0, z0), vertex(x1, y0, z1), vertex(x0, y0, z1), vertex(x0, y0, z0))),
                ((x_index, y_index + 1, z_index), (vertex(x0, y1, z0), vertex(x0, y1, z1), vertex(x1, y1, z1), vertex(x1, y1, z0))),
                ((x_index, y_index, z_index - 1), (vertex(x0, y1, z0), vertex(x1, y1, z0), vertex(x1, y0, z0), vertex(x0, y0, z0))),
                ((x_index, y_index, z_index + 1), (vertex(x0, y0, z1), vertex(x1, y0, z1), vertex(x1, y1, z1), vertex(x0, y1, z1))),
            )
            for neighbour, face in boundaries:
                if neighbour not in component:
                    faces.append(face)

    mesh = bpy.data.meshes.new(name + "_MESH")
    mesh.from_pydata(vertices, [], faces)
    mesh.validate(clean_customdata=False)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    material = next((slot.material for source in source_objects for slot in source.material_slots if slot.material), None)
    if material:
        mesh.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = False
    project_uv(obj)
    return obj


def project_uv(obj: bpy.types.Object) -> None:
    mesh = obj.data
    layer = mesh.uv_layers.new(name="UVMap")
    for polygon in mesh.polygons:
        normal = polygon.normal.normalized()
        dominant = max(range(3), key=lambda axis: abs(normal[axis]))
        for loop_index in polygon.loop_indices:
            point = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            if dominant == 0:
                u, v = point.z / METRES_PER_REPEAT, point.y / METRES_PER_REPEAT
            elif dominant == 1:
                u, v = point.x / METRES_PER_REPEAT, point.z / METRES_PER_REPEAT
            else:
                u, v = point.x / METRES_PER_REPEAT, point.y / METRES_PER_REPEAT
            if normal[dominant] < 0:
                u = -u
            layer.data[loop_index].uv = (u, v)
    mesh.uv_layers.active = layer
    mesh.update()


def export_fbx(objects: list[bpy.types.Object], output_path: Path) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    output_path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=str(output_path), use_selection=True, object_types={"MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", use_tspace=True,
        use_custom_props=False, add_leaf_bones=False, bake_anim=False,
        path_mode="AUTO", embed_textures=False,
    )


def process(asset_id: str, display_name: str, model_path: Path, manifest_path: Path, output_root: Path) -> dict:
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(model_path), use_custom_normals=True)
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    floor_names = manifest_floor_names(manifest)
    meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    by_name = {obj.name: obj for obj in meshes}
    floors = [by_name[name] for name in sorted(floor_names) if name in by_name]
    if len(floors) != len(floor_names):
        missing = sorted(floor_names - by_name.keys())
        raise RuntimeError(f"{asset_id}: manifest floor objects missing: {missing}")
    vertical_axis = find_vertical_axis(floors)
    before_min, before_max = scene_bounds(meshes)
    before_polygons = sum(len(obj.data.polygons) for obj in meshes)

    records = []
    for obj in floors:
        minimum, maximum = object_bounds(obj)
        assert_axis_aligned_box(obj, minimum, maximum)
        records.append({"object": obj, "min": minimum, "max": maximum, "top": level_key(maximum[vertical_axis])})
    top_levels = sorted({item["top"] for item in records})
    overlay_levels = {
        level for level in top_levels
        if any(abs((level - OVERLAY_HEIGHT) - candidate) <= EPSILON for candidate in top_levels)
    }
    if not overlay_levels:
        raise RuntimeError(f"{asset_id}: no 3 cm finish overlays detected")
    overlays = [item for item in records if item["top"] in overlay_levels]
    structural = [item for item in records if item["top"] not in overlay_levels]

    groups: dict[float, list[bpy.types.Object]] = defaultdict(list)
    for item in structural:
        key = level_key(item["max"][vertical_axis])
        groups[key].append(item["object"])

    new_objects = []
    retained_objects = []
    source_floor_objects = [item["object"] for item in records]
    for index, (top, objects) in enumerate(sorted(groups.items()), 1):
        if len(objects) == 1:
            retained_objects.extend(objects)
            continue
        normalized = asset_id.replace("-", "")
        name = f"{normalized}_STR_FLOOR_MERGED_{index:02d}_{int(round(top * 1000)):05d}"
        new_objects.append(build_union_mesh(name, objects, vertical_axis))

    removed_names = {obj.name for obj in source_floor_objects if obj not in retained_objects}
    for obj in source_floor_objects:
        if obj not in retained_objects:
            bpy.data.objects.remove(obj, do_unlink=True)

    output_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    after_min, after_max = scene_bounds(output_meshes)
    bounds_delta = max(
        *(abs(before_min[axis] - after_min[axis]) for axis in range(3)),
        *(abs(before_max[axis] - after_max[axis]) for axis in range(3)),
    )
    if bounds_delta > EPSILON:
        raise RuntimeError(f"{asset_id}: building bounds changed by {bounds_delta:.9f} m")
    output_path = output_root / asset_id / f"{asset_id}_{display_name}.fbx"
    export_fbx(output_meshes, output_path)
    floor_output_names = sorted([obj.name for obj in retained_objects + new_objects])
    return {
        "asset_id": asset_id,
        "name": display_name,
        "source": str(model_path),
        "manifest": str(manifest_path),
        "output": str(output_path),
        "vertical_axis": vertical_axis,
        "objects_before": len(meshes),
        "objects_after": len(output_meshes),
        "polygons_before": before_polygons,
        "polygons_after": sum(len(obj.data.polygons) for obj in output_meshes),
        "floor_objects_before": len(source_floor_objects),
        "floor_objects_after": len(floor_output_names),
        "overlay_objects_removed": len(overlays),
        "merged_structural_objects_removed": len(removed_names) - len(overlays),
        "floor_output_names": floor_output_names,
        "bounds_before": [list(before_min), list(before_max)],
        "bounds_after": [list(after_min), list(after_max)],
        "bounds_delta_m": bounds_delta,
        "bytes": output_path.stat().st_size,
    }


def main() -> None:
    args = parse_args()
    root = Path(args.root).resolve()
    output_root = Path(args.output_root).resolve()
    results = []
    for index, asset in enumerate(discover(root, args.only), 1):
        print(f"[FLOOR-REPAIR] {index} {asset[0]}", flush=True)
        results.append(process(*asset, output_root))
    if not results:
        raise RuntimeError("No matching building assets")
    report = {
        "blender_version": bpy.app.version_string,
        "strategy": "remove redundant 3 cm same-material finish overlays; weld structural slabs; preserve openings and metre-scaled UV0",
        "results": results,
    }
    report_path = Path(args.report).resolve()
    report_path.parent.mkdir(parents=True, exist_ok=True)
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(results), "report": str(report_path)}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
