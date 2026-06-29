# Phase 0 复现日志 — unity-skills CJK argv 编码

> **实验环境**
> - Windows 10 Pro 10.0.19045
> - Python 3.14.0 (.venv，Oct 7 2025 build)
> - cp936 default console code page（`GetACP() = 936`，`GetOEMCP() = 936`）
> - locale: cp936，sys.getfilesystemencoding: utf-8
> - shell: bash (Git for Windows)
>
> **测试字符串集**：`设置`、`音量`、`纹身工作台`、`Settings 设置 ▶ 音量`、200 字符 `测`

## 1. 结果总览

| 实验 | clean rate | 结论 |
|---|---:|---|
| T0.2 `shell=False` 直 argv | 4/4 | Python 3.14 wmain 已经 Unicode-safe |
| T0.2 `shell=True` via cmd.exe | 0/4 | **cmd.exe 拼接多行字符串截断**（不是编码问题，见 §3 分析） |
| T0.3 stdin binary pipe | 5/5 | stdin pipe 100% 干净 |
| T0.4 真实 argv → unity_skills.py → HTTP body | 4/4 | 字节流到 HTTP body 完整保留 UTF-8 |
| T0.5 chcp 65001 / 936 / PYTHONUTF8 / PYTHONIOENCODING | 5/5 | **shell=True + 单行命令在所有环境组合下都 clean** |

## 2. 详细取证

### T0.2 argv corruption（`repro_argv_corruption.py`）

```
Test string: '设置'  UTF-8 hex: e8 ae be e7 bd ae
  shell=False (CreateProcessW direct):  rc=0  argv[1]='设置'   OK
  shell=True  (via cmd.exe + multi-line PROBE):  rc=0  argv[1]=<stdout空>   CORRUPT

Test string: '纹身工作台'  UTF-8 hex: e7 ba b9 e8 ba ab e5 b7 a5 e4 bd 9c e5 8f b0
  shell=False:  argv[1]='纹身工作台'   OK
  shell=True :  <stdout空>            CORRUPT

Summary: shell=False 4/4 OK, shell=True 4/4 stdout 空
```

**注**：`shell=True` 失败的根因经 T0.5 排除编码问题——是 multi-line PROBE_CODE（含 `\n` 和 `{` 等字符）经过 `subprocess.list2cmdline` + cmd.exe 解析时被截断。**不是 ANSI argv 转换 bug**。

### T0.3 stdin pipe clean（`repro_stdin_clean.py`）

```
'设置'                  bytes= 6  OK   echoed_hex == expected_hex (e8 ae be e7 bd ae)
'音量'                  bytes= 6  OK
'纹身工作台'             bytes=15  OK
'Settings 设置 ▶ 音量'  bytes=26  OK (含 e2 96 b6 = U+25B6 ▶)
'测'*200                bytes=600 OK   long-string round-trip

Summary: 5/5 stdin pipe clean
```

**关键**：stdin binary pipe **完全绕开任何 ANSI / Unicode 转换**，从发送方 `input=b'...'` 到子进程 `sys.stdin.buffer.read()` 字节流位级一致。

### T0.4 真实 argv → unity_skills.py → HTTP body（`repro_via_unity_skills.py`）

通过 monkey-patch `requests.post` 拦截 HTTP body，验证 `subprocess argv → sys.argv → params dict → json.dumps → encode('utf-8')` 全链路。

```
cmd: name=TitleText text=设置
  argv 重组: [... | ui_set_text | name=TitleText | text=设置]
  HTTP body : {"name": "TitleText", "text": "设置", "verbose": false}
  body 含期望 hex (e8 ae be e7 bd ae) : OK

cmd: name=TabBtn text=纹身工作台
  HTTP body : {"name": "TabBtn", "text": "纹身工作台", "verbose": false}
  body 含期望 hex (e7 ba b9 e8 ba ab e5 b7 a5 e4 bd 9c e5 8f b0) : OK

Summary: 4/4 argv → HTTP body clean
```

**结论**：当前 Python 3.14 + 主对话 `subprocess.run(..., shell=False)` 路径下，**unity_skills.py argv 全链路已经 clean**。

