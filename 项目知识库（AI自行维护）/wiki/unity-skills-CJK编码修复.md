---
title: unity-skills SKILL CJK 编码修复
owner: tools-engineer
created: 2026-06-29
last_updated: 2026-06-29
status: active
related_specs:
  - openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/
  - openspec/specs/mcp-encoding-fix/spec.md
related_skills:
  - unity-skills
---

# unity-skills SKILL CJK 编码修复

## 1. 背景

13 期（fix-broken-prefabs）修复 4 个 UI Prefab（Settings / SelfTattoo / ThreeChoice / TattooEnchant）时发现，通过 `unity-skills` MCP 写入中文文本时 YAML 出现 `m_text: "����"` (U+FFFD 替换字符) 与 `m_text: "ȡ��"` (Latin-1 残片) 等签名。spike 排除了 server 端（C# `Encoding.UTF8` 全链路正确），结论：根因在 **Codex / Claude agent → Python CLI `unity_skills.py`** 之间不可见的胶水层，Windows cp936 默认 console code page 下 argv 被按 ANSI 当 Unicode 重解码。

不能在 13 期内修——agent 胶水层不在仓库可见范围，且任务紧迫。13 用 "直接 Edit YAML 绕过 MCP" 完成 Prefab 修复，留下 **14-mcp-encoding-fix** follow-up。

## 2. 决策

四路防御策略（详见 archive 的 design.md §3）：

| 路径 | 内容 | 性质 |
|---|---|---|
| **A. argv ctypes 加固** | `unity_skills.py` 启动时 `GetCommandLineW + CommandLineToArgvW` 重写 `sys.argv`；`try/except` 兜底 MSYS/Cygwin/WSL | 冗余但无害；Python 3.6+ wmain 已是 no-op |
| **B. `--stdin-json` 模式** | `main()` 检测 `--stdin-json` flag → 从 stdin binary 读 JSON body；完全绕开 argv | **100% 编码安全的唯一入口** |
| **C. SKILL.md 调用约定** | SKILL.md 加「中文 / CJK 参数调用约定（强制）」段；CLAUDE.md §六.5 UI 子流程交叉引用 | 文档化契约，下游调用方必须遵守 |
| **D. 回归测试** | 字节链路 monkey-patch（4 脚本 × 5 case = 20 测试）+ live Editor round-trip（7 case） | 锁定 A/B 修复未来不被回退失效 |

**核心立场**：CJK / Emoji / Latin-1 范围外字符 → 必走 `--stdin-json`；纯 ASCII 参数可继续用传统 `key=value` argv。

## 3. 被否定的备选

| 备选 | 否定理由 |
|---|---|
| **只做路径 A**（argv ctypes 加固） | Python 3.14 已是 wmain，路径 A 对当前版本是 no-op；下游版本回退 / agent 胶水层变化时仍可能再次损坏。无法 100% 保证安全。 |
| **改 Editor 端 C# server**（重新做 byte 解码层） | 13 期 spike 已证明 server 端 UTF-8 全链路对。改 C# 不是根因。 |
| **要求用户改 console code page 到 65001** | 用户体验差；对其他工具有副作用；agent 不可能保证每个会话都跑 `chcp 65001`。 |
| **直接绕开 unity-skills，所有 Prefab/UI 改动都用 Edit YAML** | 失去 unity-skills 主要价值（场景级操作 / Editor 交互 / batch 接口）。 |

## 4. 影响范围

### 代码
- [`.claude/skills/unity-skills/scripts/unity_skills.py`](../../.claude/skills/unity-skills/scripts/unity_skills.py) — 顶部 argv 加固 + `main()` `--stdin-json` 分支。备份 `unity_skills.py.bak-14`。

### 文档
- [`.claude/skills/unity-skills/SKILL.md`](../../.claude/skills/unity-skills/SKILL.md) — 新增「中文 / CJK 参数调用约定（强制）」段
- [`.claude/CLAUDE.md`](../../.claude/CLAUDE.md) §六.5 强制约束 5 — 交叉引用

### 测试
- `openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/tests/repro/` — Phase 0 复现（5 脚本）
- `openspec/changes/archive/2026-06-29-14-mcp-encoding-fix/tests/regression/` — Phase 4 回归 + Phase 5 端到端（6 脚本）
- spec sink：[`openspec/specs/mcp-encoding-fix/spec.md`](../../openspec/specs/mcp-encoding-fix/spec.md)

### Agent / SKILL
- **主对话 / Codex agent / 所有 subagent**：调 unity-skills 写 CJK 必须用 `--stdin-json`
- 受影响 SKILL：`unity-skills`（含 `ui_set_text` / `gameobject_create` / `console_log` 等所有可能传 CJK 的接口）

### 不影响
- Editor 端 C# code（13 spike 已验证 UTF-8 全链路对）
- 13 已归档的 4 个 Prefab（Phase 5 验证 mtime drift = 0.000s）

## 5. 过时检查

- **何时该 review**：unity_skills.py 重写 / 升级到新协议时；Python 版本回退到 < 3.6（无 wmain）时
- **何时该归档**：unity-skills 整体废弃（被新 MCP 替代）时；或 Anthropic / Codex 完全解决 agent 胶水层 argv 编码（可观测可验证）时
- **每月 1 号防腐**：跑 `audit_skills.py` 时核查 SKILL.md 的 CJK 约定段是否仍存在；如被误删需补回

## 6. 验证结果摘要

- Phase 0 复现：5 脚本 / 18 case 全过；根因在 agent 胶水层（不可见层）确认
- Phase 4 回归：20/20 字节链路 monkey-patch
- Phase 5 端到端：7/7 live Editor round-trip（含 emoji / ▶ / 200 字长串 / 13 期 ground truth）+ Prefab mtime drift = 0
