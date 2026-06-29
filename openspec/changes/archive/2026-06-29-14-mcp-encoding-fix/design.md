# 设计文档 — 14 unity-skills CJK 编码端到端修复

## 1. 范围与目标

修复 `unity-skills` SKILL 在 Windows 默认 cp936 code page 下传中文参数被 mojibake 的 bug。**目标**：用 MCP 写中文文本到 Prefab 后，YAML 中能正确显示 UTF-8 中文（无 `U+FFFD` REPLACEMENT 字符）。

**关键事实修正**（覆盖 13 期 spike）：

- `unity-skills` **是 SKILL 不是 MCP**——通过 Python 脚本 + HTTP 调 Unity Editor，`.mcp.json` 没有它
- 默认端口 8090；多实例时自动分配（用户实测 8091）
- 文件位置：
  - SKILL：`.claude/skills/unity-skills/SKILL.md`
  - Python 客户端：`.claude/skills/unity-skills/scripts/unity_skills.py`
  - Unity Editor server：`OutPackages/Unity-Skills-main/SkillsForUnity/Editor/Skills/SkillsHttpServer.cs`

## 2. 链路图与已知证据

```
[Claude 主对话生成中文 str]
            │
            ▼
[Codex agent / Claude Code CLI 启动 Python 子进程]
            │  ★ 此处 sys.argv 经 Windows ANSI(cp936) → Unicode 转换
            │  ★ CJK 字符不在 cp936 表内的字符 → '?' 或断码
            │  ★ 在 cp936 但被按 UTF-8 字节流读入 → Latin-1 残片（ȡ/ͣ/Ƴ/ƶ）
            ▼
[unity_skills.py main() — sys.argv[2:]]  ← BUG 入口
            │
            ▼
[json.dumps(kwargs, ensure_ascii=False).encode('utf-8')]  ← 已损坏的 str 编为 UTF-8
            │
            ▼
[HTTP POST → SkillsHttpServer.cs]  ← 收到的已经是错的
            │
            ▼
[Unity TMP_Text.text = mojibake]
            │
            ▼
[Prefab YAML 序列化：U+FFFD 被 Unity 规范化写出]
            │
            ▼
[m_text: "����"]
```

**取证（来自 13 期 spike 报告）**：

| 项 | 内容 |
|---|---|
| Settings.prefab `file` 识别 | "ASCII text, with CRLF line terminators"（无任何 ≥0x80 字节） |
| 残片字符 | `ȡ` (U+0221) / `ͣ` (U+0363) / `Ƴ` (U+01B3) / `ƶ` (U+01B6) |
| 残片特征 | UTF-8 字节流（0xE4-0xE9 区间）被按 Latin-1 / Win-1252 单字节读 |
| client 编码 | `unity_skills.py` 全链路 utf-8（line 84/87/88） |
| server 编码 | `SkillsHttpServer.cs` 全链路 UTF-8（line 325/326/865） |
| 唯一漏洞 | `unity_skills.py:307` `for arg in sys.argv[2:]` 直接吃 ANSI argv |

## 3. 修复路径（多重保险，全部实施）

### 路径 A — argv 解码加固（unity_skills.py 入口层）

**原理**：Windows 下用 `ctypes` 调 `GetCommandLineW` + `CommandLineToArgvW`，绕开 Python 默认的 ANSI → Unicode 转换。

**实施**：

```python
import sys
if sys.platform == 'win32':
    import ctypes
    from ctypes import wintypes
    GetCommandLineW = ctypes.windll.kernel32.GetCommandLineW
    GetCommandLineW.restype = wintypes.LPCWSTR
    CommandLineToArgvW = ctypes.windll.shell32.CommandLineToArgvW
    CommandLineToArgvW.argtypes = [wintypes.LPCWSTR, ctypes.POINTER(ctypes.c_int)]
    CommandLineToArgvW.restype = ctypes.POINTER(wintypes.LPWSTR)
    argc = ctypes.c_int(0)
    argv_w = CommandLineToArgvW(GetCommandLineW(), ctypes.byref(argc))
    sys.argv = [argv_w[i] for i in range(argc.value)]
```

放在 `unity_skills.py` 最前（在 `import requests` 之前，与现有 `sys.stdout = codecs.getwriter('utf-8')(...)` 一起）。

**优点**：透明、无需调用方改用法。
**风险**：依赖 Windows 私有 API；如果 Python 是 mingw / Cygwin / WSL 子系统 → 跳过该分支保持兼容。

### 路径 B — stdin JSON 模式（参数从 stdin 读）

