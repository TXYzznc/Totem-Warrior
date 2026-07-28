"""Validate final ProfileShowcaseForm page-specific PNG assets."""

from __future__ import annotations

from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
FINAL = ROOT / "最终素材"
NAMES = ["profile_showcase_icon_appearance.png", "profile_showcase_empty_mark.png"]


def inspect(name: str) -> dict:
    path = FINAL / name
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        alpha = rgba.getchannel("A")
        histogram = alpha.histogram()
        transparent = histogram[0]
        opaque = sum(histogram[1:])
        corners = [alpha.getpixel((0, 0)), alpha.getpixel((rgba.width - 1, 0)), alpha.getpixel((0, rgba.height - 1)), alpha.getpixel((rgba.width - 1, rgba.height - 1))]
        checks = {
            "RGBA": image.mode == "RGBA",
            "four transparent corners": corners == [0, 0, 0, 0],
            "contains transparent pixels": transparent > 0,
            "contains visible pixels": opaque > 0,
            "larger than 1 KB": path.stat().st_size > 1024,
        }
        return {"name": name, "mode": image.mode, "size": rgba.size, "bytes": path.stat().st_size, "corners": corners, "transparent": transparent, "visible": opaque, "checks": checks}


def main() -> None:
    results = [inspect(name) for name in NAMES]
    for result in results:
        status = "PASS" if all(result["checks"].values()) else "FAIL"
        print(f"{status}\t{result['name']}\t{result['mode']}\t{result['size'][0]}x{result['size'][1]}\t{result['bytes']}\t{result['corners']}\t{result['transparent']}\t{result['visible']}")
        for check, passed in result["checks"].items():
            if not passed:
                raise SystemExit(f"{result['name']}: failed {check}")


if __name__ == "__main__":
    main()
