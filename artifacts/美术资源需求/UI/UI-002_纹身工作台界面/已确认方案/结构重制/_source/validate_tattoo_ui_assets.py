"""Acceptance validation for dedicated TattooStudioForm UI shell PNGs."""
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
files = sorted((ROOT / "最终素材").glob("*.png"))
assert len(files) == 9, f"expected 9 final assets, got {len(files)}"

for path in files:
    with Image.open(path) as source:
        image = source.convert("RGBA")
        alpha = image.getchannel("A")
        corners = [
            image.getpixel((0, 0))[3], image.getpixel((image.width - 1, 0))[3],
            image.getpixel((0, image.height - 1))[3], image.getpixel((image.width - 1, image.height - 1))[3],
        ]
        transparent = sum(value < 255 for value in alpha.get_flattened_data())
        assert source.mode == "RGBA", f"{path.name}: source is not RGBA"
        assert path.stat().st_size > 1024, f"{path.name}: not larger than 1 KB"
        assert corners == [0, 0, 0, 0], f"{path.name}: corners are not transparent"
        assert transparent > 0, f"{path.name}: lacks transparent pixels"
        print(f"{path.name}|RGBA|{image.width}x{image.height}|{path.stat().st_size}|{transparent}|PASS")
