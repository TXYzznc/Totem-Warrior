from pathlib import Path
from PIL import Image

ROOT = Path(__file__).parent


def export_grid(source_name, columns, rows, names):
    source = Image.open(ROOT / "去绿合并图" / source_name).convert("RGBA")
    assert len(names) == columns * rows
    width, height = source.size
    for index, name in enumerate(names):
        column, row = index % columns, index // columns
        left = round(column * width / columns)
        top = round(row * height / rows)
        right = round((column + 1) * width / columns)
        bottom = round((row + 1) * height / rows)
        cell = source.crop((left, top, right, bottom))
        alpha = cell.getchannel("A")
        bbox = alpha.getbbox()
        if bbox is None:
            raise RuntimeError(f"空网格：{name}")
        pad = 2
        x0 = max(0, bbox[0] - pad)
        y0 = max(0, bbox[1] - pad)
        x1 = min(cell.width, bbox[2] + pad)
        y1 = min(cell.height, bbox[3] + pad)
        sprite = cell.crop((x0, y0, x1, y1))
        sprite.save(ROOT / f"{name}.png", optimize=True)


export_grid(
    "战绩外观_绿幕源_01.png",
    4,
    4,
    [
        "战绩外观_个人档案框",
        "战绩外观_统计环_青",
        "战绩外观_统计环_绿",
        "战绩外观_统计环_珊瑚",
        "战绩外观_图标_纹身机",
        "战绩外观_图标_勾选",
        "战绩外观_图标_日历",
        "战绩外观_图标_莲花",
        "战绩外观_预设卡框",
        "战绩外观_徽章卡框",
        "战绩外观_六边徽章框",
        "战绩外观_徽章_莲花流",
        "战绩外观_徽章_对称",
        "战绩外观_徽章_点绘",
        "战绩外观_徽章_波浪",
        "战绩外观_分享卡框",
    ],
)

export_grid(
    "战绩外观_绿幕源_02.png",
    2,
    2,
    [
        "战绩外观_徽章_阴影",
        "战绩外观_徽章_有机",
        "战绩外观_徽章_几何",
        "战绩外观_徽章_自定义",
    ],
)
