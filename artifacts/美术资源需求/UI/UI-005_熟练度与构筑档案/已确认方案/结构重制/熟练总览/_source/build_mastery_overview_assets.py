"""Build the UI-005 Mastery Overview page-specific geometric sprites.

This source intentionally contains no text, player identity, pattern artwork,
level data, or ownership state.  It uses only deterministic PIL geometry.
"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image, ImageDraw, PngImagePlugin


ROOT = Path(__file__).resolve().parents[1]
FINAL = ROOT / "最终素材"
MERGED = ROOT / "_merged"
SCALE = 4

PALETTE = {
    "mist": (206, 224, 229, 214),
    "cyan": (111, 237, 229, 235),
    "white": (244, 252, 252, 224),
    "muted": (151, 180, 188, 178),
}


def canvas(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", (size[0] * SCALE, size[1] * SCALE), (0, 0, 0, 0))


def draw_line(draw: ImageDraw.ImageDraw, points, fill, width: int = 1) -> None:
    draw.line([(x * SCALE, y * SCALE) for x, y in points], fill=fill, width=width * SCALE, joint="curve")


def draw_polygon(draw: ImageDraw.ImageDraw, points, fill, outline=None, width: int = 1) -> None:
    scaled = [(x * SCALE, y * SCALE) for x, y in points]
    draw.polygon(scaled, fill=fill)
    if outline:
        draw.line([*scaled, scaled[0]], fill=outline, width=width * SCALE, joint="curve")


def downsample(image: Image.Image) -> Image.Image:
    return image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS)


def header_mark() -> Image.Image:
    image = canvas((256, 256))
    draw = ImageDraw.Draw(image)

    # Abstract archive tabs only: deliberately unrelated to any of the eight patterns.
    draw.rounded_rectangle((57 * SCALE, 81 * SCALE, 181 * SCALE, 196 * SCALE), radius=12 * SCALE, fill=(206, 224, 229, 18), outline=PALETTE["mist"], width=2 * SCALE)
    draw.rounded_rectangle((76 * SCALE, 59 * SCALE, 200 * SCALE, 174 * SCALE), radius=12 * SCALE, fill=(244, 252, 252, 16), outline=PALETTE["white"], width=2 * SCALE)
    draw.rounded_rectangle((94 * SCALE, 38 * SCALE, 162 * SCALE, 67 * SCALE), radius=8 * SCALE, fill=(111, 237, 229, 48), outline=PALETTE["cyan"], width=2 * SCALE)
    draw_line(draw, [(96, 102), (171, 102)], PALETTE["muted"], 2)
    draw_line(draw, [(96, 126), (179, 126)], PALETTE["muted"], 2)
    draw_line(draw, [(96, 150), (151, 150)], PALETTE["muted"], 2)
    draw_line(draw, [(81, 213), (176, 213)], PALETTE["cyan"], 2)
    draw_line(draw, [(96, 225), (162, 225)], PALETTE["mist"], 1)
    return downsample(image)


def header_divider() -> Image.Image:
    image = canvas((512, 48))
    draw = ImageDraw.Draw(image)
    # Slim symmetrical divider; center remains intentionally empty for TMP title.
    draw_line(draw, [(14, 24), (128, 24), (148, 16), (172, 16), (192, 24), (220, 24)], PALETTE["mist"], 2)
    draw_line(draw, [(292, 24), (320, 24), (340, 16), (364, 16), (384, 24), (498, 24)], PALETTE["mist"], 2)
    draw_line(draw, [(14, 30), (126, 30), (148, 22), (170, 22)], PALETTE["muted"], 1)
    draw_line(draw, [(342, 22), (364, 22), (386, 30), (498, 30)], PALETTE["muted"], 1)
    for x in (148, 172, 340, 364):
        draw.ellipse(((x - 2) * SCALE, 14 * SCALE, (x + 2) * SCALE, 18 * SCALE), fill=PALETTE["cyan"])
    return downsample(image)


def empty_mark() -> Image.Image:
    image = canvas((256, 256))
    draw = ImageDraw.Draw(image)
    # Neutral empty archive tray: no lock, pattern icon, identity, or text.
    draw.rounded_rectangle((50 * SCALE, 70 * SCALE, 184 * SCALE, 181 * SCALE), radius=10 * SCALE, fill=(206, 224, 229, 18), outline=PALETTE["mist"], width=2 * SCALE)
    draw.rounded_rectangle((72 * SCALE, 51 * SCALE, 206 * SCALE, 162 * SCALE), radius=10 * SCALE, fill=(244, 252, 252, 16), outline=PALETTE["white"], width=2 * SCALE)
    draw_line(draw, [(87, 82), (174, 82)], PALETTE["muted"], 2)
    draw_line(draw, [(87, 105), (159, 105)], PALETTE["muted"], 2)
    draw_line(draw, [(87, 128), (142, 128)], PALETTE["muted"], 2)
    draw_line(draw, [(65, 194), (191, 194)], PALETTE["cyan"], 2)
    draw_line(draw, [(84, 205), (172, 205)], PALETTE["mist"], 1)
    draw.rounded_rectangle((177 * SCALE, 181 * SCALE, 201 * SCALE, 205 * SCALE), radius=5 * SCALE, fill=(111, 237, 229, 26), outline=PALETTE["cyan"], width=2 * SCALE)
    return downsample(image)


def metadata(name: str, size: tuple[int, int], role: str) -> PngImagePlugin.PngInfo:
    info = PngImagePlugin.PngInfo()
    info.add_text("asset", name)
    info.add_text("role", role)
    info.add_text("provenance", "Deterministic PIL pure geometry; no mockup crop; UI-005 structure remake.")
    info.add_text("runtime_boundary", "No text, data values, pattern artwork, player identity, or ownership state.")
    info.add_text("dimensions", f"{size[0]}x{size[1]} RGBA")
    return info


def save(image: Image.Image, name: str, role: str) -> None:
    image.save(FINAL / name, pnginfo=metadata(name, image.size, role), optimize=False)


def merged_preview(images: list[tuple[str, Image.Image]]) -> Image.Image:
    preview = Image.new("RGBA", (1024, 384), (0, 0, 0, 0))
    preview.alpha_composite(images[0][1], (64, 64))
    preview.alpha_composite(images[1][1], (256, 24))
    preview.alpha_composite(images[2][1], (704, 64))
    return preview


def main() -> None:
    FINAL.mkdir(parents=True, exist_ok=True)
    MERGED.mkdir(parents=True, exist_ok=True)
    images = [
        ("mastery_overview_header_mark.png", header_mark(), "page-header neutral archive mark"),
        ("mastery_overview_header_divider.png", header_divider(), "stretchable page-header grouping divider"),
        ("mastery_overview_empty_mark.png", empty_mark(), "empty archive and sync-fallback neutral mark"),
    ]
    for name, image, role in images:
        save(image, name, role)
    merged_preview(images).save(MERGED / "mastery_overview_geometry_merged.png", pnginfo=metadata("mastery_overview_geometry_merged.png", (1024, 384), "traceability-only geometry merge"), optimize=False)
    recipe = {
        "method": "PIL deterministic pure geometry, 4x supersampling and Lanczos downsample",
        "not_generated": ["pattern artwork", "ownership state", "mastery values", "player identity", "all text", "generic UI shells"],
        "assets": [{"file": name, "size": list(image.size), "role": role} for name, image, role in images],
    }
    (ROOT / "_source" / "geometry_recipe.json").write_text(json.dumps(recipe, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
