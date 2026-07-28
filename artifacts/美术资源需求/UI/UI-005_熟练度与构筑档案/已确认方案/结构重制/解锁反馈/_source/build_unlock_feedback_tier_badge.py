"""Build the UI-005 unlock milestone tier badge frame.

This asset is deliberately geometry-only: its empty centre is a runtime slot for
TMP tier text or a runtime PatternDefinition graphic.  It contains no gameplay
content, strings, icons, rewards, success marks, or progress data.
"""

from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
MERGED = ROOT / "_merged"
FINAL = ROOT / "最终素材"
SIZE = 512
CENTER = (SIZE // 2, SIZE // 2)


def hexagon(radius: float):
    # Pointed-top hexagon.  The half-pixel-friendly polygon is rendered at 4x.
    import math
    return [
        (
            CENTER[0] + radius * math.cos(math.radians(60 * i - 30)),
            CENTER[1] + radius * math.sin(math.radians(60 * i - 30)),
        )
        for i in range(6)
    ]


def scale_points(points, scale=4):
    scaled = [(round(x * scale), round(y * scale)) for x, y in points]
    return scaled + [scaled[0]]


def main():
    MERGED.mkdir(parents=True, exist_ok=True)
    FINAL.mkdir(parents=True, exist_ok=True)

    scale = 4
    canvas = Image.new("RGBA", (SIZE * scale, SIZE * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas)

    # A quiet transparent halo separates the badge from nearby ProgressRing
    # components while preserving a fully empty content centre.
    halo = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    halo_draw = ImageDraw.Draw(halo)
    halo_draw.line(scale_points(hexagon(205), scale), fill=(85, 229, 218, 48), width=12 * scale, joint="curve")
    halo = halo.filter(ImageFilter.GaussianBlur(7 * scale))
    canvas.alpha_composite(halo)

    # Main double bevel: neutral metal-like line plus cyan inset.  No texture
    # or material pixels are used, so PIL geometry is the intended pipeline.
    draw = ImageDraw.Draw(canvas)
    draw.line(scale_points(hexagon(190), scale), fill=(217, 226, 230, 235), width=10 * scale, joint="curve")
    draw.line(scale_points(hexagon(174), scale), fill=(85, 229, 218, 210), width=6 * scale, joint="curve")
    draw.line(scale_points(hexagon(156), scale), fill=(201, 220, 229, 125), width=3 * scale, joint="curve")

    # Three tiny, purely structural anchors make the frame legible at 32 px;
    # they are not focus indicators, data, glyphs, or decorative content.
    for x, y in ((256, 63), (91, 350), (421, 350)):
        r = 7 * scale
        draw.ellipse((x * scale - r, y * scale - r, x * scale + r, y * scale + r), fill=(191, 255, 248, 220))

    composite = canvas.resize((SIZE, SIZE), Image.Resampling.LANCZOS)
    composite.save(MERGED / "unlock_feedback_tier_badge_frame_composite.png", optimize=True)
    composite.save(FINAL / "unlock_feedback_tier_badge_frame.png", optimize=True)


if __name__ == "__main__":
    main()
