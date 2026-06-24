# Agent 协作指南

> 用于需求分析时决定是否需要多Agent协作，以及选择合适的协作模式。

---

## 谁在用本文 — 角色定位

**本文以「主对话作为 orchestrator」视角写就**：

- **主对话**：唯一的全 SKILL 持有者、最高 tier、唯一可以拆解需求并分派子 agent 的角色。本文 5 种协作模式描述的是「主对话如何编排 N 个子 agent」。
- **子 agent**（producer / gd-lead / client-unity 等 19 个）：受 `frontmatter.skills` **硬墙白名单**约束。SKILL 与归属边界见 [SKILL_MATRIX.md](./SKILL_MATRIX.md)。
- **escalate_to: main**：子 agent 越界（白名单外 SKILL / 跨职能 / 多轮收敛失败）→ 立即停止并交回主对话，由主对话重新选择本文 5 模式之一重新编排。

> **本文不讨论**：子 agent 内部如何调用 SKILL（看 SKILL_MATRIX.md）；决策门槛 grill-me / openspec 流程（看 [CLAUDE.md §五](./CLAUDE.md)）。

---

## 需求分析流程

每当收到新需求时，必须执行以下流程：

```
收到需求
  ↓
Step 1: 提取需求的核心目标和约束
  ↓
Step 2: 判断是否需要多Agent协作？
  ├─ 否 → 单Agent执行，设计实现方案
  └─ 是 → Step 3
  ↓
Step 3: 分析需求的结构（依赖、时序、并行性）
  ↓
Step 4: 选择合适的协作模式（见下表）
  ↓
Step 5: 拆分任务，分配给各Agent
  ↓
Step 6: 设计聚合和同步机制
  ↓
执行
```

---

## 何时需要多Agent协作？

### ✅ 需要多Agent的信号

| 信号 | 说明 | 例子 |
|------|------|------|
| **多个独立工作** | 不同的模块/系统各自完成不同任务 | UI更新+音效播放+动画 |
| **复杂依赖关系** | 任务间有明确的前后顺序或条件 | 战斗：伤害→动画→UI |
| **需要并行性能** | 单个Agent难以承载全部工作量 | 大型战斗（10+个单位） |
| **跨领域协作** | 需要多个专业领域的知识 | NPC对话：语音+嘴型+情绪 |
| **事件驱动设计** | 系统通过事件解耦，多个系统响应 | 玩家死亡 → 全局响应 |

### ❌ 不需要多Agent的信号

| 信号 | 说明 | 例子 |
|------|------|------|
| **单一工作流** | 任务从头到尾在一个Agent完成 | "添加一个新UI界面" |
| **线性无分支** | 没有并行或复杂依赖 | "修改一个配置参数" |
| **性能不是瓶颈** | 单个Agent响应足够快 | 简单的数据验证 |
| **逻辑内聚性强** | 工作紧密相关，强行拆分反而复杂 | "重构一个类的内部逻辑" |

---

## 四种协作模式

### 模式 1: 并行执行（Fan-Out）

```
任务分发
 ├→ Agent A（UI更新）
 ├→ Agent B（音效）
 └→ Agent C（动画）
     ↓ WhenAll（等待全部完成）
    结果聚合
```

**何时选择：**
- ✅ Agent 完全独立，无数据依赖
- ✅ 各Agent耗时相近（避免木桶效应）
- ✅ 结果汇合简单（无复杂合并逻辑）

**时序注意：**
```csharp
// ❌ 不安全：结果汇合时，某些Agent可能还未完成
Task a = agentA.DoAsync();
Task b = agentB.DoAsync();
await agentC.UseResults(a.Result, b.Result); // 危险！

// ✅ 安全：显式等待所有Agent完成
var resultA = await agentA.DoAsync();
var resultB = await agentB.DoAsync();
await agentC.UseResults(resultA, resultB);
```

**适用场景：**
- UI 更新 + 音效播放 + 特效播放（游戏事件响应）
- 多资源并行加载
- 无关的数据计算并行执行

---

### 模式 2: 顺序执行（Pipeline）

```
Task → Agent A → Agent B → Agent C → Result
```

**何时选择：**
- ✅ Agent 有明确的依赖关系（A输出 = B输入）
- ✅ 每个Agent改变系统状态，影响后续
- ✅ 顺序固定且不会改变

**时序注意：**
```csharp
// ❌ 不安全：Agent B 可能在 A 完成前开始
eventBus.Publish(new StartPhaseA());
eventBus.Publish(new StartPhaseB()); // A还没完成！

// ✅ 安全：显式顺序等待
await agentA.ExecuteAsync();
await agentB.ExecuteAsync();
await agentC.ExecuteAsync();
```

**适用场景：**
- 战斗流程：伤害计算 → 播放动画 → 更新UI
- 任务完成流：检查条件 → 扣资源 → 给奖励 → 播放反馈
- NPC对话：加载语音 → 生成嘴型 → 显示气泡

---

### 模式 3: 有向无环图（DAG）

```
        Task
        / \
      A     B    （并行）
       \   /
         C       （等A、B完成）
         |
         D       （等C完成）
```

