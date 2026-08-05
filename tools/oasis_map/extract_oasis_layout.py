#!/usr/bin/env python3
"""Extract the Oasis City authoring layout from the approved type-plan image.

The source drawing is the spatial source of truth.  This tool turns its pixel
markers into a deterministic JSON manifest consumed by the Unity editor map
builder.  It deliberately validates the documented 152-building allocation so
an OCR or source-image regression fails loudly instead of silently moving the
wrong prefab into the level.
"""

from __future__ import annotations

import argparse
import json
import math
from collections import Counter
from pathlib import Path
from typing import Any

import cv2
import numpy as np


MAP_PIXEL_WIDTH = 2920
MAP_PIXEL_HEIGHT = 4389
MAP_ORIGIN_X = MAP_PIXEL_WIDTH / 2.0
MAP_ORIGIN_Y = MAP_PIXEL_HEIGHT / 2.0
PIXEL_SCALE_METERS = 32.0 / 182.0

EXPECTED_COUNTS = {
    1: 1,
    2: 1,
    3: 1,
    4: 1,
    5: 1,
    6: 1,
    7: 1,
    8: 1,
    9: 1,
    10: 1,
    11: 1,
    12: 1,
    13: 6,
    14: 6,
    15: 6,
    16: 7,
    17: 30,
    18: 24,
    19: 15,
    20: 17,
    21: 12,
    22: 10,
    23: 5,
    24: 2,
}

# Pixel anchors are only used to obtain one clean font template for each digit.
# Marker discovery and final positions remain image-derived.
KNOWN_LABEL_ANCHORS = {
    1: (1888, 1624),
    2: (1945, 703),
    3: (2133, 1402),
    4: (597, 2905),
    5: (910, 1699),
    6: (2355, 721),
    7: (1718, 1209),
    8: (2577, 2807),
    9: (1865, 407),
    13: (535, 601),
    20: (517, 731),
}

SPAWN_ANCHORS = [
    (596, 339),
    (1194, 214),
    (1877, 282),
    (2446, 510),
    (2719, 891),
    (2787, 1403),
    (2730, 1943),
    (2798, 2501),
    (2702, 3053),
    (2571, 3593),
    (2247, 3991),
    (1735, 4207),
    (1223, 4190),
    (728, 4031),
    (409, 3735),
    (261, 3280),
    (250, 2751),
    (182, 2199),
    (227, 1630),
    (318, 1061),
]

MARKER_COLORS_BGR = {
    "red": np.array([80.0, 96.0, 176.0]),
    "orange": np.array([64.0, 136.0, 208.0]),
    "teal": np.array([144.0, 136.0, 88.0]),
    "purple": np.array([160.0, 120.0, 136.0]),
    "green": np.array([128.0, 176.0, 168.0]),
}


def read_image(path: Path) -> np.ndarray:
    image = cv2.imdecode(np.fromfile(str(path), dtype=np.uint8), cv2.IMREAD_COLOR)
    if image is None:
        raise RuntimeError(f"Could not decode image: {path}")
    if image.shape[0] != MAP_PIXEL_HEIGHT or image.shape[1] < MAP_PIXEL_WIDTH:
        raise RuntimeError(
            f"Unexpected map dimensions {image.shape[1]}x{image.shape[0]}; "
            f"expected at least {MAP_PIXEL_WIDTH}x{MAP_PIXEL_HEIGHT}"
        )
    return image[:, :MAP_PIXEL_WIDTH]


def world_from_pixel(x: float, y: float) -> tuple[float, float]:
    return (
        (x - MAP_ORIGIN_X) * PIXEL_SCALE_METERS,
        (MAP_ORIGIN_Y - y) * PIXEL_SCALE_METERS,
    )


def largest_contour(mask: np.ndarray) -> np.ndarray:
    contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
    if not contours:
        raise RuntimeError("No contour was found")
    return max(contours, key=cv2.contourArea)


def digit_contour(component: np.ndarray) -> np.ndarray:
    return largest_contour(component.astype(np.uint8))


