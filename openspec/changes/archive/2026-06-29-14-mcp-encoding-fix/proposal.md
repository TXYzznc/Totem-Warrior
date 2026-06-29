## Why

`unity-skills` SKILL（Python `unity_skills.py` → HTTP 8090/8091 → Unity Editor 内 `SkillsHttpServer`）在 Windows 上**经主对话 / Codex agent 传中文参数时**，参数会在 `sys.argv` 解码环节被 cp936 截成单字节字符，到达 Unity 时已退化为 mojibake，最终写入 Prefab YAML 时被 Unicode 规范化为 `U+FFFD (`�`)` REPLACEMENT 字符。

13-fix-broken-prefabs 的 30 分钟 spike 已得到铁证（详见归档 `art/raw/mcp-spike-report.md`）：

- **client 端 / server 端代码全链路编码正确**：`json.dumps(ensure_ascii=False)` + `.encode('utf-8')` + `Content-Type: application/json; charset=utf-8` + `Encoding.UTF8.GetBytes`，零 ASCII fallback
- **唯一漏洞在 CLI argv**：`unity_skills.py:307` `for arg in sys.argv[2:]` 直接吃 `sys.argv`，而 Windows Python 在默认 cp936 code page 下 argv 通过 ANSI → Unicode 转换，CJK 字符若不在 ANSI 表内会被替换为 `?` 或断码
- **取证**：Settings.prefab 经 MCP 写入后变成 ASCII-only 文件（`file` 命令识别），出现 `ȡ` / `ͣ` / `Ƴ` / `ƶ` 等 Latin-1 残片，符合"UTF-8 字节流被按单字节 char 读入"的特征签名

**不修的代价**：

- 所有未来 UI Prefab 自动生成链路都得「手 Edit YAML 绕开 MCP」，13 的「一交付即返工」会反复发生
- v21 / UIForm 业务等阶段如果调 MCP 写中文，再次产生大量返工
- CLAUDE.md §六 UI 制作子流程第 5 阶段「Prefab 优先 MCP 自动建」实际不可用

**本期范围**：端到端复现实验 + 多重保险加固（argv 解码 + stdin JSON 兜底 + SKILL.md 调用约定）+ 回归测试套件 + 文档更新。**不重做已污染的 4 个 Prefab**（13 已修），但要保证修复后再用 MCP 写它们不会重新破坏。

**用户要求**：本 change 由另一 Claude 会话执行，本文档必须**自含**（不依赖外部上下文也能开干）。

## What Changes

- **Phase 0 复现实验**：建 `tests/repro/` 子目录写最小复现脚本，证明「同一份中文输入 → 经 Codex agent → unity_skills.py argv → mojibake」可复现 100%；产出 `tests/repro/repro-log.md`
- **Phase 1 argv 解码加固**：`unity_skills.py` 入口加 Windows `GetCommandLineW` 调用 + UTF-16 解析 argv，绕开 ANSI 转换层；保留原行为兼容非 Windows
- **Phase 2 stdin JSON 模式**：`unity_skills.py` 增加 `--stdin-json` 模式，所有参数从 stdin 读 JSON body；SKILL.md 推荐用 `--stdin-json` 调用 CJK 任务
- **Phase 3 SKILL 文档更新**：`.claude/skills/unity-skills/SKILL.md` 增加「中文 / CJK 参数调用约定」段（必须 `--stdin-json` + 仅 ASCII 用 argv）；CLAUDE.md §六.5 加备注
- **Phase 4 回归测试**：`tests/regression/` 写 4 个 case 覆盖 ui_set_text / gameobject_create_with_chinese / prefab_text_write / round-trip 读写一致；CI 跑通
- **Phase 5 验收**：用 13 修过的 4 个 Prefab 作为 ground truth；MCP 写入 → 读出 → 与 ground truth 中文 diff 应为 0
- **不做**：
  - 不重做 13 已修的 4 个 Prefab（13 已归档完成）
  - 不动 Unity Editor 端 `SkillsHttpServer.cs` 等 C# 代码（spike 已证 server 侧无 bug）
  - 不改 Codex CLI 本身（不在 repo 内）
  - 不在本 change 重写 SKILL 路由 / 注册系统
