#!/usr/bin/env python
"""Editor-neutral AI tool usage recorder.

The stable entry point supports editor hooks plus initialization and reporting:

    python tools/log_tool_usage.py hook --source codex
    python tools/log_tool_usage.py record --source cursor --kind Skill --name unity-skills
    python tools/log_tool_usage.py migrate
    python tools/log_tool_usage.py doctor --editor codex --json
    python tools/log_tool_usage.py init --editor codex --trust-codex-hooks
    python tools/log_tool_usage.py report

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
import shutil
import subprocess
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
FIRST_CLASS_EDITORS = ("codex", "claude-code", "cursor", "kiro", "trae")
EDITOR_ALIASES = {
    "claude": "claude-code",
    "claudecode": "claude-code",
    "claude-code": "claude-code",
    "codex": "codex",
    "cursor": "cursor",
    "kiro": "kiro",
    "trae": "trae",
}
EDITOR_CONFIGS = {
    "codex": (Path(".codex/hooks.json"),),
    "claude-code": (Path(".claude/settings.json"),),
    "cursor": (Path(".cursor/hooks.json"),),
    "kiro": (Path(".kiro/hooks/ai-tool-usage.json"),),
    # TRAE officially supports importing Claude Code hooks. The project rule
    # carries the first-run instructions; no undocumented private hook path is used.
    "trae": (Path(".trae/hooks.json"), Path(".trae/rules/project_rules.md")),
}
SKILL_PATH_RE = re.compile(
    r"(?:^|/)skills/(?:[^/\"'\s]+/)*(?P<name>[A-Za-z0-9][A-Za-z0-9._:-]{0,127})/SKILL\.md",
    re.IGNORECASE,
)
AGENT_PATH_RE = re.compile(
    r"(?:^|[/\s\"'])\.codex/agents/(?P<name>[A-Za-z0-9][A-Za-z0-9._-]{0,127})\.toml",
    re.IGNORECASE,
)
NESTED_TOOL_RE = re.compile(r"\btools\.(?P<name>[A-Za-z][A-Za-z0-9_]*)\s*\(")


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


def _selected_strings(value: Any, key: str = "input") -> Iterator[str]:
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


def _names_from_input(value: Any, pattern: re.Pattern[str]) -> list[str]:
    found: set[str] = set()
    for raw in _selected_strings(value):
        normalized = raw.replace("\\", "/")
        for match in pattern.finditer(normalized):
            found.add(match.group("name"))
    return sorted(found)


def _skill_names_from_input(tool_input: Any) -> list[str]:
    return _names_from_input(tool_input, SKILL_PATH_RE)


def _agent_names_from_input(tool_input: Any) -> list[str]:
    return _names_from_input(tool_input, AGENT_PATH_RE)


def _without_javascript_literals(value: str) -> str:
    """Blank JS strings/comments while preserving positions and call syntax."""

    result = list(value)
    index = 0
    quote = ""
    line_comment = False
    block_comment = False
    while index < len(value):
        current = value[index]
        following = value[index + 1] if index + 1 < len(value) else ""
        if line_comment:
            if current in "\r\n":
                line_comment = False
            else:
                result[index] = " "
            index += 1
            continue
        if block_comment:
            result[index] = " "
            if current == "*" and following == "/":
                result[index + 1] = " "
                index += 2
                block_comment = False
            else:
                index += 1
            continue
        if quote:
            result[index] = " "
            if current == "\\":
                if index + 1 < len(value):
                    result[index + 1] = " "
                    index += 2
                else:
                    index += 1
            elif current == quote:
                quote = ""
                index += 1
            else:
                index += 1
            continue
        if current in {"'", '"', "`"}:
            result[index] = " "
            quote = current
            index += 1
        elif current == "/" and following == "/":
            result[index] = result[index + 1] = " "
            index += 2
            line_comment = True
        elif current == "/" and following == "*":
            result[index] = result[index + 1] = " "
            index += 2
            block_comment = True
        else:
            index += 1
    return "".join(result)


def _nested_tool_names_from_input(tool_input: Any) -> list[str]:
    found: set[str] = set()
    for raw in _selected_strings(tool_input):
        syntax_only = _without_javascript_literals(raw)
        found.update(match.group("name") for match in NESTED_TOOL_RE.finditer(syntax_only))
    return sorted(found)


def _tool_kind(name: str) -> str:
    lowered = name.lower()
    if lowered.startswith("mcp__") or lowered.startswith("mcp_") or "__mcp__" in lowered:
        return "MCP"
    return "Tool"


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
    raw_tool_input = _first(payload, "tool_input", "toolInput", "input")
    tool_input = _mapping(raw_tool_input)
    tool_use_id = _first(payload, "tool_use_id", "toolUseId", "call_id")
    timestamp = _first(payload, "timestamp")
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
                timestamp=timestamp,
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
    elif tool_name and lowered not in {"exec", "functions.exec"}:
        add("Tool", tool_name)

    # Modern Codex wraps most calls in a free-form `exec` program. Parse names
    # only; never persist the program, arguments, commands, or file paths.
    nested_tools = _nested_tool_names_from_input(raw_tool_input)
    for nested_tool in nested_tools:
        add(_tool_kind(nested_tool), nested_tool, inferred=True)

    # Codex usually activates a filesystem skill by reading its SKILL.md.
    reads_files = "shell_command" in nested_tools or "shell_command" in lowered
    if reads_files:
        for skill_name in _skill_names_from_input(raw_tool_input):
            add("Skill", skill_name, event="skill-read", inferred=True)

        # Project policy also permits an equivalent agent route: read the mirrored
        # TOML prompt and execute it in the main conversation without spawning.
        for agent_name in _agent_names_from_input(raw_tool_input):
            add("Agent", agent_name, event="agent-route", inferred=True)

    return events


def _parse_timestamp(value: Any) -> dt.datetime | None:
    text = _clean_text(value, limit=64)
    if not text:
        return None
    try:
        parsed = dt.datetime.fromisoformat(text.replace("Z", "+00:00"))
    except ValueError:
        return None
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=dt.timezone.utc)
    return parsed.astimezone(dt.timezone.utc)


def _same_project(left: Any, right: Path) -> bool:
    if not left:
        return False
    try:
        left_path = os.path.normcase(os.path.abspath(os.fspath(left)))
        right_path = os.path.normcase(os.path.abspath(os.fspath(right)))
    except (OSError, TypeError, ValueError):
        return False
    return left_path == right_path


def iter_codex_session_events(
    sessions_root: Path,
    *,
    project_root: Path = ROOT,
    days: int | None = None,
) -> Iterator[dict[str, Any]]:
    """Recover whitelisted usage facts from local Codex rollout JSONL files."""

    cutoff = (
        dt.datetime.now(dt.timezone.utc) - dt.timedelta(days=days)
        if days is not None
        else None
    )
    for path in sessions_root.rglob("*.jsonl") if sessions_root.exists() else ():
        if cutoff is not None:
            try:
                modified = dt.datetime.fromtimestamp(
                    path.stat().st_mtime, tz=dt.timezone.utc
                )
            except OSError:
                continue
            if modified < cutoff:
                continue
        session_id = ""
        in_project = False
        session_event: dict[str, Any] | None = None
        pending: list[dict[str, Any]] = []
        try:
            stream = path.open("r", encoding="utf-8", errors="replace")
        except OSError:
            continue
        with stream:
            for line in stream:
                try:
                    item = json.loads(line)
                except (TypeError, json.JSONDecodeError):
                    continue
                if not isinstance(item, dict):
                    continue
                timestamp = item.get("timestamp", "")
                parsed_time = _parse_timestamp(timestamp)
                if cutoff is not None and parsed_time is not None and parsed_time < cutoff:
                    continue
                item_type = item.get("type")
                payload = _mapping(item.get("payload"))
                if item_type == "session_meta":
                    session_id = _clean_text(
                        _first(payload, "session_id", "id"), limit=64
                    )
                    in_project = _same_project(payload.get("cwd"), project_root)
                    if in_project:
                        session_event = create_event(
                            source="codex",
                            event="session-start",
                            kind="Session",
                            name="start",
                            session_id=session_id,
                            project=project_root.name,
                            timestamp=timestamp,
                            event_id=_stable_id(
                                "hook", "codex", session_id, "session-start"
                            ),
                            inferred=True,
                        )
                    continue
                if not in_project or item_type != "response_item":
                    continue
                if payload.get("type") not in {"custom_tool_call", "function_call"}:
                    continue
                hook_payload = {
                    "session_id": session_id,
                    "project": project_root.name,
                    "timestamp": timestamp,
                    "tool_name": payload.get("name", ""),
                    "tool_input": payload.get("input", {}),
                    "tool_use_id": _first(payload, "call_id", "id"),
                }
                for event in adapt_hook_payload(hook_payload, source="codex"):
                    event["event"] = "session-backfill"
                    event["inferred"] = True
                    pending.append(event)
        if session_event is not None:
            yield session_event
        yield from pending


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


def _sync_codex(args: argparse.Namespace) -> int:
    events = list(
        iter_codex_session_events(
            args.sessions,
            project_root=args.project_root,
            days=args.days,
        )
    )
    written = append_events(events, path=args.output)
    print(f"discovered={len(events)} recorded={written} output={args.output}")
    return 0


def _normalize_editor(value: str) -> str:
    raw = _clean_source(value)
    if raw == "all":
        return raw
    editor = EDITOR_ALIASES.get(raw)
    if not editor:
        raise UsageEventError(f"unsupported editor: {value!r}")
    return editor


def _editor_configured(editor: str, root: Path) -> tuple[bool, list[str]]:
    missing = [path.as_posix() for path in EDITOR_CONFIGS[editor] if not (root / path).is_file()]
    return not missing, missing


def _latest_realtime_event(editor: str, events_path: Path) -> dict[str, Any] | None:
    cutoff = dt.datetime.now(dt.timezone.utc) - dt.timedelta(hours=24)
    candidates = []
    for item in iter_jsonl(events_path):
        if item.get("source") != editor or item.get("inferred", False):
            continue
        if item.get("event") in {"legacy-import", "session-backfill"}:
            continue
        timestamp = _parse_timestamp(item.get("timestamp"))
        if timestamp is not None and timestamp >= cutoff:
            candidates.append(item)
    if not candidates:
        return None
    return max(candidates, key=lambda item: str(item.get("timestamp", "")))


def _codex_trust(root: Path, *, write: bool = False) -> dict[str, Any]:
    node = shutil.which("node")
    script = root / "tools" / "codex_hook_trust.js"
    if not node:
        return {"verifiable": False, "trusted": False, "error": "node not found"}
    if not script.is_file():
        return {"verifiable": False, "trusted": False, "error": "trust helper missing"}
    command = [node, str(script), "trust" if write else "check", str(root)]
    try:
        completed = subprocess.run(
            command,
            cwd=root,
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=20,
            check=False,
        )
    except (OSError, subprocess.TimeoutExpired) as exc:
        return {"verifiable": False, "trusted": False, "error": str(exc)}
    output = completed.stdout.strip().splitlines()
    try:
        result = json.loads(output[-1]) if output else {}
    except json.JSONDecodeError:
        result = {}
    if not isinstance(result, dict):
        result = {}
    result.setdefault("verifiable", completed.returncode in {0, 3})
    result.setdefault("trusted", completed.returncode == 0)
    if completed.returncode not in {0, 3}:
        result.setdefault("error", completed.stderr.strip() or "Codex trust query failed")
    return result


def build_doctor_report(
    *,
    editor: str,
    root: Path = ROOT,
    events_path: Path = EVENTS_LOG,
    query_codex: bool = True,
) -> dict[str, Any]:
    selected = list(FIRST_CLASS_EDITORS) if editor == "all" else [_normalize_editor(editor)]
    reports: dict[str, Any] = {}
    for name in selected:
        configured, missing = _editor_configured(name, root)
        live = _latest_realtime_event(name, events_path)
        trust: dict[str, Any] = {"verifiable": False, "trusted": False}
        if name == "codex" and query_codex and configured:
            trust = _codex_trust(root)
        if name == "codex":
            active = configured and bool(trust.get("trusted"))
            state = "active" if active else ("untrusted" if configured else "not-configured")
        elif live:
            active = configured
            state = "active" if active else "event-without-config"
        else:
            active = False
            state = "pending-host-activation" if configured else "not-configured"
        reports[name] = {
            "configured": configured,
            "missing": missing,
            "trust": trust,
            "realtime_event": (
                {
                    "timestamp": live.get("timestamp"),
                    "event": live.get("event"),
                    "kind": live.get("kind"),
                    "name": live.get("name"),
                }
                if live
                else None
            ),
            "active": active,
            "state": state,
        }
    return {
        "schema_version": 1,
        "project": root.name,
        "events_log": str(events_path.relative_to(root)) if events_path.is_relative_to(root) else str(events_path),
        "editors": reports,
        "all_active": all(item["active"] for item in reports.values()),
    }


def _doctor(args: argparse.Namespace) -> int:
    report = build_doctor_report(
        editor=_normalize_editor(args.editor),
        root=args.project_root.resolve(),
        events_path=args.events,
        query_codex=not args.no_codex_query,
    )
    if args.json:
        print(json.dumps(report, ensure_ascii=False, indent=2))
    else:
        for editor, item in report["editors"].items():
            live = item["realtime_event"]
            evidence = f"，最近实时事件 {live['timestamp']}" if live else ""
            print(f"{editor}: {item['state']}{evidence}")
            if item["missing"]:
                print(f"  缺少：{', '.join(item['missing'])}")
            if item["trust"].get("error"):
                print(f"  信任检查：{item['trust']['error']}")
    return 0


def _init(args: argparse.Namespace) -> int:
    editor = _normalize_editor(args.editor)
    if editor == "all":
        raise UsageEventError("init requires one explicit editor")
    root = args.project_root.resolve()
    configured, missing = _editor_configured(editor, root)
    if not configured:
        raise UsageEventError(f"missing project adapter files: {', '.join(missing)}")
    if not args.yes:
        print(f"将初始化 {editor} 的项目级 AI 使用统计。")
        print("仅记录来源、事件、Tool/SKILL/Agent/MCP 名称和会话标识；不记录 Prompt、代码、参数或完整路径。")
        if editor == "codex" and args.trust_codex_hooks:
            print("将只信任当前项目 .codex/hooks.json 中由 Codex 计算出的当前 Hook 哈希。")
        answer = input("继续？[y/N] ").strip().lower()
        if answer not in {"y", "yes"}:
            print("已取消，未修改信任状态。")
            return 1
    if editor == "codex":
        if not args.trust_codex_hooks:
            raise UsageEventError("Codex activation requires explicit --trust-codex-hooks")
        result = _codex_trust(root, write=True)
        print(json.dumps({"editor": editor, **result}, ensure_ascii=False, indent=2))
        return 0 if result.get("trusted") else 3
    instructions = {
        "claude-code": "在 Claude Code 的项目 Hook 安全提示中批准仓库内 .claude/settings.json，然后重开会话。",
        "cursor": "将仓库标记为 Trusted Workspace；Cursor 会自动加载 .cursor/hooks.json，然后重开 Agent 会话。",
        "kiro": "Kiro 会自动发现 .kiro/hooks；若出现 shell 命令权限提示，请批准该记录命令，然后重开会话。",
        "trae": "在 TRAE Hook 安全提示或 Hook 面板中启用项目的 .trae/hooks.json，然后重开会话。",
    }
    print(json.dumps({"editor": editor, "configured": True, "state": "pending-host-activation", "next_step": instructions[editor]}, ensure_ascii=False, indent=2))
    return 0


def _report(args: argparse.Namespace) -> int:
    from audit_skill_usage import filter_events, render_report

    events = load_events(
        jsonl_path=args.events,
        legacy_path=args.legacy,
        include_legacy=not args.no_legacy,
    )
    print(render_report(filter_events(events, args.days), args.days))
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

    sync_codex = subparsers.add_parser(
        "sync-codex",
        help="Backfill whitelisted usage facts from local Codex session JSONL.",
    )
    sync_codex.add_argument(
        "--sessions",
        type=Path,
        default=Path.home() / ".codex" / "sessions",
    )
    sync_codex.add_argument("--project-root", type=Path, default=ROOT)
    sync_codex.add_argument("--days", type=int, default=None)
    sync_codex.add_argument("--output", type=Path, default=EVENTS_LOG)
    sync_codex.set_defaults(handler=_sync_codex)

    doctor = subparsers.add_parser("doctor", help="Diagnose adapter, trust, and realtime event status.")
    doctor.add_argument("--editor", default="all", choices=[*FIRST_CLASS_EDITORS, "all", "claude"])
    doctor.add_argument("--project-root", type=Path, default=ROOT)
    doctor.add_argument("--events", type=Path, default=EVENTS_LOG)
    doctor.add_argument("--json", action="store_true")
    doctor.add_argument("--no-codex-query", action="store_true", help="Do not start codex app-server for trust status.")
    doctor.set_defaults(handler=_doctor)

    init = subparsers.add_parser("init", help="Initialize one editor adapter after explicit user consent.")
    init.add_argument("--editor", required=True, choices=[*FIRST_CLASS_EDITORS, "claude"])
    init.add_argument("--project-root", type=Path, default=ROOT)
    init.add_argument("--yes", action="store_true", help="Consent was already collected by the calling AI/editor.")
    init.add_argument("--trust-codex-hooks", action="store_true", help="Trust only current-project Codex hook hashes.")
    init.set_defaults(handler=_init)

    report = subparsers.add_parser("report", help="Render the existing usage-frequency audit report.")
    report.add_argument("--days", type=int, default=None)
    report.add_argument("--events", type=Path, default=EVENTS_LOG)
    report.add_argument("--legacy", type=Path, default=LEGACY_LOG)
    report.add_argument("--no-legacy", action="store_true")
    report.set_defaults(handler=_report)
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
