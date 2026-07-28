"""生成 UI-005 图案详情页专属、无文字的透明 PNG 壳体。

仅使用 PIL 绘制纯色几何；运行时数据、图案图形、熟练度和样式内容均不在此文件中。
"""

from pathlib import Path
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parent.parent
MERGED = ROOT / "_merged"
FINAL = ROOT / "最终素材"
SCALE = 4

# 通用 UI 规范的暖白 + 青绿语义色；实际状态色由 Unity Image.color 驱动。
WARM_WHITE = (217, 226, 230, 255)
PALE = (244, 250, 251, 235)
CYAN = (53, 208, 205, 255)
CYAN_SOFT = (191, 255, 248, 220)
MUTED = (142, 167, 176, 220)


def points_scaled(points):
    return [(round(x * SCALE), round(y * SCALE)) for x, y in points]


def polygon_outline(draw, points, color, width):
    draw.line(points_scaled(points + [points[0]]), fill=color, width=width * SCALE, joint="curve")


def image(size):
    return Image.new("RGBA", (size * SCALE, size * SCALE), (0, 0, 0, 0))


def save(im, filename):
    im.resize((im.width // SCALE, im.height // SCALE), Image.Resampling.LANCZOS).save(
        MERGED / filename, "PNG", optimize=True
    )
    im.resize((im.width // SCALE, im.height // SCALE), Image.Resampling.LANCZOS).save(
        FINAL / filename, "PNG", optimize=True
    )


def mastery_badge():
    """空白六边形成长框；中心完全留空，供 TMP_Text 渲染等级。"""
    im = image(256)
    d = ImageDraw.Draw(im)
    outer = [(128, 14), (222, 68), (222, 188), (128, 242), (34, 188), (34, 68)]
    middle = [(128, 26), (211, 74), (211, 182), (128, 230), (45, 182), (45, 74)]
    inner = [(128, 40), (198, 80), (198, 176), (128, 216), (58, 176), (58, 80)]
    polygon_outline(d, outer, WARM_WHITE, 3)
    polygon_outline(d, middle, CYAN, 2)
    polygon_outline(d, inner, PALE, 1)
    # 六个无语义装饰节点强化“成长框”辨识；不包含图案或数值。
    for x, y in outer:
        r = 5
        d.ellipse((int((x-r)*SCALE), int((y-r)*SCALE), int((x+r)*SCALE), int((y+r)*SCALE)), fill=CYAN_SOFT)
        d.ellipse((int((x-2)*SCALE), int((y-2)*SCALE), int((x+2)*SCALE), int((y+2)*SCALE)), fill=CYAN)
    # 顶、底短横仅为框体饰件，保留中心留白。
    d.line(points_scaled([(108, 31), (148, 31)]), fill=CYAN_SOFT, width=2*SCALE)
    d.line(points_scaled([(108, 225), (148, 225)]), fill=CYAN_SOFT, width=2*SCALE)
    return im


def tier_crest():
    """三段式权限阶级锚点；无文字、无锁和无解锁状态。"""
    im = image(192)
    d = ImageDraw.Draw(im)
    # 三个彼此独立的向上棱形/山墙段，数量表达 tier 结构而非运行时等级。
    tiers = [
        ([(96, 18), (142, 46), (96, 74), (50, 46)], CYAN_SOFT, CYAN),
        ([(96, 62), (153, 96), (96, 130), (39, 96)], WARM_WHITE, CYAN),
        ([(96, 118), (166, 158), (96, 188), (26, 158)], PALE, MUTED),
    ]
    for points, fill, outline in tiers:
        d.polygon(points_scaled(points), fill=fill)
        polygon_outline(d, points, outline, 2)
    # 中央竖向脊线只表达权限路径；样式、名称和条件由运行时/TMP 提供。
    d.line(points_scaled([(96, 29), (96, 177)]), fill=CYAN, width=SCALE)
    for x, y in [(96, 46), (96, 96), (96, 158)]:
        d.ellipse((int((x-4)*SCALE), int((y-4)*SCALE), int((x+4)*SCALE), int((y+4)*SCALE)), fill=CYAN)
    return im


def connector_node():
    """权限链连接节点；外接线由 UGUI Divider 拉伸，此 Sprite 只包含节点。"""
    im = image(128)
    d = ImageDraw.Draw(im)
    outer = [(64, 14), (99, 34), (99, 94), (64, 114), (29, 94), (29, 34)]
    middle = [(64, 25), (89, 39), (89, 89), (64, 103), (39, 89), (39, 39)]
    d.polygon(points_scaled(middle), fill=(217, 226, 230, 185))
    polygon_outline(d, outer, WARM_WHITE, 2)
    polygon_outline(d, middle, CYAN, 2)
    d.ellipse((54*SCALE, 54*SCALE, 74*SCALE, 74*SCALE), fill=CYAN_SOFT)
    d.ellipse((59*SCALE, 59*SCALE, 69*SCALE, 69*SCALE), fill=CYAN)
    # 四向短刻度止于透明留白内，不代替外部可拉伸连接线。
    for a, b in [((64, 20), (64, 32)), ((64, 96), (64, 108)), ((35, 64), (47, 64)), ((81, 64), (93, 64))]:
        d.line(points_scaled([a, b]), fill=CYAN_SOFT, width=SCALE)
    return im


def main():
    MERGED.mkdir(exist_ok=True)
    FINAL.mkdir(exist_ok=True)
    save(mastery_badge(), "pattern_detail_mastery_badge_frame.png")
    save(tier_crest(), "pattern_detail_tier_crest.png")
    save(connector_node(), "pattern_detail_permission_connector_node.png")


if __name__ == "__main__":
    main()
