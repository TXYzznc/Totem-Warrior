"""生成第一阶段离线 UI 与 VFX 贴图。

本脚本只写入当前 OpenSpec change 的 art/production 目录，不访问 Unity Editor。
所有形状使用方盒、切角、折线和低成本几何语言，输出可重复生成。
"""

from __future__ import annotations

import json
import math
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / "production"
UI_ROOT = PRODUCTION / "ui" / "png"
VFX_ROOT = PRODUCTION / "vfx" / "textures"
PREVIEW_ROOT = PRODUCTION / "previews"

SCALE = 4

COLORS = {
    "transparent": (0, 0, 0, 0),
    # UI 色板只与绿洲新城的暖砂/青绿环境保持和谐，不复用场景纹样或材质。
    "warm_white": (239, 227, 205, 255),
    "pale_gray_blue": (157, 171, 166, 255),
    "slate": (80, 101, 103, 255),
    "deep_slate": (28, 39, 43, 255),
    "signal_blue": (55, 144, 145, 255),
    "copper": (183, 130, 79, 255),
    "positive": (89, 195, 140, 255),
    "warning": (242, 184, 75, 255),
    "danger": (230, 91, 97, 255),
    "fire": (240, 100, 60, 255),
    "ice": (98, 199, 232, 255),
    "lightning": (182, 132, 244, 255),
}


def ensure_dirs() -> None:
    for path in (
        UI_ROOT / "panels",
        UI_ROOT / "buttons",
        UI_ROOT / "icons",
        UI_ROOT / "hud",
        UI_ROOT / "backgrounds",
        VFX_ROOT,
        PREVIEW_ROOT,
    ):
        path.mkdir(parents=True, exist_ok=True)


def canvas(size: tuple[int, int], color=COLORS["transparent"]) -> Image.Image:
    return Image.new("RGBA", (size[0] * SCALE, size[1] * SCALE), color)


