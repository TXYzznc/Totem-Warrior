---
change: 24-unity-skills-port-routing
---

# 设计：unity-skills 端口路由 — cwd 自动匹配

## 一、问题定位（Code Reading）

### 现有基础设施（不改）

- **`~/.unity_skills/registry.json`**：unity-skills package 已在服务器启动时按项目路径写入 `{id, name, path, port, pid, last_active, unityVersion}`。**本次只读不写**。
- **`UnitySkills.__init__(port, target, url)`**（[unity_skills.py:56-76](.claude/skills/unity-skills/scripts/unity_skills.py#L56-L76)）：已支持 3 种寻址方式，只是没人调用。
- **`_find_port_by_target(target)`**（[:78-96](.claude/skills/unity-skills/scripts/unity_skills.py#L78-L96)）：按 id 或 name 反查 registry，已实现。
- **`connect(port, target)`** 工厂（[:196-197](.claude/skills/unity-skills/scripts/unity_skills.py#L196-L197)）：已定义但 CLI 不用。

### 四个缺口（需改）

| 缺口 | 位置 | 症状 |
|---|---|---|
| G1: 全局单例落 8090 | `_default_client = UnitySkills()` [:158](.claude/skills/unity-skills/scripts/unity_skills.py#L158) | 无 target → 走 `DEFAULT_PORT = 8090` 兜底 |
| G2: `UNITY_URL` 全局硬编码 | `UNITY_URL = "http://localhost:8090"` [:46](.claude/skills/unity-skills/scripts/unity_skills.py#L46) | `get_skills()` [:294]、`health()` [:303] 都用它，绕开 `_default_client.url` |
| G3: CLI `main()` 无路由 argv | [:312-382](.claude/skills/unity-skills/scripts/unity_skills.py#L312-L382) | 无 `--target` / `--port` 参数解析，只能命中 `_default_client` 那个 8090 |
| G4: SKILL 文档硬编码 | `unity-skills/SKILL.md` 无路由章节；`playtest-driver/SKILL.md` 描述 + curl 用 `localhost:8091`（本项目实际 8090，历史遗留错值）；`unity-dev/references/unity-skills.md` 3 处 curl 用 `localhost:8090` | AI 读文档得到错误认知，即使代码修好也会被文档误导 |

## 二、寻址优先级链（客户端）

```
显式 port=  >  显式 target=  >  UNITY_SKILLS_TARGET env  >  cwd 匹配（新增）  >  8090 兜底 + warning
```

**语义**：
- 前 3 层是**显式覆盖**，用于跨项目 / SSH / CI 场景
- **cwd 匹配是主流场景**：AI 在项目根目录（或其任意子目录）工作，自动落到当前项目服务器
- **8090 兜底**保持向后兼容，但打 `stderr` warning 让开发者可见

**为什么 env > cwd**：SSH / CI 环境下可能 cwd 恰好在某项目路径下，但意图是调外部项目 → env 更明确的显式意图应优先。

## 三、cwd 匹配算法

```python
def _find_port_by_cwd() -> Optional[Tuple[int, str]]:
    """按 cwd 反查 registry；返回 (port, source_desc)；找不到返 None。"""
    registry = _load_registry()  # 提取现有 _find_port_by_target 里的 registry 读取
    if not registry:
        return None
    cwd = os.path.normcase(os.path.normpath(os.getcwd()))
    for path_key, entry in registry.items():
        proj_path = os.path.normcase(os.path.normpath(path_key))
        try:
            if os.path.commonpath([cwd, proj_path]) == proj_path:
                return entry.get('port'), f"cwd-match({entry.get('name', path_key)})"
        except ValueError:
            continue  # 不同盘符（Windows）或不可比较路径
    return None
```

**关键点**：
- **`os.path.normcase`**：Windows 大小写不敏感规范化（`D:\Proj` == `d:\proj`）；Linux/mac 是 no-op
- **`os.path.normpath`**：消除 `..` / 斜杠差异（`D:\a\b\..\c` → `D:\a\c`）
- **`os.path.commonpath([cwd, proj_path]) == proj_path`**：严格判定「cwd 是 proj_path 本身或子目录」，避免 `D:\proj` 误匹配 `D:\proj_backup`
- **多项目嵌套**：按 registry.json 里 `dict` 迭代顺序取第一个匹配。这是几乎不发生的极端情况，不做过度设计
- **不做健康检查**：不在此处对 port 发 `/health` GET，让首次真实调用直接遇到 `ConnectionError`（`UnitySkills.call` 已有明确报错文案，[:141-147](.claude/skills/unity-skills/scripts/unity_skills.py#L141-L147)）

## 四、代码改动清单

### 4.1 `unity_skills.py` 改动

**新增函数**（模块顶层，`get_registry_path()` 之后）：

```python
def _load_registry() -> Dict[str, Any]:
    """读 registry.json；文件不存在 / 损坏返 {}。"""
    reg_path = get_registry_path()
    if not os.path.exists(reg_path):
        return {}
    try:
        with open(reg_path, 'r', encoding='utf-8') as f:
            return json.load(f)
    except (OSError, json.JSONDecodeError):
        return {}

def _find_port_by_cwd() -> Optional[Tuple[int, str]]:
    ...  # 见 §三
```

**改造 `UnitySkills.__init__`**：把内部 `open(reg_path)` 逻辑收敛到 `_load_registry()`（`_find_port_by_target` 也复用）。

**改造 `_default_client` 初始化**（[:158](.claude/skills/unity-skills/scripts/unity_skills.py#L158) 附近）：

```python
def _build_default_client() -> UnitySkills:
    # 优先级：env > cwd > 8090
    env_target = os.environ.get('UNITY_SKILLS_TARGET')
    if env_target:
        try:
            return UnitySkills(target=env_target)
        except ValueError as e:
            sys.stderr.write(
                f"[unity-skills] UNITY_SKILLS_TARGET='{env_target}' 匹配失败: {e}，回退 cwd 匹配\n"
            )
    cwd_hit = _find_port_by_cwd()
    if cwd_hit:
        port, source = cwd_hit
        # 不需要 warning：cwd 匹配是预期行为
        return UnitySkills(port=port)
    sys.stderr.write(
        f"[unity-skills] cwd 未在 registry 匹配到 Unity 项目，回退 DEFAULT_PORT={DEFAULT_PORT}。"
        f"若为多项目并存请设 UNITY_SKILLS_TARGET 或用 --target。\n"
    )
    return UnitySkills()  # 走原有 DEFAULT_PORT 分支

_default_client = _build_default_client()
```

**改造 `get_skills()` / `health()`**（[:291-307](.claude/skills/unity-skills/scripts/unity_skills.py#L291-L307)）：从 `UNITY_URL` 改为 `_default_client.url`。

**改造 `health()` 返回**：从 `bool` 变为 `dict`，暴露目标端口便于排障：

```python
def health() -> Dict[str, Any]:
    """Check Unity server + 报告寻址结果。"""
    info = {
        'url': _default_client.url,
        'source': _resolve_source(),  # cwd-match(GameDesinger) / env(...) / default(8090)
    }
    try:
        response = requests.get(f"{_default_client.url}/health", timeout=2)
        info['ok'] = response.json().get("status") == "ok"
    except Exception as e:
        info['ok'] = False
        info['error'] = str(e)
    return info
```

`_resolve_source()` 用一个模块级 `_default_client_source` 变量在 `_build_default_client()` 里记录（"env"/"cwd-match(...)"/"default"）。

**改造 CLI `main()`**（[:312](.claude/skills/unity-skills/scripts/unity_skills.py#L312) 起）：

在 `sys.argv[1]` 判断之前，先 **pre-parse `--target` / `--port`** 并从 argv 中剔除：

```python
def _pre_parse_routing():
    global _default_client, _default_client_source
    override_target = None
    override_port = None
    remaining = []
    i = 1  # skip argv[0]=script path
    while i < len(sys.argv):
        arg = sys.argv[i]
        if arg == '--target' and i + 1 < len(sys.argv):
            override_target = sys.argv[i+1]; i += 2; continue
        if arg.startswith('--target='):
            override_target = arg.split('=', 1)[1]; i += 1; continue
        if arg == '--port' and i + 1 < len(sys.argv):
            override_port = int(sys.argv[i+1]); i += 2; continue
        if arg.startswith('--port='):
            override_port = int(arg.split('=', 1)[1]); i += 1; continue
        remaining.append(arg); i += 1
    sys.argv = [sys.argv[0]] + remaining
    if override_port is not None:
        _default_client = UnitySkills(port=override_port)
        _default_client_source = f"cli-port({override_port})"
    elif override_target is not None:
        _default_client = UnitySkills(target=override_target)
        _default_client_source = f"cli-target({override_target})"

def main():
    _pre_parse_routing()  # 必须在 argv 解析之前跑
    ...  # 保持原有逻辑
```

**usage help 里加**：`[--target=<name>] [--port=<num>] [--stdin-json]`。

### 4.2 `.claude/skills/unity-skills/SKILL.md`

**新增章节**（放在「中文 / CJK 参数调用约定」之前）：

```markdown
## 多项目路由（重要）

Unity Editor 服务器绑定的**端口是动态分配的**——用户同时开多个 Unity 项目时，第一个占 8090，第二个占 8091，以此类推。绝对**不要**硬编码端口。

### 寻址优先级（客户端）

| 优先级 | 方式 | 场景 |
|---|---|---|
| 1 | `python unity_skills.py <skill> --port=<num> ...` | 已经知道端口，直连 |
| 2 | `python unity_skills.py <skill> --target=<name-or-id> ...` | 跨项目调用，按项目名/ID |
| 3 | `UNITY_SKILLS_TARGET` 环境变量 | SSH / CI / 固定绑某项目 |
| 4 | **cwd 自动匹配**（默认） | AI 在项目根目录工作 → 自动落本项目 |
| 5 | `8090` 兜底 + stderr warning | cwd 不在任何项目下 |

### registry.json 机制

`~/.unity_skills/registry.json` 由 unity-skills package 服务端自动写入，客户端只读：

    { "<project-path>": {"id": "<Name_HASH>", "name": "<Name>", "port": 8090, "pid": ..., ...} }

### 排障

    python unity_skills.py health    # 打印当前 url + 寻址来源

输出示例：`{"url": "http://localhost:8091", "source": "cwd-match(GameDesinger)", "ok": true}`

### 跨项目调用

    python unity_skills.py editor_get_selection --target=OtherProject
```

### 4.3 `.claude/skills/playtest-driver/SKILL.md`

**改描述行**（第 3 行附近）：
- 旧：`依赖 InputModule.EnableSimulator + unity-skills MCP（REST localhost:8091）`
- 新：`依赖 InputModule.EnableSimulator + unity-skills CLI（端口由 registry 自动路由，不硬编码；详见 unity-skills SKILL「多项目路由」）`

**改所有 curl 示例**：
- 旧：`curl -X POST http://localhost:8091/skill/editor_execute_menu ...`
- 新：`python .claude/skills/unity-skills/scripts/unity_skills.py editor_execute_menu menu=Tools/Playtest/...`

（curl 示例统一改成 python CLI 调用，一致性优先；不保留 curl 语义）

### 4.4 `.claude/skills/unity-dev/references/unity-skills.md`

3 处硬编码 `localhost:8090`：
- 行 11 `Server endpoint: http://localhost:8090` → 改为 `Server endpoint: 端口动态分配，客户端自动路由，详见 unity-skills SKILL「多项目路由」`
- 行 587 / 590 / 593 三处 curl 示例改为 `python unity_skills.py ...` 调用

### 4.5 `.agents/skills/**` 镜像

跑 `python tools/sync-agents.py`（source of truth 是 `.claude/`）。

## 五、Trade-off 记录

### T1: 为什么不做健康检查？

选项：
- A（选）不检查：`_default_client` 初始化只查 registry，port 对应死进程时首次调用抛 `ConnectionError`
- B 检查：初始化时对 port 发 `/health` GET，失败换目标或落 8090

**选 A 的理由**：
- 每次 Python 启动都做 HTTP 请求会拖慢 CLI 启动 100-500ms
- registry.json 里 pid 死进程是低频场景（用户重启 Unity 会重写）
- `UnitySkills.call` 已有明确 `ConnectionError` 报错文案（"Unity may be recompiling scripts, wait 3-5s"）

### T2: 为什么用 cwd 而不是 project marker file？

选项：
- A（选）cwd → registry.path 反查
- B 从 cwd 往上找 `.unity_skills_project` 或 `ProjectSettings/ProjectVersion.txt`

**选 A 的理由**：
- registry.json 已经把 "project path" 权威登记，天然的 source of truth
- 不需要新建 marker 文件（避免污染项目）
- B 需要处理 Unity 项目的多种版本文件识别，复杂

### T3: 为什么保留 env 变量层？

用户已在 grill-me 明确要求保留。理由：
- 极低成本（`os.environ.get` 一行 + 5 行 fallback）
- SSH / CI 场景 cwd 可能是任意位置，env 是唯一稳定路由
- 不影响 cwd 自动匹配的主流场景（env 未设时不走这一层）

### T4: 为什么修 curl 示例改为 python 调用，而不是保 curl 加"从 registry 读 port"？

选项：
- A（选）全改成 `python unity_skills.py ...`
- B 保 curl 语义 + 加一段说明"先 `python unity_skills.py health` 拿 port 再 curl"

**选 A 的理由**：
- 用户在 grill-me 表态倾向前者
- python CLI 天然享受 cwd 路由 + `--target` / `--port` 覆盖，AI 抄示例即用
- curl 需要 AI 自己拼 URL，容易再次硬编码；用 python 就没这个诱惑

## 六、风险与兜底

| 风险 | 兜底 |
|---|---|
| Windows 上 `os.path.commonpath` 遇到不同盘符抛 `ValueError` | try/except 忽略并继续下一个 registry 条目 |
| registry.json JSON 损坏 | `_load_registry()` 返 `{}`，落 8090 + warning |
| cwd 恰好在两个项目路径下（嵌套） | 取 dict 迭代第一个匹配（不做警告，极端场景不设计） |
| 用户在项目路径下但服务器没启 | 首次 `call` 抛 `ConnectionError`，`UnitySkills.call` 已有明确文案 |
| 老调用姿势兼容 | 不传 target / cwd 不匹配 → 落 8090 + warning，行为与今天完全一致 |

## 七、平台差异

| 平台 | `os.path.normcase` 行为 | `os.path.commonpath` 行为 |
|---|---|---|
| Windows | 大小写归一 + 分隔符归一 | 不同盘符抛 ValueError（已 try/except） |
| macOS | no-op（大小写敏感） | 正常 |
| Linux | no-op（大小写敏感） | 正常 |

Windows 是主场景。mac/linux 代码不抛，但 registry.json 里 path 大小写与 cwd 完全一致才匹配——这是操作系统本身的语义，不做特殊处理。

## 八、验收测试矩阵

见 `tasks.md` 阶段 5。核心 5 条：cwd 命中 / cwd 未命中兜底 / `--target` 覆盖 / 双项目并行不串 / 硬编码 grep 归零。
