"""Replace edge-connected neutral studio backdrops with a fixed flat color."""
from __future__ import annotations

from collections import deque
from pathlib import Path
import sys

from PIL import Image

TARGET = (181, 176, 170)  # #B5B0AA


def background_median(pixels: Image.Image) -> tuple[int, int, int]:
    width, height = pixels.size
    samples = []
    for x in range(0, width, max(1, width // 32)):
        samples.extend((pixels.getpixel((x, 0)), pixels.getpixel((x, height - 1))))
    for y in range(0, height, max(1, height // 32)):
        samples.extend((pixels.getpixel((0, y)), pixels.getpixel((width - 1, y))))
    return tuple(sorted(v[channel] for v in samples)[len(samples) // 2] for channel in range(3))


def flatten(path: Path) -> Path:
    image = Image.open(path).convert("RGB")
    width, height = image.size
    background = background_median(image)
    tolerance_sq = 30 * 30
    seen = bytearray(width * height)
    queue: deque[tuple[int, int]] = deque()

    def enqueue(x: int, y: int) -> None:
        index = y * width + x
        if seen[index]:
            return
        color = image.getpixel((x, y))
        distance_sq = sum((color[i] - background[i]) ** 2 for i in range(3))
        if distance_sq <= tolerance_sq:
            seen[index] = 1
            queue.append((x, y))

    for x in range(width):
        enqueue(x, 0)
        enqueue(x, height - 1)
    for y in range(height):
        enqueue(0, y)
        enqueue(width - 1, y)

    while queue:
        x, y = queue.popleft()
        image.putpixel((x, y), TARGET)
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < width and 0 <= ny < height:
                enqueue(nx, ny)

    output = path.with_name(f"{path.stem}_flat.png")
    image.save(output)
    return output


if __name__ == "__main__":
    for raw in sys.argv[1:]:
        print(flatten(Path(raw)))
