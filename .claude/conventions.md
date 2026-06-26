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

---

## 八、UI 制作时序（强制）

> 编排细节见 [CLAUDE.md §六「UI 制作子流程」](./CLAUDE.md)。本节是**编码视角的硬约束**，写 UIForm / 改 Prefab 前必须满足。

### 5 阶段不可跳

```
需求设计 → 效果图设计 → 效果图生成 → Prefab + 代码（并行） → 联调微调
（三表）   （prompts.md）  （mockups/）   （Fan-Out 模式 1）        （对比效果图）
```

| 前置条件未满足 | 禁止做的事 |
|---|---|
| 三表（页面 / 组件 / 状态）未齐 | 不许写效果图提示词 |
| 效果图未生成或用户未确认 | 不许动 Prefab 与 UIForm 脚本 |
| 标注稿（间距 / 字号 / 锚点）未给出 | 不许调 unity-skills MCP 建 Prefab |
| 运行时截图未与效果图对比 | 不许声称该 UI 完成 |

### Prefab 创建路径

1. **首选**：`client-unity` 调用 `unity-skills` MCP，按 art-ui 标注稿自动建 Canvas 层级 + AddComponent
2. **回退**：MCP 不可用 → 通知用户在 Unity Editor 手动搭（兼容 §十二「Prefab 必须手动建」原则）
3. **禁止**：跳过标注稿凭感觉创建组件层级

### UIForm 脚本约束

- 字段一律 `[SerializeField] private` + PascalCase 命名（与 §七 一致）
- Prefab 未完成时用占位字段，等 Prefab 搭好后再拖引用，不许在运行时 `Find` 抓
- 与 Prefab 的对接必须在脚本头部注释明确依赖的 Prefab 路径与节点结构

### 效果图位置约定

```
openspec/changes/<NN-name>/art/
├─ prompts.md       ← 每页一条效果图提示词
├─ mockups/         ← 完整页面效果图（codex-image-gen 输出）
│  ├─ <PageName>.png
│  └─ 生成记录.md
└─ raw/             ← 单独素材切片（icon / 头像等）
   └─ <ResName>.png
```

`mockups/` 与 `raw/` **严格分目录**——前者是「页面参考稿」，后者是「最终切片」。混存视为违规。