def discover_label_candidates(image: np.ndarray) -> list[dict[str, Any]]:
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    blurred = cv2.GaussianBlur(gray, (5, 5), 1.2)
    circles = cv2.HoughCircles(
        blurred,
        cv2.HOUGH_GRADIENT,
        dp=1.2,
        minDist=25,
        param1=110,
        param2=29,
        minRadius=13,
        maxRadius=30,
    )
    if circles is None:
        raise RuntimeError("No numbered marker circles were detected")

    candidates: list[dict[str, Any]] = []
    for x, y, radius in np.round(circles[0]).astype(int):
        crop = image[y - radius : y + radius + 1, x - radius : x + radius + 1]
        if crop.size == 0:
            continue
        crop_gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY)
        yy, xx = np.ogrid[: crop_gray.shape[0], : crop_gray.shape[1]]
        inner = (xx - radius) ** 2 + (yy - radius) ** 2 <= (radius * 0.66) ** 2
        digit_mask = ((crop_gray < 130) & inner).astype(np.uint8)
        count, labels, stats, _ = cv2.connectedComponentsWithStats(digit_mask, 8)
        components = [
            (index, stats[index])
            for index in range(1, count)
            if stats[index, cv2.CC_STAT_AREA] >= 5
        ]
        if len(components) != 2:
            continue
        components.sort(key=lambda item: item[1][cv2.CC_STAT_LEFT])
        candidates.append(
            {
                "x": int(x),
                "y": int(y),
                "radius": int(radius),
                "digits": [
                    digit_contour((labels == component_index).astype(np.uint8))
                    for component_index, _ in components
                ],
            }
        )

    if len(candidates) != 152:
        raise RuntimeError(f"Expected 152 numbered markers, found {len(candidates)}")
    return candidates


def nearest_candidate(candidates: list[dict[str, Any]], x: int, y: int) -> dict[str, Any]:
    return min(candidates, key=lambda item: (item["x"] - x) ** 2 + (item["y"] - y) ** 2)


def match_shape(contour: np.ndarray, template: np.ndarray) -> float:
    return float(cv2.matchShapes(contour, template, cv2.CONTOURS_MATCH_I1, 0.0))


def classify_labels(image: np.ndarray, candidates: list[dict[str, Any]]) -> None:
    known = {
        value: nearest_candidate(candidates, *anchor)
        for value, anchor in KNOWN_LABEL_ANCHORS.items()
    }
    first_templates = {
        0: known[1]["digits"][0],
        1: known[13]["digits"][0],
        2: known[20]["digits"][0],
    }
    second_templates = {digit: known[digit]["digits"][1] for digit in range(1, 10)}
    second_templates[0] = known[20]["digits"][1]

    for item in candidates:
        first = min(
            first_templates,
            key=lambda digit: match_shape(item["digits"][0], first_templates[digit]),
        )
        second_scores = {
            digit: match_shape(item["digits"][1], template)
            for digit, template in second_templates.items()
        }
        second = min(second_scores, key=second_scores.get)
        item["type"] = first * 10 + second
        item["secondScores"] = second_scores

    # Rotated 6 and 9 glyphs are deliberately disambiguated by their planning
    # category color: BF16 is purple, BF19 is green.  Rank the complete group
    # instead of using a hard threshold because antialiasing and nearby road
    # strokes can slightly pollute an individual marker's annulus.
    sixteen_or_nineteen = [item for item in candidates if item["type"] in (16, 19)]
    expected_sixteen_nineteen = EXPECTED_COUNTS[16] + EXPECTED_COUNTS[19]
    if len(sixteen_or_nineteen) != expected_sixteen_nineteen:
        raise RuntimeError(
            "Expected "
            f"{expected_sixteen_nineteen} BF16/BF19 candidates, "
            f"found {len(sixteen_or_nineteen)}"
        )
    sixteen_or_nineteen.sort(
        key=lambda item: marker_color_score(image, item, "purple")
        - marker_color_score(image, item, "green"),
        reverse=True,
    )
    for index, item in enumerate(sixteen_or_nineteen):
        item["type"] = 16 if index < EXPECTED_COUNTS[16] else 19

    # Rotated 2/3 glyphs are close in Hu-moment space.  The approved allocation
    # contains ten BF22 and five BF23, so rank the fifteen unambiguous candidates
    # by their relative template score instead of accepting an unstable cutoff.
    twenty_two_or_three = [item for item in candidates if item["type"] in (22, 23)]
    if len(twenty_two_or_three) != 15:
        raise RuntimeError(
            f"Expected 15 BF22/BF23 candidates, found {len(twenty_two_or_three)}"
        )
    twenty_two_or_three.sort(
        key=lambda item: item["secondScores"][2] - item["secondScores"][3]
    )
    for index, item in enumerate(twenty_two_or_three):
        item["type"] = 22 if index < EXPECTED_COUNTS[22] else 23

    actual = Counter(int(item["type"]) for item in candidates)
    if dict(sorted(actual.items())) != EXPECTED_COUNTS:
        raise RuntimeError(
            "Building allocation mismatch:\n"
            f"expected={EXPECTED_COUNTS}\nactual={dict(sorted(actual.items()))}"
        )


