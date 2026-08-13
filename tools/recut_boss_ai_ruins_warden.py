"""Recut the Boss AI Ruins Warden frames from the original alpha sheets.

The previous export divided each generated sheet into equal-width cells.  The
generated poses do not consistently respect those cell boundaries, so limbs and
weapons could be cut between adjacent frames.  This tool finds the nearest
transparent separator for every intended frame, crops the complete pose, and
normalizes it to the runtime 512x512 canvas without changing Unity .meta files.
"""

from __future__ import annotations

import hashlib
import json
import shutil
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import cv2
from PIL import Image


CHARACTER = "boss_ai_ruins_warden"
ROOT = Path("openspec/changes/produce-totem-art-assets/art/raw/characters") / CHARACTER
ASSET_ROOT = Path("Assets/Game/Sprites/Actors/BossAIruinsWarden")
FRAME_SIZE = 512
FOOT_BASELINE_Y = 480
ALPHA_THRESHOLD = 15
MAX_POSE_WIDTH = 500
MAX_POSE_HEIGHT = FOOT_BASELINE_Y
SEAM_SEARCH_CELL_RATIO = 0.42
CONTACT_SEAM_OVERLAP = 2
FRAME_COUNTS = {"idle": 4, "walk": 6, "attack": 6, "death": 8}
# These two source sheets contain only seven complete death poses.  Their
# penultimate nominal cells contain a thin piece of the final pose, so retain
# the final complete pose for one additional frame instead of exporting debris.
INCOMPLETE_SOURCE_FRAME_FALLBACKS = {
    ("death", "right", 6): 7,
    ("death", "up", 6): 7,
}


@dataclass(frozen=True)
class SourceFrame:
    action: str
    direction: str
    source: Path
    image: np.ndarray
    seams: list[int]
    seam_alpha_counts: list[int]
    crops: list[np.ndarray]
    source_bboxes: list[tuple[int, int, int, int]]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def alpha_bbox(image: np.ndarray) -> tuple[int, int, int, int]:
    alpha = image[:, :, 3] > ALPHA_THRESHOLD
    ys, xs = np.where(alpha)
    if len(xs) == 0:
        raise RuntimeError("Frame contains no visible pixels")
    x0, x1 = int(xs.min()), int(xs.max()) + 1
    y0, y1 = int(ys.min()), int(ys.max()) + 1
    return x0, y0, x1 - x0, y1 - y0


def retain_largest_component(image: np.ndarray) -> np.ndarray:
    """Remove neighbouring-pose debris from a death-frame segment."""

    mask = (image[:, :, 3] > ALPHA_THRESHOLD).astype(np.uint8)
    count, labels, stats, _centroids = cv2.connectedComponentsWithStats(mask, connectivity=8)
    if count <= 1:
        raise RuntimeError("Death-frame segment contains no visible component")
    largest = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    output = image.copy()
    output[labels != largest, 3] = 0
    return output


def find_seams(alpha: np.ndarray, frame_count: int) -> tuple[list[int], list[int], list[int]]:
    """Find each pose separator closest to the nominal equal-width boundary.

    A completely transparent column is always preferred.  If generated poses
    touch, use the lowest-coverage column and later retain a small overlap on
    both sides so the contact edge is not erased.
    """

    height, width = alpha.shape
    del height
    coverage = (alpha > ALPHA_THRESHOLD).sum(axis=0)
    cell_width = width / frame_count
    seams = [0]
    nominal_boundaries: list[int] = []
    seam_alpha_counts: list[int] = []
    for frame in range(1, frame_count):
        nominal = round(frame * cell_width)
        radius = round(cell_width * SEAM_SEARCH_CELL_RATIO)
        low = max(1, nominal - radius)
        high = min(width - 1, nominal + radius)
        candidates = np.arange(low, high + 1)
        # Coverage is dominant; proximity stabilizes the choice within a gap.
        scores = coverage[candidates] * 100_000 + np.abs(candidates - nominal)
        seam = int(candidates[np.argmin(scores)])
        seams.append(seam)
        nominal_boundaries.append(nominal)
        seam_alpha_counts.append(int(coverage[seam]))
    seams.append(width)
    return seams, nominal_boundaries, seam_alpha_counts


def split_complete_poses(
    image: np.ndarray, frame_count: int, *, retain_only_largest_component: bool
) -> tuple[list[np.ndarray], list[tuple[int, int, int, int]], list[int], list[int], list[int]]:
    seams, nominal_boundaries, seam_alpha_counts = find_seams(image[:, :, 3], frame_count)
    crops: list[np.ndarray] = []
    bboxes: list[tuple[int, int, int, int]] = []
    for frame in range(frame_count):
        left, right = seams[frame], seams[frame + 1]
        if frame > 0 and seam_alpha_counts[frame - 1] > 0:
            left = max(0, left - CONTACT_SEAM_OVERLAP)
        if frame < frame_count - 1 and seam_alpha_counts[frame] > 0:
            right = min(image.shape[1], right + CONTACT_SEAM_OVERLAP)

        segment = image[:, left:right]
        if retain_only_largest_component:
            segment = retain_largest_component(segment)
        x, y, width, height = alpha_bbox(segment)
        crops.append(segment[y : y + height, x : x + width])
        bboxes.append((left + x, y, width, height))
    return crops, bboxes, seams, nominal_boundaries, seam_alpha_counts


