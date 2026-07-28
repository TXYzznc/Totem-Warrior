from pathlib import Path
from PIL import Image, ImageDraw
import math

OUTPUT = Path(__file__).parent
SCALE = 3


def canvas(width, height):
    return Image.new("RGBA", (width * SCALE, height * SCALE), (0, 0, 0, 0))


def save(image, filename):
    image.resize(
        (image.width // SCALE, image.height // SCALE), Image.Resampling.LANCZOS
    ).save(OUTPUT / filename, optimize=False)


def draw_connector():
    image = canvas(512, 256)
    draw = ImageDraw.Draw(image)
    points = [(256, 210), (110, 60), (220, 48), (350, 50), (425, 92)]
    points = [(x * SCALE, y * SCALE) for x, y in points]
    line = (60, 112, 148, 235)
    glow = (30, 190, 215, 90)
    for point in points[1:]:
        draw.line((points[0], point), fill=glow, width=7 * SCALE)
        draw.line((points[0], point), fill=line, width=2 * SCALE)
    for x, y in points:
        draw.ellipse(
            (x - 7 * SCALE, y - 7 * SCALE, x + 7 * SCALE, y + 7 * SCALE),
            fill=(245, 249, 248, 255),
            outline=line,
            width=2 * SCALE,
        )
    save(image, "构筑快照_构筑连接线.png")


def draw_success_marker():
    image = canvas(256, 256)
    draw = ImageDraw.Draw(image)
    draw.ellipse(
        (37 * SCALE, 37 * SCALE, 219 * SCALE, 219 * SCALE),
        fill=(248, 252, 251, 235),
        outline=(44, 163, 182, 255),
        width=8 * SCALE,
    )
    draw.line(
        [(76 * SCALE, 131 * SCALE), (112 * SCALE, 167 * SCALE), (182 * SCALE, 88 * SCALE)],
        fill=(38, 153, 172, 255),
        width=10 * SCALE,
        joint="curve",
    )
    save(image, "构筑快照_时间线成功标记.png")


def draw_favorite_star():
    image = canvas(256, 256)
    draw = ImageDraw.Draw(image)
    center_x = center_y = 128 * SCALE
    outer_radius = 88 * SCALE
    inner_radius = 37 * SCALE
    points = []
    for index in range(10):
        radius = outer_radius if index % 2 == 0 else inner_radius
        angle = -math.pi / 2 + index * math.pi / 5
        points.append(
            (
                center_x + int(radius * math.cos(angle)),
                center_y + int(radius * math.sin(angle)),
            )
        )
    draw.polygon(
        points,
        fill=(250, 252, 250, 235),
        outline=(33, 72, 107, 255),
        width=7 * SCALE,
    )
    save(image, "构筑快照_收藏星标.png")


draw_connector()
draw_success_marker()
draw_favorite_star()


def validate():
    source = Image.open(OUTPUT / "绿幕源.png").convert("RGB")
    alpha_sheet = Image.open(OUTPUT / "去绿合并图" / "绿幕源.png").convert("RGBA")
    key = (0, 255, 0)
    source_pixels = list(source.getdata())
    alpha_values = [pixel[3] for pixel in alpha_sheet.getdata()]
    print(
        "GREEN_SOURCE",
        source.mode,
        source.size,
        "zero_alpha_nonkey=",
        sum(alpha == 0 and color != key for color, alpha in zip(source_pixels, alpha_values)),
        "key_foreground=",
        sum(color == key and alpha > 0 for color, alpha in zip(source_pixels, alpha_values)),
        "zero_alpha=",
        sum(alpha == 0 for alpha in alpha_values),
    )
    filenames = [
        "构筑快照_档案卡_普通.png",
        "构筑快照_档案卡_选中.png",
        "构筑快照_主详情面板.png",
        "构筑快照_战绩侧面板.png",
        "构筑快照_指标瓷砖.png",
        "构筑快照_图案节点.png",
        "构筑快照_颜料色片.png",
        "构筑快照_头像圆环.png",
        "构筑快照_构筑连接线.png",
        "构筑快照_时间线成功标记.png",
        "构筑快照_收藏星标.png",
    ]
    for filename in filenames:
        path = OUTPUT / filename
        image = Image.open(path).convert("RGBA")
        width, height = image.size
        pixels = image.load()
        corners = [
            pixels[0, 0][3],
            pixels[width - 1, 0][3],
            pixels[0, height - 1][3],
            pixels[width - 1, height - 1][3],
        ]
        alphas = [pixel[3] for pixel in image.getdata()]
        transparent = sum(alpha < 40 for alpha in alphas)
        valid = (
            image.mode == "RGBA"
            and max(corners) == 0
            and transparent > 0
            and path.stat().st_size > 1024
        )
        print(
            "OK" if valid else "FAIL",
            filename,
            "mode=", image.mode,
            "size=", image.size,
            "bytes=", path.stat().st_size,
            "corners=", corners,
            "transparent=", f"{transparent}/{len(alphas)}",
        )


validate()