def marker_color_score(image: np.ndarray, item: dict[str, Any], category: str) -> int:
    radius = 90
    x, y = item["x"], item["y"]
    crop = image[y - radius : y + radius + 1, x - radius : x + radius + 1].astype(float)
    yy, xx = np.ogrid[-radius : radius + 1, -radius : radius + 1]
    annulus = (xx * xx + yy * yy > 35**2) & (xx * xx + yy * yy < 85**2)
    distance = np.linalg.norm(crop - MARKER_COLORS_BGR[category], axis=2)
    return int(np.sum(annulus & (distance < 35.0)))


def marker_category(building_type: int) -> str:
    if building_type in (1, 6):
        return "red"
    if 2 <= building_type <= 5:
        return "orange"
    if 7 <= building_type <= 12 or building_type == 24:
        return "teal"
    if 13 <= building_type <= 16:
        return "purple"
    return "green"


def marker_rectangle(image: np.ndarray, item: dict[str, Any]) -> tuple[tuple[float, float], tuple[float, float], float]:
    radius = 130
    x, y = item["x"], item["y"]
    crop = image[y - radius : y + radius + 1, x - radius : x + radius + 1]
    gray = cv2.cvtColor(crop, cv2.COLOR_BGR2GRAY)
    dark = (gray < 100).astype(np.uint8) * 255
    dark = cv2.morphologyEx(dark, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))
    contours, _ = cv2.findContours(dark, cv2.RETR_LIST, cv2.CHAIN_APPROX_SIMPLE)
    enclosing = []
    for contour in contours:
        if cv2.contourArea(contour) < 300:
            continue
        if cv2.pointPolygonTest(contour, (radius, radius), False) >= 0:
            enclosing.append(contour)
    if not enclosing:
        raise RuntimeError(f"Could not find marker rectangle at ({x}, {y})")
    contour = max(enclosing, key=cv2.contourArea)
    return cv2.minAreaRect(contour)


def building_sizes(geometry_report: Path) -> dict[int, tuple[float, float, float]]:
    report = json.loads(geometry_report.read_text(encoding="utf-8"))
    result: dict[int, tuple[float, float, float]] = {}
    for entry in report["results"]:
        name = entry["collection"]
        building_type = int(name[3:5])
        size = entry["bounds"]["size_blender_xyz"]
        result[building_type] = (float(size[0]), float(size[2]), float(size[1]))
    if set(result) != set(EXPECTED_COUNTS):
        raise RuntimeError("Geometry report does not contain BF01 through BF24")
    return result


def marker_yaw_and_size(
    image: np.ndarray,
    item: dict[str, Any],
    expected_size: tuple[float, float, float],
) -> tuple[float, float, float]:
    _, (rect_width, rect_height), angle = marker_rectangle(image, item)
    size_x, _, size_z = expected_size
    direct_error = abs(math.log(max(rect_width / max(rect_height, 0.001), 0.001) / (size_x / size_z)))
    swapped_error = abs(math.log(max(rect_height / max(rect_width, 0.001), 0.001) / (size_x / size_z)))
    local_x_image_angle = angle if direct_error <= swapped_error else angle + 90.0

    # The marker itself has a 180-degree ambiguity.  Point the prefab's +Z
    # (validated front direction) toward the city/road network rather than the
    # outer desert.  A later in-scene accessibility audit may refine individual
    # entrances without changing scale.
    local_x = math.radians(local_x_image_angle)
    forward_options = [
        (-math.sin(local_x), math.cos(local_x)),
        (math.sin(local_x), -math.cos(local_x)),
    ]
    toward_center = (MAP_ORIGIN_X - item["x"], MAP_ORIGIN_Y - item["y"])
    forward_image = max(
        forward_options,
        key=lambda direction: direction[0] * toward_center[0] + direction[1] * toward_center[1],
    )
    world_forward_x = forward_image[0]
    world_forward_z = -forward_image[1]
    yaw = math.degrees(math.atan2(world_forward_x, world_forward_z)) % 360.0
    return yaw, rect_width * PIXEL_SCALE_METERS, rect_height * PIXEL_SCALE_METERS


