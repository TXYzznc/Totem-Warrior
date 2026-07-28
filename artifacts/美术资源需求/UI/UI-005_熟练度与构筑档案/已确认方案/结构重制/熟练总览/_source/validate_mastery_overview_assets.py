"""Validate final UI-005 Mastery Overview sprites without modifying them."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path

from PIL import Image


OUT = Path(__file__).resolve().parents[1] / "最终素材"
EXPECTED = 3


def inspect(path: Path) -> dict:
    with Image.open(path) as image:
        alpha = image.getchannel("A").histogram()
        colors = dict(image.convert("RGB").getcolors(image.width * image.height))
        corners = [
            image.getpixel((0, 0))[3],
            image.getpixel((image.width - 1, 0))[3],
            image.getpixel((0, image.height - 1))[3],
            image.getpixel((image.width - 1, image.height - 1))[3],
        ]
        return {
            "file": path.name,
            "mode": image.mode,
            "size": image.size,
            "bytes": path.stat().st_size,
            "corners": corners,
            "transparent_pixels": alpha[0],
            "green_key_pixels": colors.get((0, 255, 0), 0),
            "sha256": hashlib.sha256(path.read_bytes()).hexdigest().upper(),
        }


def main() -> None:
    rows = [inspect(path) for path in sorted(OUT.glob("*.png"))]
    valid = len(rows) == EXPECTED and all(
        row["mode"] == "RGBA"
        and row["bytes"] > 1024
        and row["corners"] == [0, 0, 0, 0]
        and row["transparent_pixels"] > 0
        and row["green_key_pixels"] == 0
        for row in rows
    )
    print(json.dumps({"passed": valid, "assets": rows}, ensure_ascii=False, indent=2))
    if not valid:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
