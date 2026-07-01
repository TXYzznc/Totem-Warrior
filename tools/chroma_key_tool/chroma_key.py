#!/usr/bin/env python
"""Chroma-key 抠图工具。

把纯色背景（默认绿色 #00ff00）的 PNG 转成真透明 alpha PNG。
接口与 tools/ImageCut_Tool/image_cut.py 对齐：支持 CLI 与 --json 输出。

工作原理：
1. 像素颜色与 key 色的欧氏距离 < threshold → alpha = 0（完全透明）
2. 距离在 [threshold, threshold+soft_edge] 范围 → alpha 线性渐变（软边缘抗锯齿）
3. 远超 threshold → alpha 保持 255

细节增强（相对基础距离阈值算法的三点改进）：
- **边缘反混合 (Vlahos unmix)**：半透明像素 (0<α<1) 本质是 `α·前景 + (1-α)·key`
  的观测混合，直接输出会留下 key 色调。这里反解出干净前景：
  `fg = (obs - (1-α)·key) / α`，头发丝、边缘细节色更纯。
- **全局去溢色 (spill suppression)**：把主导通道钳到其他两通道最大值
  （经典公式 `G := min(G, max(R,B))`），对所有前景像素生效，
  不再只处理软边缘 —— 这能修掉"主体被绿光染色"的残留绿边。
- **R/G/B 键色对称**：绿/红/蓝三种纯键色都能正确识别主导通道。
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image


DEFAULT_KEY_COLOR = (0, 255, 0)   # 纯绿
DEFAULT_THRESHOLD = 80            # 颜色距离阈值（0-441，欧氏距离 sqrt(3*255^2)≈441）
DEFAULT_SOFT_EDGE = 30            # 软边缘渐变宽度
DEFAULT_DESPILL = 0.85            # 去溢色强度（0-1，把残留绿色减弱）
DEFAULT_EDGE_UNMIX = True         # 边缘 unmix：半透明像素反解出纯前景色
DEFAULT_CHROMA_LO = 60.0          # spill (G-max(R,B)) 低于此值不降 alpha
DEFAULT_CHROMA_HI = 200.0         # spill 高于此值 alpha→0（判定为混入过多背景）
DEFAULT_ALPHA_FLOOR = 8           # alpha < 此值 (0-255) 直接清零，去边缘幽灵像素
DEFAULT_SMOOTHSTEP = True         # 软边缘用 smoothstep 代替线性，过渡更柔和
DEFAULT_GRAY_WEIGHT_K = 80.0      # 灰度权重系数：|R-B|<此值→按灰处理，>此值→按彩色艺术保护


def parse_color(s: str) -> tuple[int, int, int]:
    """支持 '#00ff00' / '0,255,0' / 'green' 三种格式。"""
    s = s.strip().lower()
    if s.startswith("#"):
        s = s[1:]
        return (int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16))
    if "," in s:
        r, g, b = (int(x) for x in s.split(","))
        return (r, g, b)
    named = {"green": (0, 255, 0), "magenta": (255, 0, 255), "cyan": (0, 255, 255)}
    if s in named:
        return named[s]
    raise ValueError(f"unsupported color format: {s}")


def _dominant_channel(key_color: tuple[int, int, int]) -> int | None:
    """识别纯 R/G/B 键色的主导通道；返回 0/1/2 或 None（非纯键色）。"""
    r, g, b = key_color
    hi, lo = 200, 100
    if g >= hi and r < lo and b < lo:
        return 1
    if r >= hi and g < lo and b < lo:
        return 0
    if b >= hi and r < lo and g < lo:
        return 2
    return None


def remove_chroma_key(
    img: Image.Image,
    key_color: tuple[int, int, int] = DEFAULT_KEY_COLOR,
    threshold: float = DEFAULT_THRESHOLD,
    soft_edge: float = DEFAULT_SOFT_EDGE,
    despill: float = DEFAULT_DESPILL,
    edge_unmix: bool = DEFAULT_EDGE_UNMIX,
    chroma_lo: float = DEFAULT_CHROMA_LO,
    chroma_hi: float = DEFAULT_CHROMA_HI,
    alpha_floor: int = DEFAULT_ALPHA_FLOOR,
    smoothstep: bool = DEFAULT_SMOOTHSTEP,
    gray_weight_k: float = DEFAULT_GRAY_WEIGHT_K,
) -> Image.Image:
    """核心算法：返回 RGBA 透明图。"""
    img = img.convert("RGBA")
    arr = np.array(img).astype(np.float32)
    rgb = arr[:, :, :3].copy()
    key = np.array(key_color, dtype=np.float32)
    dominant = _dominant_channel(key_color)

    # ---- 1. 欧氏距离 → alpha (0..1) ----
    dist = np.sqrt(np.sum((rgb - key) ** 2, axis=2))
    if soft_edge > 0:
        t = np.clip((dist - threshold) / soft_edge, 0.0, 1.0)
        # smoothstep 3t²-2t³ 让边缘从"斜坡"变成"缓入缓出的 S 型"
        alpha = t * t * (3.0 - 2.0 * t) if smoothstep else t
    else:
        alpha = (dist > threshold).astype(np.float32)

    # ---- 1b. 色度 alpha：spill 高的像素强制降 alpha (按灰度权重自适应) ----
    # 问题背景：浅色前景 (R,B 都不小) 与绿背景混合后欧氏距离仍然大，
    # 会被判为完全前景 → 保留大量绿光染色。用 spill = primary - max(others)
    # 直接量化"绿分量过剩"，把这类像素的 alpha 拉下来。
    # 灰度权重 gray_weight = 1 - |R-B|/K (钳位 0..1)：R≈B 的灰白像素 → 全力削 alpha；
    # R,B 差异大的彩色像素（如黄绿魔法光效）→ 保留，因为多半是美术设计不是溢色。
    if dominant is not None and chroma_hi > chroma_lo:
        others_pre = [c for c in (0, 1, 2) if c != dominant]
        primary_pre = rgb[..., dominant]
        c1 = rgb[..., others_pre[0]]
        c2 = rgb[..., others_pre[1]]
        spill_pre = np.maximum(0.0, primary_pre - np.maximum(c1, c2))
        # 灰权重：|R-B|=0 → 1，|R-B|>=K → 0
        gray_w = np.clip(1.0 - np.abs(c1 - c2) / max(1e-3, gray_weight_k), 0.0, 1.0)
        # 用 sqrt 软化——彩色像素也保留一部分削 alpha 能力（防止彩色前景吃绿光时不被清理）
        # 极端 gray_w=0（纯彩色艺术光效）→ 0 保护；gray_w=1 → 1 全削；中间值走 sqrt 曲线
        chroma_gate = np.sqrt(gray_w)
        alpha_chroma_cut = chroma_gate * np.clip(
            (spill_pre - chroma_lo) / (chroma_hi - chroma_lo), 0.0, 1.0
        )
        alpha = np.minimum(alpha, 1.0 - alpha_chroma_cut)
    else:
        gray_w = None

    # ---- 2. 边缘反混合 (Vlahos unmix) ----
    # 半透明像素 obs = α·fg + (1-α)·key，反解 fg = (obs - (1-α)·key) / α
    # 仅在 α ∈ [0.15, 0.95] 生效：太小会放大噪声，太大没必要
    if edge_unmix:
        lo, hi = 0.15, 0.95
        unmix_mask = (alpha > lo) & (alpha < hi)
        if np.any(unmix_mask):
            a = alpha[..., None]  # (H,W,1)
            a_div = np.maximum(a, lo)
            unmixed = (rgb - (1.0 - a) * key) / a_div
            rgb = np.where(unmix_mask[..., None], np.clip(unmixed, 0.0, 255.0), rgb)

    # ---- 3. 全局去溢色 (spill suppression) ——两遍收敛，按灰度权重自适应强度 ----
    # 单遍 despill=0.85 剩 15% 残留；两遍剩 2.3%，几乎收敛。
    # 灰白色素上视为"绿光染色"→ despill 拉满 1.0；彩色艺术光效 → 保持基础 despill。
    if despill > 0 and dominant is not None:
        others = [c for c in (0, 1, 2) if c != dominant]
        keep = alpha > 0
        for _pass in range(2):
            primary = rgb[..., dominant]
            c1v = rgb[..., others[0]]
            c2v = rgb[..., others[1]]
            other_max = np.maximum(c1v, c2v)
            spill = np.maximum(0.0, primary - other_max)
            gray_w_post = np.clip(1.0 - np.abs(c1v - c2v) / max(1e-3, gray_weight_k), 0.0, 1.0)
            eff_despill = despill + (1.0 - despill) * gray_w_post
            rgb[..., dominant] = np.where(
                keep, primary - spill * eff_despill, primary
            )

    # ---- 4. Alpha floor: 极小 alpha 清零，去边缘"幽灵像素" ----
    alpha_u8 = np.clip(alpha * 255.0, 0.0, 255.0)
    if alpha_floor > 0:
        alpha_u8 = np.where(alpha_u8 < alpha_floor, 0.0, alpha_u8)

    out = np.zeros_like(arr)
    out[:, :, :3] = np.clip(rgb, 0.0, 255.0)
    out[:, :, 3] = alpha_u8
    return Image.fromarray(out.astype(np.uint8), mode="RGBA")


def process_one(
    src: Path,
    dst: Path | None,
    key_color: tuple[int, int, int],
    threshold: float,
    soft_edge: float,
    despill: float,
    edge_unmix: bool = DEFAULT_EDGE_UNMIX,
) -> dict:
    """处理单张图，返回 manifest dict。"""
    img = Image.open(src)
    out = remove_chroma_key(img, key_color, threshold, soft_edge, despill, edge_unmix)
    if dst is None:
        dst = src.with_name(src.stem + "_alpha" + src.suffix)
    dst.parent.mkdir(parents=True, exist_ok=True)
    out.save(dst, format="PNG", optimize=True)

    arr = np.array(out)
    a = arr[:, :, 3]
    total = a.size
    alpha_zero = int(np.sum(a == 0))
    alpha_full = int(np.sum(a == 255))
    return {
        "source": str(src),
        "output": str(dst),
        "size": {"width": out.width, "height": out.height},
        "key_color": list(key_color),
        "threshold": threshold,
        "soft_edge": soft_edge,
        "despill": despill,
        "edge_unmix": edge_unmix,
        "alpha_zero_pixels": alpha_zero,
        "alpha_full_pixels": alpha_full,
        "alpha_zero_ratio": round(alpha_zero / total, 4),
        "status": "ok",
    }


def main():
    p = argparse.ArgumentParser(
        description="Remove a flat chroma-key background from PNGs and write true alpha PNGs."
    )
    p.add_argument("inputs", nargs="+", help="PNG file(s) or folder(s).")
    p.add_argument("-o", "--output", default=None,
                   help="Output folder (default: same dir, suffix _alpha).")
    p.add_argument("--key", default="#00ff00",
                   help="Chroma key color (#hex / r,g,b / green/magenta/cyan). Default: #00ff00")
    p.add_argument("--threshold", type=float, default=DEFAULT_THRESHOLD,
                   help=f"Color distance threshold (0-441). Default: {DEFAULT_THRESHOLD}")
    p.add_argument("--soft-edge", type=float, default=DEFAULT_SOFT_EDGE,
                   help=f"Soft edge width for anti-aliasing. Default: {DEFAULT_SOFT_EDGE}")
    p.add_argument("--despill", type=float, default=DEFAULT_DESPILL,
                   help=f"Despill strength 0-1 (reduce residual key color, applied globally). Default: {DEFAULT_DESPILL}")
    p.add_argument("--no-edge-unmix", action="store_true",
                   help="Disable Vlahos edge unmix on semi-transparent pixels.")
    p.add_argument("--json", action="store_true", help="Write manifest JSON to output dir.")
    p.add_argument("--overwrite", action="store_true",
                   help="Overwrite existing output files (default: skip).")
    args = p.parse_args()

    key_color = parse_color(args.key)

    # 收集输入文件
    input_files: list[Path] = []
    for inp in args.inputs:
        path = Path(inp)
        if path.is_file() and path.suffix.lower() == ".png":
            input_files.append(path)
        elif path.is_dir():
            input_files.extend(sorted(path.glob("*.png")))

    out_dir = Path(args.output) if args.output else None
    manifest_items: list[dict] = []
    for src in input_files:
        if out_dir:
            dst = out_dir / src.name
        else:
            dst = src.with_name(src.stem + "_alpha.png")
        if dst.exists() and not args.overwrite:
            print(f"[skip] {src.name} -> {dst} (exists)", file=sys.stderr)
            continue
        try:
            info = process_one(
                src, dst, key_color,
                args.threshold, args.soft_edge, args.despill,
                edge_unmix=not args.no_edge_unmix,
            )
            print(
                f"[ok] {src.name} -> {dst.name}  "
                f"alpha=0 {info['alpha_zero_ratio']*100:.1f}%",
                file=sys.stderr,
            )
            manifest_items.append(info)
        except Exception as e:
            print(f"[fail] {src.name}: {e}", file=sys.stderr)
            manifest_items.append({"source": str(src), "status": "failed", "error": str(e)})

    if args.json:
        manifest_path = (out_dir or input_files[0].parent) / "chroma_key_manifest.json"
        manifest_path.write_text(
            json.dumps({"items": manifest_items}, ensure_ascii=False, indent=2),
            encoding="utf-8",
        )
        print(f"[manifest] {manifest_path}", file=sys.stderr)


if __name__ == "__main__":
    main()