### T0.5 chcp / PYTHONIOENCODING 对比（`repro_console_codepage.py`）

单行 PROBE_CODE 经 `subprocess.list2cmdline` + `shell=True` 跑 cmd.exe，前置加 chcp 切换 / 环境变量切换：

```
[baseline (默认 cp936, 无 env)]            OK   argv=["设置音量"] hex="e8 ae be e7 bd ae e9 9f b3 e9 87 8f"
[chcp 65001 (切 UTF-8)]                    OK   argv=["设置音量"]
[PYTHONUTF8=1]                             OK
[PYTHONIOENCODING=utf-8]                   OK
[chcp 65001 + PYTHONUTF8=1]                OK
```

**说明**：单行命令在 cmd.exe 不会截断，argv 在所有 console code page / Python env 组合下都正确。

## 3. 根因再分析（结合 13 spike）

### 13 期 Prefab 残片的来源

13 期 spike 在 `Settings.prefab` 等 4 个 Prefab YAML 上观察到：
- `m_text: "����"`（U+FFFD 替换字符）
- `m_text: "ȡ��"`（Latin-1 ȡ = U+0221）
- `m_text: "��ͣ"`（U+0363 ͣ）
- 整个 prefab 文件 `file` 命令识别为 "ASCII text"（无任何 ≥0x80 字节）

这些是 "UTF-8 字节流按 Latin-1 / Windows-1252 当单字节读" 的典型签名。

### 当前实验不复现的可能原因

| 假说 | 评估 |
|---|---|
| 当时 Python < 3.6（无 wmain） | Python 3.6 (2016 年) 起 Windows argv 已用 wmain；除非用了非常老的 embeded Python，否则不可能 |
| Codex agent 内部用 CreateProcessA + ANSI argv | 最可能。Codex 的 shell 调用胶水层不在本仓库，无法直接探针 |
| 某个上游中间件 `data.encode('latin-1')` 重编了一遍 | 与残片特征吻合（Latin-1 单字节读 UTF-8） |
| Unity Editor 端 PathPipe / Domain Reload 残留 | 13 spike 已排除——server 端 `Encoding.UTF8` 全链路对 |

**根因不在 unity_skills.py 内部代码**，**在 Codex/Claude agent → Python CLI 的胶水层**——这一段我们看不见、也改不了。

### 为什么仍要实施修复

即使当前 Python 3.14 下手动跑 `python unity_skills.py ui_set_text text=设置` clean，**新 Claude 会话 / Codex agent / 不同 Python 版本可能再次损坏**。修复策略：

1. **路径 A（GetCommandLineW + CommandLineToArgvW）**：冗余但无害。在 Python 3.14 wmain 上 effectively no-op；保护任何 < 3.6 或编译 flag 异常的 Python 环境。
2. **路径 B（`--stdin-json` 模式）**：100% 编码安全的入口。stdin pipe 完全绕开任何 ANSI / argv / shell 解析层。**这是真正的根本性保障**。
3. **路径 C（SKILL.md 调用约定）**：让所有调用方（Claude 主对话 / Codex agent / qa 测试脚本）统一走 `--stdin-json`——文档化的契约比代码 hardening 更重要。

## 4. 实验输出文件

| 文件 | 用途 |
|---|---|
| `repro_argv_corruption.py` + `.out.json` | T0.2 argv 路径对比 |
| `repro_stdin_clean.py` + `.out.json` | T0.3 stdin pipe 干净性 |
| `repro_via_unity_skills.py` + `.out.json` | T0.4 真实 unity_skills.py argv → HTTP body |
| `repro_console_codepage.py` + `.out.json` | T0.5 chcp / PYTHONIOENCODING 切换对比 |
| `repro-log.md` (本文) | 汇总报告 + 字节码取证 + 结论 |

## 5. 进入 Phase 1/2/3 决策

✅ Phase 1 argv 解码加固（路径 A）：实施。冗余但 belt-and-suspenders。
✅ Phase 2 stdin-json 模式（路径 B）：**必须实施**——唯一 100% 安全入口。
✅ Phase 3 SKILL.md 调用约定（路径 C）：**最重要**——文档化契约，杜绝下游调用方走 argv。