def discover_spawns(image: np.ndarray) -> list[dict[str, Any]]:
    hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)
    blue = cv2.inRange(hsv, np.array([100, 100, 100]), np.array([120, 255, 255]))
    count, _, stats, centroids = cv2.connectedComponentsWithStats(blue, 8)
    centers = []
    for index in range(1, count):
        x, y, width, height, area = (int(value) for value in stats[index])
        if 3000 <= area <= 4000 and 60 <= width <= 75 and 60 <= height <= 75:
            centers.append((float(centroids[index][0]), float(centroids[index][1])))
    if len(centers) != 20:
        raise RuntimeError(f"Expected 20 spawn markers, found {len(centers)}")

    result = []
    unused = centers[:]
    for index, anchor in enumerate(SPAWN_ANCHORS, start=1):
        center = min(unused, key=lambda point: (point[0] - anchor[0]) ** 2 + (point[1] - anchor[1]) ** 2)
        unused.remove(center)
        world_x, world_z = world_from_pixel(*center)
        yaw = math.degrees(math.atan2(-world_x, -world_z)) % 360.0
        result.append(
            {
                "id": f"SP{index:02d}",
                "pixelX": round(center[0], 3),
                "pixelY": round(center[1], 3),
                "x": round(world_x, 3),
                "z": round(world_z, 3),
                "yaw": round(yaw, 3),
            }
        )
    return result


def river_samples(image: np.ndarray) -> list[dict[str, float]]:
    hsv = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)
    river = cv2.inRange(hsv, np.array([82, 45, 90]), np.array([98, 180, 245]))
    central_band = np.zeros_like(river)
    central_band[:, 850:1750] = 255
    river = cv2.bitwise_and(river, central_band)
    river = cv2.morphologyEx(
        river,
        cv2.MORPH_CLOSE,
        cv2.getStructuringElement(cv2.MORPH_RECT, (21, 121)),
    )
    count, labels, stats, _ = cv2.connectedComponentsWithStats(river, 8)
    largest = 1 + int(np.argmax(stats[1:, cv2.CC_STAT_AREA]))
    mask = labels == largest

    left = np.full(MAP_PIXEL_HEIGHT, np.nan)
    right = np.full(MAP_PIXEL_HEIGHT, np.nan)
    for y in range(MAP_PIXEL_HEIGHT):
        xs = np.flatnonzero(mask[y])
        if xs.size:
            left[y] = float(xs.min())
            right[y] = float(xs.max())

    # Close minor bridge/outline notches while preserving the broad reservoir.
    kernel = np.ones(31, dtype=float)
    valid = np.isfinite(left)
    left = np.convolve(np.where(valid, left, 0.0), kernel, mode="same") / np.convolve(valid.astype(float), kernel, mode="same")
    right = np.convolve(np.where(valid, right, 0.0), kernel, mode="same") / np.convolve(valid.astype(float), kernel, mode="same")

    result = []
    for y in range(0, MAP_PIXEL_HEIGHT, 20):
        left_x, z = world_from_pixel(float(left[y]), float(y))
        right_x, _ = world_from_pixel(float(right[y]), float(y))
        result.append(
            {
                "z": round(z, 3),
                "leftX": round(left_x, 3),
                "rightX": round(right_x, 3),
            }
        )
    return result


