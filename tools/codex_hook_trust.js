#!/usr/bin/env node
"use strict";

// Uses the same app-server methods as Codex's native hook review UI. It only
// reads or writes trust entries returned for this project's .codex/hooks.json.
const { spawn } = require("child_process");
const path = require("path");

const mode = process.argv[2] || "check";
const cwd = path.resolve(process.argv[3] || process.cwd());
if (!new Set(["check", "trust"]).has(mode)) {
  process.stderr.write("mode must be check or trust\n");
  process.exit(2);
}

const child = spawn("codex", ["app-server", "--stdio"], {
  cwd,
  shell: process.platform === "win32",
  windowsHide: true,
  stdio: ["pipe", "pipe", "pipe"],
});
let stdout = "";
let stderr = "";
let finished = false;
const expectedPath = path.join(cwd, ".codex", "hooks.json").toLowerCase();

function send(message) {
  child.stdin.write(JSON.stringify(message) + "\n");
}

function finish(code, data) {
  if (finished) return;
  finished = true;
  clearTimeout(timer);
  process.stdout.write(JSON.stringify(data) + "\n");
  try { child.stdin.end(); } catch (_) {}
  setTimeout(() => { try { child.kill(); } catch (_) {} }, 100).unref();
  process.exitCode = code;
}

function projectHooks(message) {
  const groups = message && message.result && Array.isArray(message.result.data)
    ? message.result.data : [];
  return groups.flatMap(group => Array.isArray(group.hooks) ? group.hooks : [])
    .filter(hook => hook.source === "project" &&
      String(hook.sourcePath || "").toLowerCase() === expectedPath);
}

function status(hooks) {
  const trusted = hooks.length > 0 && hooks.every(hook => hook.trustStatus === "trusted");
  return {
    verifiable: true,
    trusted,
    count: hooks.length,
    hooks: hooks.map(hook => ({
      event: hook.eventName,
      command: hook.command,
      trust_status: hook.trustStatus,
      key: hook.key,
      current_hash: hook.currentHash,
    })),
  };
}

child.stdout.setEncoding("utf8");
child.stderr.setEncoding("utf8");
child.stderr.on("data", chunk => { stderr += chunk; });
child.stdout.on("data", chunk => {
  stdout += chunk;
  let index;
  while ((index = stdout.indexOf("\n")) >= 0) {
    const line = stdout.slice(0, index);
    stdout = stdout.slice(index + 1);
    let message;
    try { message = JSON.parse(line); } catch (_) { continue; }
    if (message.id === 1) {
      send({ method: "initialized", params: {} });
      send({ method: "hooks/list", id: 2, params: { cwds: [cwd] } });
    } else if (message.id === 2) {
      if (message.error) return finish(2, { verifiable: false, trusted: false, error: JSON.stringify(message.error) });
      const hooks = projectHooks(message);
      if (mode === "check") return finish(hooks.every(h => h.trustStatus === "trusted") && hooks.length ? 0 : 3, status(hooks));
      if (!hooks.length) return finish(3, { ...status(hooks), error: "No current-project hooks discovered" });
      const value = {};
      for (const hook of hooks) value[hook.key] = { trusted_hash: hook.currentHash };
      send({
        method: "config/batchWrite",
        id: 3,
        params: {
          edits: [{ keyPath: "hooks.state", value, mergeStrategy: "upsert" }],
          filePath: null,
          expectedVersion: null,
          reloadUserConfig: true,
        },
      });
    } else if (message.id === 3) {
      if (message.error) return finish(2, { verifiable: false, trusted: false, error: JSON.stringify(message.error) });
      send({ method: "hooks/list", id: 4, params: { cwds: [cwd] } });
    } else if (message.id === 4) {
      if (message.error) return finish(2, { verifiable: false, trusted: false, error: JSON.stringify(message.error) });
      const result = status(projectHooks(message));
      return finish(result.trusted ? 0 : 3, result);
    }
  }
});
child.on("error", error => finish(2, { verifiable: false, trusted: false, error: String(error) }));
child.on("exit", code => {
  if (!finished) finish(2, { verifiable: false, trusted: false, error: stderr.trim() || `codex app-server exited ${code}` });
});
const timer = setTimeout(() => finish(2, { verifiable: false, trusted: false, error: "codex app-server timed out" }), 15000);
send({ method: "initialize", id: 1, params: { clientInfo: { name: "ai-usage-bootstrap", title: "AI Usage Bootstrap", version: "1.0" } } });