def save_downsample(image: Image.Image, path: Path) -> None:
    target = (image.width // SCALE, image.height // SCALE)
    image.resize(target, Image.Resampling.LANCZOS).save(path, optimize=True)


def pts(points: list[tuple[float, float]]) -> list[tuple[int, int]]:
    return [(round(x * SCALE), round(y * SCALE)) for x, y in points]


def rect(draw: ImageDraw.ImageDraw, box, fill, outline=None, width=1) -> None:
    draw.rectangle(tuple(round(v * SCALE) for v in box), fill=fill, outline=outline, width=width * SCALE)


def line(draw: ImageDraw.ImageDraw, points, fill, width=1, joint="curve") -> None:
    draw.line(pts(points), fill=fill, width=width * SCALE, joint=joint)


def chamfer_polygon(box, cut: int) -> list[tuple[int, int]]:
    x0, y0, x1, y1 = box
    return pts([
        (x0 + cut, y0), (x1 - cut, y0), (x1, y0 + cut), (x1, y1 - cut),
        (x1 - cut, y1), (x0 + cut, y1), (x0, y1 - cut), (x0, y0 + cut),
    ])


def draw_chamfer_panel(size, fill, border, cut=8, border_width=3, inner=None) -> Image.Image:
    image = canvas(size)
    draw = ImageDraw.Draw(image)
    outer = chamfer_polygon((1, 1, size[0] - 1, size[1] - 1), cut)
    draw.polygon(outer, fill=fill)
    draw.line(outer + [outer[0]], fill=border, width=border_width * SCALE, joint="curve")
    if inner:
        inset = 10
        inner_poly = chamfer_polygon((inset, inset, size[0] - inset, size[1] - inset), max(2, cut - 3))
        draw.line(inner_poly + [inner_poly[0]], fill=inner, width=SCALE)
    return image


def generate_panels_and_buttons() -> dict[str, dict]:
    metadata: dict[str, dict] = {}
    panels = {
        "UI_FP_Panel_Primary_128.png": (COLORS["deep_slate"], COLORS["copper"], COLORS["signal_blue"]),
        "UI_FP_Panel_Secondary_128.png": ((43, 58, 61, 238), COLORS["slate"], (183, 130, 79, 150)),
        "UI_FP_Panel_Light_128.png": (COLORS["warm_white"], COLORS["slate"], COLORS["signal_blue"]),
    }
    for name, palette in panels.items():
        fill, border, inner = palette
        save_downsample(draw_chamfer_panel((128, 128), fill, border, cut=8, inner=inner), UI_ROOT / "panels" / name)
        metadata[name] = {"spriteMode": "Single", "border": [12, 12, 12, 12], "pivot": [0.5, 0.5], "sRGB": True}

    button_specs = {
        "UI_FP_Button_Normal_512x96.png": ((43, 58, 61, 248), COLORS["slate"], COLORS["copper"], False),
        "UI_FP_Button_Focused_512x96.png": ((48, 70, 71, 255), COLORS["signal_blue"], COLORS["warm_white"], True),
        "UI_FP_Button_Pressed_512x96.png": ((24, 34, 37, 255), COLORS["signal_blue"], COLORS["signal_blue"], True),
        "UI_FP_Button_Disabled_512x96.png": ((53, 61, 61, 175), (92, 104, 103, 180), (125, 133, 128, 110), False),
    }
    for name, (fill, border, inner, focus) in button_specs.items():
        image = draw_chamfer_panel((512, 96), fill, border, cut=10, border_width=3, inner=inner)
        draw = ImageDraw.Draw(image)
        if focus:
            draw.polygon(pts([(0, 28), (20, 48), (0, 68)]), fill=COLORS["signal_blue"])
            rect(draw, (26, 22, 32, 74), COLORS["signal_blue"])
        save_downsample(image, UI_ROOT / "buttons" / name)
        metadata[name] = {"spriteMode": "Single", "border": [16, 16, 16, 16], "pivot": [0.5, 0.5], "sRGB": True}
    return metadata


def icon_base() -> tuple[Image.Image, ImageDraw.ImageDraw]:
    image = canvas((64, 64))
    return image, ImageDraw.Draw(image)


def icon_fire() -> Image.Image:
    image, draw = icon_base()
    for i, inset in enumerate((8, 16, 24)):
        y = 54 - i * 14
        draw.polygon(pts([(inset, y), (32, y - 16), (56 - inset, y), (32, y - 7)]), fill=COLORS["fire"])
    return image


def icon_ice() -> Image.Image:
    image, draw = icon_base()
    for inset, width in ((8, 4), (18, 3), (27, 2)):
        poly = pts([(32, inset), (64 - inset, 32), (32, 64 - inset), (inset, 32), (32, inset)])
        draw.line(poly, fill=COLORS["ice"], width=width * SCALE, joint="curve")
    return image


def icon_lightning() -> Image.Image:
    image, draw = icon_base()
    draw.polygon(pts([(36, 4), (12, 36), (28, 36), (22, 60), (52, 26), (36, 26)]), fill=COLORS["lightning"])
    rect(draw, (43, 8, 50, 15), COLORS["lightning"])
    return image


def icon_p01() -> Image.Image:
    image, draw = icon_base()
    rect(draw, (9, 9, 55, 55), None, COLORS["signal_blue"], 5)
    rect(draw, (18, 28, 46, 36), COLORS["signal_blue"])
    return image


def icon_p02() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(8, 8), (27, 8), (27, 56), (8, 56), (8, 8)], COLORS["signal_blue"], 5)
    line(draw, [(56, 8), (37, 8), (37, 56), (56, 56), (56, 8)], COLORS["signal_blue"], 5)
    rect(draw, (14, 22, 26, 29), COLORS["signal_blue"])
    rect(draw, (38, 35, 50, 42), COLORS["signal_blue"])
    return image


def icon_weakpoint() -> Image.Image:
    image, draw = icon_base()
    for inset, width in ((7, 4), (19, 3)):
        shape = pts([(32, inset), (64 - inset, 32), (32, 64 - inset), (inset, 32), (32, inset)])
        draw.line(shape, fill=COLORS["warning"], width=width * SCALE, joint="curve")
    rect(draw, (29, 29, 35, 35), COLORS["warm_white"])
    return image


