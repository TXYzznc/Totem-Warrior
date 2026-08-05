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

        self.assertEqual(
            {
                ("Tool", "functions.shell_command"),
                ("Skill", "openai-docs"),
            },
            {(event["kind"], event["name"]) for event in events},
        )
        skill_event = next(event for event in events if event["kind"] == "Skill")
        self.assertTrue(skill_event["inferred"])
        serialized = json.dumps(events[0])
        self.assertNotIn("private prompt", serialized)
        self.assertNotIn("Get-Content", serialized)
        self.assertNotIn("Users", serialized)

    def test_codex_freeform_exec_recovers_nested_usage_without_payload(self) -> None:
        private_command = "private-command-that-must-not-persist"
        payload = {
            "session_id": "codex-session",
            "tool_name": "exec",
            "tool_input": (
                "const a = await tools.mcp__server__query({secret: 'value'}); "
                "const b = await tools.shell_command({command: '"
                + private_command
                + " .claude/skills/grill-me/SKILL.md "
                ".codex/agents/gd-lead.toml'});"
            ),
            "tool_use_id": "call-1",
        }

        events = usage.adapt_hook_payload(payload, source="codex")
        observed = {(event["kind"], event["name"]) for event in events}

        self.assertEqual(
            {
                ("MCP", "mcp__server__query"),
                ("Tool", "shell_command"),
                ("Skill", "grill-me"),
                ("Agent", "gd-lead"),
            },
            observed,
        )
        serialized = json.dumps(events)
        self.assertNotIn(private_command, serialized)
        self.assertNotIn("secret", serialized)
        self.assertNotIn(".claude", serialized)

    def test_codex_exec_ignores_tool_examples_inside_patch_text(self) -> None:
        payload = {
            "tool_name": "exec",
            "tool_input": (
                "const patch = \"example tools.mcp__fake__call({}) "
                ".claude/skills/not-used/SKILL.md\"; "
                "await tools.apply_patch(patch);"
            ),
            "tool_use_id": "call-patch",
        }

        events = usage.adapt_hook_payload(payload, source="codex")

        self.assertEqual(
            {("Tool", "apply_patch")},
            {(event["kind"], event["name"]) for event in events},
        )

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

    def test_cursor_kiro_and_trae_camel_case_payloads_are_supported(self) -> None:
        for source in ("cursor", "kiro", "trae"):
            with self.subTest(source=source):
                event = usage.adapt_hook_payload(
                    {
                        "sessionId": "session-1",
                        "hookEventName": "PreToolUse",
                        "toolName": "shell_command",
                        "toolInput": {"command": "secret command"},
                        "toolUseId": "tool-1",
                    },
                    source=source,
                )[0]
                self.assertEqual(source, event["source"])
                self.assertEqual(("Tool", "shell_command"), (event["kind"], event["name"]))
                self.assertNotIn("secret", json.dumps(event))

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

    def test_codex_session_backfill_is_project_scoped_and_idempotent(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            project = root / "project"
            project.mkdir()
            sessions = root / "sessions"
            sessions.mkdir()
            rollout = sessions / "rollout.jsonl"
            records = [
                {
                    "timestamp": "2026-08-03T10:00:00Z",
                    "type": "session_meta",
                    "payload": {
                        "session_id": "session-1",
                        "cwd": str(project),
                    },
                },
                {
                    "timestamp": "2026-08-03T10:01:00Z",
                    "type": "response_item",
                    "payload": {
                        "type": "custom_tool_call",
                        "id": "item-1",
                        "call_id": "call-1",
                        "name": "exec",
                        "input": (
                            "await tools.shell_command({command: "
                            "'.claude/skills/grill-me/SKILL.md'});"
                        ),
                    },
                },
            ]
            rollout.write_text(
                "".join(json.dumps(record) + "\n" for record in records),
                encoding="utf-8",
            )

            events = list(
                usage.iter_codex_session_events(
                    sessions,
                    project_root=project,
                )
            )
            observed = {(event["kind"], event["name"]) for event in events}
            self.assertEqual(
                {
                    ("Session", "start"),
                    ("Tool", "shell_command"),
                    ("Skill", "grill-me"),
                },
                observed,
            )

            output = root / "events.jsonl"
            self.assertEqual(3, usage.append_events(events, path=output))
            self.assertEqual(0, usage.append_events(events, path=output))

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

    def test_report_has_no_coverage_warning_with_all_first_class_sources(self) -> None:
        events = [
            usage.create_event(source=source, kind="Skill", name=source)
            for source in usage.FIRST_CLASS_EDITORS
        ]
        report = audit.render_report(events, days=30)
        self.assertNotIn("## 覆盖提示", report)
        self.assertIn("（最近 30 天）", report)

    def test_report_includes_tool_usage(self) -> None:
        events = [usage.create_event(source="codex", kind="Tool", name="shell_command")]
        report = audit.render_report(events, days=None)
        self.assertIn("## Tool 调用频次", report)
        self.assertIn("shell_command", report)
        self.assertIn("Tool 调用：1", report)


class ActivationCliTests(unittest.TestCase):
    def _write_adapter_files(self, root: Path) -> None:
        for paths in usage.EDITOR_CONFIGS.values():
            for relative in paths:
                path = root / relative
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("{}\n", encoding="utf-8")

    def test_doctor_is_read_only_and_reports_live_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_adapter_files(root)
            events = root / ".ai" / "usage" / "events.jsonl"
            usage.append_events(
                [usage.create_event(source="cursor", kind="Session", name="start")],
                path=events,
            )
            before = events.read_bytes()

            report = usage.build_doctor_report(
                editor="cursor",
                root=root,
                events_path=events,
                query_codex=False,
            )

            self.assertTrue(report["editors"]["cursor"]["active"])
            self.assertEqual(before, events.read_bytes())

    def test_doctor_does_not_claim_host_activation_without_realtime_event(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_adapter_files(root)
            report = usage.build_doctor_report(
                editor="kiro",
                root=root,
                events_path=root / "events.jsonl",
                query_codex=False,
            )
            item = report["editors"]["kiro"]
            self.assertFalse(item["active"])
            self.assertEqual("pending-host-activation", item["state"])

    def test_codex_doctor_uses_native_trust_result(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_adapter_files(root)
            with mock.patch.object(
                usage,
                "_codex_trust",
                return_value={"verifiable": True, "trusted": True, "count": 2},
            ):
                report = usage.build_doctor_report(
                    editor="codex",
                    root=root,
                    events_path=root / "events.jsonl",
                )
            self.assertTrue(report["editors"]["codex"]["active"])

    def test_init_requires_explicit_codex_trust_switch(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self._write_adapter_files(root)
            args = argparse.Namespace(
                editor="codex",
                project_root=root,
                yes=True,
                trust_codex_hooks=False,
            )
            with self.assertRaises(usage.UsageEventError):
                usage._init(args)

    def test_project_adapter_json_files_parse(self) -> None:
        for relative in (
            ".codex/hooks.json",
            ".claude/settings.json",
            ".cursor/hooks.json",
            ".kiro/hooks/ai-tool-usage.json",
            ".trae/hooks.json",
        ):
            with self.subTest(path=relative):
                json.loads((usage.ROOT / relative).read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
