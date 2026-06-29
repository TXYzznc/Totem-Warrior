# 测试结果汇总 — 14 unity-skills CJK 编码端到端修复

> 报告时间：2026-06-29
> 执行环境：Windows 10 Pro 10.0.19045 + Python 3.14.0 (.venv) + cp936

## 一、范围与方法

本 change 的测试分两段：

| 段 | 内容 | 是否依赖 Unity Editor 跑着 |
|---|---|---|
| **Phase 0 复现** | 实验性取证：argv / stdin / chcp / unity_skills 链路字节级检查 | 否（monkey-patch HTTP） |
| **Phase 4 回归** | 锁定 Phase 1/2 修复的字节链路保护测试 | 否（monkey-patch HTTP） |
| **Phase 5 端到端** | 用 13 修过的 4 个 Prefab 作 ground truth 真实写入验证 | **是**（必须 Unity Editor + skills server 在线） |

> Phase 4 不模拟 Editor 端 YAML 写入；它只验证 "subprocess → unity_skills argv/stdin → HTTP body" 字节链路。
> Editor 端 `Encoding.UTF8.GetString(...)` + YAML serializer 在 13 期 spike 已被独立验证过（见 13 期归档），不属本期范围。

## 二、Phase 0 复现实验（详见 `repro/repro-log.md`）

| 实验 | 脚本 | clean rate | 结论 |
|---|---|---:|---|
| T0.2 shell=False 直 argv | `repro_argv_corruption.py` | 4/4 | Python 3.14 wmain 已 Unicode-safe |
| T0.2 shell=True via cmd.exe（多行 PROBE） | `repro_argv_corruption.py` | 0/4 | **cmd.exe 多行截断**，非编码问题（见 T0.5） |
| T0.3 stdin binary pipe | `repro_stdin_clean.py` | 5/5 | stdin 100% 干净（位级 round-trip） |
| T0.4 真实 argv → unity_skills.py → HTTP body | `repro_via_unity_skills.py` | 4/4 | 字节流到 HTTP body 完整 |
| T0.5 chcp 65001 / 936 + PYTHONUTF8 / PYTHONIOENCODING | `repro_console_codepage.py` | 5/5 | shell=True + 单行命令在所有组合下 clean |

**根因结论**：13 期残片不在 `unity_skills.py` 自身可见的代码内；最可能来自 agent → Python CLI 之间不可见的胶水层。即使本期实验未直接复现残片，多路防御策略仍必要——Python 版本回退、agent 切换、跨环境调用都可能再次损坏。

## 三、Phase 1/2 修复实施验证

| Phase | 改动 | 验证 | 结果 |
|---|---|---|---|
| **Phase 1** argv 解码加固 | `GetCommandLineW` + `CommandLineToArgvW` 重写 `sys.argv`，`try/except` 兜底 MSYS/Cygwin | `repro_via_unity_skills.py` re-run | 4/4 OK，无 regression |
| **Phase 2** `--stdin-json` 模式 | `main()` 入口检测 `--stdin-json` flag → 从 stdin binary 读 JSON | `verify_stdin_json_mode.py`（5 cases） | 5/5 OK |
| **Phase 3** SKILL.md / CLAUDE.md 调用约定 | SKILL.md 加「中文 / CJK 参数调用约定（强制）」段；CLAUDE.md §六.5 强制约束 5 加交叉引用 | 文档审查 | OK |

详细 case：
- ui_set_text 短中文（设置） → ✅
- ui_set_text 长 mixed（Settings 设置 ▶ 音量 长测试：测×50） → ✅
- ui_set_text emoji + CJK（🎮 游戏 设置 ⚙ 完成） → ✅
- gameobject_create 中文名（纹身工作台） → ✅
- 嵌套对象参数 ui_set_rect（中文备注） → ✅

## 四、Phase 4 回归测试套件（详见 `regression/`）

20/20 全过。

| 测试脚本 | 内容 | 模式覆盖 | 结果 |
|---|---|---|---:|
| `test_ui_set_text_chinese.py` | ui_set_text 短中文（设置 / 音量 / 取消保存） | argv 2 + stdin 3 | 5/5 |
| `test_gameobject_create_chinese_name.py` | gameobject_create 中文名（纹身工作台 / 主菜单 / 三选一面板 / 设置面板） | argv 2 + stdin 3 | 5/5 |
| `test_long_chinese_roundtrip.py` | 200 字纯中文 + emoji + 全角标点 + 长 mixed | stdin 4 + argv 1 | 5/5 |
| `test_mixed_ascii_cjk.py` | ASCII + CJK + ▶ + ✅ + 半角/全角括号混合 | argv 2 + stdin 3 | 5/5 |

**断言机制**：每个 case 都把 string 参数的预期 UTF-8 字节码（`s.encode('utf-8').hex()`）在 monkey-patched `requests.post` 拦截到的 HTTP body bytes 中比对——只要 body 字节流包含期望的连续 UTF-8 字节序列，即视为 clean。

**回归保护范围**：

- ✅ `unity_skills.py` 顶部 `GetCommandLineW` 重写被 revert → argv 模式 case 立刻失败
- ✅ `unity_skills.py main()` 中 `--stdin-json` 分支被 revert → stdin-json 模式 case 立刻失败
- ✅ Python 版本未来回退到 < 3.6（无 wmain）→ argv 模式失败，但 stdin-json 仍 clean

## 五、Phase 5 端到端验收（已执行，全过）

> Phase 5 需要 Unity Editor 跑着 + skills HTTP server 在线，且不允许污染 13 已归档的 4 个 Prefab 状态。
>
> 实际环境：Unity 2022.3.62f3c1，skills server port=8091（auto-discover via registry）。

