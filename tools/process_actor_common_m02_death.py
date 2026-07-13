"""Slice, normalize, and validate the actor_common_m02 death sprite sheets.

This is a narrow, non-Unity art-processing helper. It only touches the openspec
raw-art directory passed in and preserves the chroma-key source sheets.
"""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

import cv2
import numpy as np
from PIL import Image


ROOT = Path("openspec/changes/produce-totem-art-assets/art/raw/characters/actor_common_m02")
DIRECTIONS = ("down", "up", "left", "right")
FRAME_COUNT = 8
FRAME_SIZE = 512


def largest_component_bbox(alpha: np.ndarray) -> tuple[int, int, int, int, int]:
    mask = (alpha > 15).astype(np.uint8)
    count, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError("No alpha foreground found in source cell")
    index = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    x, y, width, height, area = (int(v) for v in stats[index])
    return x, y, width, height, area


def despill(arr: np.ndarray) -> np.ndarray:
    """Remove any remaining vivid chroma green on partially opaque edge pixels."""
    out = arr.copy()
    rgb = out[:, :, :3].astype(np.int16)
    alpha = out[:, :, 3]
    green = (alpha > 0) & (rgb[:, :, 1] > rgb[:, :, 0] + 25) & (rgb[:, :, 1] > rgb[:, :, 2] + 25) & (rgb[:, :, 1] > 100)
    # Key-colored anti-alias pixels cannot be part of this warm gray/brown character.
    out[green, 3] = 0
    return out


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def process_direction(direction: str) -> tuple[list[dict], dict]:
    sheet_path = ROOT / f"actor_common_m02_death_{direction}_sheet.png"
    sheet = np.array(Image.open(sheet_path).convert("RGBA"))
    h, w = sheet.shape[:2]
    # Generated sheets do not always respect the requested equal-cell grid: a
    # falling limb can spill into the next nominal cell. Find the lowest-alpha
    # vertical seam near every nominal division instead, so a complete pose is
    # retained before extracting the largest foreground component.
    column_alpha = (sheet[:, :, 3] > 15).sum(axis=0)
    cuts = [0]
    for i in range(1, FRAME_COUNT):
        target = round(i * w / FRAME_COUNT)
        lo = max(cuts[-1] + 1, target - 120)
        hi = min(w - 1, target + 120)
        seam = lo + int(np.argmin(column_alpha[lo:hi + 1]))
        cuts.append(seam)
    cuts.append(w)
    cells: list[tuple[np.ndarray, tuple[int, int, int, int, int], tuple[int, int]]] = []
    for i in range(FRAME_COUNT):
        x0, x1 = cuts[i], cuts[i + 1]
        cell = sheet[:, x0:x1].copy()
        bbox = largest_component_bbox(cell[:, :, 3])
        cells.append((cell, bbox, (x0, x1)))

    max_w = max(b[2] for _cell, b, _span in cells)
    max_h = max(b[3] for _cell, b, _span in cells)
    # Preserve a small top margin; horizontal space is deliberately generous for death poses.
    scale = min(500 / max_w, 500 / max_h, 1.0)
    rows: list[dict] = []
    for i, (cell, bbox, span) in enumerate(cells, start=1):
        x, y, bw, bh, area = bbox
        subject = cell[y:y + bh, x:x + bw]
        nw = max(1, round(bw * scale))
        nh = max(1, round(bh * scale))
        resized = np.array(Image.fromarray(subject, "RGBA").resize((nw, nh), Image.Resampling.LANCZOS))
        resized = despill(resized)
        canvas = np.zeros((FRAME_SIZE, FRAME_SIZE, 4), dtype=np.uint8)
        dst_x = (FRAME_SIZE - nw) // 2
        dst_y = FRAME_SIZE - nh
        canvas[dst_y:dst_y + nh, dst_x:dst_x + nw] = resized
        # Reinforce baseline after fractional resize and ensure output never has a colored fringe.
        alpha = canvas[:, :, 3]
        ys, xs = np.where(alpha > 15)
        if len(ys) == 0:
            raise RuntimeError(f"No foreground after resize for {direction}/{i}")
        bottom = int(ys.max())
        if bottom != 511:
            raise RuntimeError(f"Baseline mismatch after placement for {direction}/{i}: {bottom}")
        out_path = ROOT / f"actor_common_m02_death_{direction}_{i:02d}.png"
        Image.fromarray(canvas, "RGBA").save(out_path)
        rgb = canvas[:, :, :3].astype(np.int16)
        green = (alpha > 15) & (rgb[:, :, 1] > rgb[:, :, 0] + 25) & (rgb[:, :, 1] > rgb[:, :, 2] + 25) & (rgb[:, :, 1] > 100)
        corners = [int(alpha[0, 0]), int(alpha[0, -1]), int(alpha[-1, 0]), int(alpha[-1, -1])]
        rows.append({
            "direction": direction,
            "frame": i,
            "source_cell_x": list(span),
            "source_component_bbox": [x, y, bw, bh],
            "source_component_area": area,
            "output": out_path.name,
            "mode": "RGBA",
            "size": [FRAME_SIZE, FRAME_SIZE],
            "baseline_y": bottom,
            "corner_alpha": corners,
            "green_residual_pixels": int(green.sum()),
            "sha256": sha256(out_path),
        })
    return rows, {"sheet": sheet_path.name, "sheet_size": [w, h], "seams": cuts, "scale": scale, "sha256": sha256(sheet_path)}


def main() -> None:
    all_rows: list[dict] = []
    sheets: dict[str, dict] = {}
    for direction in DIRECTIONS:
        rows, sheet = process_direction(direction)
        all_rows.extend(rows)
        sheets[direction] = sheet
    report = {
        "action": "death",
        "frame_count_per_direction": FRAME_COUNT,
        "directions": list(DIRECTIONS),
        "sheets": sheets,
        "frames": all_rows,
        "all_rgba_512": all(row["mode"] == "RGBA" and row["size"] == [512, 512] for row in all_rows),
        "all_baseline_y511": all(row["baseline_y"] == 511 for row in all_rows),
        "all_corners_transparent": all(row["corner_alpha"] == [0, 0, 0, 0] for row in all_rows),
        "all_green_residual_zero": all(row["green_residual_pixels"] == 0 for row in all_rows),
    }
    (ROOT / "death_validation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    # A black-background contact sheet makes alpha fringe and action continuity reviewable
    # without becoming a Unity asset.
    thumb = 192
    preview = Image.new("RGBA", (FRAME_COUNT * thumb, len(DIRECTIONS) * thumb), (0, 0, 0, 255))
    for row_index, direction in enumerate(DIRECTIONS):
        for frame_index in range(1, FRAME_COUNT + 1):
            frame = Image.open(ROOT / f"actor_common_m02_death_{direction}_{frame_index:02d}.png").convert("RGBA")
            frame.thumbnail((thumb, thumb), Image.Resampling.LANCZOS)
            x = (frame_index - 1) * thumb + (thumb - frame.width) // 2
            y = row_index * thumb + thumb - frame.height
            preview.alpha_composite(frame, (x, y))
    preview.save(ROOT / "actor_common_m02_death_contact_black_preview.png")
    print(json.dumps({k: report[k] for k in report if k.startswith("all_")}, ensure_ascii=False))


if __name__ == "__main__":
    main()