**何时选择：**
- ✅ 复杂的依赖关系（不是简单的线性）
- ✅ 需要充分的并行性（既并行又有依赖）
- ✅ 依赖关系相对固定（不会动态变化）

**时序注意：**
```csharp
// ⚠️ 关键陷阱：循环依赖会导致死锁
// A → B → C → A （禁止！）

// ⚠️ 资源竞争：多个Agent修改同一个状态
// 结果不确定，取决于执行顺序

// ✅ 安全做法：
// 1. 验证无环性（拓扑排序）
// 2. 避免共享可变状态
// 3. 使用原子操作或锁
```

**适用场景：**
- 大型战斗系统（多个角色、多个阶段）
- 复杂任务链（多个目标，互有依赖）
- 资源加载系统（某些资源依赖其他资源）

---

### 模式 4: 发布-订阅（Event-Driven）

```
事件发布
  ├→ 订阅者 A（异步响应）
  ├→ 订阅者 B（异步响应）
  └→ 订阅者 C（异步响应）
```

**何时选择：**
- ✅ Agent 完全解耦，互不知道对方
- ✅ 响应是"最好努力"（不必全部成功）
- ✅ 系统易于扩展（新增订阅者无需改现有代码）

**时序注意：**
```csharp
// ❌ 顺序不可控：谁先谁后执行？
eventBus.Publish(new PlayerDied());
// A 可能先处理，也可能后处理

// ✅ 如果顺序重要，用等待版本
await eventBus.PublishAndWaitAsync(new PlayerDied());
// 所有订阅者都处理完了，再继续

// ✅ 如果不关心顺序，Fire-and-Forget
eventBus.Publish(new PlayerDied());
```

**适用场景：**
- UI 响应游戏事件（敌人死亡）
- 日志系统（记录所有事件）
- 通知系统（发送推送消息）
- 全局事件响应（多个无关系统)

---

### 模式 5: 骨架先行 + 并行填充（Skeleton-First Fan-Out）

```
Step 1（顺序，单Agent/主流程）：生成公共骨架
  全局事件总表 + 各模块空壳（签名完整，实现占位） + 公共基础设施 + 入口注册
  ↓
  裁定设计分歧（见下方清单）
  ↓
Step 2（并行，N个Agent）：各Agent只填充自己模块的内部实现
```

**何时选择：**
- ✅ 一个需求拆分为多个**互相引用**的子模块（如STG游戏的Player/Enemy/Bullet/UI等子系统）
- ✅ 子模块之间存在事件依赖、跨模块 `GetModule<T>()` 查询、共享数据结构
- ✅ 各子模块已分头完成 design.md，但独立设计时产生了接口分歧（命名、分类、签名不一致）

**第一步：生成公共骨架（顺序，单Agent）**

骨架必须包含，并保证整体可编译：
1. **全局事件总表**（如 `STGEvents.cs`）——所有跨模块事件类定义在同一文件，作为全局契约，Step 2 禁止修改签名
2. **各模块"空壳类"**——完整的 `ModuleCategory` / `Dependencies` / 构造函数（或 `Init()` 注入）/ 所有公共方法签名，方法体用 `// TODO` 占位
3. **公共基础设施**——对象池、Tick驱动等所有模块都会用到的工具，避免各模块各建一套
4. **入口文件注册**——`GameApp.cs` 中注册全部模块，确保编译通过

