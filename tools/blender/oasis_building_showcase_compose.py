"""Compose Oasis building exterior/interior renders into one lossless PNG sheet."""

from __future__ import annotations

import argparse
import json
import math
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def load_font(size: int, bold: bool = False) -> ImageFont.FreeTypeFont:
    candidates = [
        Path("C:/Windows/Fonts/msyhbd.ttc" if bold else "C:/Windows/Fonts/msyh.ttc"),
        Path("C:/Windows/Fonts/simhei.ttf"),
    ]
    for path in candidates:
        if path.exists():
            return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("manifest")
    parser.add_argument("output")
    parser.add_argument("--columns", type=int, default=3)
    parser.add_argument("--preview-output", default="", help="Optional downscaled PNG for quick inspection")
    parser.add_argument("--preview-width", type=int, default=4096)
    args = parser.parse_args()

    manifest_path = Path(args.manifest)
    data = json.loads(manifest_path.read_text(encoding="utf-8"))
    assets = data["assets"]
    view_w, view_h = data["view_resolution"]
    scale = view_h / 1000.0
    header_h = round(112 * scale)
    label_h = round(62 * scale)
    tile_w = view_w * 2
    tile_h = header_h + view_h + label_h
    columns = max(1, args.columns)
    rows = math.ceil(len(assets) / columns)
    sheet = Image.new("RGB", (tile_w * columns, tile_h * rows), (12, 18, 28))
    draw = ImageDraw.Draw(sheet)
    title_font = load_font(round(42 * scale), bold=True)
    label_font = load_font(round(30 * scale), bold=False)
    line_color = (56, 78, 105)

    for index, asset in enumerate(assets):
        col = index % columns
        row = index // columns
        x = col * tile_w
        y = row * tile_h
        exterior = Image.open(asset["exterior"]).convert("RGB")
        interior = Image.open(asset["interior"]).convert("RGB")
        if exterior.size != (view_w, view_h) or interior.size != (view_w, view_h):
            raise ValueError(f"Unexpected render size for {asset['asset_id']}")
        sheet.paste(exterior, (x, y + header_h))
        sheet.paste(interior, (x + view_w, y + header_h))
        line_width = max(3, round(3 * scale))
        draw.rectangle((x, y, x + tile_w - 1, y + tile_h - 1), outline=line_color, width=line_width)
        draw.line((x + view_w, y + header_h, x + view_w, y + header_h + view_h), fill=line_color, width=line_width)
        title = f"{asset['asset_id']}  {asset['name']}"
        inset_x = round(34 * scale)
        draw.text((x + inset_x, y + round(27 * scale)), title, font=title_font, fill=(235, 241, 248))
        label_y = y + header_h + view_h + round(10 * scale)
        draw.text((x + inset_x, label_y), "外观轴测 / Exterior Axon", font=label_font, fill=(179, 205, 230))
        draw.text((x + view_w + inset_x, label_y), "内部剖切 / Interior Cutaway", font=label_font, fill=(244, 190, 112))

    output = Path(args.output)
    output.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output, format="PNG", compress_level=6)
    preview_info = None
    if args.preview_output:
        preview_width = max(640, min(args.preview_width, sheet.width))
        preview_height = round(sheet.height * preview_width / sheet.width)
        preview = sheet.resize((preview_width, preview_height), Image.Resampling.LANCZOS)
        preview_output = Path(args.preview_output)
        preview_output.parent.mkdir(parents=True, exist_ok=True)
        preview.save(preview_output, format="PNG", compress_level=6)
        preview_info = {"output": str(preview_output), "resolution": list(preview.size)}
    print(json.dumps({"output": str(output), "resolution": list(sheet.size), "assets": len(assets), "preview": preview_info}, ensure_ascii=False))


if __name__ == "__main__":
    main()