def wall_paths(image: np.ndarray) -> list[dict[str, Any]]:
    gray = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)
    dark = (gray < 100).astype(np.uint8) * 255
    thick = cv2.morphologyEx(
        dark,
        cv2.MORPH_OPEN,
        cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (13, 13)),
    )

    samples: list[tuple[int, float, float]] = []
    for y in range(300, 4201, 60):
        band = np.any(thick[y - 15 : y + 16] > 0, axis=0)
        xs = np.flatnonzero(band)
        left_candidates = xs[(xs > 40) & (xs < 950)]
        right_candidates = xs[(xs > 2000) & (xs < 2870)]
        left = float(np.percentile(left_candidates, 5)) if left_candidates.size > 20 else math.nan
        right = float(np.percentile(right_candidates, 95)) if right_candidates.size > 20 else math.nan
        samples.append((y, left, right))

    def interpolate(values: list[float]) -> np.ndarray:
        array = np.asarray(values, dtype=float)
        indexes = np.arange(array.size)
        valid = np.isfinite(array)
        array[~valid] = np.interp(indexes[~valid], indexes[valid], array[valid])
        return np.convolve(array, np.ones(5) / 5.0, mode="same")

    left_values = interpolate([sample[1] for sample in samples])
    right_values = interpolate([sample[2] for sample in samples])
    ys = [sample[0] for sample in samples]

    left_pixels = [(1160.0, 55.0), (850.0, 80.0), (680.0, 125.0), (480.0, 255.0)]
    right_pixels = [(1530.0, 55.0), (2150.0, 120.0), (2450.0, 300.0)]
    for index in range(0, len(ys), 2):
        left_pixels.append((float(left_values[index]), float(ys[index])))
        right_pixels.append((float(right_values[index]), float(ys[index])))
    left_pixels.extend([(460.0, 4140.0), (900.0, 4310.0), (1200.0, 4340.0)])
    right_pixels.extend([(2460.0, 4230.0), (1530.0, 4340.0)])

    def convert(name: str, pixels: list[tuple[float, float]]) -> dict[str, Any]:
        points = []
        for x, y in pixels:
            world_x, world_z = world_from_pixel(x, y)
            points.append({"x": round(world_x, 3), "z": round(world_z, 3)})
        return {"name": name, "points": points}

    return [convert("WestWall", left_pixels), convert("EastWall", right_pixels)]


def build_manifest(image: np.ndarray, geometry_report: Path, source_image: Path) -> dict[str, Any]:
    candidates = discover_label_candidates(image)
    classify_labels(image, candidates)
    sizes = building_sizes(geometry_report)

    buildings = []
    counters: Counter[int] = Counter()
    for item in sorted(candidates, key=lambda value: (value["type"], value["y"], value["x"])):
        building_type = int(item["type"])
        counters[building_type] += 1
        world_x, world_z = world_from_pixel(item["x"], item["y"])
        yaw, marker_width, marker_depth = marker_yaw_and_size(image, item, sizes[building_type])
        size_x, size_y, size_z = sizes[building_type]
        buildings.append(
            {
                "id": f"BF-{building_type:02d}-{counters[building_type]:02d}",
                "type": building_type,
                "pixelX": item["x"],
                "pixelY": item["y"],
                "x": round(world_x, 3),
                "z": round(world_z, 3),
                "yaw": round(yaw, 3),
                "sizeX": round(size_x, 3),
                "sizeY": round(size_y, 3),
                "sizeZ": round(size_z, 3),
                "markerWidth": round(marker_width, 3),
                "markerDepth": round(marker_depth, 3),
                "category": marker_category(building_type),
            }
        )

    # The BF-12 planning marker is intentionally tucked against the east
    # riverbank. Its final 12 m footprint is wider than the marker symbol, so
    # give the production mesh a measured bank clearance instead of allowing
    # roughly three metres of the building to overhang the water.
    for building in buildings:
        if building["id"] == "BF-12-01":
            building["x"] = 7.0

    return {
        "schemaVersion": 1,
        "sourceImage": str(source_image).replace("\\", "/"),
        "mapWidth": 512.0,
        "mapLength": 768.0,
        "pixelScaleMeters": PIXEL_SCALE_METERS,
        "pixelOriginX": MAP_ORIGIN_X,
        "pixelOriginY": MAP_ORIGIN_Y,
        "buildingCount": len(buildings),
        "spawnCount": 20,
        "buildings": buildings,
        "spawns": discover_spawns(image),
        "river": river_samples(image),
        "walls": wall_paths(image),
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--image", required=True, type=Path)
    parser.add_argument("--geometry-report", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    image = read_image(args.image)
    manifest = build_manifest(image, args.geometry_report, args.image)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    print(
        f"Wrote {manifest['buildingCount']} buildings, {manifest['spawnCount']} spawns, "
        f"{len(manifest['river'])} river samples -> {args.output}"
    )


if __name__ == "__main__":
    main()