**原理**：彻底绕开 argv。`unity_skills.py` 增加 `--stdin-json` 模式，参数从 stdin 读 JSON。

**实施**：

```python
# CLI 接口示例
# python unity_skills.py ui_set_text --stdin-json
# < {"name": "TitleText", "text": "设置"}

if '--stdin-json' in sys.argv:
    params = json.loads(sys.stdin.read())
    skill_name = sys.argv[1]
    result = call_skill(skill_name, **params)
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return
```

调用方（Claude/Codex）应使用 `subprocess.run([..., '--stdin-json'], input=json.dumps(params).encode('utf-8'))`，stdin 是 binary pipe 不走 ANSI。

**优点**：100% 不受 argv 编码影响；future-proof。
**风险**：调用方需要改用法；旧脚本兼容性。

### 路径 C — SKILL.md 调用约定

`.claude/skills/unity-skills/SKILL.md` 增加：

```markdown
## 中文 / CJK 参数调用约定（强制）

任何参数含 CJK / Emoji / Latin-1 范围外字符时，**必须**用 `--stdin-json` 模式调用：

✅ 正确：
  echo '{"name":"T","text":"设置"}' | python unity_skills.py ui_set_text --stdin-json

❌ 错误：
  python unity_skills.py ui_set_text name=T text=设置  # argv 编码 bug

主对话 / Codex agent 通过 `subprocess.run(..., input=..., text=False)` 走 binary pipe。
```

**优点**：文档侧防御，提醒所有调用方。

### 路径 D — 回归测试套件

`tests/regression/test_chinese_roundtrip.py`：

1. `ui_set_text` 写入「设置」→ 读取 prefab YAML → 断言 `m_text: "设置"`（精确匹配）
2. `gameobject_create` 名字含中文 → 读取 prefab → 断言名字 UTF-8 正确
3. 200 字符长中文文本 round-trip
4. 混合 ASCII + CJK + emoji round-trip

CI 跑通才能 merge。

## 4. 复现实验设计（Phase 0 必须先做）

```
tests/repro/
├─ repro_argv_corruption.py   # 启 Python 子进程传中文 argv，读取 sys.argv 看是否 mojibake
├─ repro_stdin_clean.py        # 同样数据走 stdin pipe，验证 100% 干净
├─ repro_via_unity_skills.py   # 真实场景：调 unity_skills.py ui_set_text 中文 → grep 输出 prefab
└─ repro-log.md                # 实验记录 + 日志输出 + 字节码 dump
```

**关键证据**：实验必须证明
- argv 路径 → 中文 mojibake（100%）
- stdin pipe 路径 → 中文 clean（100%）
- 修复后 argv 路径 → 中文 clean（100%）

## 5. 验收

| 门槛 | 标准 |
|---|---|
| 复现 | Phase 0 100% 复现 mojibake 现象，日志含字节码 dump |
| 修复后 argv | 中文 argv 100% 不再 mojibake（用 13 修过的 4 个 Prefab 作 ground truth） |
| 修复后 stdin | 中文 stdin JSON 100% 不 mojibake |
| 回归 | 4 个回归测试 case 全过 |
| 文档 | SKILL.md 调用约定段更新 + 主对话能看到 |
| 不破坏 | 13 已修的 4 个 Prefab YAML 不被本期工具反向覆盖（mtime 校验） |

## 6. 风险与不可控点

| 风险 | 缓解 |
|---|---|
| `GetCommandLineW` 在 Cygwin/MSYS/WSL Python 不可用 | `try/except OSError` 兜底走旧路径 + 在 SKILL.md 标注「这些环境必须用 `--stdin-json`」 |
| Codex agent 内部子进程 spawn 用 `shell=True` 走 cmd.exe | 测试 spawn 命令在 cp936 cmd 下能否传中文；不行则强制 `subprocess` API 用列表 + binary stdin |
| 多 Unity 实例端口分配（8090 vs 8091） | 不在本期范围，沿用现有 `_find_port_by_target` 逻辑 |
| 13 已修 Prefab 被本期实验脚本误改 | 复现脚本只读不写；回归测试写到 `/tmp/test_*.prefab` 临时目录 |

## 7. 不在本期范围

- ❌ Unity Editor 端 C# 代码改动（`SkillsHttpServer.cs` / `SkillRouter.cs` / `UISkills.cs` 等）
- ❌ Codex CLI 本体修改
- ❌ SKILL 注册 / 路由系统重构
- ❌ 重做 13 已修的 4 个 Prefab（已归档）
- ❌ 跨平台（macOS / Linux）回归——本 bug 仅 Windows
- ❌ 改 `.mcp.json`（unity-skills 不在那里）
