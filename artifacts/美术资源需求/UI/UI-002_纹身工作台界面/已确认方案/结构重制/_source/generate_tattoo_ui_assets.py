"""Generate the C_v2 TattooStudioForm page-specific, pure-geometry UI shells.

These assets intentionally contain no material, gloss, text, runtime content, or
generic component duplicate.  They are PIL-drawn vector-like geometry per the
component specification, not crops from the C_v2 mockup.
"""
from pathlib import Path
from PIL import Image, ImageDraw, PngImagePlugin
import json

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "最终素材"
MERGED = ROOT / "_merged"
OUT.mkdir(parents=True, exist_ok=True)
MERGED.mkdir(parents=True, exist_ok=True)

S = 4  # supersampling factor for clean raster edges
INK = (199, 211, 213, 238)
MUTED = (129, 151, 157, 210)
FAINT = (103, 124, 130, 165)
CORAL = (230, 121, 105, 232)

def canvas(w=512, h=512):
    return Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))

def line(draw, points, fill=INK, width=5, joint="curve"):
    draw.line([(x * S, y * S) for x, y in points], fill=fill, width=width * S, joint=joint)

def ellipse(draw, box, fill=None, outline=INK, width=5):
    draw.ellipse(tuple(v * S for v in box), fill=fill, outline=outline, width=width * S)

def polygon(draw, points, fill=None, outline=INK, width=5):
    p = [(x * S, y * S) for x, y in points]
    draw.polygon(p, fill=fill)
    if outline:
        draw.line(p + [p[0]], fill=outline, width=width * S, joint="curve")

