"""
把 1536x1024 RGBA sprite sheet 下采样到 Unity 实际目标尺寸：
- Player1/2/3: 1536x1024 → 384x96  (从原图取 1536x384 上中间一条带 → 等比缩到 384x96，
  考虑到 codex 实际生成的角色多铺满 1024 高、保留 1024 全高度反而能截到完整角色)
  方案: 直接 thumbnail 到 384x256 (保持 16:1 宽高错 → 改 1536x1024 等比缩 → 此处直接放大到 384x96 不保持比例)
  实际选择: LANCZOS resize 直接到 384x96，让 4 帧角色变扁但仍可辨识。
  更佳: 强制 4:1 比例的目标 96 高度对应原 1024 → 等比应是 384x256；要 384x96 → 高度需 crop。
  最终方案：
    1. 把原图等比缩到 width=384，高度变成 256
    2. 居中裁切到 384x96（保留中部带状）
- Boss1: 1536x1024 → 512x128 (类似流程)

输入：Asset 目标路径下的 1536x1024 PNG（已经过 chroma_key）
输出：同路径覆盖为目标尺寸的 PNG
"""
import sys
from pathlib import Path
from PIL import Image

# 目标规格表
TARGET = {
    "Player1": (384, 96),
    "Player2": (384, 96),
    "Player3": (384, 96),
    "Boss1":   (512, 128),
}

ASSET_BASE = Path(__file__).resolve().parents[5] / "Assets" / "Resources" / "Sprite" / "Character"


def resize_one(path: Path) -> tuple[bool, str]:
    """resize 单张：返回 (success, msg)。"""
    char = path.parts[-3]  # .../Character/<char>/<action>/<dir>.png
    if char not in TARGET:
        return False, f"unknown char dir: {char}"
    tw, th = TARGET[char]
    try:
        img = Image.open(path).convert("RGBA")
    except Exception as e:
        return False, f"open failed: {e}"
    sw, sh = img.size
    if (sw, sh) == (tw, th):
        return True, "already at target size"

    # Step 1: 等比缩放到 width=tw（按宽适配）→ 临时高度 = sh * tw / sw
    scale = tw / sw
    new_w = tw
    new_h = max(1, int(round(sh * scale)))
    img = img.resize((new_w, new_h), Image.LANCZOS)

    # Step 2: 居中裁切高度到 th（若 new_h > th）；若 new_h < th，则下方补透明
    if new_h > th:
        # 居中裁
        top = (new_h - th) // 2
        img = img.crop((0, top, tw, top + th))
    elif new_h < th:
        # 居中粘贴到透明画布
        canvas = Image.new("RGBA", (tw, th), (0, 0, 0, 0))
        top = (th - new_h) // 2
        canvas.paste(img, (0, top), img)
        img = canvas

    img.save(path, "PNG", optimize=True)
    return True, f"{sw}x{sh} -> {tw}x{th}"


def main():
    targets = []
    for char in TARGET:
        cdir = ASSET_BASE / char
        if not cdir.exists():
            continue
        targets.extend(cdir.rglob("*.png"))

    if not targets:
        print("no PNGs found")
        return

    ok = 0
    failed = 0
    for p in targets:
        success, msg = resize_one(p)
        if success:
            ok += 1
            print(f"  OK  {p.relative_to(ASSET_BASE)}: {msg}")
        else:
            failed += 1
            print(f"  FAIL {p.relative_to(ASSET_BASE)}: {msg}")
    print(f"\nresize summary: OK={ok} FAILED={failed} total={len(targets)}")


if __name__ == "__main__":
    main()
