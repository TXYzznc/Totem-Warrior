"""Deploy validated retiled building FBXs to source, desktop backup, and Unity."""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import shutil
from pathlib import Path


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest()


def update_manifest(path: Path, model_path: Path, scales: dict, timestamp: str):
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    relative_model = model_path.relative_to(path.parent).as_posix()
    model_entry = next((entry for entry in data.get("files", []) if entry.get("path", "").lower().endswith(".fbx")), None)
    if model_entry is None:
        raise RuntimeError(f"FBX entry missing in {path}")
    model_entry["path"] = relative_model
    model_entry["bytes"] = model_path.stat().st_size
    model_entry["sha256"] = sha256(model_path)
    data["retiled_at_local"] = timestamp
    data["uv_strategy"] = "world-aligned metre-scaled UV0; continuous structural meshes; no extra Unity tile GameObjects"
    data["metres_per_repeat"] = scales
    path.write_text(json.dumps(data, ensure_ascii=False, indent=4), encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--retile-report", required=True)
    parser.add_argument("--validation-report", required=True)
    parser.add_argument("--desktop-current", required=True)
    parser.add_argument("--desktop-prechange-backup", required=True)
    parser.add_argument("--unity-model-root", required=True)
    parser.add_argument("--output-report", required=True)
    args = parser.parse_args()

    retile = json.loads(Path(args.retile_report).read_text(encoding="utf-8"))
    validation = json.loads(Path(args.validation_report).read_text(encoding="utf-8"))
    if not validation.get("success") or len(retile.get("results", [])) != 24:
        raise RuntimeError("Validated set of exactly 24 buildings is required")

    desktop_current = Path(args.desktop_current).resolve()
    desktop_backup = Path(args.desktop_prechange_backup).resolve()
    unity_root = Path(args.unity_model_root).resolve()
    if desktop_backup.exists():
        raise FileExistsError(f"Pre-change backup already exists: {desktop_backup}")
    shutil.copytree(desktop_current, desktop_backup)

    timestamp = dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    deployment = []
    for item in retile["results"]:
        asset_id = item["asset_id"]
        normalized = asset_id.replace("-", "")
        staged = Path(item["output"])
        source = Path(item["source"])
        source_manifest = source.parent.parent / "export_manifest.json"

        desktop_candidates = list(desktop_current.glob(f"{asset_id}_*/Export/Models/*.fbx"))
        if len(desktop_candidates) != 1:
            raise RuntimeError(f"Expected one desktop target for {asset_id}, found {len(desktop_candidates)}")
        desktop_model = desktop_candidates[0]
        desktop_manifest = desktop_model.parent.parent / "export_manifest.json"
        unity_model = unity_root / f"SM_Oasis_{normalized}.fbx"
        if not unity_model.exists():
            raise FileNotFoundError(unity_model)

        shutil.copy2(staged, source)
        shutil.copy2(staged, desktop_model)
        shutil.copy2(staged, unity_model)
        scales = item["metres_per_repeat"]
        update_manifest(source_manifest, source, scales, timestamp)
        shutil.copy2(source_manifest, desktop_manifest)

        hashes = {name: sha256(path) for name, path in {
            "staged": staged,
            "source": source,
            "desktop": desktop_model,
            "unity": unity_model,
        }.items()}
        if len(set(hashes.values())) != 1:
            raise RuntimeError(f"Hash mismatch after deployment: {asset_id}")
        deployment.append({
            "asset_id": asset_id,
            "sha256": next(iter(hashes.values())),
            "bytes": staged.stat().st_size,
            "source": str(source),
            "desktop": str(desktop_model),
            "unity": str(unity_model),
        })

    output = Path(args.output_report).resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps({
        "success": True,
        "deployed_at_local": timestamp,
        "prechange_backup": str(desktop_backup),
        "assets": deployment,
    }, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"success": True, "count": len(deployment), "report": str(output)}, ensure_ascii=False))


if __name__ == "__main__":
    main()