def finish(im, name):
    im = im.resize((im.width // S, im.height // S), Image.Resampling.LANCZOS)
    # Transparent corners and a declared RGBA format are intentional acceptance invariants.
    info = PngImagePlugin.PngInfo()
    info.add_text("asset", name)
    info.add_text("generator", "PIL pure-geometry; C_v2 TattooStudioForm dedicated shell")
    info.add_text("usage", "Image Simple unless stated in 素材清单.md")
    im.save(OUT / f"{name}.png", pnginfo=info, optimize=False)

def body_head():
    im = canvas(); d = ImageDraw.Draw(im)
    ellipse(d, (176, 76, 336, 236), outline=INK, width=6)
    line(d, [(205, 219), (204, 267), (238, 291), (252, 346)], width=6)
    line(d, [(306, 218), (309, 266), (274, 291), (259, 346)], width=6)
    line(d, [(206, 268), (258, 284), (309, 267)], fill=MUTED, width=4)
    ellipse(d, (262, 152, 281, 171), fill=INK, outline=None)
    finish(im, "tattoo_bodypart_icon_head")

def body_torso():
    im = canvas(); d = ImageDraw.Draw(im)
    line(d, [(183, 93), (130, 135), (151, 190), (165, 330), (203, 408)], width=6)
    line(d, [(329, 93), (382, 135), (361, 190), (347, 330), (309, 408)], width=6)
    line(d, [(183, 93), (224, 119), (256, 106), (288, 119), (329, 93)], width=6)
    line(d, [(203, 408), (256, 428), (309, 408)], width=6)
    line(d, [(256, 109), (256, 415)], fill=MUTED, width=4)
    finish(im, "tattoo_bodypart_icon_torso")

def body_arm():
    im = canvas(); d = ImageDraw.Draw(im)
    line(d, [(258, 64), (202, 105), (190, 187), (220, 251), (241, 337), (195, 406), (209, 454)], width=7)
    line(d, [(294, 76), (264, 126), (262, 184), (282, 246), (298, 328), (249, 396), (259, 451)], width=7)
    line(d, [(204, 107), (235, 123), (265, 124)], fill=MUTED, width=4)
    finish(im, "tattoo_bodypart_icon_arm")

def body_leg():
    im = canvas(); d = ImageDraw.Draw(im)
    line(d, [(204, 58), (309, 58), (295, 178), (303, 295), (333, 406), (316, 457), (195, 457), (178, 426), (220, 379), (209, 293), (217, 177), (204, 58)], width=7)
    line(d, [(218, 177), (294, 177)], fill=MUTED, width=4)
    line(d, [(209, 293), (302, 295)], fill=MUTED, width=4)
    finish(im, "tattoo_bodypart_icon_leg")

def compare_arrow():
    im = canvas(512, 256); d = ImageDraw.Draw(im)
    line(d, [(62, 128), (390, 128)], width=10)
    polygon(d, [(390, 72), (463, 128), (390, 184)], fill=INK, outline=None)
    line(d, [(62, 102), (62, 154)], fill=MUTED, width=6)
    finish(im, "tattoo_compare_arrow")

def empty_slot():
    im = canvas(); d = ImageDraw.Draw(im)
    # Corner brackets preserve a neutral, stretch-safe Sliced shell with no item content.
    brackets = [
        [(90, 170), (90, 112), (148, 112)], [(364, 112), (422, 112), (422, 170)],
        [(90, 342), (90, 400), (148, 400)], [(364, 400), (422, 400), (422, 342)],
    ]
    for b in brackets: line(d, b, fill=MUTED, width=6)
    polygon(d, [(256, 219), (293, 256), (256, 293), (219, 256)], outline=FAINT, width=4)
    line(d, [(202, 256), (310, 256)], fill=FAINT, width=3)
    finish(im, "tattoo_workbench_empty_slot")

def divider_diamond():
    im = canvas(256, 256); d = ImageDraw.Draw(im)
    polygon(d, [(128, 44), (212, 128), (128, 212), (44, 128)], outline=MUTED, width=5)
    polygon(d, [(128, 83), (173, 128), (128, 173), (83, 128)], outline=INK, width=4)
    finish(im, "tattoo_section_divider_diamond")

def preview_empty():
    im = canvas(); d = ImageDraw.Draw(im)
    # A neutral projection-frame glyph: deliberately not a character model or body sprite.
    for p in [
        [(91, 164), (91, 91), (164, 91)], [(348, 91), (421, 91), (421, 164)],
        [(91, 348), (91, 421), (164, 421)], [(348, 421), (421, 421), (421, 348)],
    ]: line(d, p, fill=MUTED, width=6)
    ellipse(d, (189, 189, 323, 323), outline=FAINT, width=5)
    polygon(d, [(256, 212), (300, 256), (256, 300), (212, 256)], outline=INK, width=5)
    finish(im, "tattoo_preview_empty_shell")

def preview_error():
    im = canvas(); d = ImageDraw.Draw(im)
    for p in [
        [(91, 164), (91, 91), (164, 91)], [(348, 91), (421, 91), (421, 164)],
        [(91, 348), (91, 421), (164, 421)], [(348, 421), (421, 421), (421, 348)],
    ]: line(d, p, fill=MUTED, width=6)
    polygon(d, [(256, 170), (339, 326), (173, 326)], outline=CORAL, width=7)
    line(d, [(256, 218), (256, 271)], fill=CORAL, width=7)
    ellipse(d, (252, 292, 260, 300), fill=CORAL, outline=None)
    finish(im, "tattoo_preview_error_shell")

body_head(); body_torso(); body_arm(); body_leg(); compare_arrow(); empty_slot(); divider_diamond(); preview_empty(); preview_error()

metadata = {
    "workflow": "PIL pure-color geometry; no imagegen required because no material, highlight, texture, or non-geometric rendering is present.",
    "canvas": "512 square except tattoo_compare_arrow 512x256 and tattoo_section_divider_diamond 256x256",
    "dedicated_assets": 9,
    "generic_reference_not_generated": ["state_loading", "state_success", "state_failure", "state_lock", "state_disabled", "panel_", "card_", "row_", "focus_", "selection_", "progress_", "prompt_"],
}
(ROOT / "_source" / "PIL参数.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")

# Review-only merged contact sheet; final files remain individual transparent PNGs.
items = [
    "tattoo_bodypart_icon_head", "tattoo_bodypart_icon_torso", "tattoo_bodypart_icon_arm",
    "tattoo_bodypart_icon_leg", "tattoo_compare_arrow", "tattoo_workbench_empty_slot",
    "tattoo_section_divider_diamond", "tattoo_preview_empty_shell", "tattoo_preview_error_shell",
]
sheet = Image.new("RGBA", (1536, 1536), (31, 47, 54, 255))
for index, asset in enumerate(items):
    image = Image.open(OUT / f"{asset}.png").convert("RGBA")
    image.thumbnail((430, 430), Image.Resampling.LANCZOS)
    x = (index % 3) * 512 + (512 - image.width) // 2
    y = (index // 3) * 512 + (512 - image.height) // 2
    sheet.alpha_composite(image, (x, y))
sheet.save(MERGED / "tattoo_workbench_dedicated_shells_contact_sheet.png", optimize=False)
