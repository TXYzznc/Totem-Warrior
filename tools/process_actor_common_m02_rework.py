"""Process the approved Actor Common M02 animation rework source sheets.

The script mirrors the original M02 pipeline: it consumes already-keyed RGBA
sheets, finds the strongest foreground component in each nominal frame cell,
normalizes it to a transparent 512x512 canvas with a shared foot baseline, and
writes an audit report plus black-background contact sheets.  It deliberately
writes only to the animation_rework staging directory; promotion into the
canonical raw-frame directory is a separate, reviewed step.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image


ROOT = Path("openspec/changes/produce-totem-art-assets/art/raw/characters/actor_common_m02")
REWORK = ROOT / "animation_rework"
ALPHA = REWORK / "alpha"
FRAMES = REWORK / "staged_frames"
PREVIEWS = REWORK / "previews"
DIRECTIONS = ("down", "up", "left", "right")
FRAME_SIZE = 512
SPECS = {
    "attack": {"frames": 6, "loop": False},
    "hit": {"frames": 4, "loop": False},
    "roll": {"frames": 8, "loop": False},
    "sprint": {"frames": 6, "loop": True},
}


def sheet_stem(action: str, direction: str) -> str:
    return f"actor_common_m02_{action}_{direction}_sheet"


def source_for(action: str, direction: str) -> Path:
    if direction == "down":
        suffix = "review_v2" if action == "attack" else "review"
        return REWORK / "review" / "down" / f"actor_common_m02_{action}_down_{suffix}_chromakey.png"
    return REWORK / "source" / f"actor_common_m02_{action}_{direction}_source_chromakey.png"


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def largest_component_bbox(alpha: np.ndarray) -> tuple[int, int, int, int, int]:
    mask = (alpha > 15).astype(np.uint8)
    count, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError("No alpha foreground found in source cell")
    index = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    x, y, width, height, area = (int(value) for value in stats[index])
    return x, y, width, height, area


def despill(arr: np.ndarray) -> np.ndarray:
    """Remove only vivid chroma green from partially opaque edge pixels."""
    out = arr.copy()
    rgb = out[:, :, :3].astype(np.int16)
    alpha = out[:, :, 3]
    green = (
        (alpha > 0)
        & (rgb[:, :, 1] > rgb[:, :, 0] + 25)
        & (rgb[:, :, 1] > rgb[:, :, 2] + 25)
        & (rgb[:, :, 1] > 100)
    )
    out[green, 3] = 0
    return out


def retain_largest_component(arr: np.ndarray) -> np.ndarray:
    """Discard detached cross-cell or generation artifacts inside a crop bbox."""
    out = arr.copy()
    mask = (out[:, :, 3] > 15).astype(np.uint8)
    count, labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError("No foreground component remains after resize")
    keep = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    out[labels != keep, 3] = 0
    return out


def seams(sheet: np.ndarray, frame_count: int) -> list[int]:
    """Find low-alpha cuts close to each nominal equal-width frame division."""
    width = sheet.shape[1]
    column_alpha = (sheet[:, :, 3] > 15).sum(axis=0)
    cuts = [0]
    search = max(24, min(120, round(width / frame_count / 3)))
    for index in range(1, frame_count):
        target = round(index * width / frame_count)
        lo = max(cuts[-1] + 1, target - search)
        hi = min(width - 1, target + search)
        cuts.append(lo + int(np.argmin(column_alpha[lo : hi + 1])))
    cuts.append(width)
    return cuts


def process_direction(action: str, direction: str) -> tuple[list[dict], dict]:
    frame_count = SPECS[action]["frames"]
    alpha_path = ALPHA / f"{sheet_stem(action, direction)}.png"
    sheet = np.array(Image.open(alpha_path).convert("RGBA"))
    height, width = sheet.shape[:2]
    cuts = seams(sheet, frame_count)
    cells: list[tuple[np.ndarray, tuple[int, int, int, int, int], tuple[int, int]]] = []
    for index in range(frame_count):
        x0, x1 = cuts[index], cuts[index + 1]
        cell = sheet[:, x0:x1].copy()
        bbox = largest_component_bbox(cell[:, :, 3])
        cells.append((cell, bbox, (x0, x1)))

    max_width = max(bbox[2] for _cell, bbox, _span in cells)
    max_height = max(bbox[3] for _cell, bbox, _span in cells)
    scale = min(500 / max_width, 500 / max_height, 1.0)
    rows: list[dict] = []
    for index, (cell, bbox, span) in enumerate(cells, start=1):
        x, y, bbox_width, bbox_height, area = bbox
        subject = cell[y : y + bbox_height, x : x + bbox_width]
        new_width = max(1, round(bbox_width * scale))
        new_height = max(1, round(bbox_height * scale))
        resized = np.array(
            Image.fromarray(subject, "RGBA").resize((new_width, new_height), Image.Resampling.LANCZOS)
        )
        resized = retain_largest_component(despill(resized))
        canvas = np.zeros((FRAME_SIZE, FRAME_SIZE, 4), dtype=np.uint8)
        target_x = (FRAME_SIZE - new_width) // 2
        target_y = FRAME_SIZE - new_height
        canvas[target_y : target_y + new_height, target_x : target_x + new_width] = resized
        alpha = canvas[:, :, 3]
        ys, _xs = np.where(alpha > 15)
        if len(ys) == 0:
            raise RuntimeError(f"No foreground after resize for {action}/{direction}/{index}")
        baseline = int(ys.max())
        if baseline != 511:
            raise RuntimeError(f"Baseline mismatch after placement for {action}/{direction}/{index}: {baseline}")
        output = FRAMES / f"actor_common_m02_{action}_{direction}_{index:02d}.png"
        Image.fromarray(canvas, "RGBA").save(output)
        rgb = canvas[:, :, :3].astype(np.int16)
        green = (
            (alpha > 15)
            & (rgb[:, :, 1] > rgb[:, :, 0] + 25)
            & (rgb[:, :, 1] > rgb[:, :, 2] + 25)
            & (rgb[:, :, 1] > 100)
        )
        corners = [int(alpha[0, 0]), int(alpha[0, -1]), int(alpha[-1, 0]), int(alpha[-1, -1])]
        rows.append(
            {
                "action": action,
                "direction": direction,
                "frame": index,
                "source_cell_x": list(span),
                "source_component_bbox": [x, y, bbox_width, bbox_height],
                "source_component_area": area,
                "output": output.name,
                "mode": "RGBA",
                "size": [FRAME_SIZE, FRAME_SIZE],
                "baseline_y": baseline,
                "corner_alpha": corners,
                "green_residual_pixels": int(green.sum()),
                "sha256": sha256(output),
            }
        )
    return rows, {"sheet": alpha_path.name, "sheet_size": [width, height], "seams": cuts, "scale": scale, "sha256": sha256(alpha_path)}


def write_preview(action: str) -> None:
    frame_count = SPECS[action]["frames"]
    thumb = 192
    preview = Image.new("RGBA", (frame_count * thumb, len(DIRECTIONS) * thumb), (0, 0, 0, 255))
    for row, direction in enumerate(DIRECTIONS):
        for frame in range(1, frame_count + 1):
            image = Image.open(FRAMES / f"actor_common_m02_{action}_{direction}_{frame:02d}.png").convert("RGBA")
            image.thumbnail((thumb, thumb), Image.Resampling.LANCZOS)
            x = (frame - 1) * thumb + (thumb - image.width) // 2
            y = row * thumb + thumb - image.height
            preview.alpha_composite(image, (x, y))
    preview.save(PREVIEWS / f"actor_common_m02_{action}_contact_black_preview.png")


def main() -> None:
    if not ALPHA.exists():
        raise RuntimeError(f"Missing keyed alpha sheets directory: {ALPHA}")
    FRAMES.mkdir(parents=True, exist_ok=True)
    PREVIEWS.mkdir(parents=True, exist_ok=True)
    all_rows: list[dict] = []
    sheets: dict[str, dict] = {}
    for action in SPECS:
        for direction in DIRECTIONS:
            source = source_for(action, direction)
            if not source.exists():
                raise FileNotFoundError(f"Missing source sheet: {source}")
            alpha = ALPHA / f"{sheet_stem(action, direction)}.png"
            if not alpha.exists():
                raise FileNotFoundError(f"Missing keyed alpha sheet: {alpha}")
            rows, sheet = process_direction(action, direction)
            all_rows.extend(rows)
            sheets[f"{action}_{direction}"] = sheet
        write_preview(action)
    report = {
        "actions": SPECS,
        "directions": list(DIRECTIONS),
        "sheets": sheets,
        "frames": all_rows,
        "all_rgba_512": all(row["mode"] == "RGBA" and row["size"] == [512, 512] for row in all_rows),
        "all_baseline_y511": all(row["baseline_y"] == 511 for row in all_rows),
        "all_corners_transparent": all(row["corner_alpha"] == [0, 0, 0, 0] for row in all_rows),
        "all_green_residual_zero": all(row["green_residual_pixels"] == 0 for row in all_rows),
    }
    report_path = REWORK / "rework_validation.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    for action, spec in SPECS.items():
        action_rows = [row for row in all_rows if row["action"] == action]
        action_report = {
            "action": action,
            "frame_count_per_direction": spec["frames"],
            "loop": spec["loop"],
            "directions": list(DIRECTIONS),
            "sheets": {key: value for key, value in sheets.items() if key.startswith(action + "_")},
            "frames": action_rows,
            "all_rgba_512": all(row["mode"] == "RGBA" and row["size"] == [512, 512] for row in action_rows),
            "all_baseline_y511": all(row["baseline_y"] == 511 for row in action_rows),
            "all_corners_transparent": all(row["corner_alpha"] == [0, 0, 0, 0] for row in action_rows),
            "all_green_residual_zero": all(row["green_residual_pixels"] == 0 for row in action_rows),
        }
        (REWORK / f"{action}_validation.json").write_text(
            json.dumps(action_report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    print(json.dumps({key: value for key, value in report.items() if key.startswith("all_")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