def icon_health() -> Image.Image:
    image, draw = icon_base()
    rect(draw, (26, 8, 38, 56), COLORS["positive"])
    rect(draw, (8, 26, 56, 38), COLORS["positive"])
    return image


def icon_shield() -> Image.Image:
    image, draw = icon_base()
    draw.polygon(pts([(10, 10), (54, 10), (54, 38), (32, 57), (10, 38)]), fill=COLORS["pale_gray_blue"])
    draw.polygon(pts([(18, 18), (46, 18), (46, 34), (32, 47), (18, 34)]), fill=COLORS["deep_slate"])
    return image


def icon_downed() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(9, 9), (55, 9), (55, 23)], COLORS["danger"], 5)
    line(draw, [(55, 55), (9, 55), (9, 41)], COLORS["danger"], 5)
    line(draw, [(17, 18), (47, 48)], COLORS["danger"], 8)
    return image


def icon_rescue() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(8, 20), (8, 8), (24, 8)], COLORS["positive"], 4)
    line(draw, [(56, 44), (56, 56), (40, 56)], COLORS["positive"], 4)
    rect(draw, (27, 16, 37, 48), COLORS["positive"])
    rect(draw, (16, 27, 48, 37), COLORS["positive"])
    return image


def icon_warning() -> Image.Image:
    image, draw = icon_base()
    draw.polygon(pts([(32, 5), (59, 55), (5, 55)]), fill=COLORS["warning"])
    rect(draw, (28, 20, 36, 40), COLORS["deep_slate"])
    rect(draw, (28, 46, 36, 52), COLORS["deep_slate"])
    return image


def icon_ammo() -> Image.Image:
    image, draw = icon_base()
    for x, h in ((10, 36), (25, 46), (40, 30)):
        rect(draw, (x, 56 - h, x + 10, 56), COLORS["warm_white"])
        draw.polygon(pts([(x, 56 - h), (x + 5, 56 - h - 8), (x + 10, 56 - h)]), fill=COLORS["warning"])
    return image


def icon_team() -> Image.Image:
    image, draw = icon_base()
    rect(draw, (7, 12, 29, 34), COLORS["signal_blue"])
    rect(draw, (35, 30, 57, 52), COLORS["pale_gray_blue"])
    line(draw, [(29, 23), (40, 23), (40, 30)], COLORS["warm_white"], 4)
    return image


def icon_build() -> Image.Image:
    image, draw = icon_base()
    for y in (9, 35):
        for x in (6, 24, 42):
            rect(draw, (x, y, x + 14, y + 20), COLORS["pale_gray_blue"], COLORS["slate"], 2)
    return image


def icon_request() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(8, 20), (48, 20)], COLORS["signal_blue"], 6)
    draw.polygon(pts([(48, 10), (60, 20), (48, 30)]), fill=COLORS["signal_blue"])
    line(draw, [(56, 44), (16, 44)], COLORS["positive"], 6)
    draw.polygon(pts([(16, 34), (4, 44), (16, 54)]), fill=COLORS["positive"])
    return image


def icon_heat_shock() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(7, 7), (29, 7), (29, 57), (7, 57)], COLORS["fire"], 5)
    line(draw, [(57, 7), (35, 7), (35, 57), (57, 57)], COLORS["ice"], 5)
    draw.polygon(pts([(22, 32), (32, 22), (42, 32), (32, 42)]), fill=COLORS["warm_white"])
    return image


def icon_overload() -> Image.Image:
    image, draw = icon_base()
    nodes = [(32, 8), (8, 52), (56, 52)]
    line(draw, [nodes[0], nodes[1], nodes[2], nodes[0]], COLORS["lightning"], 4)
    for x, y in nodes:
        rect(draw, (x - 4, y - 4, x + 4, y + 4), COLORS["warning"])
    rect(draw, (27, 27, 37, 37), COLORS["warm_white"])
    return image


