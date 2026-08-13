from __future__ import annotations

import json
import re
import shutil
from pathlib import Path

from PIL import Image


SOURCE_ROOT = Path(r"C:\Users\WIN10\Desktop\INT-PROP_可交互道具")
GAME_ROOT = Path(r"D:\unity\UnityProject\GameDesinger\Assets\Game")
MODEL_ROOT = GAME_ROOT / "Models" / "InteractiveObj"
TEXTURE_ROOT = GAME_ROOT / "Textures" / "InteractiveObj"
MATERIAL_ROOT = GAME_ROOT / "Materials" / "InteractiveObj"
PREFAB_ROOT = GAME_ROOT / "Prefabs" / "InteractiveObj"
PREVIEW_ROOT = TEXTURE_ROOT / "Previews"
CONFIG_ROOT = GAME_ROOT / "Config" / "InteractiveObj"


def collect_assets() -> list[tuple[str, Path]]:
    assets: list[tuple[str, Path]] = []
    for index in range(1, 13):
        parent = next(SOURCE_ROOT.glob(f"INT-PROP-{index:03d}_*"))
        assets.append((f"INT_PROP_{index:03d}", next(parent.glob("*End"))))
    assets.append(("GUN_001", SOURCE_ROOT / "枪_模型" / "枪End"))
    return assets


def build_metallic_smoothness(roughness_path: Path, metallic_path: Path, output_path: Path) -> None:
    roughness = Image.open(roughness_path).convert("L")
    metallic = Image.open(metallic_path).convert("L")
    if roughness.size != metallic.size:
        roughness = roughness.resize(metallic.size, Image.Resampling.LANCZOS)
    smoothness = roughness.point(lambda value: 255 - value)
    zero = Image.new("L", metallic.size, 0)
    Image.merge("RGBA", (metallic, zero, zero, smoothness)).save(output_path, compress_level=6)


def main() -> None:
    for folder in (MODEL_ROOT, TEXTURE_ROOT, MATERIAL_ROOT, PREFAB_ROOT, PREVIEW_ROOT, CONFIG_ROOT):
        folder.mkdir(parents=True, exist_ok=True)

    report = []
    for asset_id, end_folder in collect_assets():
        model_source = next(end_folder.glob("*.fbx"))
        model_dest = MODEL_ROOT / f"SM_{asset_id}.fbx"
        shutil.copy2(model_source, model_dest)

        comparison_name = "GUN-001" if asset_id == "GUN_001" else asset_id.replace("_", "-")
        preview = end_folder / f"{comparison_name}_Comparison.png"
        if preview.exists():
            shutil.copy2(preview, PREVIEW_ROOT / f"PV_{asset_id}.png")

        texture_dest = TEXTURE_ROOT / asset_id
        material_dest = MATERIAL_ROOT / asset_id
        texture_dest.mkdir(parents=True, exist_ok=True)
        material_dest.mkdir(parents=True, exist_ok=True)

        texture_groups: dict[str, dict[str, Path]] = {}
        for texture in (end_folder / "Textures").glob("*.png"):
            match = re.search(
                r"((?:tripo_part_\d+)|(?:tripo_node_[0-9a-f-]+))_(BaseColor|Normal|Roughness|Metallic)\.png$",
                texture.name,
                re.IGNORECASE,
            )
            if match:
                texture_groups.setdefault(match.group(1).lower(), {})[match.group(2).lower()] = texture

        complete_pbr = 0
        def part_sort_key(item: tuple[str, dict[str, Path]]) -> tuple[int, str]:
            match = re.search(r"tripo_part_(\d+)$", item[0])
            return (int(match.group(1)), item[0]) if match else (0, item[0])

        for fallback_index, (part, maps) in enumerate(sorted(texture_groups.items(), key=part_sort_key)):
            match = re.search(r"tripo_part_(\d+)$", part)
            part_index = int(match.group(1)) if match else fallback_index
            stem = f"T_{asset_id}_P{part_index:02d}"
            suffixes = {"basecolor": "D", "normal": "N", "roughness": "R", "metallic": "M"}
            for channel, suffix in suffixes.items():
                if channel in maps:
                    shutil.copy2(maps[channel], texture_dest / f"{stem}_{suffix}.png")
            if "roughness" in maps and "metallic" in maps:
                build_metallic_smoothness(
                    maps["roughness"],
                    maps["metallic"],
                    texture_dest / f"{stem}_MS.png",
                )
            if all(channel in maps for channel in suffixes):
                complete_pbr += 1

        report.append(
            {
                "asset": asset_id,
                "model": f"Assets/Game/Models/InteractiveObj/Models/SM_{asset_id}.fbx",
                "parts": len(texture_groups),
                "complete_pbr": complete_pbr,
            }
        )

    (CONFIG_ROOT / "import_manifest.json").write_text(
        json.dumps({"assets": report}, indent=2), encoding="utf-8"
    )
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()
