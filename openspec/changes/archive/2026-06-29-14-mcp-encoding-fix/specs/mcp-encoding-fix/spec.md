## ADDED Requirements

### Requirement: unity_skills.py MUST 在 Windows cp936 默认 code page 下正确接收 CJK argv 参数

`unity_skills.py` 在 Windows 平台启动时 MUST 通过 `GetCommandLineW` + `CommandLineToArgvW` 重建 `sys.argv`，绕开 Python 默认的 ANSI(cp936) → Unicode 转换层。MUST 兼容 MSYS/Cygwin/WSL Python（`try/except OSError` 兜底，跳过该分支保持原行为）。

#### Scenario: cp936 cmd 下传中文 argv 正确解码

- **GIVEN** Windows 默认 code page 是 936（GBK），Python 是原生 Windows 版本
- **WHEN** 调用 `python unity_skills.py ui_set_text name=TitleText text=设置`
- **THEN** `sys.argv[3]` MUST 等于 `"text=设置"`（UTF-8 字节流为 `0x74 0x65 0x78 0x74 0x3d 0xe8 0xae 0xbe 0xe7 0xbd 0xae`）
- **AND** MUST NOT 包含 `U+FFFD` / Latin-1 残片（`ȡ` / `ͣ` / `Ƴ` / `ƶ` 等）
- **AND** 最终写入 Prefab YAML 的 `m_text:` 字段 MUST 完全等于 `"设置"`

#### Scenario: MSYS/Cygwin Python 兜底

- **GIVEN** 在 MSYS / Cygwin / WSL 环境 `GetCommandLineW` 不可用
- **WHEN** `unity_skills.py` 启动
- **THEN** MUST 通过 `try/except OSError` 跳过该分支
- **AND** MUST 保持原 `sys.argv` 行为不报错
- **AND** SKILL.md MUST 明确这些环境调用方必须用 `--stdin-json` 模式

### Requirement: unity_skills.py MUST 提供 --stdin-json 模式作为 argv 的替代

`unity_skills.py` MUST 支持 `--stdin-json` 命令行 flag。检测到该 flag 时 MUST 从 stdin 读取一个 JSON 对象作为参数字典，跳过 argv 解析逻辑。MUST 兼容旧调用方式（无 `--stdin-json` 时维持原 argv 行为）。

#### Scenario: stdin JSON 模式调用 CJK 参数

- **GIVEN** stdin 数据为 `{"name":"TitleText","text":"设置"}`（UTF-8 字节）
- **WHEN** 调用 `python unity_skills.py ui_set_text --stdin-json` 且 stdin 是 binary pipe
- **THEN** `unity_skills.py` MUST 解析 JSON 得到 `params = {"name": "TitleText", "text": "设置"}`
- **AND** MUST 调用 `call_skill("ui_set_text", **params)`
- **AND** 最终 HTTP body MUST 是 UTF-8 编码的 JSON，含正确中文字节

#### Scenario: subprocess 调用方使用 stdin JSON 模式

- **GIVEN** Claude / Codex agent 通过 `subprocess.run(["python", "unity_skills.py", "ui_set_text", "--stdin-json"], input=b'{"name":"T","text":"设置"}', capture_output=True)` 调用
- **WHEN** 子进程退出
- **THEN** `returncode` MUST 为 0
- **AND** stdout MUST 含 server 端响应 `"status":"ok"`
- **AND** Prefab YAML 中 `m_text:` MUST 等于 `"设置"`

### Requirement: 修复 MUST NOT 改动 13-fix-broken-prefabs 已修的 4 个 Prefab

修复过程 MUST 仅修改 `unity_skills.py` + SKILL.md + CLAUDE.md + 测试目录文件。MUST NOT 触碰 `Assets/Resources/Prefab/UI/Settings.prefab` / `SelfTattoo.prefab` / `ThreeChoice.prefab` / `TattooEnchant.prefab`。验收脚本写 Prefab 时 MUST 使用临时副本目录（`/tmp/` 或 `tests/regression/temp_prefabs/`），完成后 MUST 删除副本。

#### Scenario: 13 已修 Prefab mtime 未被本期改动

- **GIVEN** 14-mcp-encoding-fix 全部任务完成
- **WHEN** 比较 13 已修的 4 个 Prefab 修复前后 mtime
- **THEN** mtime MUST 不变
- **AND** YAML 内容 MUST 不变
- **AND** `grep U+FFFD` 在 4 个 Prefab 上 MUST 仍返回 0

### Requirement: SKILL.md 与 CLAUDE.md MUST 文档化 CJK 参数调用约定

`.claude/skills/unity-skills/SKILL.md` MUST 增加「中文 / CJK 参数调用约定」段，说明：

- 含 CJK / Latin-1 范围外字符的参数 MUST 使用 `--stdin-json` 模式（强制）
- argv 模式仅适用于 ASCII 参数（保守约定，即使 argv 解码修复已部署）
- MSYS / Cygwin / WSL Python 必须用 `--stdin-json`

`.claude/CLAUDE.md` §六.5 UI 制作子流程「强制约束 5 (Prefab 优先 MCP 自动建)」MUST 加备注引用 SKILL.md 调用约定。

#### Scenario: 后续会话读取 SKILL.md 能获得正确调用方式

- **GIVEN** 14 完成后，新 Claude 会话加载 unity-skills SKILL
- **WHEN** 该会话需要写中文文本到 Prefab
- **THEN** SKILL.md 内容 MUST 引导其使用 `--stdin-json` 模式
- **AND** 不会再退回到含 `text=中文` 的 argv 调用方式

### Requirement: 回归测试套件 MUST 覆盖 4 个 CJK 编码典型场景

`openspec/changes/14-mcp-encoding-fix/tests/regression/` 下 MUST 含至少 4 个测试 case，覆盖：

1. 短 CJK 字符串 `ui_set_text` round-trip
2. CJK GameObject 名字 round-trip
3. 长字符串（≥200 字符）+ emoji + 标点 round-trip
4. ASCII + CJK 混合字符串 round-trip（如 `"Settings 设置 ▶ 音量"`）

所有 case MUST 跑通后 `tests/results.md` MUST 记录 pass。

#### Scenario: 回归测试套件全部通过

- **GIVEN** Phase 4 任务全部完成
- **WHEN** 跑 `pytest tests/regression/`
- **THEN** 4 个测试 case MUST 全部 pass
- **AND** `tests/results.md` MUST 含 pass 证据 + 执行时间戳

### Requirement: 修复 MUST 通过端到端复现实验证明根因

Phase 0 复现实验 MUST 在 `tests/repro/` 下产出至少 3 个脚本：
- `repro_argv_corruption.py`：证明 argv 路径下 cp936 mojibake 100% 复现
- `repro_stdin_clean.py`：证明 stdin pipe 路径下 100% 干净
- `repro_via_unity_skills.py`：真实场景端到端复现

复现日志 + hex dump MUST 汇总到 `tests/repro/repro-log.md`，含 `chcp 65001` vs `chcp 936` 对比 + `PYTHONIOENCODING` 设置/未设置对比。

#### Scenario: 复现日志含字节码铁证

- **GIVEN** Phase 0 全部任务完成
- **WHEN** 检查 `repro-log.md`
- **THEN** 文件 MUST 含至少 1 段 hex dump 对比（argv vs stdin 同一中文字符串的字节流差异）
- **AND** MUST 含 `chcp` 切换前后的实验对比
- **AND** MUST 显式记录 mojibake 100% 复现的命中率
