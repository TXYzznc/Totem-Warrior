#!/usr/bin/env python
"""Editor-neutral AI tool usage recorder.

The stable entry point supports three modes:

    python tools/log_tool_usage.py hook --source codex
    python tools/log_tool_usage.py record --source cursor --kind Skill --name unity-skills
    python tools/log_tool_usage.py migrate

With no arguments it remains compatible with the original Claude Code
PreToolUse hook. Hook mode is deliberately fail-open and never blocks the
calling editor.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import os
import re
import sys
import time
import uuid
from pathlib import Path
from typing import Any, Iterable, Iterator


ROOT = Path(__file__).resolve().parent.parent
EVENTS_LOG = ROOT / ".ai" / "usage" / "events.jsonl"
LEGACY_LOG = ROOT / ".claude" / "skills" / "_usage.log"
SCHEMA_VERSION = 1
ADAPTER_VERSION = "1"
VALID_KINDS = {"Skill", "Agent", "MCP", "Session", "Tool"}
SKILL_PATH_RE = re.compile(
    r"(?:^|/)skills/(?:[^/\"'\s]+/)*(?P<name>[A-Za-z0-9][A-Za-z0-9._:-]{0,127})/SKILL\.md",
    re.IGNORECASE,
)


class UsageEventError(ValueError):
    """Raised when an explicit event cannot be normalized."""


class LogLockTimeout(TimeoutError):
    """Raised when the short-lived append lock cannot be acquired."""


def _clean_text(value: Any, *, limit: int = 160) -> str:
    if value is None:
        return ""
    text = str(value)
    text = "".join(ch for ch in text if ch.isprintable())
    text = re.sub(r"\s+", " ", text).strip()
    return text[:limit]


def _clean_source(value: Any) -> str:
    source = _clean_text(value, limit=64).lower().replace(" ", "-")
    return re.sub(r"[^a-z0-9._:-]", "-", source).strip("-")


def _clean_project(value: Any) -> str:
    text = _clean_text(value, limit=512).replace("\\", "/").rstrip("/")
    leaf = text.rsplit("/", 1)[-1] if text else ""
    return _clean_text(leaf, limit=96) or ROOT.name


def _normalize_kind(value: Any) -> str:
    raw = _clean_text(value, limit=32).lower()
    aliases = {
        "skill": "Skill",
        "agent": "Agent",
        "mcp": "MCP",
        "session": "Session",
        "tool": "Tool",
    }
    kind = aliases.get(raw, _clean_text(value, limit=32))
    if kind not in VALID_KINDS:
        raise UsageEventError(f"unsupported kind: {value!r}")
    return kind


def _utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).isoformat(timespec="seconds").replace("+00:00", "Z")


def _stable_id(prefix: str, *parts: Any) -> str:
    raw = "\x1f".join(_clean_text(part, limit=1024) for part in parts)
    digest = hashlib.sha256(raw.encode("utf-8")).hexdigest()[:32]
    return f"{prefix}:{digest}"


def create_event(
    *,
    source: Any,
    kind: Any,
    name: Any,
    event: Any = "use",
    session_id: Any = "",
    project: Any = "",
    timestamp: Any = "",
    event_id: Any = "",
    inferred: bool = False,
) -> dict[str, Any]:
    """Create a privacy-filtered schema v1 event."""

    normalized_source = _clean_source(source)
    normalized_kind = _normalize_kind(kind)
    normalized_name = _clean_text(name)
    normalized_event = _clean_source(event) or "use"
    normalized_session = _clean_text(session_id, limit=64)
    normalized_project = _clean_project(project)
    normalized_timestamp = _clean_text(timestamp, limit=64) or _utc_now()

    if not normalized_source:
        raise UsageEventError("source is required")
    if not normalized_name:
        raise UsageEventError("name is required")

    normalized_id = _clean_text(event_id, limit=96)
    if not normalized_id:
        normalized_id = f"evt:{uuid.uuid4().hex}"

    result: dict[str, Any] = {
        "schema_version": SCHEMA_VERSION,
        "timestamp": normalized_timestamp,
        "source": normalized_source,
        "event": normalized_event,
        "kind": normalized_kind,
        "name": normalized_name,
        "session_id": normalized_session,
        "project": normalized_project,
        "event_id": normalized_id,
        "adapter_version": ADAPTER_VERSION,
    }
    if inferred:
        result["inferred"] = True
    return result


class _AppendLock:
    def __init__(self, path: Path, timeout: float = 0.35) -> None:
        self.path = path
        self.timeout = timeout
        self.fd: int | None = None

    def __enter__(self) -> "_AppendLock":
        self.path.parent.mkdir(parents=True, exist_ok=True)
        deadline = time.monotonic() + self.timeout
        while True:
            try:
                self.fd = os.open(self.path, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
                os.write(self.fd, str(os.getpid()).encode("ascii", errors="ignore"))
                return self
            except FileExistsError:
                try:
                    if time.time() - self.path.stat().st_mtime > 15:
                        self.path.unlink()
                        continue
                except OSError:
                    pass
                if time.monotonic() >= deadline:
                    raise LogLockTimeout(str(self.path))
                time.sleep(0.02)

    def __exit__(self, exc_type: Any, exc: Any, traceback: Any) -> None:
        if self.fd is not None:
            os.close(self.fd)
        try:
            self.path.unlink()
        except OSError:
            pass


def iter_jsonl(path: Path = EVENTS_LOG) -> Iterator[dict[str, Any]]:
    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        try:
            event = json.loads(line)
        except (TypeError, json.JSONDecodeError):
            continue
        if isinstance(event, dict) and event.get("event_id"):
            yield event


def append_events(
    events: Iterable[dict[str, Any]],
    *,
    path: Path = EVENTS_LOG,
    lock_timeout: float = 0.35,
) -> int:
    """Append unique events and return the number written."""

    pending = list(events)
    if not pending:
        return 0
    lock_path = path.with_suffix(path.suffix + ".lock")
    with _AppendLock(lock_path, timeout=lock_timeout):
        existing = {str(item.get("event_id")) for item in iter_jsonl(path)}
        unique: list[dict[str, Any]] = []
        for item in pending:
            event_id = str(item.get("event_id", ""))
            if not event_id or event_id in existing:
                continue
            existing.add(event_id)
            unique.append(item)
        if not unique:
            return 0
        path.parent.mkdir(parents=True, exist_ok=True)
        payload = "".join(
            json.dumps(item, ensure_ascii=False, separators=(",", ":")) + "\n"
            for item in unique
        )
        with path.open("a", encoding="utf-8", newline="\n") as stream:
            stream.write(payload)
        return len(unique)


def iter_legacy(path: Path = LEGACY_LOG) -> Iterator[dict[str, Any]]:
    """Yield deterministic schema v1 events from the old TSV file."""

    if not path.exists():
        return
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        parts = line.split("\t")
        if len(parts) < 3:
            continue
        timestamp, kind, name = parts[:3]
        session_id = parts[3] if len(parts) > 3 else ""
        try:
            yield create_event(
                source="claude-code",
                event="legacy-import",
                kind=kind,
                name=name,
                session_id=session_id,
                timestamp=timestamp,
                event_id=_stable_id("legacy", timestamp, kind, name, session_id),
            )
        except UsageEventError:
            continue


def load_events(
    *,
    jsonl_path: Path = EVENTS_LOG,
    legacy_path: Path = LEGACY_LOG,
    include_legacy: bool = True,
) -> list[dict[str, Any]]:
    events = list(iter_jsonl(jsonl_path))
    seen = {str(item.get("event_id")) for item in events}
    if include_legacy:
        for item in iter_legacy(legacy_path):
            event_id = str(item["event_id"])
            if event_id not in seen:
                events.append(item)
                seen.add(event_id)
    return events


def _mapping(value: Any) -> dict[str, Any]:
    return value if isinstance(value, dict) else {}


def _first(mapping: dict[str, Any], *keys: str) -> Any:
    for key in keys:
        value = mapping.get(key)
        if value not in (None, ""):
            return value
    return ""


def _selected_strings(value: Any, key: str = "") -> Iterator[str]:
    """Visit only fields that can carry a tool command/path, never prompts."""

    allowed = {
        "command",
        "cmd",
        "path",
        "file",
        "file_path",
        "skill_path",
        "args",
        "arguments",
        "input",
    }
    if isinstance(value, str):
        if key.lower() in allowed:
            yield value
    elif isinstance(value, dict):
        for child_key, child in value.items():
            if str(child_key).lower() in allowed:
                yield from _selected_strings(child, str(child_key))
    elif isinstance(value, list) and key.lower() in allowed:
        for child in value:
            yield from _selected_strings(child, key)


def _skill_names_from_input(tool_input: dict[str, Any]) -> list[str]:
    found: set[str] = set()
    for raw in _selected_strings(tool_input):
        normalized = raw.replace("\\", "/")
        for match in SKILL_PATH_RE.finditer(normalized):
            found.add(match.group("name"))
    return sorted(found)


def adapt_hook_payload(
    payload: dict[str, Any],
    *,
    source: str,
    forced_event: str = "",
) -> list[dict[str, Any]]:
    """Normalize Claude Code, Codex, or canonical stdin hook payloads."""

    session_id = _first(payload, "session_id", "sessionId", "conversation_id", "thread_id")
    project = _first(payload, "project", "project_name") or ROOT.name
    hook_event = _clean_source(
        forced_event or _first(payload, "hook_event_name", "hookEventName", "event")
    )

    if hook_event in {"session", "session-start", "sessionstart"}:
        event_id = (
            _stable_id("hook", source, session_id, "session-start")
            if session_id
            else ""
        )
        return [
            create_event(
                source=source,
                event="session-start",
                kind="Session",
                name="start",
                session_id=session_id,
                project=project,
                event_id=event_id,
            )
        ]

    # Canonical adapter input. Unknown fields are intentionally discarded.
    if payload.get("kind") and payload.get("name"):
        return [
            create_event(
                source=source or payload.get("source"),
                event=_first(payload, "event") or "use",
                kind=payload.get("kind"),
                name=payload.get("name"),
                session_id=session_id,
                project=project,
                timestamp=payload.get("timestamp"),
                event_id=payload.get("event_id"),
                inferred=bool(payload.get("inferred", False)),
            )
        ]

    tool_name = _clean_text(_first(payload, "tool_name", "toolName"), limit=256)
    tool_input = _mapping(_first(payload, "tool_input", "toolInput", "input"))
    tool_use_id = _first(payload, "tool_use_id", "toolUseId", "call_id")
    lowered = tool_name.lower()
    events: list[dict[str, Any]] = []

    def add(kind: str, name: Any, *, event: str = "tool-use", inferred: bool = False) -> None:
        clean_name = _clean_text(name)
        if not clean_name:
            return
        stable = (
            _stable_id("hook", source, session_id, tool_use_id, kind, clean_name)
            if tool_use_id
            else ""
        )
        events.append(
            create_event(
                source=source,
                event=event,
                kind=kind,
                name=clean_name,
                session_id=session_id,
                project=project,
                event_id=stable,
                inferred=inferred,
            )
        )

    if tool_name == "Skill" or lowered.endswith(".skill"):
        add("Skill", _first(tool_input, "skill", "name"))
    elif tool_name == "Agent" or "spawn_agent" in lowered or lowered.endswith(".agent"):
        add(
            "Agent",
            _first(tool_input, "subagent_type", "agent_type", "role", "task_name")
            or "default",
        )
    elif lowered.startswith("mcp__") or lowered.startswith("mcp_") or "__mcp__" in lowered:
        add("MCP", tool_name)

    # Codex usually activates a filesystem skill by reading its SKILL.md.
    for skill_name in _skill_names_from_input(tool_input):
        add("Skill", skill_name, event="skill-read", inferred=True)

    return events


def _read_stdin_json() -> dict[str, Any]:
    data = json.load(sys.stdin)
    if not isinstance(data, dict):
        raise UsageEventError("stdin payload must be a JSON object")
    return data


def _hook(args: argparse.Namespace) -> int:
    try:
        payload = _read_stdin_json()
        events = adapt_hook_payload(
            payload,
            source=args.source,
            forced_event=args.event,
        )
        append_events(events, path=args.output)
    except Exception:
        # Hooks are telemetry only: never block the editor operation.
        return 0
    return 0


def _record(args: argparse.Namespace) -> int:
    event = create_event(
        source=args.source,
        event=args.event,
        kind=args.kind,
        name=args.name,
        session_id=args.session,
        project=args.project,
        timestamp=args.timestamp,
        event_id=args.event_id,
        inferred=args.inferred,
    )
    written = append_events([event], path=args.output)
    print(f"recorded={written} event_id={event['event_id']}")
    return 0


def _migrate(args: argparse.Namespace) -> int:
    legacy = list(iter_legacy(args.legacy))
    written = append_events(legacy, path=args.output)
    print(f"legacy={len(legacy)} migrated={written} output={args.output}")
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Record editor-neutral AI tool usage events.")
    subparsers = parser.add_subparsers(dest="command")

    hook = subparsers.add_parser("hook", help="Read an editor hook payload from stdin.")
    hook.add_argument("--source", required=True, help="Editor/adapter identifier.")
    hook.add_argument("--event", default="", help="Force a lifecycle event such as session.")
    hook.add_argument("--output", type=Path, default=EVENTS_LOG)
    hook.set_defaults(handler=_hook)

    record = subparsers.add_parser("record", help="Record one explicit usage event.")
    record.add_argument("--source", required=True)
    record.add_argument("--kind", required=True, choices=sorted(VALID_KINDS))
    record.add_argument("--name", required=True)
    record.add_argument("--event", default="use")
    record.add_argument("--session", default="")
    record.add_argument("--project", default=ROOT.name)
    record.add_argument("--timestamp", default="")
    record.add_argument("--event-id", default="")
    record.add_argument("--inferred", action="store_true")
    record.add_argument("--output", type=Path, default=EVENTS_LOG)
    record.set_defaults(handler=_record)

    migrate = subparsers.add_parser("migrate", help="Idempotently migrate the legacy TSV log.")
    migrate.add_argument("--legacy", type=Path, default=LEGACY_LOG)
    migrate.add_argument("--output", type=Path, default=EVENTS_LOG)
    migrate.set_defaults(handler=_migrate)
    return parser


def main(argv: list[str] | None = None) -> int:
    args_list = list(sys.argv[1:] if argv is None else argv)
    if not args_list:
        # Backward compatibility with the original Claude Code hook command.
        args_list = ["hook", "--source", os.environ.get("AI_EDITOR", "claude-code")]
    parser = build_parser()
    args = parser.parse_args(args_list)
    if not hasattr(args, "handler"):
        parser.print_help()
        return 2
    try:
        return int(args.handler(args))
    except (OSError, UsageEventError, LogLockTimeout, json.JSONDecodeError) as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
