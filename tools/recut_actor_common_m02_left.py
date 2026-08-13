"""Recut the Actor Common M02 left-facing walk and sprint sheets by full pose.

Unlike the original equal-cell splitter, this tool identifies the six complete
alpha-connected character poses across each source sheet.  That prevents a limb
which crosses a nominal cell boundary from being cut off or included in its
neighbouring frame.
"""

from __future__ import annotations

import hashlib
import json
import shutil
from pathlib import Path

import cv2
import numpy as np
from PIL import Image


CHARACTER = "actor_common_m02"
ROOT = Path("openspec/changes/produce-totem-art-assets/art/raw/characters") / CHARACTER
ASSET_ROOT = Path("Assets/Game/Sprites/Actors/ActorCommonM02")
FRAME_SIZE = 512
ALPHA_THRESHOLD = 15
SPECS = {"walk": 6, "sprint": 6}
# The supplied sprint sheet has two small previous-pose residues attached to
# its left edge. These rectangles are in their respective complete-pose crop
# coordinates and do not overlap the intended silhouette.
EDGE_RESIDUE_RECTS = {
    ("sprint", 3): (0, 330, 24, 400),
    ("sprint", 5): (0, 240, 24, 280),
}


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def complete_pose_bboxes(alpha: np.ndarray, expected_count: int) -> list[tuple[int, int, int, int, int]]:
    mask = (alpha > ALPHA_THRESHOLD).astype(np.uint8)
    count, _labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    components = []
    for index in range(1, count):
        x, y, width, height, area = (int(value) for value in stats[index])
        if area > 100:
            components.append((x, y, width, height, area))

    components.sort(key=lambda component: component[0])
    if len(components) != expected_count:
        raise RuntimeError(
            f"Expected {expected_count} complete poses but found {len(components)}: {components}"
        )
    return components


def retain_largest_component(image: np.ndarray) -> np.ndarray:
    output = image.copy()
    count, labels, stats, _centroids = cv2.connectedComponentsWithStats(
        (output[:, :, 3] > ALPHA_THRESHOLD).astype(np.uint8), connectivity=8
    )
    if count <= 1:
        raise RuntimeError("No pose foreground remains after resize")

    keep = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    output[labels != keep, 3] = 0
    return output


def remove_edge_residue(pose: np.ndarray, action: str, frame: int) -> np.ndarray:
    residue = EDGE_RESIDUE_RECTS.get((action, frame))
    if residue is None:
        return pose

    x0, y0, x1, y1 = residue
    output = pose.copy()
    output[y0:y1, x0:x1, 3] = 0
    return output


def recut_action(action: str, frame_count: int) -> list[dict]:
    sheet_path = ROOT / f"{CHARACTER}_{action}_left_sheet.png"
    sheet = np.array(Image.open(sheet_path).convert("RGBA"))
    bboxes = complete_pose_bboxes(sheet[:, :, 3], frame_count)

    max_width = max(bbox[2] for bbox in bboxes)
    max_height = max(bbox[3] for bbox in bboxes)
    scale = min(500 / max_width, 500 / max_height, 1.0)
    rows = []
    for frame, (x, y, width, height, area) in enumerate(bboxes, start=1):
        pose = remove_edge_residue(sheet[y : y + height, x : x + width], action, frame)
        output_width = max(1, round(width * scale))
        output_height = max(1, round(height * scale))
        if (output_width, output_height) != (width, height):
            pose = np.array(
                Image.fromarray(pose, "RGBA").resize((output_width, output_height), Image.Resampling.LANCZOS)
            )
        pose = retain_largest_component(pose)

        canvas = np.zeros((FRAME_SIZE, FRAME_SIZE, 4), dtype=np.uint8)
        destination_x = (FRAME_SIZE - output_width) // 2
        destination_y = FRAME_SIZE - output_height
        canvas[destination_y : destination_y + output_height, destination_x : destination_x + output_width] = pose

        alpha = canvas[:, :, 3]
        ys, xs = np.where(alpha > ALPHA_THRESHOLD)
        if len(xs) == 0 or int(ys.max()) != FRAME_SIZE - 1:
            raise RuntimeError(f"Invalid normalized pose for {action}/left/{frame:02d}")

        file_name = f"{CHARACTER}_{action}_left_{frame:02d}.png"
        raw_output = ROOT / file_name
        asset_output = ASSET_ROOT / file_name
        Image.fromarray(canvas, "RGBA").save(raw_output)
        shutil.copyfile(raw_output, asset_output)
        rows.append(
            {
                "frame": frame,
                "source_pose_bbox": [x, y, width, height],
                "source_pose_area": area,
                "scale": scale,
                "output": file_name,
                "size": [FRAME_SIZE, FRAME_SIZE],
                "baseline_y": int(ys.max()),
                "sha256": sha256(raw_output),
            }
        )
    return rows


def main() -> None:
    report = {"method": "whole-sheet alpha connected components", "actions": {}}
    for action, frame_count in SPECS.items():
        report["actions"][action] = recut_action(action, frame_count)
    report_path = ROOT / f"{CHARACTER}_left_recut_validation.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Recut {sum(len(rows) for rows in report['actions'].values())} frames; report: {report_path}")


if __name__ == "__main__":
    main()
