from pathlib import Path
from PIL import Image

root = Path(__file__).parent

for path in sorted(root.glob("战绩外观_绿幕源_*.png")):
    image = Image.open(path).convert("RGBA")
    width, height = image.size
    pixels = image.load()
    border = [pixels[x, 0][:3] for x in range(width)] + [pixels[x, height - 1][:3] for x in range(width)]
    border += [pixels[0, y][:3] for y in range(1, height - 1)] + [pixels[width - 1, y][:3] for y in range(1, height - 1)]
    pure_green = sum(pixel[:3] == (0, 255, 0) for pixel in image.getdata())
    samples = [pixels[0, 0], pixels[width - 1, 0], pixels[0, height - 1], pixels[width - 1, height - 1], pixels[width // 2, 0]]
    print(f"SOURCE|{path.name}|{width}x{height}|rgba={image.mode == 'RGBA'}|border_green={all(pixel == (0, 255, 0) for pixel in border)}|pure_green={pure_green}/{width * height}|samples={samples}")

for path in sorted(root.glob("战绩外观_*.png")):
    if "绿幕源" in path.name:
        continue
    image = Image.open(path).convert("RGBA")
    width, height = image.size
    pixels = image.load()
    corners = [pixels[0, 0][3], pixels[width - 1, 0][3], pixels[0, height - 1][3], pixels[width - 1, height - 1][3]]
    alpha = list(image.getchannel("A").getdata())
    transparent = sum(value < 40 for value in alpha)
    foreground_green = sum(value[3] > 16 and value[:3] == (0, 255, 0) for value in image.getdata())
    valid = image.mode == "RGBA" and max(corners) == 0 and transparent > 0 and path.stat().st_size > 1024 and foreground_green == 0
    print(f"{'OK' if valid else 'FAIL'}|{path.name}|{width}x{height}|bytes={path.stat().st_size}|corners={corners}|transparent={transparent}/{width * height}|foreground_green={foreground_green}")
