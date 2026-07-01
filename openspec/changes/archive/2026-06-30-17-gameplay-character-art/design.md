# Design — 17-gameplay-character-art

## 1. 设计共识（来自 grill-me Stage A 5/5）

| 决策点 | 选择 | 备选理由 |
|---|---|---|
| 美术风格 | **复用现有 UI 美术风格** | 项目已有 SettingsForm / SelfTattooForm 视觉基调，保持一致 |
| 角色方向 | **4 方向（上/下/左/右）** | 角色非左右对称，**禁用 flipX** |
| 可玩角色数 | **3 个可选**，仅 Player1 全套动画 | 其它 2 个仅 Idle 供 CharacterSelect 预览 |
| Bot（49 个其它玩家位） | **复用 Player1 美术**，不出新图 | shader 染色区分 → 推 #19 |
| Boss | **1 个全套**（同 Player1 结构），其它 2 个推迟 | 验证流程 + 美术成本 |
| 动作集 | **Idle / Walk / Death / Attack（占位）** | Attack 不改伤害逻辑，只触发动画 |
| 帧数 | **4 帧 / clip** | 平衡风格表达与生产成本 |
| 动画方案 | **Animator + AnimatorController** | unity-skills MCP 已有 `animator_*` 全套工具 |
| AnimationClip 创建 | **自写 Editor 工具**（unity-skills MCP 无 native） | tools-engineer 负责 |
| 武器 | **本轮固定占位**（不做选择流程） | WeaponSelect → 推 #18 |
| 视觉增强 | **本轮不做** | Bot shader / VFX / 阴影描边 / 音效 → 推 #19 |
| TC-Art 验收 | **5 条严格 PASS**（playtest 自动跑） | 全部明确动作 → 期望 → 实际 |
| 失败重试 | **codex-image-gen 3 轮重试上限** | 第 3 轮仍失败 → 阻塞通知用户 |
| 权限模型 | **一路信任**（Auto Mode + Bypass） | 与 #16 一致 |

## 2. 目录约定

```
Assets/Resources/
├─ Sprite/Character/
│  ├─ Player1/
│  │  ├─ Idle/    Up.png Down.png Left.png Right.png         (4 张 sprite sheet, 每张含 4 帧)
│  │  ├─ Walk/    Up.png Down.png Left.png Right.png
│  │  ├─ Death/   Up.png Down.png Left.png Right.png
│  │  └─ Attack/  Up.png Down.png Left.png Right.png
│  ├─ Player2/Idle/  Up.png Down.png Left.png Right.png      (仅 Idle)
│  ├─ Player3/Idle/  Up.png Down.png Left.png Right.png      (仅 Idle)
│  └─ Boss1/     {Idle,Walk,Death,Attack}/...                (全套，同 Player1)
└─ Anim/Character/
   ├─ Player1/
   │  ├─ Idle_Up.anim ... Attack_Right.anim                  (16 clip)
   │  └─ Controller.controller
   ├─ Player2/Idle_*.anim + Controller.controller            (4 clip + 1 controller)
   ├─ Player3/Idle_*.anim + Controller.controller
   └─ Boss1/{16 clip + 1 controller}

Assets/Resources/Prefab/Character/
├─ Player1.prefab    (SpriteRenderer + Animator + EntityRef)
├─ Player2.prefab
├─ Player3.prefab
└─ Boss1.prefab
```

## 3. 角色 Prefab 结构

```
Player1 (GameObject)
├─ Transform (position from SpawnerModule)
├─ SpriteRenderer (sprite = 当前帧, 由 AnimationClip 驱动)
├─ Animator (controller = Player1/Controller.controller)
└─ EntityRef (IsPlayer, MaxHP, Target — 不变)
```

## 4. AnimatorController 状态机

```
              ┌──────┐
        ┌─────┤ Idle ├─────┐
        │     └──────┘     │
        │                  │
   Move│↑     ↓ MoveEnd    │
        │                  │
        ▼                  ▼
   ┌──────┐          ┌──────────┐
   │ Walk ├──────────┤  Attack  │  (Trigger: AttackTrigger)
   └───┬──┘          └────┬─────┘
       │                  │
       │   Die            │   Die
       ▼                  ▼
   ┌────────────────────────┐
   │         Death          │  (terminal)
   └────────────────────────┘
```

**参数表**：

| 名称 | 类型 | 来源 |
|---|---|---|
| `Direction` | Int (0=Down, 1=Up, 2=Left, 3=Right) | MoveTickEvent / 玩家朝向 |
| `IsMoving` | Bool | MoveTickEvent.Magnitude > 阈值 |
| `AttackTrigger` | Trigger | AttackHitEvent / Player 自身攻击键 |
| `Die` | Trigger | PlayerDiedEvent / TargetKilledEvent |

每个 State 内部根据 `Direction` 切到对应方向的 AnimationClip（**用 BlendTree 或 SubStateMachine**：第一版用 4 个 child state + condition `Direction == X` 简单实现，可读性优先）。

## 5. PlayerAnimatorBridge.cs

