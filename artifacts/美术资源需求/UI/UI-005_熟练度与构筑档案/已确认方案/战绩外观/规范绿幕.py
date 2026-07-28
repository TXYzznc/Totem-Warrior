from pathlib import Path
from PIL import Image

root = Path(__file__).parent
for path in sorted(root.glob("战绩外观_绿幕源_*.png")):
    image = Image.open(path).convert("RGBA")
    pixels = image.load()
    normalized = 0
    for y in range(image.height):
        for x in range(image.width):
            red, green, blue, alpha = pixels[x, y]
            if green >= 130 and green - red >= 70 and green - blue >= 70:
                pixels[x, y] = (0, 255, 0, alpha)
                normalized += 1
    image.save(path, optimize=True)
    print(f"{path.name}: normalized={normalized}")