def icon_stasis() -> Image.Image:
    image, draw = icon_base()
    line(draw, [(6, 18), (6, 6), (24, 6)], COLORS["ice"], 5)
    line(draw, [(58, 18), (58, 6), (40, 6)], COLORS["lightning"], 5)
    line(draw, [(6, 46), (6, 58), (24, 58)], COLORS["lightning"], 5)
    line(draw, [(58, 46), (58, 58), (40, 58)], COLORS["ice"], 5)
    rect(draw, (26, 26, 38, 38), COLORS["pale_gray_blue"])
    return image


def generate_icons() -> dict[str, dict]:
    icons = {
        "ICO_FP_Element_Fire.png": icon_fire,
        "ICO_FP_Element_Ice.png": icon_ice,
        "ICO_FP_Element_Lightning.png": icon_lightning,
        "ICO_FP_Tattoo_P01.png": icon_p01,
        "ICO_FP_Tattoo_P02.png": icon_p02,
        "ICO_FP_Combat_Weakpoint.png": icon_weakpoint,
        "ICO_FP_State_Health.png": icon_health,
        "ICO_FP_State_Shield.png": icon_shield,
        "ICO_FP_State_Downed.png": icon_downed,
        "ICO_FP_State_Rescue.png": icon_rescue,
        "ICO_FP_State_Warning.png": icon_warning,
        "ICO_FP_Weapon_Ammo.png": icon_ammo,
        "ICO_FP_Team_Duo.png": icon_team,
        "ICO_FP_Build_Slots.png": icon_build,
        "ICO_FP_Request_Pigment.png": icon_request,
        "ICO_FP_Reaction_HeatShock.png": icon_heat_shock,
        "ICO_FP_Reaction_Overload.png": icon_overload,
        "ICO_FP_Reaction_Stasis.png": icon_stasis,
    }
    metadata = {}
    rendered = []
    for name, factory in icons.items():
        image = factory()
        save_downsample(image, UI_ROOT / "icons" / name)
        rendered.append((name, image.resize((64, 64), Image.Resampling.LANCZOS)))
        metadata[name] = {"spriteMode": "Single", "pivot": [0.5, 0.5], "sRGB": True, "alpha": True}

    atlas = Image.new("RGBA", (512, 512), COLORS["transparent"])
    for index, (_, icon) in enumerate(rendered):
        x = (index % 8) * 64
        y = (index // 8) * 64
        atlas.alpha_composite(icon, (x, y))
    atlas.save(UI_ROOT / "icons" / "UI_FP_IconAtlas_512.png", optimize=True)
    metadata["UI_FP_IconAtlas_512.png"] = {
        "spriteMode": "Multiple",
        "cell": [64, 64],
        "count": len(rendered),
        "order": [name for name, _ in rendered],
        "sRGB": True,
    }
    return metadata


def generate_hud() -> dict[str, dict]:
    metadata = {}

    reticle = canvas((128, 128))
    draw = ImageDraw.Draw(reticle)
    c = COLORS["warm_white"]
    b = COLORS["deep_slate"]
    for points_ in (
        [(18, 46), (18, 18), (46, 18)],
        [(82, 18), (110, 18), (110, 46)],
        [(110, 82), (110, 110), (82, 110)],
        [(46, 110), (18, 110), (18, 82)],
    ):
        line(draw, points_, b, 8)
        line(draw, points_, c, 4)
    rect(draw, (61, 61, 67, 67), COLORS["signal_blue"])
    save_downsample(reticle, UI_ROOT / "hud" / "UI_FP_Reticle_Default_128.png")
    metadata["UI_FP_Reticle_Default_128.png"] = {"spriteMode": "Single", "pivot": [0.5, 0.5], "sRGB": True}

    hit = canvas((128, 128))
    draw = ImageDraw.Draw(hit)
    for inset, color, width in ((22, COLORS["signal_blue"], 6), (40, COLORS["warm_white"], 3)):
        p = pts([(64, inset), (128 - inset, 64), (64, 128 - inset), (inset, 64), (64, inset)])
        draw.line(p, fill=color, width=width * SCALE, joint="curve")
    save_downsample(hit, UI_ROOT / "hud" / "UI_FP_HitConfirm_Weakpoint_128.png")
    metadata["UI_FP_HitConfirm_Weakpoint_128.png"] = {"spriteMode": "Single", "pivot": [0.5, 0.5], "sRGB": True}

    danger = canvas((512, 512))
    draw = ImageDraw.Draw(danger)
    color = COLORS["danger"]
    for offset in (20, 44):
        segment = 130
        line(draw, [(offset, offset + segment), (offset, offset), (offset + segment, offset)], color, 8)
        line(draw, [(512 - offset - segment, offset), (512 - offset, offset), (512 - offset, offset + segment)], color, 8)
        line(draw, [(512 - offset, 512 - offset - segment), (512 - offset, 512 - offset), (512 - offset - segment, 512 - offset)], color, 8)
        line(draw, [(offset + segment, 512 - offset), (offset, 512 - offset), (offset, 512 - offset - segment)], color, 8)
    save_downsample(danger, UI_ROOT / "hud" / "UI_FP_DangerFrame_512.png")
    metadata["UI_FP_DangerFrame_512.png"] = {"spriteMode": "Single", "border": [96, 96, 96, 96], "pivot": [0.5, 0.5], "sRGB": True}
    return metadata


def coherent_noise(size=256, seed=3107) -> Image.Image:
    rng = random.Random(seed)
    coarse = Image.new("L", (32, 32))
    coarse.putdata([rng.randrange(20, 236) for _ in range(32 * 32)])
    broad = coarse.resize((size, size), Image.Resampling.BICUBIC).filter(ImageFilter.GaussianBlur(2.0))
    fine = Image.new("L", (size, size))
    fine.putdata([rng.randrange(0, 256) for _ in range(size * size)])
    return Image.blend(broad, fine, 0.22)


def generate_vfx_textures() -> dict[str, dict]:
    metadata = {}
    noise = coherent_noise()
    noise.save(VFX_ROOT / "T_FP_VFX_Noise_256.png", optimize=True)
    metadata["T_FP_VFX_Noise_256.png"] = {"sRGB": False, "wrap": "Repeat", "filter": "Bilinear", "format": "R8 preferred"}

    ramp = Image.new("RGBA", (256, 16))
    pixels = []
    for _y in range(16):
        for x in range(256):
            t = x / 255.0
            a = int(max(0.0, min(1.0, 1.0 - abs(t * 2.0 - 1.0))) ** 0.65 * 255)
            value = int((t ** 0.55) * 255)
            pixels.append((value, value, value, a))
    ramp.putdata(pixels)
    ramp.save(VFX_ROOT / "T_FP_VFX_CoreRamp_256x16.png", optimize=True)
    metadata["T_FP_VFX_CoreRamp_256x16.png"] = {"sRGB": False, "wrap": "Clamp", "filter": "Bilinear", "alpha": True}

    dither = Image.new("L", (64, 64))
    bayer = [0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5]
    dither.putdata([round(bayer[(y % 4) * 4 + (x % 4)] / 15 * 255) for y in range(64) for x in range(64)])
    dither.save(VFX_ROOT / "T_FP_VFX_Dither4x4_64.png", optimize=True)
    metadata["T_FP_VFX_Dither4x4_64.png"] = {"sRGB": False, "wrap": "Repeat", "filter": "Point", "format": "R8 preferred"}

    atlas = Image.new("L", (512, 256), 0)
    draw = ImageDraw.Draw(atlas)
    cell = 128
    # 方框、菱形、楔形、棱片、分叉、分段条、十字、括号。
    draw.rectangle((16, 16, 112, 112), outline=255, width=12)
    draw.line([(192, 16), (240, 64), (192, 112), (144, 64), (192, 16)], fill=255, width=12, joint="curve")
    draw.polygon([(270, 108), (320, 14), (370, 108), (320, 78)], fill=255)
    draw.polygon([(405, 104), (438, 16), (493, 45), (470, 112)], fill=255)
    draw.line([(18, 190), (48, 158), (70, 210), (110, 148)], fill=255, width=10)
    for x in (146, 178, 210):
        draw.rectangle((x, 176, x + 18, 208), fill=255)
    draw.rectangle((296, 145, 344, 239), fill=255)
    draw.rectangle((273, 168, 367, 216), fill=255)
    draw.line([(402, 176), (402, 146), (438, 146)], fill=255, width=10)
    draw.line([(494, 208), (494, 238), (458, 238)], fill=255, width=10)
    atlas.save(VFX_ROOT / "T_FP_VFX_ShapeAtlas_512x256.png", optimize=True)
    metadata["T_FP_VFX_ShapeAtlas_512x256.png"] = {
        "sRGB": False,
        "wrap": "Clamp",
        "filter": "Bilinear",
        "cells": [4, 2],
        "order": ["box_frame", "diamond_frame", "wedge", "shard", "branch", "segments", "cross", "brackets"],
    }
    return metadata


def generate_background() -> dict[str, dict]:
    """背景不由确定性 UI 生成器重绘，只登记当前保留候选。"""
    return {
        "T_UI_FP_MainMenu_Background_Oasis_v02.png": {
            "textureType": "Default",
            "sRGB": True,
            "wrap": "Clamp",
            "filter": "Bilinear",
            "maxSize": 4096,
            "compression": "Normal or High Quality per target",
            "role": "首选主菜单背景候选；以绿洲新城现有地图与装饰资源为参考",
        },
    }


def generate_preview() -> None:
    preview = Image.new("RGB", (1920, 1080), COLORS["deep_slate"][:3])
    draw = ImageDraw.Draw(preview, "RGBA")
    draw.rectangle((64, 54, 1856, 1026), outline=COLORS["slate"][:3], width=3)
    # 左侧菜单按钮预览。
    for index, name in enumerate(("Normal", "Focused", "Pressed", "Disabled")):
        src = Image.open(UI_ROOT / "buttons" / f"UI_FP_Button_{name}_512x96.png").convert("RGBA")
        preview.paste(src, (110, 170 + index * 125), src)
    # 图标网格。
    icon_files = sorted((UI_ROOT / "icons").glob("ICO_*.png"))
    for index, path in enumerate(icon_files):
        icon = Image.open(path).convert("RGBA").resize((80, 80), Image.Resampling.NEAREST)
        x = 820 + (index % 6) * 145
        y = 150 + (index // 6) * 145
        draw.rectangle((x - 12, y - 12, x + 92, y + 92), fill=(43, 58, 61, 255), outline=COLORS["slate"][:3], width=2)
        preview.paste(icon, (x, y), icon)
    reticle = Image.open(UI_ROOT / "hud" / "UI_FP_Reticle_Default_128.png").convert("RGBA").resize((220, 220), Image.Resampling.NEAREST)
    preview.paste(reticle, (220, 700), reticle)
    danger = Image.open(UI_ROOT / "hud" / "UI_FP_DangerFrame_512.png").convert("RGBA").resize((320, 320), Image.Resampling.LANCZOS)
    preview.paste(danger, (520, 680), danger)
    preview.save(PREVIEW_ROOT / "UI_FP_OfflineAssetContactSheet_1920x1080.png", optimize=True)


def main() -> None:
    ensure_dirs()
    metadata = {
        "generator": "art/tools/generate_offline_art.py",
        "design": "方盒、切角、折线与低面数形状优先",
        "unityTarget": "2022.3.62f3",
        "assets": {},
    }
    for batch in (generate_panels_and_buttons(), generate_icons(), generate_hud(), generate_vfx_textures(), generate_background()):
        metadata["assets"].update(batch)
    generate_preview()
    (PRODUCTION / "offline-art-import.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"generated={len(metadata['assets'])}")
    print(f"output={PRODUCTION}")


if __name__ == "__main__":
    main()
