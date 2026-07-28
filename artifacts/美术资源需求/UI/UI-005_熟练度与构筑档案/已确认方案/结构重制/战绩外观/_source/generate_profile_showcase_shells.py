"""Generate the UI-005 ProfileShowcaseForm-only static geometric sprites.

The two assets intentionally convey only page-local semantics.  Character
appearance, presets, emblems, player identity, records, share compositions,
input glyphs, and every piece of text remain runtime/TMP content.
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
    "cyan": (111, 237, 229, 238),
    "white": (244, 252, 252, 226),
    "muted": (151, 180, 188, 178),
    "fill": (206, 224, 229, 18),
    "cyan_fill": (111, 237, 229, 34),
}


def canvas(size: tuple[int, int]) -> Image.Image:
    return Image.new("RGBA", (size[0] * SCALE, size[1] * SCALE), (0, 0, 0, 0))


def point(x: float, y: float) -> tuple[int, int]:
    return round(x * SCALE), round(y * SCALE)


def line(draw: ImageDraw.ImageDraw, points: list[tuple[float, float]], fill, width: int = 1) -> None:
    draw.line([point(x, y) for x, y in points], fill=fill, width=width * SCALE, joint="curve")


def rounded(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], radius: int, *, fill, outline, width: int = 1) -> None:
    draw.rounded_rectangle(tuple(value * SCALE for value in box), radius=radius * SCALE, fill=fill, outline=outline, width=width * SCALE)


def downsample(image: Image.Image) -> Image.Image:
    return image.resize((image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS)


def appearance_icon() -> Image.Image:
    """Abstract appearance-library marker: layered display planes, never a character or outfit."""
    image = canvas((192, 192))
    draw = ImageDraw.Draw(image)
    # Three offset planes signify selectable appearance layers without depicting any avatar or garment.
    rounded(draw, (37, 67, 124, 145), 11, fill=PALETTE["fill"], outline=PALETTE["muted"], width=2)
    rounded(draw, (54, 50, 141, 128), 11, fill=(244, 252, 252, 15), outline=PALETTE["mist"], width=2)
    rounded(draw, (71, 33, 158, 111), 11, fill=PALETTE["cyan_fill"], outline=PALETTE["white"], width=2)
    # A small neutral aperture and rail distinguish this from a generic ContentCard.
    draw.ellipse((103 * SCALE, 55 * SCALE, 126 * SCALE, 78 * SCALE), fill=(0, 0, 0, 0), outline=PALETTE["cyan"], width=2 * SCALE)
    line(draw, [(85, 93), (144, 93)], PALETTE["muted"], 2)
    line(draw, [(85, 105), (132, 105)], PALETTE["muted"], 2)
    line(draw, [(49, 158), (143, 158)], PALETTE["cyan"], 2)
    line(draw, [(66, 169), (126, 169)], PALETTE["mist"], 1)
    for x in (55, 67, 79):
        draw.ellipse(((x - 2) * SCALE, 152 * SCALE, (x + 2) * SCALE, 156 * SCALE), fill=PALETTE["mist"])
    return downsample(image)


def empty_mark() -> Image.Image:
    """Neutral archive empty-state marker with no lock, badge, player, or copy."""
    image = canvas((192, 192))
    draw = ImageDraw.Draw(image)
    # Open archive tray: an empty container only, suitable for both empty preset and empty emblem states.
    rounded(draw, (35, 78, 154, 135), 10, fill=PALETTE["fill"], outline=PALETTE["mist"], width=2)
    rounded(draw, (52, 59, 171, 116), 10, fill=(244, 252, 252, 15), outline=PALETTE["white"], width=2)
    line(draw, [(67, 83), (151, 83)], PALETTE["muted"], 2)
    line(draw, [(67, 101), (139, 101)], PALETTE["muted"], 2)
    line(draw, [(52, 136), (154, 136)], PALETTE["cyan"], 2)
    line(draw, [(67, 148), (139, 148)], PALETTE["mist"], 1)
    # Empty aperture; it is intentionally not a badge, pattern, avatar, or loading spinner.
    draw.arc((111 * SCALE, 121 * SCALE, 153 * SCALE, 163 * SCALE), start=208, end=44, fill=PALETTE["cyan"], width=2 * SCALE)
    draw.ellipse((131 * SCALE, 139 * SCALE, 135 * SCALE, 143 * SCALE), fill=PALETTE["cyan"])
    return downsample(image)


def metadata(name: str, size: tuple[int, int], role: str) -> PngImagePlugin.PngInfo:
    info = PngImagePlugin.PngInfo()
    info.add_text("asset", name)
    info.add_text("role", role)
    info.add_text("provenance", "Deterministic PIL pure geometry; 4x supersampling; no mockup crop.")
    info.add_text("runtime_boundary", "No character, outfit, preset, emblem, player identity, record data, share preview, input glyph, or text.")
    info.add_text("dimensions", f"{size[0]}x{size[1]} RGBA")
    return info


def save(image: Image.Image, name: str, role: str, directory: Path) -> None:
    image.save(directory / name, pnginfo=metadata(name, image.size, role), optimize=False)


def build_merged_preview(images: list[tuple[str, Image.Image, str]]) -> Image.Image:
    """Traceability-only transparent arrangement; it is not a UI mockup or a crop."""
    preview = Image.new("RGBA", (512, 256), (0, 0, 0, 0))
    preview.alpha_composite(images[0][1], (40, 32))
    preview.alpha_composite(images[1][1], (280, 32))
    return preview


def main() -> None:
    FINAL.mkdir(parents=True, exist_ok=True)
    MERGED.mkdir(parents=True, exist_ok=True)
    assets = [
        ("profile_showcase_icon_appearance.png", appearance_icon(), "appearance-preset section semantic marker"),
        ("profile_showcase_empty_mark.png", empty_mark(), "empty preset / empty emblem neutral archive marker"),
    ]
    for name, image, role in assets:
        save(image, name, role, FINAL)
        save(image, name, role, MERGED)
    preview = build_merged_preview(assets)
    save(preview, "profile_showcase_shells_merged.png", "traceability-only transparent source arrangement", MERGED)
    recipe = {
        "method": "PIL deterministic pure geometry, 4x supersampling and Lanczos downsample",
        "not_generated": [
            "character appearance", "outfit or tattoo layers", "preset content or thumbnails", "eight pattern emblems",
            "player identity or avatar", "record values", "share preview", "all text", "input glyphs", "generic panels/cards/focus/progress/state shells",
        ],
        "assets": [{"file": name, "size": list(image.size), "role": role} for name, image, role in assets],
    }
    (ROOT / "_source" / "geometry_recipe.json").write_text(json.dumps(recipe, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
