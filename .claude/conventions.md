# 项目规范

> AI 和开发者共同遵守的项目约定。

---

## 一、ModuleCategory 优先级设置规则

```
调度优先级 = ModuleCategory × 100 - OutDegree
（值越小越优先。ModuleRunner 运行时计算）
```

| Category | 名称 | 说明 | 典型模块 |
|---|---|---|---|
| 0 | 基础设施 | 数据/配置/资源/事件等基础服务 | ConfigModule, ResourceModule |
| 1 | 系统服务 | 为玩法提供能力，不直接面对玩家 | AudioModule, NetworkModule, PoolModule |
| 2 | 用户交互 | 直接影响玩家看到/操作的内容 | UIModule, InputModule, CameraModule |
| 3 | 核心玩法 | 游戏逻辑，依赖基础设施+服务+交互 | CombatModule, QuestModule, InventoryModule |
| 4 | 辅助系统 | 非关键路径，不影响玩家体验 | AnalyticsModule, CrashReportModule |

**设置流程：**

1. 确认模块属于哪个 Category
2. 默认 Category = 4（忘记设置时排到最后）
3. OutDegree 由 ModuleRunner 自动计算，不需要手动设置

---

## 二、Dependencies 规则

- **只放具体的 Module 类型**，不支持接口
- **Dependencies 确保初始化时序**——如果 A 需要在 B 之后初始化，B 应出现在 A 的 Dependencies 中
- **InitAsync 期间不发布事件**，初始化间的通信用 Dependencies 表达

```csharp
// ✅ 正确
public Type[] Dependencies => new[] { typeof(ResourceModule), typeof(InputModule) };

// ❌ 错误：接口依赖
public Type[] Dependencies => new[] { typeof(IResourceProvider) };
```

---

## 三、事件处理规范

### 3.1 事件命名

```
[描述]Event  例：CombatEndEvent, HPChangedEvent, ItemPickedUpEvent
```

### 3.2 Handler 方法命名

```
On[事件名去掉Event]  例：OnCombatEnd, OnHPChanged, OnItemPickedUp
```

### 3.3 方法签名

```csharp
// 异步版本（推荐）
[EventHandler]
UniTask OnCombatEnd(CombatEndEvent evt) { ... }

// 同步版本
[EventHandler]
void OnDamage(DamageEvent evt) { ... }

// 参数有且仅有一个，类型为引用类型
```

### 3.4 事件定义位置

- **跨模块共享的事件** → `Assets/Scripts/Events/`
- **模块内部事件** → 模块文件夹下的 `Events/` 子目录

### 3.5 通信原则

```
通知（某件事发生了） → EventBus.Publish
等待（大家都处理完） → EventBus.PublishAndWaitAsync
查询（当前状态）     → ModuleRunner.GetModule<T>() + 读属性
修改（改变状态）     → EventBus.Publish + 接收方内部处理
```

---

## 四、模块文件组织

每个模块文件夹的标准结构：

```
ModuleName/
├── README.md           ← 模块功能说明
├── ModuleName.cs       ← IGameModule 实现
└── Events/             ← 模块专属事件（可选）
    └── XxxEvent.cs
```

---

## 五、日志规范

使用 `FrameworkLogger`，格式见 `02-AI友好型日志规范.md`。

```csharp
FrameworkLogger.Error("ModuleName", $"Key=Value Key2=Value2");
FrameworkLogger.Warn("ModuleName", $"Key=Value Key2=Value2");
FrameworkLogger.Info("ModuleName", $"Key=Value Key2=Value2");
```

`[CallerFilePath]` 和 `[CallerLineNumber]` 自动填充 Location 字段。

---

## 六、异步规范

- 所有异步方法必须以 `Async` 结尾
- 返回值必须是 `UniTask` 或 `UniTask<T>`
- 禁止 `async void`，一律返回 `UniTask`
- MonoBehaviour 异步方法需处理对象销毁：`this.GetCancellationTokenOnDestroy()`

---

## 七、编码规范

- 公开属性用 PascalCase：`CurrentHP`, `MaxHP`
- 私有字段用 _camelCase：`_character`, `_subs`
- 方法用 PascalCase：`OnCombatEnd`, `InitializeAsync`
- 模块只暴露数据（public 只读属性），行为方法设 internal