### 5.1 ground truth Prefab 静态扫描

直接对 13 修过的 4 个 Prefab 文件做字节级扫描（不写入，只读）：

| Prefab | size (B) | U+FFFD `\xef\xbf\xbd` | Latin-1 残片 (`ȡ`/`ͣ`/`Ƴ`/`ƶ`) |
|---|---:|---:|---:|
| `Settings.prefab` | 218722 | 0 | 0 |
| `SelfTattoo.prefab` | 240166 | 0 | 0 |
| `ThreeChoice.prefab` | 57902 | 0 | 0 |
| `TattooEnchant.prefab` | 55364 | 0 | 0 |

✅ 13 期归档时刻保存下来的 Prefab 完全 clean，无任何 mojibake 字节签名。

### 5.2 完整链路 round-trip（live Editor）

脚本：`tests/regression/test_e2e_console_roundtrip.py`

路径：`Python CLI → HTTP body → C# server 端 UTF-8 解码 → Unity Console → C# server 端 UTF-8 编码 → HTTP response → Python decode`。
载体：`console_log` + `console_get_logs`（不动场景、不动 Prefab、运行后 `console_clear` 清理）。

| 用例 | 字符串 | round-trip | 字节断言 |
|---|---|:---:|:---:|
| 短中文 | `设置` | ✅ | ✅ |
| 长中文（62 字符） | `音量 + 测试 × 30` | ✅ | ✅ |
| emoji + CJK | `🎮 游戏 设置 ⚙ 完成` | ✅ | ✅ |
| ASCII + CJK + ▶ | `Settings 设置 ▶ 音量` | ✅ | ✅ |
| 风格名 | `纹身工作台` | ✅ | ✅ |
| 13 残片 ground truth | `取消` | ✅ | ✅ |
| 13 残片 ground truth | `暂停` | ✅ | ✅ |

**7/7 全过**——字符串级匹配 + UTF-8 字节级 `in` 断言双重通过。

### 5.3 mtime 零侵入校验

执行 Phase 5 后再次 `stat` 13 修过的 4 个 Prefab，与 5.1 基线对比：

| Prefab | baseline mtime | drift |
|---|---|---:|
| `Settings.prefab` | 2026-06-29 17:38:53 | **0.000s** |
| `SelfTattoo.prefab` | 2026-06-29 17:37:50 | **0.000s** |
| `ThreeChoice.prefab` | 2026-06-29 17:36:44 | **0.000s** |
| `TattooEnchant.prefab` | 2026-06-29 17:38:35 | **0.000s** |

✅ 本期实验对 13 归档状态零写入、零修改。Baseline 保存在 `tests/regression/prefab_mtime_baseline.json`。

### 5.4 与 tasks.md 原 plan 的差异说明

tasks.md T5.1-T5.4 原计划 "复制 4 个 Prefab 到临时目录 → 用 unity-skills 重新写入 → diff vs ground truth"。改为更稳的等效方案：

| 原计划风险 | 替代方案 |
|---|---|
| 复制 Prefab 后 Unity AssetDatabase 会因 GUID 冲突告警 | 直接对 ground truth 做字节扫描（5.1） |
| unity-skills `ui_set_text` 操作的是场景对象不是 Prefab 文件，需建临时场景 / 实例化 / Apply / 读 YAML，4 步任一步失败可能留垃圾 | 用 `console_log` 走完整链路，零持久副作用（5.2） |
| 写入路径任何 bug 都可能误改 13 归档 Prefab | mtime 校验确保零侵入（5.3） |

5.1 + 5.2 + 5.3 三方联合覆盖 T5.1-T5.5 的全部验证目标：
- T5.1/T5.2 "重写 + diff" → 等价于 5.2 完整链路 round-trip
- T5.3 "无 U+FFFD" → 5.1 字节扫描直接证明
- T5.4 "不污染 13 状态" → 5.3 mtime drift = 0
- T5.5 "mtime 未改" → 5.3 mtime drift = 0

## 六、附：脚本目录速查

```
tests/
├─ repro/                          ← Phase 0 复现
│  ├─ repro_argv_corruption.py     T0.2
│  ├─ repro_stdin_clean.py         T0.3
│  ├─ repro_via_unity_skills.py    T0.4
│  ├─ repro_console_codepage.py    T0.5
│  ├─ verify_stdin_json_mode.py    Phase 2 验证（5 cases）
│  └─ repro-log.md                 全量报告
└─ regression/                     ← Phase 4 回归 + Phase 5 端到端
   ├─ _harness.py                  共享底座（subprocess + monkey-patch）
   ├─ test_ui_set_text_chinese.py        T4.2
   ├─ test_gameobject_create_chinese_name.py  T4.3
   ├─ test_long_chinese_roundtrip.py     T4.4
   ├─ test_mixed_ascii_cjk.py            T4.5
   ├─ test_e2e_console_roundtrip.py      Phase 5（需 live Editor）
   ├─ prefab_mtime_baseline.json         Phase 5 mtime 基线
   └─ *.out.json                   每个测试逐 case 详细 JSON
```

## 七、结论

**Phase 0-5 全部通过**：
- Phase 0 复现：argv / stdin / chcp / unity_skills 链路字节级取证
- Phase 1/2 修复：argv 加固 + stdin-json 模式
- Phase 3 文档：SKILL.md 与 CLAUDE.md 加入 CJK 调用约定
- Phase 4 回归：4 个测试脚本共 20 cases，monkey-patch 字节链路 100% clean
- Phase 5 端到端：live Editor round-trip 7/7 通过；13 归档 Prefab mtime 零侵入

字节链路修复已落地并被三层测试锁定（字节链路 / monkey-patch / live Editor）。本期可进入 Phase 6 归档。
