from __future__ import annotations

import argparse
import json
from dataclasses import asdict, dataclass
from pathlib import Path

import cv2
import numpy as np


CANVAS_SUFFIX = "_orthographic_5views_v01.png"
VIEWS = ("front", "rear", "left", "right", "top")


@dataclass
class SplitResult:
    canvas: str
    outputs: list[str]
    seams: dict[str, int]
    seam_foreground_pixels: dict[str, int]
    decode_ok: bool
    error: str = ""


def read_image(path: Path) -> np.ndarray:
    data = np.fromfile(path, dtype=np.uint8)
    image = cv2.imdecode(data, cv2.IMREAD_COLOR)
    if image is None:
        raise ValueError(f"无法解码图片：{path}")
    return image


def write_png(path: Path, image: np.ndarray) -> None:
    ok, encoded = cv2.imencode(".png", image)
    if not ok:
        raise ValueError(f"无法编码 PNG：{path}")
    encoded.tofile(path)


def build_foreground_mask(image: np.ndarray) -> np.ndarray:
    """Find non-background pixels on the smooth, neutral multiview backdrop."""
    smooth = cv2.GaussianBlur(image, (0, 0), sigmaX=8, sigmaY=8)
    detail = np.max(cv2.absdiff(image, smooth), axis=2)
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 40, 100)

    # The generated canvases use a neutral gray gradient. Saturation and
    # high-frequency detail separate the assets without turning that gradient
    # into one giant foreground component.
    mask = ((detail > 18) | (edges > 0)).astype(np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))

    count, labels, stats, _ = cv2.connectedComponentsWithStats(mask, 8)
    cleaned = np.zeros_like(mask)
    for label in range(1, count):
        if stats[label, cv2.CC_STAT_AREA] >= 80:
            cleaned[labels == label] = 1
    return cv2.dilate(cleaned, np.ones((3, 3), np.uint8), iterations=1)


def build_seam_mask(image: np.ndarray) -> np.ndarray:
    """Use hard image edges only so smooth background gradients cannot move cuts."""
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    edges = cv2.Canny(gray, 35, 90)
    return cv2.dilate(
        (edges > 0).astype(np.uint8),
        np.ones((3, 3), np.uint8),
        iterations=1,
    )


