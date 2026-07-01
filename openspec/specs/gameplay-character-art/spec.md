# gameplay-character-art Specification

## Purpose
TBD - created by archiving change 17-gameplay-character-art. Update Purpose after archive.
## Requirements
### Requirement: 角色 sprite 资源组织

系统 SHALL 在 `Assets/Resources/Sprite/Character/<Name>/<Action>/<Direction>.png` 路径下管理 2D 角色 sprite，其中：
- `<Name>` ∈ {Player1, Player2, Player3, Boss1}
- `<Action>` ∈ {Idle, Walk, Death, Attack}
- `<Direction>` ∈ {Up, Down, Left, Right}
- 每个 `.png` 文件 SHALL 是 4 帧水平 sprite sheet

#### Scenario: Player1 必须含 16 张

- **WHEN** 加载 Player1 角色
- **THEN** 路径下 SHALL 存在 16 张 sprite sheet 文件（4 动作 × 4 方向）

#### Scenario: Player2/3 仅 Idle

- **WHEN** 加载 Player2 或 Player3
- **THEN** 仅 `Idle/` 子目录 SHALL 存在 4 张 sprite sheet

### Requirement: AnimatorController 参数契约

每个角色的 AnimatorController SHALL 暴露以下参数：

| 参数名 | 类型 | 含义 |
|---|---|---|
| `Direction` | Int | 0=Down, 1=Up, 2=Left, 3=Right |
| `IsMoving` | Bool | 是否在移动 |
| `AttackTrigger` | Trigger | 触发攻击动画 |
| `Die` | Trigger | 触发死亡动画（终结状态） |

#### Scenario: 参数读写

- **WHEN** PlayerAnimatorBridge 调用 `SetInteger("Direction", 1)`
- **THEN** Animator SHALL 切到向上方向的动画状态

### Requirement: SpawnerModule 禁用 Cube 占位

`SpawnerModule.CreateScene()` SHALL NOT 调用 `GameObject.CreatePrimitive(PrimitiveType.Cube)` 创建任何角色实体（地面/灯保留）。

#### Scenario: Player 实例化

- **WHEN** `InitializeAsync` 执行完成
- **THEN** `SpawnerModule.Player` SHALL 指向通过 `Resources.Load<GameObject>("Prefab/Character/Player1")` 实例化的对象

#### Scenario: 49 个 Bot 复用 Player1

- **WHEN** `InitializeAsync` 执行完成
- **THEN** `Enemies` 列表 SHALL 含 49 个实例，每个 SHALL 由 Player1 prefab 实例化

#### Scenario: Boss 出现

- **WHEN** `InitializeAsync` 执行完成
- **THEN** 场景内 SHALL 存在恰好 1 个 Boss1 实例，位置为 (0, 0, 15)

### Requirement: PlayerAnimatorBridge 事件桥接

`PlayerAnimatorBridge` SHALL 订阅以下事件并驱动 Animator 参数：

| 事件 | 行为 |
|---|---|
| `MoveTickEvent` | 若 Magnitude < 0.05 → IsMoving=false；否则 IsMoving=true + 更新 Direction |
| `AttackHitEvent` | 触发 AttackTrigger |
| `PlayerDiedEvent` | 触发 Die |

#### Scenario: 移动方向计算

- **WHEN** MoveTickEvent.Direction = (0.7, 0.7)（右上）
- **THEN** Direction SHALL 被 set 为 3（Right，因 |x|=|y| 时优先左右）—— **注**：实际优先级看 [design.md §5](../../design.md#5-playeranimatorbridgecs) `ComputeDirection`，规约只断言"非 0 输入必产出 0/1/2/3 之一"

### Requirement: 0 Console Error 退出门槛

playtest loop SHALL 仅在以下两条件同时满足时退出：
1. 5 条 TC-Art 全部 PASS
2. Console Errors 数量 == 0（AudioMixer warning 等已知无害告警不计）

#### Scenario: 失败安全网

- **WHEN** 同一 bug 连续 5 轮 status=OPEN 且无 status 变化
- **THEN** loop SHALL 终止并交回用户

### Requirement: 美术素材失败重试上限

`codex-image-gen` 调用 SHALL 单图最多重试 3 轮。

#### Scenario: 重试耗尽

- **WHEN** 同一 sprite 提示词调整 3 次后用户仍不满意
- **THEN** 主对话 SHALL 阻塞并通知用户手动介入，禁止继续无限重试

