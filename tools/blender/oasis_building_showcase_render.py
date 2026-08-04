"""Render exterior and cutaway axonometric previews for Oasis City building FBXs.

Run with Blender, for example:
  blender --background --factory-startup --python oasis_building_showcase_render.py -- \
    --root <building-design-directory> --output <output-directory>

The script imports the final FBX files read-only. It never saves a .blend or FBX.
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


VIEW_WIDTH = 1400
VIEW_HEIGHT = 1000
CAMERA_DIRECTION = Vector((1.35, -1.55, 1.15))


def parse_args() -> argparse.Namespace:
    argv = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", required=True)
    parser.add_argument("--output", required=True)
    parser.add_argument("--only", default="", help="Optional asset id, e.g. BF-01")
    parser.add_argument("--model-override", default="", help="Optional FBX used for a single --only sample")
    parser.add_argument("--width", type=int, default=VIEW_WIDTH, help="Output width for each view")
    parser.add_argument("--height", type=int, default=VIEW_HEIGHT, help="Output height for each view")
    return parser.parse_args(argv)


def clear_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def discover_assets(root: Path, only: str) -> list[tuple[str, str, Path, Path]]:
    assets = []
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
            assets.append((asset_id, display_name, models[0], manifest))
    return assets


def make_pbr_material(texture_dir: Path, texture_set: str, pbr_manifest: dict) -> bpy.types.Material:
    mat = bpy.data.materials.new(f"SHOWCASE_{texture_set}")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])

    set_info = pbr_manifest.get("sets", {}).get(texture_set, {})
    base_name = set_info.get("base_color", f"T_OASIS_{texture_set}_BC.jpg")
    normal_name = set_info.get("normal_map", f"T_OASIS_{texture_set}_N.jpg")
    orm_name = set_info.get("orm", f"T_OASIS_{texture_set}_ORM.jpg")

    base_path = texture_dir / base_name
    if base_path.exists():
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(base_path), check_existing=True)
        tex.image.colorspace_settings.name = "sRGB"
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])

    normal_path = texture_dir / normal_name
    if normal_path.exists():
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(normal_path), check_existing=True)
        tex.image.colorspace_settings.name = "Non-Color"
        normal = nodes.new("ShaderNodeNormalMap")
        normal.inputs["Strength"].default_value = min(float(set_info.get("normal_strength", 1.0)), 2.0)
        links.new(tex.outputs["Color"], normal.inputs["Color"])
        links.new(normal.outputs["Normal"], bsdf.inputs["Normal"])

    orm_path = texture_dir / orm_name
    if orm_path.exists():
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(str(orm_path), check_existing=True)
        tex.image.colorspace_settings.name = "Non-Color"
        separate = nodes.new("ShaderNodeSeparateColor")
        links.new(tex.outputs["Color"], separate.inputs["Color"])
        links.new(separate.outputs["Green"], bsdf.inputs["Roughness"])
        links.new(separate.outputs["Blue"], bsdf.inputs["Metallic"])
    else:
        bsdf.inputs["Roughness"].default_value = float(set_info.get("roughness", 0.65))
        bsdf.inputs["Metallic"].default_value = float(set_info.get("metallic", 0.0))
    return mat


def apply_manifest_materials(manifest_path: Path) -> None:
    manifest = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    texture_dir = manifest_path.parent / "Textures"
    pbr_path = texture_dir / "pbr_texture_manifest.json"
    pbr_manifest = json.loads(pbr_path.read_text(encoding="utf-8-sig")) if pbr_path.exists() else {}
    materials: dict[str, bpy.types.Material] = {}
    mapped_objects: set[str] = set()

    for mapping in manifest.get("materials", []):
        texture_set = mapping.get("texture_set", "PLASTER")
        material = materials.get(texture_set)
        if material is None:
            material = make_pbr_material(texture_dir, texture_set, pbr_manifest)
            materials[texture_set] = material
        for object_name in mapping.get("objects", []):
            obj = bpy.data.objects.get(object_name)
            if obj is None or obj.type != "MESH":
                continue
            obj.data.materials.clear()
            obj.data.materials.append(material)
            mapped_objects.add(obj.name)

    fallback = materials.get("PLASTER") or make_pbr_material(texture_dir, "PLASTER", pbr_manifest)
    for obj in bpy.context.scene.objects:
        if obj.type == "MESH" and obj.name not in mapped_objects:
            obj.data.materials.clear()
            obj.data.materials.append(fallback)


def mesh_bounds(visible_only: bool = True) -> tuple[Vector, Vector]:
    points = []
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or (visible_only and obj.hide_render) or obj.name == "SHOWCASE_GROUND":
            continue
        points.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not points:
        raise RuntimeError("No visible mesh bounds")
    return (
        Vector((min(p.x for p in points), min(p.y for p in points), min(p.z for p in points))),
        Vector((max(p.x for p in points), max(p.y for p in points), max(p.z for p in points))),
    )


def look_at(camera: bpy.types.Object, target: Vector) -> None:
    camera.rotation_euler = (target - camera.location).to_track_quat("-Z", "Y").to_euler()


def frame_camera(camera: bpy.types.Object, bounds_min: Vector, bounds_max: Vector) -> None:
    target = (bounds_min + bounds_max) * 0.5
    size = bounds_max - bounds_min
    camera.location = target + CAMERA_DIRECTION.normalized() * max(size.length * 2.5, 30.0)
    look_at(camera, target)
    bpy.context.view_layer.update()
    inverse = camera.matrix_world.inverted()
    projected = []
    for x in (bounds_min.x, bounds_max.x):
        for y in (bounds_min.y, bounds_max.y):
            for z in (bounds_min.z, bounds_max.z):
                projected.append(inverse @ Vector((x, y, z)))
    width = max(p.x for p in projected) - min(p.x for p in projected)
    height = max(p.y for p in projected) - min(p.y for p in projected)
    camera.data.ortho_scale = max(height, width / (VIEW_WIDTH / VIEW_HEIGHT)) * 1.24


def setup_render_scene() -> bpy.types.Object:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = VIEW_WIDTH
    scene.render.resolution_y = VIEW_HEIGHT
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.film_transparent = False
    scene.render.use_file_extension = True
    scene.render.resolution_percentage = 100
    scene.view_settings.look = "AgX - Medium Low Contrast"
    scene.world.color = (0.025, 0.035, 0.055)
    bg = scene.world.node_tree.nodes.get("Background") if scene.world.use_nodes else None
    if not scene.world.use_nodes:
        scene.world.use_nodes = True
        bg = scene.world.node_tree.nodes.get("Background")
    bg.inputs["Color"].default_value = (0.035, 0.055, 0.085, 1.0)
    bg.inputs["Strength"].default_value = 0.65

    camera_data = bpy.data.cameras.new("SHOWCASE_CAMERA")
    camera_data.type = "ORTHO"
    camera_data.lens = 50
    camera = bpy.data.objects.new("SHOWCASE_CAMERA", camera_data)
    scene.collection.objects.link(camera)
    scene.camera = camera
    return camera


def add_lighting(bounds_min: Vector, bounds_max: Vector) -> None:
    target = (bounds_min + bounds_max) * 0.5
    span = max((bounds_max - bounds_min).length, 10.0)
    for name, kind, energy, location, size in (
        ("SHOWCASE_KEY", "AREA", 2400.0, target + Vector((-0.5, -0.8, 1.5)) * span, span * 1.1),
        ("SHOWCASE_FILL", "AREA", 1900.0, target + Vector((1.1, 0.4, 0.9)) * span, span * 1.0),
    ):
        data = bpy.data.lights.new(name, kind)
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        obj = bpy.data.objects.new(name, data)
        bpy.context.scene.collection.objects.link(obj)
        obj.location = location
        obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()
    sun_data = bpy.data.lights.new("SHOWCASE_SUN", "SUN")
    sun_data.energy = 1.4
    sun_data.angle = math.radians(20)
    sun = bpy.data.objects.new("SHOWCASE_SUN", sun_data)
    bpy.context.scene.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(35), math.radians(-25), math.radians(-35))


def add_ground(bounds_min: Vector, bounds_max: Vector) -> None:
    size = max(bounds_max.x - bounds_min.x, bounds_max.y - bounds_min.y) * 3.0
    bpy.ops.mesh.primitive_plane_add(size=max(size, 20.0), location=((bounds_min.x + bounds_max.x) / 2, (bounds_min.y + bounds_max.y) / 2, bounds_min.z - 0.025))
    plane = bpy.context.object
    plane.name = "SHOWCASE_GROUND"
    mat = bpy.data.materials.new("SHOWCASE_GROUND_MAT")
    mat.diffuse_color = (0.055, 0.07, 0.09, 1.0)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (0.055, 0.07, 0.09, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.9
    plane.data.materials.append(mat)


def set_cutaway(enabled: bool) -> int:
    hidden = 0
    roof_tokens = ("CEILING", "ROOF", "CANOPY")
    # FBX import maps the architectural N wall to -Y and W wall to +X.
    # Those are the two sides nearest CAMERA_DIRECTION.
    near_wall_tokens = ("_WALL_N_", "_WALL_W_", "_W_F1_N_", "_W_F2_N_", "_W_F1_W_", "_W_F2_W_")
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH" or obj.name == "SHOWCASE_GROUND":
            continue
        upper = obj.name.upper()
        should_hide = enabled and (any(token in upper for token in roof_tokens) or any(token in upper for token in near_wall_tokens))
        obj.hide_render = should_hide
        if should_hide:
            hidden += 1
    return hidden


def render_view(path: Path, camera: bpy.types.Object) -> None:
    bounds_min, bounds_max = mesh_bounds()
    frame_camera(camera, bounds_min, bounds_max)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_asset(asset_id: str, display_name: str, fbx_path: Path, manifest_path: Path, output: Path) -> dict:
    clear_scene()
    bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
    apply_manifest_materials(manifest_path)
    camera = setup_render_scene()
    full_min, full_max = mesh_bounds()
    add_lighting(full_min, full_max)
    add_ground(full_min, full_max)

    asset_output = output / asset_id
    asset_output.mkdir(parents=True, exist_ok=True)
    set_cutaway(False)
    exterior_path = asset_output / f"{asset_id}_Exterior_Axon.png"
    render_view(exterior_path, camera)
    hidden = set_cutaway(True)
    interior_path = asset_output / f"{asset_id}_Interior_Cutaway.png"
    render_view(interior_path, camera)
    return {
        "asset_id": asset_id,
        "name": display_name,
        "fbx": str(fbx_path),
        "exterior": str(exterior_path),
        "interior": str(interior_path),
        "cutaway_hidden_objects": hidden,
    }


def main() -> None:
    global VIEW_WIDTH, VIEW_HEIGHT
    args = parse_args()
    if args.width < 640 or args.height < 480:
        raise ValueError("Render resolution must be at least 640x480")
    VIEW_WIDTH = args.width
    VIEW_HEIGHT = args.height
    root = Path(args.root).resolve()
    output = Path(args.output).resolve()
    output.mkdir(parents=True, exist_ok=True)
    assets = discover_assets(root, args.only)
    if not assets:
        raise RuntimeError("No matching building assets found")
    if args.model_override:
        if len(assets) != 1:
            raise RuntimeError("--model-override requires exactly one asset selected by --only")
        asset_id, display_name, _, manifest_path = assets[0]
        assets = [(asset_id, display_name, Path(args.model_override).resolve(), manifest_path)]
    results = []
    for index, (asset_id, display_name, fbx_path, manifest_path) in enumerate(assets, 1):
        print(f"[SHOWCASE] {index}/{len(assets)} {asset_id} {display_name}", flush=True)
        results.append(render_asset(asset_id, display_name, fbx_path, manifest_path, output))
    report_path = output / "render_manifest.json"
    report_path.write_text(json.dumps({"view_resolution": [VIEW_WIDTH, VIEW_HEIGHT], "assets": results}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[SHOWCASE] complete: {report_path}", flush=True)


if __name__ == "__main__":
    main()