```csharp
// Assets/Scripts/Modules/Combat/PlayerAnimatorBridge.cs
public sealed class PlayerAnimatorBridge : MonoBehaviour
{
    Animator _anim;
    EventBus _bus;
    readonly List<IDisposable> _subs = new();
    int _dir = 0; // 0=Down

    public void Init(EventBus bus)
    {
        _anim = GetComponent<Animator>();
        _bus = bus;
        _subs.Add(_bus.Subscribe<MoveTickEvent>(OnMove));
        _subs.Add(_bus.Subscribe<AttackHitEvent>(OnAttack));
        _subs.Add(_bus.Subscribe<PlayerDiedEvent>(OnDie));
    }

    void OnMove(MoveTickEvent e)
    {
        if (e.Magnitude < 0.05f) { _anim.SetBool("IsMoving", false); return; }
        _anim.SetBool("IsMoving", true);
        _dir = ComputeDirection(e.Direction); // vec2 → 0/1/2/3
        _anim.SetInteger("Direction", _dir);
    }

    void OnAttack(AttackHitEvent e) { _anim.SetTrigger("AttackTrigger"); }
    void OnDie(PlayerDiedEvent e)   { _anim.SetTrigger("Die"); }

    void OnDestroy() { foreach (var d in _subs) d.Dispose(); }

    int ComputeDirection(Vector2 v)
    {
        // 优先级：|y| > |x| → 上/下；否则左/右
        if (Mathf.Abs(v.y) >= Mathf.Abs(v.x)) return v.y >= 0 ? 1 : 0;
        return v.x >= 0 ? 3 : 2;
    }
}
```

## 6. SpawnerModule 改造点

- `CreateScene()` 内 `GameObject.CreatePrimitive(Cube)` 替换为 `Resources.Load<GameObject>("Prefab/Character/Player1")` + `Instantiate`
- 49 个 actor 同理，全部 Player1 prefab（后续 #19 加 shader 染色）
- Boss 单独 spawn（新增 `SpawnBoss()` 方法，初始位置 (0, 0, 15)）
- Player 实例化后挂 PlayerAnimatorBridge + `Init(_bus)`
- Bot 实例化后挂 BotAnimatorBridge（与 PlayerAnimatorBridge 同结构，但订阅 BotController 内部事件——本轮可只挂 Animator 不挂 bridge，让 Bot 一直 Idle，留给 #19）

## 7. Editor 工具（tools-engineer）

**输入**：`Assets/Resources/Sprite/Character/<Name>/<Action>/<Dir>.png`（已 sliced 成 4 帧 sprite sheet）

**输出**：
- `Assets/Resources/Anim/Character/<Name>/<Action>_<Dir>.anim`（4 帧 Loop=Idle/Walk, 4 帧 OneShot=Attack/Death）
- `Assets/Resources/Anim/Character/<Name>/Controller.controller`（含 4 个 state + 参数 + transition）
- `Assets/Resources/Prefab/Character/<Name>.prefab`（SpriteRenderer + Animator + EntityRef）

**菜单**：`Tools/Character/Generate Animator from Sprite Folder`

**实现要点**：
- 用 `UnityEditor.Animations.AnimatorController` API 建 controller / state / parameter / transition
- 用 `UnityEditor.AnimationUtility.SetObjectReferenceCurve` 建 sprite-based AnimationClip
- 4 帧采样率 8 fps（=0.5s / clip），Loop = (Action == Idle || Action == Walk)
- 检测到对应 sprite 文件缺失时跳过该 clip 但不报错（先生其他可生的）

## 8. Agent 编排（AGENTS.md 模式 5 + 模式 1 混合）

```
主对话 (orchestrator)
├─ Step 1 (顺序): producer + art-director 写 design.md 落地（已完成）
├─ Step 2 (顺序): tools-engineer 写 Editor 工具
│      产出: Tools/Character/Generate Animator from Sprite Folder 菜单可用
├─ Step 3 (Fan-Out 模式 1, 并行多 Agent):
│      ├─ art-director 写 art/requirements.md (4 角色三表)
│      ├─ art-director 写 art/prompts.md (16 + 4 + 4 + 16 = 40 张图提示词)
│      └─ client-unity 改 SpawnerModule.cs (Cube → Prefab.Load, PlayerAnimatorBridge 挂载)
│      WhenAll → 汇合
├─ Step 4 (Fan-Out 模式 1, N 个子 Agent 并行出图):
│      Player1: 16 张 (4 动作 × 4 方向) → 16 个 agent 并行调 codex-image-gen
│      Player2: 4 张                    → 4 agent
│      Player3: 4 张                    → 4 agent
│      Boss1:   16 张                   → 16 agent
│      共 40 个并行任务，3 轮重试上限
├─ Step 5 (顺序, 主对话):
│      在 Editor 菜单 Tools/Character/Generate Animator from Sprite Folder
│      × 4 (Player1/Player2/Player3/Boss1) → 自动生成 .anim + .controller + .prefab
├─ Step 6 (Fan-Out 模式 1, 并行):
│      ├─ client-unity 写 PlayerAnimatorBridge.cs + 挂到 Player Prefab
│      └─ qa-engineer 写 tests/min-plan.md 5 条 TC-Art
│      WhenAll
└─ Step 7 (Loop, qa-engineer 主导, ≤5 轮收敛):
       playtest 13 TC（原 #16）+ 5 TC-Art → 0 errors → 退出
```

## 9. 边界与失败安全网

- **同一 bug 连续 5 轮未解** → loop 终止交回用户（与 #16 标准一致）
- **codex-image-gen 同一图 3 轮重试仍不通过** → 阻塞通知用户人工介入
- **Editor 工具生成失败** → 报错列出缺失文件清单，不要静默成功