**骨架阶段裁定清单**（解决各子模块design.md独立设计产生的分歧）：
- [ ] ModuleRunner/EventBus 获取方式统一（构造函数注入 vs MonoBehaviour用 `Init()` 方法注入）
- [ ] ModuleCategory 数值口径统一（按 [IGameModule.cs](../Assets/Scripts/Core/IGameModule.cs#L12-L18) 真实分层，不是各模块自行猜测）
- [ ] 跨模块共享数据结构的字段命名统一（如 `id` vs `enemyId`）
- [ ] OnUpdate 驱动机制统一（一个 Driver 调用所有需要Tick的模块，不是每模块各建一个）
- [ ] 工具类/对象池命名统一（如统一用 `GenericObjectPool<T>`，不是每个模块各写一套Pool）

**第二步：并行填充实现（并行，N个Agent）**

每个Agent只负责填充自己模块的方法体实现（替换 `// TODO`）和内部私有方法/类。

**禁止修改**：
- 全局事件文件（`STGEvents.cs`）
- 其他模块的公共方法签名
- 入口注册文件（`GameApp.cs`）
- 公共基础设施代码

**关键运行时规则：**
```csharp
// ❌ 危险：非Dependencies模块的初始化时机不确定
public UniTask InitializeAsync(CancellationToken ct)
{
    _enemyModule = _runner.GetModule<EnemyModule>(); // EnemyModule不在Dependencies中！
    return UniTask.CompletedTask;
}

// ✅ 安全：运行时（事件处理/OnUpdate）再查询
[EventHandler]
void OnPlayerHit(PlayerHitEvent @event)
{
    var hp = _runner.GetModule<PlayerModule>().GetHP(); // 此时所有模块已初始化完成
}
```
> ModuleRunner 只保证 `Dependencies` 中列出的模块按序初始化完成；非Dependencies模块的 `GetModule<T>()` 必须在运行时调用点（OnUpdate/事件处理方法）调用，不能在 `InitializeAsync` 中缓存。

**适用场景：**
- 一个大需求拆分为多个互相引用的子模块，且 `2.需求列表` 中已为各子模块生成独立的 design.md
- 配合 `openspec/changes/<NN-name>/CONTRACT.md` 全局契约文件，骨架代码必须与其保持一致（多模块功能时由 lead 创建此文件，承载事件总表 / 模块依赖 / 跨模块查询接口表 / 配置表结构等）

---

## 协作模式对比表

| 模式 | 并行度 | 时序复杂度 | 适用性 | 风险 |
|------|--------|-----------|-------|------|
| 并行执行 | 高 | 低 | 完全独立的任务 | 木桶效应、结果一致性 |
| 顺序执行 | 0 | 低 | 线性依赖链 | 效率低 |
| DAG | 中等 | 高 | 复杂依赖 | 循环依赖、资源竞争 |
| 事件驱动 | 高 | 中等 | 解耦系统 | 隐藏控制流、顺序不可控 |
| 骨架先行+并行填充 | 高（填充阶段） | 中（骨架阶段需裁定分歧） | 多个互相引用的子模块 | 骨架裁定不充分导致接口冲突 |

---

## 实践检查清单

### 需求分析时

- [ ] 是否有多个独立的子任务？
- [ ] 子任务之间是否有依赖关系？
- [ ] 依赖关系的形状是什么？（线性/树形/图形）
- [ ] 是否需要严格的执行顺序？
- [ ] 哪些任务可以并行？
- [ ] 并行度的收益是否值得增加复杂度？

### 设计时

- [ ] 已明确定义各Agent的职责？
- [ ] 已列出所有的数据依赖？
- [ ] 已检查是否有循环依赖？
- [ ] 已定义结果聚合方式？
- [ ] 已设计错误处理和超时机制？
- [ ] 已考虑时序和竞态条件？
- [ ] 若多个子模块互相引用（事件/跨模块查询/共享数据结构），是否先生成公共骨架并裁定接口分歧（模式5）？

### 实现时

- [ ] 异步等待都用了 `await UniTask.WhenAll()` 还是 `await UniTask.WhenAny()`？
- [ ] 是否有死锁风险（等待循环）？
- [ ] 是否有资源泄露（未清理的订阅）？
- [ ] 错误是否被正确地传播和处理？

---

## 常见陷阱速查

| 陷阱 | 表现 | 解决方案 |
|------|------|---------|
| **不同步** | Agent A完成，但代码假设已完成 | 使用 `await` 显式同步 |
| **顺序错误** | 任务执行顺序不符合预期 | 验证依赖关系，打印日志 |
| **死锁** | 程序卡住，无响应 | 检查是否有循环等待 |
| **竞态条件** | 随机性错误，偶发故障 | 避免共享可变状态，用锁 |
| **内存泄露** | 订阅未取消，占用越来越多 | 用 `IDisposable` 管理生命周期 |
| **性能退化** | 并行后比顺序还慢 | 检查任务是否真的独立，是否有锁争用 |
| **接口冲突** | 多个并行Agent各自定义了同名/不同签名的事件或方法 | 先生成公共骨架统一签名（模式5），Step 2 禁止修改公共骨架 |
| **GetModule时机错误** | InitializeAsync中调用非Dependencies模块的`GetModule<T>()`抛异常 | 改为在OnUpdate/事件处理方法（运行时）中调用 |
| **子 agent 越界** | 子 agent 调用白名单外 SKILL，或任务跨职能 | escalate_to: main，主对话重新拆解并按 5 模式重新编排 |

---

## 示例：战斗流程的Agent拆分

**需求**：玩家发起攻击，要有伤害计算、动画播放、音效、UI更新。

**分析**：
```
特点：
- 伤害计算 → 依赖：玩家属性、目标属性
- 动画播放 → 依赖：伤害值（决定是暴击还是普通）
- 音效播放 → 依赖：伤害类型
- UI更新 → 依赖：伤害值

结构：
计算(必须第一) → 动画/音效(可并行) → UI(最后)

最优模式：混合
- 伤害计算：单Agent（CalcModule）
- 动画+音效：并行（AnimModule, AudioModule）
- UI更新：单Agent（UIModule）
```

**实现框架**（伪代码）：
```csharp
// Step 1: 伤害计算
var damageResult = await calcModule.CalculateDamageAsync(attacker, target);

// Step 2: 动画和音效并行
await UniTask.WhenAll(
    animModule.PlayAttackAnimAsync(damageResult),
    audioModule.PlayEffectSoundAsync(damageResult)
);

// Step 3: UI更新
await uiModule.UpdateHealthBarAsync(target, damageResult);

// Step 4: 发送事件通知全局
eventBus.Publish(new AttackCompleted(damageResult));
```

---

*最后更新: 2026-06-24（02-skill-routing-unification 后：补 orchestrator 角色定位 + 子 agent 越界陷阱）*
