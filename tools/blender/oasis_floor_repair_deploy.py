"""Deploy validated floor-repaired FBXs without touching Unity .meta files."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import shutil
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def atomic_copy(source: Path, destination: Path) -> None:
    temporary = destination.with_name(destination.name + ".floor-repair.tmp")
    shutil.copy2(source, temporary)
    os.replace(temporary, destination)


def update_manifest(path: Path, model_path: Path, item: dict, validation: dict, timestamp: str) -> None:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    floor_mapping = next(
        (entry for entry in data.get("materials", []) if entry.get("name") == "MAT_BF_FLOOR"),
        None,
    )
    if floor_mapping is None:
        raise RuntimeError(f"MAT_BF_FLOOR mapping missing: {path}")
    floor_mapping["objects"] = item["floor_output_names"]
    model_entry = next(
        (entry for entry in data.get("files", []) if entry.get("path", "").lower().endswith(".fbx")),
        None,
    )
    if model_entry is None:
        raise RuntimeError(f"FBX entry missing: {path}")
    model_entry["path"] = model_path.relative_to(path.parent).as_posix()
    model_entry["bytes"] = model_path.stat().st_size
    model_entry["sha256"] = sha256(model_path)
    geometry = data.setdefault("geometry", {})
    geometry["mesh_objects"] = validation["staged_objects"]
    geometry["vertices"] = validation["staged_vertices"]
    geometry["polygons"] = validation["staged_polygons"]
    geometry["triangles_estimated"] = validation["staged_polygons"] * 2
    data["floor_repaired_at_local"] = timestamp
    data["floor_repair_strategy"] = (
        "removed redundant 3 cm same-material finish overlays; rebuilt structural slabs as closed welded meshes; "
        "preserved openings, building bounds, orientation, origin and metre-scaled UV0"
    )
    data["floor_repair_stats"] = {
        "floor_objects_before": item["floor_objects_before"],
        "floor_objects_after": item["floor_objects_after"],
        "overlay_objects_removed": item["overlay_objects_removed"],
        "scene_bounds_delta_m": validation["scene_bounds_delta_m"],
    }
    path.write_text(json.dumps(data, ensure_ascii=False, indent=4), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repair-report", required=True)
    parser.add_argument("--validation-report", required=True)
    parser.add_argument("--desktop-current", required=True)
    parser.add_argument("--desktop-backup", required=True)
    parser.add_argument("--unity-model-root", required=True)
    parser.add_argument("--output-report", required=True)
    args = parser.parse_args()

    repair = json.loads(Path(args.repair_report).read_text(encoding="utf-8"))
    validation = json.loads(Path(args.validation_report).read_text(encoding="utf-8"))
    validation_by_id = {item["asset_id"]: item for item in validation.get("results", [])}
    items = repair.get("results", [])
    if len(items) != 24 or not validation.get("success") or len(validation_by_id) != 24:
        raise RuntimeError("A validated set of exactly 24 buildings is required")

    desktop_current = Path(args.desktop_current).resolve()
    desktop_backup = Path(args.desktop_backup).resolve()
    unity_root = Path(args.unity_model_root).resolve()
    if desktop_backup.exists():
        raise FileExistsError(f"Backup already exists: {desktop_backup}")

    preflight = []
    for item in items:
        asset_id = item["asset_id"]
        source = Path(item["source"])
        staged = Path(item["output"])
        desktop_candidates = list(desktop_current.glob(f"{asset_id}_*/Export/Models/*.fbx"))
        if len(desktop_candidates) != 1:
            raise RuntimeError(f"Expected one desktop target for {asset_id}, got {len(desktop_candidates)}")
        desktop = desktop_candidates[0]
        unity = unity_root / f"SM_Oasis_{asset_id.replace('-', '')}.fbx"
        for target in (source, staged, desktop, unity):
            if not target.exists():
                raise FileNotFoundError(target)
        current_hashes = {sha256(source), sha256(desktop), sha256(unity)}
        if len(current_hashes) != 1:
            raise RuntimeError(f"Current source/desktop/Unity FBXs differ before deployment: {asset_id}")
        preflight.append((item, source, staged, desktop, unity))

    shutil.copytree(desktop_current, desktop_backup)
    timestamp = dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    deployed = []
    for item, source, staged, desktop, unity in preflight:
        asset_id = item["asset_id"]
        validation_item = validation_by_id[asset_id]
        for target in (source, desktop, unity):
            atomic_copy(staged, target)
        source_manifest = Path(item["manifest"])
        desktop_manifest = desktop.parent.parent / "export_manifest.json"
        update_manifest(source_manifest, source, item, validation_item, timestamp)
        shutil.copy2(source_manifest, desktop_manifest)
        hashes = {name: sha256(path) for name, path in {
            "staged": staged, "source": source, "desktop": desktop, "unity": unity,
        }.items()}
        if len(set(hashes.values())) != 1:
            raise RuntimeError(f"Hash mismatch after deployment: {asset_id}")
        deployed.append({
            "asset_id": asset_id,
            "sha256": hashes["staged"],
            "bytes": staged.stat().st_size,
            "source": str(source),
            "desktop": str(desktop),
            "unity": str(unity),
            "floor_objects_before": item["floor_objects_before"],
            "floor_objects_after": item["floor_objects_after"],
        })

    output = Path(args.output_report).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({
        "success": True,
        "deployed_at_local": timestamp,
        "desktop_prechange_backup": str(desktop_backup),
        "unity_meta_files_touched": False,
        "assets": deployed,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(deployed), "report": str(output)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
