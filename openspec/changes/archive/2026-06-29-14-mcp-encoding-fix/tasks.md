# 任务清单 — 14 unity-skills CJK 编码端到端修复

> 顺序：Phase 0 复现 → Phase 1-3 修复（可并行）→ Phase 4 回归 → Phase 5 文档 → Phase 6 归档
>
> 执行说明：本 change 交由另一 Claude 会话执行。开干前先把 [proposal.md](./proposal.md) + [design.md](./design.md) 通读一遍。

## Phase 0 — 端到端复现实验

- [x] T0.1 在 `tests/repro/` 下建实验目录；准备一个含 CJK 中文的测试字符串集（"设置" / "音量" / "纹身工作台" / "▶ 音量"）
- [x] T0.2 写 `repro_argv_corruption.py`：启动子进程 `python -c "import sys; print(sys.argv[1].encode('unicode_escape'))"` 传中文 argv，捕获 stdout；预期：cp936 系统下输出含 `\xc9\xe8\xd6\xc3` 等 GBK 字节码而非 UTF-8
- [x] T0.3 写 `repro_stdin_clean.py`：同样测试字符串，通过 stdin binary pipe 传给子进程，断言 stdin 路径 100% 不损坏
- [x] T0.4 写 `repro_via_unity_skills.py`：真实场景调用 `python unity_skills.py ui_set_text name=X text=测试`（需 Editor 跑着），观察 server 端 log 收到的字符串字节流
- [x] T0.5 把 T0.2-T0.4 的输出（含 hex dump）汇总到 `tests/repro/repro-log.md`；含 chcp 65001 vs chcp 936 对比；含 PYTHONIOENCODING 设置/未设置对比

## Phase 1 — argv 解码加固（路径 A）

- [x] T1.1 备份 `.claude/skills/unity-skills/scripts/unity_skills.py` 到 `unity_skills.py.bak-14`
- [x] T1.2 在文件最前（现有 `if sys.platform == 'win32'` 块之后）加 `GetCommandLineW + CommandLineToArgvW` 调用，重写 `sys.argv`
- [x] T1.3 `try/except OSError` 兜底；MSYS/Cygwin/WSL Python 跳过该分支，保持原 argv
- [x] T1.4 验证：跑 Phase 0 的 `repro_argv_corruption.py`，断言修复后 argv 含正确 UTF-8 字节
- [x] T1.5 验证：跑 `python unity_skills.py ui_set_text name=TitleText text=设置` 在新临时 Prefab 上，读 YAML 断言 `m_text: "设置"`

## Phase 2 — stdin JSON 模式（路径 B）

- [x] T2.1 在 `unity_skills.py main()` 入口加 `--stdin-json` 分支：检测到则从 stdin 读 JSON body，跳过 argv 解析
- [x] T2.2 兼容：`--stdin-json` 可与 `skill_name` 在 argv 同存（skill_name 是 ASCII 安全）
- [x] T2.3 写示例：`echo '{"name":"TitleText","text":"设置"}' | python unity_skills.py ui_set_text --stdin-json`，断言通过
- [x] T2.4 写示例：从 Python 调用方角度 `subprocess.run(['python', 'unity_skills.py', 'ui_set_text', '--stdin-json'], input=json.dumps({...}).encode('utf-8'))`
- [x] T2.5 验证：复杂 case（200 字符中文 + emoji + 多字段）round-trip 一致

## Phase 3 — SKILL.md 调用约定更新（路径 C）

- [x] T3.1 在 `.claude/skills/unity-skills/SKILL.md` 添加「中文 / CJK 参数调用约定」段（见 design.md §3 路径 C 模板）
- [x] T3.2 在 SKILL.md 同段加「已知限制」：MSYS/Cygwin/WSL Python 必须用 `--stdin-json`
- [x] T3.3 在 `.claude/CLAUDE.md` §六.5 (UI 制作子流程「强制约束 5」) 加备注：「unity-skills 写 CJK 必须 stdin-json 模式」

## Phase 4 — 回归测试套件

- [x] T4.1 建 `tests/regression/` 子目录
- [x] T4.2 写 `test_ui_set_text_chinese.py`：写「设置」到临时 Prefab → 读 YAML → 断言 `m_text: "设置"`
- [x] T4.3 写 `test_gameobject_create_chinese_name.py`：建中文名 GameObject → 读 prefab `m_Name:` 断言
- [x] T4.4 写 `test_long_chinese_roundtrip.py`：200 字符中文 + emoji + 标点 round-trip
- [x] T4.5 写 `test_mixed_ascii_cjk.py`：`"Settings 设置 ▶ 音量"` 混合字符串 round-trip
- [x] T4.6 全部跑过 → 写 `tests/results.md`

## Phase 5 — 验收（用 13 的 4 个 Prefab 作 ground truth）

- [x] T5.1 在一个**临时副本目录**复制 13 修过的 4 个 Prefab（Settings/SelfTattoo/ThreeChoice/TattooEnchant）
- [x] T5.2 用修复后的 unity-skills MCP 重新写入它们的所有中文文本字段（按 ground truth 内容）
- [x] T5.3 diff 临时副本 vs 13 已修 Prefab：所有中文字段 MUST 完全一致；`m_text:` 字段 MUST NOT 出现 `U+FFFD`
- [x] T5.4 删除临时副本（不污染 13 已归档的状态）
- [x] T5.5 验证 13 已修 Prefab mtime 未被本期实验改动

## Phase 6 — 文档与归档

- [x] T6.1 写 `tests/results.md` 汇总：Phase 0 复现日志 + Phase 4 回归通过 + Phase 5 ground truth diff
- [x] T6.2 `openspec validate 14-mcp-encoding-fix --strict`
- [x] T6.3 `openspec archive 14-mcp-encoding-fix --yes`
- [x] T6.4 同步更新 `项目知识库（AI自行维护）/INDEX.md`：active → archive 迁移 + 新增 spec sink 链接 + 在 wiki 加条目说明「unity-skills CJK 修复落地」

## 附录 — 完成本 change 后留给后续工作的接口承诺

- `unity_skills.py` 提供两条调用入口：
  1. argv 模式（兼容旧调用 + Windows native Python 修复后无 bug）
  2. `--stdin-json` 模式（**所有 CJK 任务推荐**；唯一保证 100% 编码正确的入口）
- SKILL.md 内含调用约定，主对话 / Codex agent / 任何 subagent 必须遵守
- 修复完成后 CLAUDE.md §六.5 强制约束 5 可以恢复为「Prefab 优先 MCP 自动建」无附加条件
