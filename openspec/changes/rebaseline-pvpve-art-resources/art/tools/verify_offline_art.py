#!/usr/bin/env python3
"""不启动 Unity 的首轮美术资源静态验收。"""

from __future__ import annotations

import json
from pathlib import Path

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
PRODUCTION = ROOT / "production"


def verify_pngs() -> list[str]:
    errors: list[str] = []
    pngs = sorted(PRODUCTION.rglob("*.png"))
    if len(pngs) < 30:
        errors.append(f"PNG 数量异常：{len(pngs)}，预期至少 30")

    for path in pngs:
        try:
            with Image.open(path) as image:
                image.verify()
            with Image.open(path) as image:
                if image.width <= 0 or image.height <= 0:
                    errors.append(f"尺寸非法：{path}")
                if "icons" in path.parts and image.size != (512, 512) and image.size != (64, 64):
                    errors.append(f"图标尺寸不符合 64 单图或 512 图集：{path.name} {image.size}")
                if "previews" not in path.parts and path.name.startswith(("ICO_", "UI_FP_")) and image.mode != "RGBA":
                    errors.append(f"UI 资源缺少 RGBA：{path.name} mode={image.mode}")
        except Exception as exc:  # Pillow 给出具体损坏原因
            errors.append(f"PNG 无法读取：{path}: {exc}")
    return errors


def verify_manifest() -> list[str]:
    errors: list[str] = []
    manifest_path = PRODUCTION / "offline-art-import.json"
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except Exception as exc:
        return [f"导入清单无效：{exc}"]

    filenames = {path.name for path in PRODUCTION.rglob("*.png") if "previews" not in path.parts}
    declared = set(manifest.get("assets", {}))
    missing = sorted(filenames - declared)
    if missing:
        errors.append("导入清单缺项：" + ", ".join(missing))
    return errors


def verify_shaders() -> list[str]:
    errors: list[str] = []
    shader_files = sorted(PRODUCTION.rglob("*.shader")) + sorted(PRODUCTION.rglob("*.hlsl"))
    if len(shader_files) != 4:
        errors.append(f"Shader/HLSL 数量异常：{len(shader_files)}，预期 4")

    for path in shader_files:
        source = path.read_text(encoding="utf-8")
        if source.count("{") != source.count("}"):
            errors.append(f"花括号不平衡：{path.name}")
        if path.suffix == ".shader" and '"RenderPipeline" = "UniversalPipeline"' not in source:
            errors.append(f"缺少 URP 标签：{path.name}")
        if "SurfaceDescription" in source or "Shader Graph" in source:
            errors.append(f"发现非预期复杂生成代码：{path.name}")
    return errors


def main() -> int:
    errors = verify_pngs() + verify_manifest() + verify_shaders()
    if errors:
        print("离线验收失败：")
        for error in errors:
            print(f"- {error}")
        return 1

    png_count = len(list(PRODUCTION.rglob("*.png")))
    print(f"离线验收通过：PNG {png_count} 个，Shader/HLSL 4 个，导入清单完整。")
    print("注意：这不替代 Unity Shader 编译、材质检查和测试场景调效。")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