def load_sources() -> tuple[list[SourceFrame], dict[str, float]]:
    sources: list[SourceFrame] = []
    action_scales: dict[str, float] = {}
    for action, frame_count in FRAME_COUNTS.items():
        action_frames: list[SourceFrame] = []
        max_width = 0
        max_height = 0
        for direction in ("down", "up", "left", "right"):
            source = ROOT / f"{CHARACTER}_{action}_{direction}_sheet_alpha_original.png"
            if not source.is_file():
                raise FileNotFoundError(f"Missing original alpha sheet: {source}")
            image = np.array(Image.open(source).convert("RGBA"))
            crops, bboxes, seams, _nominal, seam_alpha_counts = split_complete_poses(
                image,
                frame_count,
                retain_only_largest_component=action == "death",
            )
            max_width = max(max_width, *(crop.shape[1] for crop in crops))
            max_height = max(max_height, *(crop.shape[0] for crop in crops))
            action_frames.append(SourceFrame(action, direction, source, image, seams, seam_alpha_counts, crops, bboxes))

        scale = min(MAX_POSE_WIDTH / max_width, MAX_POSE_HEIGHT / max_height, 1.0)
        action_scales[action] = scale
        sources.extend(action_frames)
    return sources, action_scales


def normalize_pose(pose: np.ndarray, scale: float) -> np.ndarray:
    source_height, source_width = pose.shape[:2]
    width = min(MAX_POSE_WIDTH, max(1, round(source_width * scale)))
    height = min(MAX_POSE_HEIGHT, max(1, round(source_height * scale)))
    if (width, height) != (source_width, source_height):
        pose = np.array(Image.fromarray(pose, "RGBA").resize((width, height), Image.Resampling.LANCZOS))

    canvas = np.zeros((FRAME_SIZE, FRAME_SIZE, 4), dtype=np.uint8)
    x = (FRAME_SIZE - width) // 2
    y = FOOT_BASELINE_Y - height + 1
    if y < 0:
        raise RuntimeError(f"Normalized pose exceeds top of canvas: {width}x{height}")
    canvas[y : y + height, x : x + width] = pose
    return canvas


def main() -> None:
    sources, action_scales = load_sources()
    report: dict[str, object] = {
        "method": "transparent-seam full-pose recut from original alpha sheets; death frames retain their largest connected subject",
        "frame_size": [FRAME_SIZE, FRAME_SIZE],
        "foot_baseline_y": FOOT_BASELINE_Y,
        "actions": {},
    }

    staged: dict[str, np.ndarray] = {}
    for source in sources:
        frame_rows = []
        scale = action_scales[source.action]
        for frame_index, pose in enumerate(source.crops):
            canvas = normalize_pose(pose, scale)
            alpha = canvas[:, :, 3] > ALPHA_THRESHOLD
            ys, xs = np.where(alpha)
            if len(xs) == 0 or int(ys.max()) != FOOT_BASELINE_Y:
                raise RuntimeError(f"Invalid normalized baseline for {source.action}/{source.direction}/{frame_index:02d}")

            file_name = f"{CHARACTER}_{source.action}_{source.direction}_{frame_index:02d}.png"
            staged[file_name] = canvas
            frame_rows.append(
                {
                    "frame": frame_index,
                    "source_pose_bbox": list(source.source_bboxes[frame_index]),
                    "output_alpha_bbox": [int(xs.min()), int(ys.min()), int(xs.max()) - int(xs.min()) + 1, int(ys.max()) - int(ys.min()) + 1],
                }
            )

        action_report = report["actions"]
        assert isinstance(action_report, dict)
        action_report[f"{source.action}_{source.direction}"] = {
            "source": source.source.as_posix(),
            "scale": scale,
            "seams": source.seams,
            "seam_alpha_counts": source.seam_alpha_counts,
            "frames": frame_rows,
        }

    for (action, direction, frame), replacement_frame in INCOMPLETE_SOURCE_FRAME_FALLBACKS.items():
        target_name = f"{CHARACTER}_{action}_{direction}_{frame:02d}.png"
        replacement_name = f"{CHARACTER}_{action}_{direction}_{replacement_frame:02d}.png"
        staged[target_name] = staged[replacement_name].copy()
        action_report = report["actions"]
        assert isinstance(action_report, dict)
        action_report[f"{action}_{direction}"]["fallback_frames"] = {str(frame): replacement_frame}

    for file_name, canvas in staged.items():
        raw_output = ROOT / file_name
        Image.fromarray(canvas, "RGBA").save(raw_output)
        shutil.copyfile(raw_output, ASSET_ROOT / raw_output.name)

    hashes = {}
    for file_name in staged:
        raw_output = ROOT / file_name
        hashes[raw_output.name] = sha256(raw_output)
    report["sha256"] = hashes
    report_path = ROOT / f"{CHARACTER}_recut_validation.json"
    report_path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(f"Recut {len(staged)} Boss frames; report: {report_path}")


if __name__ == "__main__":
    main()
