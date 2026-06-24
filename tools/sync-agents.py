#!/usr/bin/env python3
"""sync-agents.py

将 .claude/agents/*.md（source of truth）同步到 .codex/agents/*.toml。

用法：
    python tools/sync-agents.py            # 同步
    python tools/sync-agents.py --check    # 仅校验差异，不写入（CI 用）
    python tools/sync-agents.py --verbose  # 输出每个文件的同步细节

约定：
- .claude/agents/<name>.md 的 frontmatter 与 body 是 source of truth
- .codex/agents/<name>.toml 从 .md frontmatter 生成 + 把 body 写到 `instructions` 字段
- 若 .claude/agents/ 删了某个 agent，.codex/agents/ 对应 toml 也会被删
- .agents/skills/ 镜像简单复制 .claude/skills/ 的所有 SKILL.md 文件
"""

from __future__ import annotations

import argparse
import shutil
import sys
from pathlib import Path

try:
    import yaml  # PyYAML
except ImportError:
    print("ERROR: 需要 PyYAML。请运行: pip install pyyaml")
    sys.exit(2)


ROOT = Path(__file__).resolve().parent.parent
CLAUDE_AGENTS = ROOT / ".claude" / "agents"
CODEX_AGENTS = ROOT / ".codex" / "agents"
CLAUDE_SKILLS = ROOT / ".claude" / "skills"
AGENTS_SKILLS = ROOT / ".agents" / "skills"


def parse_md(path: Path) -> tuple[dict, str]:
    """从 .md 中提取 frontmatter dict 与 body 字符串。"""
    text = path.read_text(encoding="utf-8")
    if not text.startswith("---"):
        raise ValueError(f"{path} 缺少 frontmatter")
    parts = text.split("---", 2)
    if len(parts) < 3:
        raise ValueError(f"{path} frontmatter 格式不正确")
    fm = yaml.safe_load(parts[1]) or {}
    body = parts[2].lstrip("\n")
    return fm, body


def to_toml(fm: dict, body: str) -> str:
    """把 frontmatter 字段 + body 渲染为 TOML 字符串。"""
    lines = []
    lines.append(f'name = "{fm.get("name", "")}"')
    if "model" in fm:
        lines.append(f'model = "{fm["model"]}"')
    if "tier" in fm:
        lines.append(f'tier = "{fm["tier"]}"')
    if "escalate_to" in fm:
        lines.append(f'escalate_to = "{fm["escalate_to"]}"')
    if "tools" in fm:
        tools = fm["tools"]
        if isinstance(tools, str):
            tools = [t.strip() for t in tools.split(",")]
        lines.append("tools = [" + ", ".join(f'"{t}"' for t in tools) + "]")
    if "skills" in fm:
        skills = fm["skills"]
        if isinstance(skills, str):
            skills = [s.strip() for s in skills.split(",")]
        lines.append("skills = [" + ", ".join(f'"{s}"' for s in skills) + "]")
    if "description" in fm:
        desc = fm["description"].replace('"', '\\"')
        lines.append(f'description = "{desc}"')

    # body 用 TOML 多行字符串
    body_escaped = body.replace("\\", "\\\\").replace('"""', '\\"\\"\\"')
    lines.append("")
    lines.append('instructions = """')
    lines.append(body_escaped.rstrip())
    lines.append('"""')
    return "\n".join(lines) + "\n"


def sync_agents(check: bool, verbose: bool) -> int:
    """返回 0 = 同步通过 / 1 = 存在差异（check 模式下）"""
    if not CLAUDE_AGENTS.exists():
        print(f"ERROR: 源不存在 {CLAUDE_AGENTS}")
        return 2
    CODEX_AGENTS.mkdir(parents=True, exist_ok=True)

    diff_count = 0
    source_names = set()

    for md_path in sorted(CLAUDE_AGENTS.glob("*.md")):
        name = md_path.stem
        source_names.add(name)
        try:
            fm, body = parse_md(md_path)
        except Exception as e:
            print(f"ERROR: 解析 {md_path} 失败: {e}")
            return 2

        toml_path = CODEX_AGENTS / f"{name}.toml"
        new_content = to_toml(fm, body)
        old_content = toml_path.read_text(encoding="utf-8") if toml_path.exists() else ""

        if new_content != old_content:
            diff_count += 1
            if check:
                if verbose:
                    print(f"DIFF: {toml_path.relative_to(ROOT)}")
            else:
                toml_path.write_text(new_content, encoding="utf-8")
                if verbose:
                    print(f"WROTE: {toml_path.relative_to(ROOT)}")
        elif verbose:
            print(f"OK:    {toml_path.relative_to(ROOT)}")

    # 清理 .codex/agents/ 中 .claude/agents/ 已不存在的项
    for toml_path in sorted(CODEX_AGENTS.glob("*.toml")):
        if toml_path.stem not in source_names:
            diff_count += 1
            if check:
                if verbose:
                    print(f"EXTRA: {toml_path.relative_to(ROOT)}（源已删，应同步删除）")
            else:
                toml_path.unlink()
                if verbose:
                    print(f"REMOVED: {toml_path.relative_to(ROOT)}")

    return 1 if (check and diff_count > 0) else 0


def sync_skills_mirror(check: bool, verbose: bool) -> int:
    """把 .claude/skills/ 镜像到 .agents/skills/（skill4agent MCP 用）"""
    if not CLAUDE_SKILLS.exists():
        return 0
    AGENTS_SKILLS.mkdir(parents=True, exist_ok=True)

    # 简单做：清空 .agents/skills/，重新整体复制
    # 体积小（< 100MB）所以拷贝可接受
    if check:
        # check 模式只比对存在性
        source_dirs = {p.name for p in CLAUDE_SKILLS.iterdir() if p.is_dir()}
        mirror_dirs = {p.name for p in AGENTS_SKILLS.iterdir() if p.is_dir()}
        diff = source_dirs.symmetric_difference(mirror_dirs)
        if diff and verbose:
            print(f"SKILLS DIFF: {sorted(diff)[:5]}{'...' if len(diff) > 5 else ''}")
        return 1 if diff else 0

    # 直接全量同步
    if AGENTS_SKILLS.exists():
        shutil.rmtree(AGENTS_SKILLS)
    shutil.copytree(CLAUDE_SKILLS, AGENTS_SKILLS, ignore=shutil.ignore_patterns("SKILLS_INDEX.md"))
    if verbose:
        print(f"MIRRORED: .claude/skills/ → .agents/skills/")
    return 0


def main() -> int:
    p = argparse.ArgumentParser(description="同步 .claude/agents/ → .codex/agents/ + .agents/skills/ 镜像")
    p.add_argument("--check", action="store_true", help="仅校验，不写入；存在差异时退出码非 0")
    p.add_argument("--verbose", "-v", action="store_true", help="输出每个文件的处理结果")
    p.add_argument("--skip-skills", action="store_true", help="跳过 .agents/skills 镜像")
    args = p.parse_args()

    rc1 = sync_agents(args.check, args.verbose)
    rc2 = 0 if args.skip_skills else sync_skills_mirror(args.check, args.verbose)

    if args.check:
        if rc1 or rc2:
            print("[FAIL] 存在差异。请运行 `python tools/sync-agents.py` 同步后重新提交。")
            return 1
        print("[OK] 三处一致。")
    else:
        print(f"[OK] sync 完成（agents rc={rc1}, skills mirror rc={rc2}）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
