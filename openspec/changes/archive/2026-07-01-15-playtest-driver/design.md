# Design — 15-playtest-driver

## 一、整体架构

```
┌────────────── Claude 主对话 / qa-engineer ──────────────┐
│                                                          │
│  playtest-driver SKILL（编排 SOP）                       │
│      ↓                                                   │
│  unity-skills MCP            uloop CLI                   │
│  (editor_*/console_*/event_invoke)  (execute-dynamic-code) │
│      ↓                            ↓                      │
└──────┼────────────────────────────┼──────────────────────┘
       │                            │
       ▼                            ▼
┌──────────── Unity Editor 进程 ──────────────────────────┐
│                                                          │
│  Edit ▶ Play/Pause/Step  ←  快捷键 / 菜单                │
│  Console (FrameworkLogger → Debug.Log)                   │
│  GameObject.onClick (event_invoke)                       │
│                                                          │
│  GameApp.Instance.ModuleRunner                           │
│      └─ InputModule                                       │
│           ├─ _simulator: IInputSimulator?  ←─ uloop 注入 │
│           └─ 查询方法：sim?.Consume ?? Input.GetKey*     │
└──────────────────────────────────────────────────────────┘
```

## 二、IInputSimulator 与 InputSimulator

### 接口（仅 Editor / Dev 编译）

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public interface IInputSimulator
{
    // KeyDown 语义：仅下一次查询返回 true，消费即清
    bool ConsumeKeyDown(KeyCode key);
    // KeyHold 语义：状态式，直到 Release
    bool IsKeyHeld(KeyCode key);
    // 鼠标 Down 一次性
    bool ConsumeMouseDown(int button);
    // 鼠标 Hold
    bool IsMouseHeld(int button);
    // 移动方向覆盖（null = 不覆盖，走 Unity Input）
    Vector2? GetMoveOverride();
}
#endif
```

### 实现

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;

public sealed class InputSimulator : IInputSimulator
{
    readonly HashSet<KeyCode> _keyDownQueue = new();
    readonly HashSet<KeyCode> _keyHeld = new();
    readonly HashSet<int> _mouseDownQueue = new();
    readonly HashSet<int> _mouseHeld = new();
    Vector2? _moveOverride;

    // ===== 注入入口（外部 uloop 调用）=====
    public void PressKey(KeyCode k) => _keyDownQueue.Add(k);
    public void HoldKey(KeyCode k, bool held) { if (held) _keyHeld.Add(k); else _keyHeld.Remove(k); }
    public void PressMouse(int b) => _mouseDownQueue.Add(b);
    public void HoldMouse(int b, bool held) { if (held) _mouseHeld.Add(b); else _mouseHeld.Remove(b); }
    public void SetMove(Vector2? dir) => _moveOverride = dir;
    public void ClearAll()
    {
        _keyDownQueue.Clear(); _keyHeld.Clear();
        _mouseDownQueue.Clear(); _mouseHeld.Clear();
        _moveOverride = null;
    }

    // ===== 消费入口（InputModule 调用）=====
    public bool ConsumeKeyDown(KeyCode key) => _keyDownQueue.Remove(key);
    public bool IsKeyHeld(KeyCode key) => _keyHeld.Contains(key);
    public bool ConsumeMouseDown(int button) => _mouseDownQueue.Remove(button);
    public bool IsMouseHeld(int button) => _mouseHeld.Contains(button);
    public Vector2? GetMoveOverride() => _moveOverride;
}
#endif
```

**为什么是 HashSet 而不是队列？**
- `Input.GetKeyDown` 语义就是「按下那一帧 true」，多次注入同键无意义（按一次就是一次）
- HashSet 去重，Remove 返回 bool 天然契合「消费一次」语义
- 性能：每帧 O(1) 查询，无 GC

## 三、InputModule 改造（partial class + 条件编译）

### 文件拆分

```
Assets/Scripts/Modules/Input/
├─ InputModule.cs                ← 现有，改 partial + 在每个查询方法前接入 simulator
├─ InputModule.Simulator.cs      ← 新增，整文件 #if UNITY_EDITOR || DEVELOPMENT_BUILD
├─ IInputSimulator.cs            ← 新增，整文件条件编译
└─ InputSimulator.cs             ← 新增，整文件条件编译
```

### InputModule.cs 改造（双源融合范式）

```csharp
public sealed partial class InputModule : IGameModule
{
    // ... 现有字段 ...

    public Vector2 GetMoveDirection()
    {
        var sim = GetSimulatorOverride();
        if (sim.HasValue) return sim.Value;

        // 原 WASD 逻辑保持不变
        float x = 0f, y = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
        // ...
    }

    public bool IsAttackPressed()
        => ConsumeSimMouseDown(0) || Input.GetMouseButtonDown(0);

    public bool IsSkillPressed()
        => ConsumeSimKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.E);

    // ... 其余 9 个查询方法同模板 ...
}
```

### InputModule.Simulator.cs（新增 partial）

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public sealed partial class InputModule
{
    IInputSimulator _simulator;

    /// <summary>测试代码（uloop）调用：装配模拟器。再次调用覆盖。</summary>
    public void EnableSimulator(IInputSimulator sim) => _simulator = sim;
    public void DisableSimulator() => _simulator = null;
    public IInputSimulator GetSimulator() => _simulator;

    bool ConsumeSimKeyDown(KeyCode k) => _simulator != null && _simulator.ConsumeKeyDown(k);
    bool IsSimKeyHeld(KeyCode k)      => _simulator != null && _simulator.IsKeyHeld(k);
    bool ConsumeSimMouseDown(int b)   => _simulator != null && _simulator.ConsumeMouseDown(b);
    bool IsSimMouseHeld(int b)        => _simulator != null && _simulator.IsMouseHeld(b);
    Vector2? GetSimulatorOverride()   => _simulator?.GetMoveOverride();
}

