from __future__ import annotations

import json
import argparse
import io
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

import audit_skill_usage as audit
import log_tool_usage as usage


class EventProtocolTests(unittest.TestCase):
    def test_create_event_has_only_whitelisted_fields(self) -> None:
        event = usage.create_event(
            source="My Editor",
            kind="skill",
            name=" unity-skills\n",
            session_id="session-1",
            project=r"D:\private\GameDesinger",
        )

        self.assertEqual("my-editor", event["source"])
        self.assertEqual("Skill", event["kind"])
        self.assertEqual("unity-skills", event["name"])
        self.assertNotIn("prompt", event)
        self.assertNotIn("command", event)
        self.assertEqual("GameDesinger", event["project"])
        self.assertNotIn("private", json.dumps(event))
        self.assertEqual(usage.SCHEMA_VERSION, event["schema_version"])

    def test_invalid_explicit_event_is_rejected(self) -> None:
        with self.assertRaises(usage.UsageEventError):
            usage.create_event(source="", kind="Skill", name="unity-skills")
        with self.assertRaises(usage.UsageEventError):
            usage.create_event(source="editor", kind="Unknown", name="x")

    def test_claude_payload_adapts_skill_agent_and_mcp(self) -> None:
        fixtures = [
            (
                {"tool_name": "Skill", "tool_input": {"skill": "unity-skills"}},
                ("Skill", "unity-skills"),
            ),
            (
                {"tool_name": "Agent", "tool_input": {"subagent_type": "client-unity"}},
                ("Agent", "client-unity"),
            ),
            (
                {"tool_name": "mcp__server__query", "tool_input": {}},
                ("MCP", "mcp__server__query"),
            ),
        ]
        for payload, expected in fixtures:
            with self.subTest(payload=payload):
                event = usage.adapt_hook_payload(payload, source="claude-code")[0]
                self.assertEqual(expected, (event["kind"], event["name"]))

    def test_codex_infers_skill_without_persisting_sensitive_input(self) -> None:
        payload = {
            "session_id": "codex-session",
            "tool_name": "functions.shell_command",
            "tool_input": {
                "command": (
                    r"Get-Content C:\Users\dev\.codex\skills\.system"
                    r"\openai-docs\SKILL.md"
                ),
                "prompt": "private prompt",
                "code": "private code",
            },
        }

        events = usage.adapt_hook_payload(payload, source="codex")

        self.assertEqual(1, len(events))
        self.assertEqual("openai-docs", events[0]["name"])
        self.assertTrue(events[0]["inferred"])
        serialized = json.dumps(events[0])
        self.assertNotIn("private prompt", serialized)
        self.assertNotIn("Get-Content", serialized)
        self.assertNotIn("Users", serialized)

    def test_codex_adapts_agent_and_mcp_names(self) -> None:
        agent = usage.adapt_hook_payload(
            {
                "tool_name": "collaboration.spawn_agent",
                "tool_input": {"agent_type": "explorer", "task_name": "scan"},
            },
            source="codex",
        )[0]
        mcp = usage.adapt_hook_payload(
            {"tool_name": "mcp__codex_apps__github__search", "tool_input": {}},
            source="codex",
        )[0]

        self.assertEqual(("Agent", "explorer"), (agent["kind"], agent["name"]))
        self.assertEqual("MCP", mcp["kind"])

    def test_session_hook_creates_deterministic_event(self) -> None:
        payload = {"session_id": "same-session"}
        first = usage.adapt_hook_payload(payload, source="codex", forced_event="session")[0]
        second = usage.adapt_hook_payload(payload, source="codex", forced_event="session")[0]
        self.assertEqual(first["event_id"], second["event_id"])
        self.assertEqual("Session", first["kind"])

    def test_canonical_stdin_payload_ignores_unknown_fields(self) -> None:
        event = usage.adapt_hook_payload(
            {
                "kind": "Skill",
                "name": "unity-skills",
                "session_id": "generic-session",
                "prompt": "must not persist",
                "metadata": {"code": "must not persist"},
            },
            source="generic-editor",
        )[0]
        serialized = json.dumps(event)
        self.assertEqual("generic-editor", event["source"])
        self.assertNotIn("must not persist", serialized)

    def test_hook_mode_is_fail_open(self) -> None:
        args = argparse.Namespace(
            source="codex",
            event="",
            output=Path("unused.jsonl"),
        )
        with mock.patch.object(sys, "stdin", io.StringIO("{invalid")):
            self.assertEqual(0, usage._hook(args))


class StorageAndMigrationTests(unittest.TestCase):
    def test_append_deduplicates_event_ids(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            output = Path(directory) / "events.jsonl"
            event = usage.create_event(
                source="test",
                kind="Skill",
                name="unity-skills",
                event_id="fixed-id",
            )
            self.assertEqual(1, usage.append_events([event], path=output))
            self.assertEqual(0, usage.append_events([event], path=output))
            self.assertEqual(1, len(list(usage.iter_jsonl(output))))

    def test_legacy_migration_and_dual_read_are_idempotent(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            legacy = root / "usage.tsv"
            output = root / "events.jsonl"
            legacy.write_text(
                "2026-07-01T10:00:00\tSkill\tunity-skills\ts1\n"
                "2026-07-01T10:01:00\tAgent\tclient-unity\ts1\n",
                encoding="utf-8",
            )
            legacy_events = list(usage.iter_legacy(legacy))

            self.assertEqual(2, usage.append_events(legacy_events, path=output))
            self.assertEqual(0, usage.append_events(legacy_events, path=output))
            combined = usage.load_events(jsonl_path=output, legacy_path=legacy)
            self.assertEqual(2, len(combined))

    def test_report_shows_sources_and_coverage_warning(self) -> None:
        events = [
            usage.create_event(
                source="claude-code",
                kind="Skill",
                name="unity-skills",
            )
        ]
        report = audit.render_report(events, days=None)
        self.assertIn("claude-code", report)
        self.assertIn("缺少一等适配器来源：codex", report)
        self.assertIn("不能直接作为删除依据", report)

    def test_report_has_no_coverage_warning_with_both_first_class_sources(self) -> None:
        events = [
            usage.create_event(source="claude-code", kind="Skill", name="a"),
            usage.create_event(source="codex", kind="Skill", name="b"),
        ]
        report = audit.render_report(events, days=30)
        self.assertNotIn("## 覆盖提示", report)
        self.assertIn("（最近 30 天）", report)


if __name__ == "__main__":
    unittest.main()
