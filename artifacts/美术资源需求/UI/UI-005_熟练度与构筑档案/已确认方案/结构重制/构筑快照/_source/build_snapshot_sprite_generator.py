"""Generate UI-005 BuildSnapshotForm's geometry-only page sprites.

The generated art deliberately contains no gameplay, character, pattern, pigment,
avatar, result, date, or textual content.  All eight files are simple semantic
shells designed to be tinted/positioned by Unity at runtime.
"""

from __future__ import annotations

import math
import shutil
from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
MERGED = ROOT / "_merged"
FINAL = ROOT / "最终素材"
SCALE = 4

INK = (46, 83, 105, 235)
STEEL = (179, 205, 215, 220)
MIST = (229, 244, 246, 180)
CYAN = (88, 214, 216, 230)
GLOW = (118, 238, 232, 100)
WHITE = (248, 253, 252, 220)


def px(value: int) -> int:
    return value * SCALE


def surface(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", (px(size[0]), px(size[1])), (0, 0, 0, 0))


def line(draw: ImageDraw.ImageDraw, points, fill, width: int) -> None:
    draw.line([(px(x), px(y)) for x, y in points], fill=fill, width=px(width), joint="curve")


def ellipse(draw: ImageDraw.ImageDraw, box, fill=None, outline=None, width: int = 1) -> None:
    draw.ellipse(tuple(px(v) for v in box), fill=fill, outline=outline, width=px(width))


def rect(draw: ImageDraw.ImageDraw, box, radius: int, fill=None, outline=None, width: int = 1) -> None:
    draw.rounded_rectangle(tuple(px(v) for v in box), radius=px(radius), fill=fill, outline=outline, width=px(width))


def polygon(draw: ImageDraw.ImageDraw, points, fill=None, outline=None, width: int = 1) -> None:
    points = [(px(x), px(y)) for x, y in points]
    draw.polygon(points, fill=fill)
    if outline:
        draw.line(points + [points[0]], fill=outline, width=px(width), joint="curve")


def save_layers(name: str, base: Image.Image, accent: Image.Image) -> None:
    """Persist source geometry, merged alpha composition, and runtime final."""
    MERGED.mkdir(parents=True, exist_ok=True)
    FINAL.mkdir(parents=True, exist_ok=True)
    source_path = ROOT / "_source" / f"{name}.png"
    merged_path = MERGED / f"{name}.png"
    base.resize((base.width // SCALE, base.height // SCALE), Image.Resampling.LANCZOS).save(source_path)
    composed = Image.alpha_composite(base, accent)
    composed = composed.resize((composed.width // SCALE, composed.height // SCALE), Image.Resampling.LANCZOS)
    composed.save(merged_path)
    shutil.copy2(merged_path, FINAL / f"{name}.png")


def archive_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    rect(d, (22, 32, 106, 100), 11, fill=MIST, outline=INK, width=4)
    rect(d, (30, 21, 98, 47), 8, fill=(210, 233, 237, 195), outline=INK, width=4)
    line(d, [(44, 63), (84, 63)], STEEL, 4)
    line(d, [(44, 78), (75, 78)], STEEL, 4)
    rect(a, (85, 77, 98, 90), 3, fill=CYAN)
    return base, accent


def edit_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    polygon(d, [(35, 89), (40, 68), (82, 26), (102, 46), (60, 88)], fill=MIST, outline=INK, width=4)
    polygon(d, [(82, 26), (91, 17), (111, 37), (102, 46)], fill=STEEL, outline=INK, width=4)
    line(d, [(35, 89), (57, 84)], INK, 4)
    line(a, [(42, 83), (87, 38)], CYAN, 4)
    return base, accent


def connection_line() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((256, 128)), surface((256, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    center = (128, 66)
    endpoints = [(46, 28), (210, 28), (46, 103), (210, 103)]
    for endpoint in endpoints:
        line(d, [center, endpoint], (75, 121, 143, 190), 3)
        line(a, [center, endpoint], GLOW, 7)
    for x, y in [center, *endpoints]:
        ellipse(d, (x - 9, y - 9, x + 9, y + 9), fill=MIST, outline=INK, width=3)
        ellipse(a, (x - 4, y - 4, x + 4, y + 4), fill=CYAN)
    return base, accent


def favorite_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    points = []
    for index in range(10):
        radius = 47 if index % 2 == 0 else 21
        angle = -math.pi / 2 + index * math.pi / 5
        points.append((64 + int(radius * math.cos(angle)), 64 + int(radius * math.sin(angle))))
    polygon(d, points, fill=MIST, outline=INK, width=4)
    polygon(a, [(64, 27), (70, 51), (95, 52), (75, 67), (82, 92), (64, 78)], fill=(113, 221, 218, 125))
    return base, accent


def total_uses_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    for y, width in [(35, 48), (61, 69), (87, 38)]:
        rect(d, (30, y, 30 + width, y + 12), 5, fill=MIST, outline=INK, width=3)
    for x, y in [(42, 41), (42, 67), (42, 93)]:
        ellipse(a, (x - 3, y - 3, x + 3, y + 3), fill=CYAN)
    return base, accent


def success_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    polygon(d, [(64, 18), (101, 38), (101, 85), (64, 109), (27, 85), (27, 38)], fill=MIST, outline=INK, width=4)
    line(d, [(42, 64), (58, 80), (87, 49)], STEEL, 7)
    line(a, [(43, 63), (58, 78), (86, 48)], CYAN, 3)
    return base, accent


def success_rate_icon() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((128, 128)), surface((128, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    ellipse(d, (23, 23, 105, 105), outline=INK, width=8)
    # A semantic gauge arc, deliberately no percentage or outcome text.
    d.arc((px(23), px(23), px(105), px(105)), start=218, end=38, fill=CYAN, width=px(8))
    line(d, [(64, 64), (88, 43)], STEEL, 5)
    ellipse(a, (59, 59, 69, 69), fill=CYAN)
    return base, accent


def timeline_track() -> tuple[Image.Image, Image.Image]:
    base, accent = surface((512, 128)), surface((512, 128))
    d, a = ImageDraw.Draw(base), ImageDraw.Draw(accent)
    rect(d, (26, 53, 486, 75), 10, fill=(203, 226, 232, 105), outline=INK, width=3)
    line(d, [(45, 64), (467, 64)], STEEL, 3)
    for x in (54, 256, 458):
        ellipse(d, (x - 16, 48, x + 16, 80), fill=MIST, outline=INK, width=4)
        ellipse(a, (x - 6, 58, x + 6, 70), fill=CYAN)
    # The border-safe caps give the Image/Sliced asset a 12 px protected edge.
    line(a, [(28, 48), (28, 80)], GLOW, 3)
    line(a, [(484, 48), (484, 80)], GLOW, 3)
    return base, accent


ASSETS = {
    "build_snapshot_icon_archive": archive_icon,
    "build_snapshot_icon_edit": edit_icon,
    "build_snapshot_connection_line": connection_line,
    "build_snapshot_icon_favorite": favorite_icon,
    "build_snapshot_stat_total_uses": total_uses_icon,
    "build_snapshot_stat_success": success_icon,
    "build_snapshot_stat_success_rate": success_rate_icon,
    "build_snapshot_timeline_track": timeline_track,
}


if __name__ == "__main__":
    for filename, render in ASSETS.items():
        save_layers(filename, *render())
    print(f"Generated {len(ASSETS)} geometry-only sprites in {FINAL}")