public sealed partial class InputModule
{
    // 在生产构建下，下列符号必须存在以让 InputModule.cs 调用方编译通过：
    // 用编译屏障 stub。
}
#else
public sealed partial class InputModule
{
    bool ConsumeSimKeyDown(UnityEngine.KeyCode k) => false;
    bool IsSimKeyHeld(UnityEngine.KeyCode k) => false;
    bool ConsumeSimMouseDown(int b) => false;
    bool IsSimMouseHeld(int b) => false;
    UnityEngine.Vector2? GetSimulatorOverride() => null;
}
#endif
```

**为什么用 partial + 条件编译？**
- partial 拆分让"业务查询"与"测试注入"在源码上物理分离
- 条件编译保证生产构建零成本（字段、方法、Stub 全剥离）
- `EnableSimulator` 接口只在 Editor/Dev 暴露，生产代码无法调用

## 四、playtest-driver SKILL 设计

### 触发关键词（写进 description）

```
playtest / 自动测试 / 模拟输入 / 跑界面 / 测试驱动 / 模拟点击 / 模拟按键 / 跑功能 / 验证 UI / playtest 报告
```

### 标准流程（SOP）

```
┌─ STEP 1: 准备 ───────────────────────────┐
│ unity-skills.editor_get_state            │
│   → 如果 isPlaying=true，先 editor_stop  │
│ unity-skills.console_clear               │
└──────────────────────────────────────────┘
        ↓
┌─ STEP 2: 启动 Play ──────────────────────┐
│ unity-skills.editor_play                 │
│ 轮询 editor_get_state，等 isPlaying=true │
│   && !isCompiling                        │
│ sleep 3s 等模块就绪                       │
│ console_get_logs 验证 "游戏就绪" 字样     │
└──────────────────────────────────────────┘
        ↓
┌─ STEP 3: 装配模拟器 ──────────────────────┐
│ uloop execute-dynamic-code:              │
│   var app = UnityEngine.Object           │
│     .FindFirstObjectByType<GameApp>();    │
│   if (!app.TryGetRuntime(out _, out var r)) return "not ready"; │
│   var input = r.GetModule<InputModule>(); │
│   input.EnableSimulator(new InputSimulator()); │
│   return "ok";                            │
└──────────────────────────────────────────┘
        ↓
┌─ STEP 4: 单步注入循环 ────────────────────┐
│ for each test step:                      │
│   uloop execute-dynamic-code:            │
│     input.GetSimulator().PressKey(KeyCode.E); │
│   editor_execute_menu "Edit/Step" (可选，精准 1 帧) │
│   sleep 0.5s                             │
│   console_get_logs filter=Log limit=20   │
│   断言关键字日志                          │
│   UI 按钮：event_invoke objectName=..    │
└──────────────────────────────────────────┘
        ↓
┌─ STEP 5: 收尾 + 报告 ────────────────────┐
│ console_get_logs filter=Error limit=50   │
│ editor_stop                              │
│ 写 tools/playtest/reports/<ts>-<scn>.md  │
└──────────────────────────────────────────┘
```

### Pause/Resume 与单帧推进（用户确认了 Unity 菜单）

- **播放**：`editor_play` 或 `editor_execute_menu Edit/Play`
- **暂停切换**：`editor_pause` 或 `editor_execute_menu Edit/Pause`（toggle 语义）
- **单帧推进**：`editor_execute_menu Edit/Step`（前提：当前已暂停）

**典型节奏**：
- 探索式跑通：不暂停，连续注入 + sleep + 读日志
- 精准时序：暂停 → 注入 → Step → 读日志 → 暂停回看

## 五、测试报告模板

文件：`tools/playtest/reports/YYYY-MM-DD-HHMM-<scenario>.md`

```markdown
---
test_time: 2026-06-30 14:32
scenario: e-key-triggers-skill
result: PASS  # PASS / FAIL / PARTIAL
duration_sec: 12.4
errors_found: 0
warnings_found: 1
---

# Playtest Report — <scenario>

## 概要
- 测试目标：验证 E 键按下后 SkillModule 响应技能释放
- 测试结果：✅ 通过
- 关键日志：[SkillModule|INFO] Action=SkillCast Cost=10
- 耗时：12.4s

## 测试流程
1. editor_play → 等模块就绪（3.2s）
2. 装配 InputSimulator → 成功
3. 注入 PressKey(E) → 帧 +1 → SkillModule 响应
4. console_get_logs → 命中预期日志
5. editor_stop → 清理

## 遇到的问题
- WARN: [SkillModule] 冷却未读取，使用默认值 5s
  → 建议：补 DataTable cooldown 字段（不阻塞）

## 后续
- [ ] 跟踪 SkillCD 默认值的来源
```

## 六、关键时序与陷阱

| 陷阱 | 影响 | 应对 |
|---|---|---|
| Simulator 注入后业务还没到 OnUpdate → KeyDown 被吞 | 中 | 注入后至少 sleep 1 帧或 `Edit/Step` 一次 |
| 业务在同一帧两次调用 `IsSkillPressed()` → 第二次返回 false | 中 | 这正是 `Input.GetKeyDown` 的原生语义，不修复 |
| 多个 simulator 实例覆盖 → 上次注入被清 | 低 | `EnableSimulator` 文档明确"再次调用覆盖"；SOP 中每次 PoC 复用同一个 |
| Editor 在 compile 期间调 uloop → 阻塞或失败 | 中 | STEP 2 等 `!isCompiling` |
| 测试报告丢路径 / 文件名碰撞 | 低 | 用 ISO 时间戳 + scenario 名拼接 |