def choose_blank_run(
    samples: list[tuple[int, int, float]],
    nominal: int,
) -> tuple[int, int]:
    """Choose the center of a foreground-free run nearest the expected split."""
    zero_runs: list[list[tuple[int, int, float]]] = []
    current: list[tuple[int, int, float]] = []
    for sample in samples:
        if sample[1] == 0:
            current.append(sample)
        else:
            if len(current) >= 4:
                zero_runs.append(current)
            current = []
    if len(current) >= 4:
        zero_runs.append(current)

    if zero_runs:
        candidates: list[tuple[int, int]] = []
        for run in zero_runs:
            center_sample = run[len(run) // 2]
            candidates.append((abs(center_sample[0] - nominal), center_sample[0]))
        _, coordinate = min(candidates)
        return coordinate, 0

    coordinate, foreground, _ = min(
        samples,
        key=lambda item: (item[1], abs(item[0] - nominal), item[2]),
    )
    return coordinate, foreground


def find_projection_seams(
    mask: np.ndarray,
    y0: int,
    y1: int,
    group_count: int,
) -> list[tuple[int, int]]:
    projection = mask[y0:y1, :].sum(axis=0)
    coordinates = np.flatnonzero(projection > 0).astype(np.float64)
    if coordinates.size < group_count * 4:
        raise ValueError("轮廓投影不足，无法识别视图组")

    centers = np.linspace(coordinates.min(), coordinates.max(), group_count)
    labels = np.zeros(coordinates.shape, dtype=np.int32)
    for _ in range(50):
        distances = np.abs(coordinates[:, None] - centers[None, :])
        new_labels = np.argmin(distances, axis=1)
        new_centers = centers.copy()
        for group in range(group_count):
            members = coordinates[new_labels == group]
            if members.size:
                new_centers[group] = members.mean()
        if np.array_equal(new_labels, labels) and np.allclose(new_centers, centers):
            break
        labels = new_labels
        centers = new_centers

    order = np.argsort(centers)
    groups = [np.sort(coordinates[labels == group]) for group in order]
    if any(group.size == 0 for group in groups):
        raise ValueError("轮廓投影分组为空")

    seams: list[tuple[int, int]] = []
    for left, right in zip(groups, groups[1:]):
        left_edge = int(left.max())
        right_edge = int(right.min())
        seam = (left_edge + right_edge) // 2
        x0 = max(0, seam - 3)
        x1 = min(mask.shape[1], seam + 4)
        risk = int(mask[y0:y1, x0:x1].sum())
        seams.append((seam, risk))
    return seams


def find_vertical_seam(
    mask: np.ndarray,
    image: np.ndarray,
    nominal_ratio: float,
    y0: int,
    y1: int,
    search_ratio: float,
) -> tuple[int, int]:
    height, width = mask.shape
    nominal = int(round(width * nominal_ratio))
    radius = max(8, int(round(width * search_ratio)))
    low = max(8, nominal - radius)
    high = min(width - 8, nominal + radius)

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    gradient = np.abs(np.diff(gray.astype(np.int16), axis=1))
    samples: list[tuple[int, int, float]] = []
    for x in range(low, high + 1):
        x0 = max(0, x - 3)
        x1 = min(width, x + 4)
        foreground_pixels = int(mask[y0:y1, x0:x1].sum())
        edge_score = float(gradient[y0:y1, max(0, x - 1) : min(width - 1, x + 1)].mean())
        samples.append((x, foreground_pixels, edge_score))
    return choose_blank_run(samples, nominal)


def find_horizontal_seam(
    mask: np.ndarray,
    image: np.ndarray,
    nominal_ratio: float = 0.5,
    search_ratio: float = 0.18,
) -> tuple[int, int]:
    height, width = mask.shape
    nominal = int(round(height * nominal_ratio))
    radius = max(8, int(round(height * search_ratio)))
    low = max(8, nominal - radius)
    high = min(height - 8, nominal + radius)

    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    gradient = np.abs(np.diff(gray.astype(np.int16), axis=0))
    samples: list[tuple[int, int, float]] = []
    for y in range(low, high + 1):
        y0 = max(0, y - 3)
        y1 = min(height, y + 4)
        foreground_pixels = int(mask[y0:y1, :].sum())
        edge_score = float(gradient[max(0, y - 1) : min(height - 1, y + 1), :].mean())
        samples.append((y, foreground_pixels, edge_score))
    return choose_blank_run(samples, nominal)


def find_view_boxes(
    mask: np.ndarray,
    old_de01_layout: bool,
    row_y: int,
    top_x1: int,
    top_x2: int,
    bottom_x: int,
) -> tuple[dict[str, tuple[int, int, int, int]], dict[str, int]]:
    height, width = mask.shape
    if old_de01_layout:
        view_regions = {
            "front": (0, top_x1, 0.25),
            "rear": (top_x1, top_x2, 0.25),
            "left": (top_x2, width, 0.25),
            "right": (0, bottom_x, 0.75),
            "top": (bottom_x, width, 0.75),
        }
    else:
        view_regions = {
            "front": (0, top_x1, 0.25),
            "rear": (top_x1, top_x2, 0.25),
            "top": (top_x2, width, 0.25),
            "left": (0, bottom_x, 0.75),
            "right": (bottom_x, width, 0.75),
        }

    assigned: dict[str, list[tuple[int, int, int, int]]] = {view: [] for view in VIEWS}
    assigned_pixels = {view: 0 for view in VIEWS}

    for view, (region_x0, region_x1, target_y) in view_regions.items():
        local_mask = mask[:, region_x0:region_x1]
        count, _, stats, centroids = cv2.connectedComponentsWithStats(local_mask, 8)
        for label in range(1, count):
            x, y, box_width, box_height, area = stats[label]
            if area < 40:
                continue
            _, cy = centroids[label]
            normalized_y = cy / height
            # Some bottom-row side views are very tall and extend above the
            # visual row gap. Classify by the view's center of mass, not by the
            # horizontal cut line.
            belongs_to_top = normalized_y < 0.5
            wants_top = target_y < 0.5
            if belongs_to_top != wants_top:
                continue
            assigned[view].append(
                (
                    region_x0 + x,
                    y,
                    region_x0 + x + box_width,
                    y + box_height,
                )
            )
            assigned_pixels[view] += int(area)

    boxes: dict[str, tuple[int, int, int, int]] = {}
    padding = max(14, int(round(min(width, height) * 0.025)))
    for view, components in assigned.items():
        if not components:
            raise ValueError(f"未识别到 {view} 视图主体")
        x0 = min(box[0] for box in components)
        y0 = min(box[1] for box in components)
        x1 = max(box[2] for box in components)
        y1 = max(box[3] for box in components)
        boxes[view] = (
            max(0, x0 - padding),
            max(0, y0 - padding),
            min(width, x1 + padding),
            min(height, y1 + padding),
        )
    return boxes, assigned_pixels


def isolate_component(
    image: np.ndarray,
    mask: np.ndarray,
    target_label: int,
    labels: np.ndarray,
    stats: np.ndarray,
) -> np.ndarray:
    height, width = mask.shape
    x, y, box_width, box_height, _ = stats[target_label]
    padding = max(20, int(round(min(width, height) * 0.025)))
    x0 = max(0, x - padding)
    y0 = max(0, y - padding)
    x1 = min(width, x + box_width + padding)
    y1 = min(height, y + box_height + padding)
    crop = image[y0:y1, x0:x1].copy()

    other = (
        (labels[y0:y1, x0:x1] > 0)
        & (labels[y0:y1, x0:x1] != target_label)
    ).astype(np.uint8)
    if other.any():
        other = cv2.dilate(other, np.ones((5, 5), np.uint8), iterations=1) * 255
        crop = cv2.inpaint(crop, other, 5, cv2.INPAINT_TELEA)
    return crop


def fill_cluster_mask(mask: np.ndarray) -> np.ndarray:
    closed = cv2.morphologyEx(
        mask.astype(np.uint8),
        cv2.MORPH_CLOSE,
        np.ones((7, 7), np.uint8),
        iterations=1,
    )
    contours, _ = cv2.findContours(
        closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE
    )
    filled = np.zeros_like(closed)
    cv2.drawContours(filled, contours, -1, 1, thickness=cv2.FILLED)
    return filled


def extract_isolated_views(
    image: np.ndarray,
    old_de01_layout: bool,
) -> dict[str, np.ndarray]:
    height, width = image.shape[:2]
    foreground = build_foreground_mask(image)
    count, labels, stats, centroids = cv2.connectedComponentsWithStats(
        foreground, 8
    )
    component_labels = [
        label
        for label in range(1, count)
        if stats[label, cv2.CC_STAT_AREA] >= 80
    ]
    if len(component_labels) < 5:
        raise ValueError("完整前景组件少于五个")

    if old_de01_layout:
        initial = {
            "front": (0.17, 0.25),
            "rear": (0.50, 0.25),
            "left": (0.83, 0.25),
            "right": (0.25, 0.75),
            "top": (0.75, 0.75),
        }
    else:
        initial = {
            "front": (0.17, 0.25),
            "rear": (0.50, 0.25),
            "top": (0.83, 0.25),
            "left": (0.25, 0.75),
            "right": (0.70, 0.75),
        }

    names = list(initial)
    y_weight = 1.65
    points = np.array(
        [
            (
                centroids[label][0] / width,
                centroids[label][1] / height * y_weight,
            )
            for label in component_labels
        ],
        dtype=np.float64,
    )
    anchors = np.array(
        [(initial[name][0], initial[name][1] * y_weight) for name in names],
        dtype=np.float64,
    )
    assignments = np.zeros(points.shape[0], dtype=np.int32)
    for _ in range(30):
        distances = np.sum((points[:, None, :] - anchors[None, :, :]) ** 2, axis=2)
        new_assignments = np.argmin(distances, axis=1)
        new_anchors = anchors.copy()
        for index in range(len(names)):
            members = points[new_assignments == index]
            if members.shape[0] < 1:
                raise ValueError(f"视图聚类组件不足：{names[index]}")
            new_anchors[index] = np.median(members, axis=0)
        if np.array_equal(assignments, new_assignments) and np.allclose(
            anchors, new_anchors
        ):
            assignments = new_assignments
            break
        assignments = new_assignments
        anchors = new_anchors

    cluster_masks: dict[str, np.ndarray] = {}
    for index, name in enumerate(names):
        cluster = np.zeros_like(foreground)
        for component_label, selected_index in zip(
            component_labels, assignments
        ):
            if selected_index == index:
                cluster[labels == component_label] = 1
        cluster_masks[name] = fill_cluster_mask(cluster)

    padding = max(18, int(round(min(width, height) * 0.025)))
    outputs: dict[str, np.ndarray] = {}
    for name in names:
        target = cluster_masks[name]
        target_y, target_x = np.nonzero(target)
        if target_x.size < 40:
            raise ValueError(f"视图主体为空：{name}")
        x0 = max(0, int(target_x.min()) - padding)
        y0 = max(0, int(target_y.min()) - padding)
        x1 = min(width, int(target_x.max()) + padding + 1)
        y1 = min(height, int(target_y.max()) + padding + 1)

        crop = image[y0:y1, x0:x1].copy()
        other = np.zeros_like(foreground)
        for other_name, other_mask in cluster_masks.items():
            if other_name != name:
                other |= other_mask
        other_crop = other[y0:y1, x0:x1]
        if other_crop.any():
            inpaint_mask = cv2.dilate(
                other_crop,
                np.ones((7, 7), np.uint8),
                iterations=1,
            ) * 255
            crop = cv2.inpaint(crop, inpaint_mask, 7, cv2.INPAINT_TELEA)
        outputs[name] = crop
    return outputs


def split_canvas(path: Path, overwrite: bool) -> SplitResult:
    image = read_image(path)
    height, width = image.shape[:2]
    seam_mask = build_seam_mask(image)

    row_y, row_risk = find_horizontal_seam(seam_mask, image)
    try:
        top_seams = find_projection_seams(seam_mask, 0, row_y, 3)
        (top_x1, top_risk1), (top_x2, top_risk2) = top_seams
    except ValueError:
        top_x1, top_risk1 = find_vertical_seam(
            seam_mask, image, 0.40, 0, row_y, 0.22
        )
        top_x2, top_risk2 = find_vertical_seam(
            seam_mask, image, 0.78, 0, row_y, 0.20
        )
    try:
        [(bottom_x, bottom_risk)] = find_projection_seams(
            seam_mask, row_y, height, 2
        )
    except ValueError:
        bottom_x, bottom_risk = find_vertical_seam(
            seam_mask, image, 1 / 2, row_y, height, 0.28
        )

    if not (0 < top_x1 < top_x2 < width and 0 < bottom_x < width and 0 < row_y < height):
        raise ValueError(f"切线顺序异常：{path}")

    # DE-01's previously approved original canvas predates the unified layout.
    old_de01_layout = path.name == "DE-01_orthographic_5views_v01.png"
    top_cells = (
        image[0:row_y, 0:top_x1],
        image[0:row_y, top_x1:top_x2],
        image[0:row_y, top_x2:width],
    )
    bottom_cells = (
        image[row_y:height, 0:bottom_x],
        image[row_y:height, bottom_x:width],
    )
    try:
        cells = extract_isolated_views(image, old_de01_layout)
    except ValueError:
        if old_de01_layout:
            cells = {
                "front": top_cells[0],
                "rear": top_cells[1],
                "left": top_cells[2],
                "right": bottom_cells[0],
                "top": bottom_cells[1],
            }
        else:
            cells = {
                "front": top_cells[0],
                "rear": top_cells[1],
                "top": top_cells[2],
                "left": bottom_cells[0],
                "right": bottom_cells[1],
            }

    if path.name == "DE-02_variant_2_orthographic_5views_v01.png":
        foreground = build_foreground_mask(image)
        count, labels, stats, centroids = cv2.connectedComponentsWithStats(
            foreground, 8
        )
        top_labels = [
            label
            for label in range(1, count)
            if stats[label, cv2.CC_STAT_AREA] >= 80
            and centroids[label][1] < height * 0.60
        ]
        if top_labels:
            top_label = max(top_labels, key=lambda label: centroids[label][0])
            cells["top"] = isolate_component(
                image, foreground, top_label, labels, stats
            )

    if path.name == "DE-15_orthographic_5views_v01.png":
        manual_row = 530
        cells = {
            "front": image[0:manual_row, 0:580],
            "rear": image[0:manual_row, 580:1082],
            "top": image[0:manual_row, 1082:width],
            "left": image[manual_row:height, 0:557],
            "right": image[manual_row:height, 557:width],
        }

    if path.name == "DE-16_orthographic_5views_v01.png":
        manual_row = 640
        cells = {
            "front": image[0:manual_row, 0:663],
            "rear": image[0:manual_row, 663:1300],
            "top": image[0:manual_row, 1300:width],
            "left": image[manual_row:height, 0:665],
            "right": image[manual_row:height, 665:width],
        }

    if path.name == "DE-05-1_orthographic_5views_v01.png":
        cells["rear"] = image[150:620, 480:960]

    stem = path.name[: -len(CANVAS_SUFFIX)]
    outputs: list[str] = []
    for view in VIEWS:
        output = path.with_name(f"{stem}_{view}_v01.png")
        if output.exists() and not overwrite:
            raise FileExistsError(f"输出已存在：{output}")
        crop = cells[view]
        if crop.size == 0 or crop.shape[0] < 64 or crop.shape[1] < 64:
            raise ValueError(f"切图尺寸异常：{output} -> {crop.shape}")
        write_png(output, crop)
        decoded = read_image(output)
        if decoded.shape[:2] != crop.shape[:2]:
            raise ValueError(f"写入后尺寸不一致：{output}")
        outputs.append(str(output))

    return SplitResult(
        canvas=str(path),
        outputs=outputs,
        seams={
            "row_y": row_y,
            "top_x1": top_x1,
            "top_x2": top_x2,
            "bottom_x": bottom_x,
        },
        seam_foreground_pixels={
            "row": row_risk,
            "top_x1": top_risk1,
            "top_x2": top_risk2,
            "bottom_x": bottom_risk,
        },
        decode_ok=True,
    )


def main() -> int:
    parser = argparse.ArgumentParser(description="切分绿洲新城装饰资产五视图画布")
    parser.add_argument("root", type=Path)
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    canvases = sorted(args.root.rglob(f"*{CANVAS_SUFFIX}"))
    results: list[SplitResult] = []
    for canvas in canvases:
        try:
            results.append(split_canvas(canvas, args.overwrite))
        except Exception as exc:
            results.append(
                SplitResult(
                    canvas=str(canvas),
                    outputs=[],
                    seams={},
                    seam_foreground_pixels={},
                    decode_ok=False,
                    error=str(exc),
                )
            )

    report = {
        "canvas_count": len(canvases),
        "output_count": sum(len(result.outputs) for result in results),
        "failed_count": sum(not result.decode_ok for result in results),
        "seam_risk_count": sum(
            any(
                value > 0
                for key, value in result.seam_foreground_pixels.items()
                if not key.endswith("_pixels")
            )
            for result in results
            if result.decode_ok
        ),
        "results": [asdict(result) for result in results],
    }
    payload = json.dumps(report, ensure_ascii=False, indent=2)
    if args.report:
        args.report.write_text(payload, encoding="utf-8")
    print(payload)
    return 1 if report["failed_count"] else 0


if __name__ == "__main__":
    raise SystemExit(main())
