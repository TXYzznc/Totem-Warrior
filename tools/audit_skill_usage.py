#!/usr/bin/env python
"""Report multi-editor SKILL, Agent, and MCP usage frequency."""

from __future__ import annotations

import argparse
import datetime as dt
from collections import Counter
from pathlib import Path
from typing import Any

from log_tool_usage import EVENTS_LOG, LEGACY_LOG, ROOT, load_events


SKILLS_DIR = ROOT / ".claude" / "skills"
AGENTS_DIR = ROOT / ".claude" / "agents"
FIRST_CLASS_SOURCES = {"claude-code", "codex", "cursor", "kiro", "trae"}


def _parse_time(value: Any) -> dt.datetime | None:
    text = str(value or "").strip()
    if not text:
        return None
    try:
        parsed = dt.datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        return None
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=dt.datetime.now().astimezone().tzinfo)
    return parsed.astimezone(dt.timezone.utc)


def filter_events(events: list[dict[str, Any]], days: int | None) -> list[dict[str, Any]]:
    if not days:
        return events
    cutoff = dt.datetime.now(dt.timezone.utc) - dt.timedelta(days=days)
    result = []
    for event in events:
        timestamp = _parse_time(event.get("timestamp"))
        if timestamp is None or timestamp >= cutoff:
            result.append(event)
    return result


def _project_skill_names() -> set[str]:
    if not SKILLS_DIR.exists():
        return set()
    return {
        path.name
        for path in SKILLS_DIR.iterdir()
        if path.is_dir() and not path.name.startswith(("_", "."))
    }


def _project_agent_names() -> set[str]:
    if not AGENTS_DIR.exists():
        return set()
    return {path.stem for path in AGENTS_DIR.glob("*.md")}


def render_report(events: list[dict[str, Any]], days: int | None) -> str:
    suffix = f"（最近 {days} 天）" if days else "（全部历史）"
    lines = [f"# AI 工具使用频次报告{suffix}", ""]

    by_source = Counter(str(event.get("source", "unknown")) for event in events)
    by_kind: dict[str, Counter[str]] = {
        "Skill": Counter(),
        "Agent": Counter(),
        "MCP": Counter(),
        "Tool": Counter(),
        "Session": Counter(),
    }
    for event in events:
        kind = str(event.get("kind", ""))
        name = str(event.get("name", ""))
        if kind in by_kind and name:
            by_kind[kind][name] += 1

    lines.extend(["## 来源覆盖"])
    if by_source:
        lines.extend(f"  {count:>4d}  {source}" for source, count in by_source.most_common())
    else:
        lines.append("  （无记录）")
    lines.append("")

    missing_sources = sorted(FIRST_CLASS_SOURCES - set(by_source))
    if missing_sources:
        lines.extend(
            [
                "## 覆盖提示",
                f"  缺少一等适配器来源：{', '.join(missing_sources)}。",
                "  本报告中的 0 召回项只能作为候选，不能直接作为删除依据。",
                "",
            ]
        )

    for kind in ("Skill", "Agent", "MCP", "Tool", "Session"):
        counts = by_kind[kind]
        lines.append(f"## {kind} 调用频次")
        if counts:
            lines.extend(f"  {count:>4d}  {name}" for name, count in counts.most_common(30))
        else:
            lines.append("  （无记录）")
        lines.append("")

    all_skills = _project_skill_names()
    used_skills = set(by_kind["Skill"])
    zero_skills = sorted(all_skills - used_skills)
    lines.append(f"## 0 次召回的项目 SKILL 候选（{len(zero_skills)} 个 / 共 {len(all_skills)}）")
    lines.extend(f"  - {name}" for name in zero_skills)
    lines.append("")

    all_agents = _project_agent_names()
    used_agents = set(by_kind["Agent"])
    zero_agents = sorted(all_agents - used_agents)
    lines.append(f"## 0 次召回的 Agent 候选（{len(zero_agents)} 个 / 共 {len(all_agents)}）")
    lines.extend(f"  - {name}" for name in zero_agents)
    lines.append("")

    timestamps = [
        str(event.get("timestamp"))
        for event in events
        if event.get("timestamp")
    ]
    lines.extend(
        [
            "## 总览",
            f"  事件数：{len(events)}",
            (
                f"  时间范围：{min(timestamps)} ~ {max(timestamps)}"
                if timestamps
                else "  时间范围：无"
            ),
            f"  来源数：{len(by_source)}",
            f"  Skill 调用：{sum(by_kind['Skill'].values())}（{len(by_kind['Skill'])} 个不同 SKILL）",
            f"  Agent 调用：{sum(by_kind['Agent'].values())}（{len(by_kind['Agent'])} 个不同 Agent）",
            f"  MCP 调用：{sum(by_kind['MCP'].values())}（{len(by_kind['MCP'])} 个不同 MCP tool）",
            f"  Tool 调用：{sum(by_kind['Tool'].values())}（{len(by_kind['Tool'])} 个不同 Tool）",
            "",
        ]
    )
    return "\n".join(lines)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser()
    parser.add_argument("--days", type=int, default=None, help="只统计最近 N 天")
    parser.add_argument("--events", type=Path, default=EVENTS_LOG, help="JSONL 事件日志")
    parser.add_argument("--legacy", type=Path, default=LEGACY_LOG, help="旧 TSV 日志")
    parser.add_argument(
        "--no-legacy",
        action="store_true",
        help="不合并旧 TSV 日志",
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    events = load_events(
        jsonl_path=args.events,
        legacy_path=args.legacy,
        include_legacy=not args.no_legacy,
    )
    print(render_report(filter_events(events, args.days), args.days))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
