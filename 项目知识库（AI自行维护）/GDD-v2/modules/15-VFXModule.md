# 15-VFXModule 模块详设

> **主导 Agent**: client-ta
> **对应系统 GDD**: ../systems/02-战斗手感.md
> **当前代码状态**: 已存在，路径 `Assets/Scripts/Modules/VFX/VFXModule.cs`（01-tattoo-framework-rewrite 已完成 URP 透明 + Material 共享池优化）

---

## 一、模块职责一句话

订阅 `VFXTriggerEvent`，根据 `ShapeName × ElementName` 在场景内用代码生成（LineRenderer / ParticleSystem / MeshRenderer）的临时 GameObject 表现战斗视觉，所有资产零 prefab 依赖，到期自动销毁。

---

## 二、IGameModule 接口签名

```csharp
namespace Tattoo.VFX
{
    // VFXModule.cs:28
    public sealed class VFXModule : IGameModule, ITickable
    {
        public int    ModuleCategory => 3;                          // 游戏逻辑层
        public Type[] Dependencies   => new[] { typeof(SpawnerModule) };

        // InitializeAsync：预热 7 个 Element Material 池 + 订阅 VFXTriggerEvent
        public UniTask InitializeAsync(CancellationToken ct = default);

        // ShutdownAsync：Dispose 订阅 + Destroy 全部 VFXInstance + 销毁 Material 池
        public UniTask ShutdownAsync(CancellationToken ct = default);

        // ITickable.OnUpdate：推进全部 VFXInstance 生命周期、执行 Tick 委托、超时销毁
        public void OnUpdate(float dt);
    }
}
```

**关键内部数据结构**（`VFXModule.cs:678`）：

```csharp
sealed class VFXInstance
{
    public GameObject                  Go;
    public float                       Duration;
    public float                       Elapsed;
    public Action<VFXInstance, float>  Tick;   // 可空；null = 仅等到期
}
```

`_instances`（`List<VFXInstance>`）在 `OnUpdate` 倒序遍历，保证 `RemoveAt` 安全（`VFXModule.cs:110`）。

---

## 三、订阅 / 发布事件（全签名）

### 订阅

```csharp
// CONTRACT §1.2 — v2 加入 Actor Source 字段
public class VFXTriggerEvent
{
    public Actor   Source;        // v2 新增：区分 50 个 actor 的发起者
    public string  PartName;
    public string  ElementName;
    public string  ShapeName;
    public Target  PrimaryTarget;
    public Target[] NearbyTargets;
    public float   Magnitude;
    public bool    Intercepted;
}
```

处理方法：`OnVFXTrigger(VFXTriggerEvent e)`（`VFXModule.cs:126`）。

> **v2 迁移点**：现有代码（`VFXModule.cs:132`）用 `_spawner.Player.transform.position` 作为固定发射源；v2 需改为从 `e.Source` 对应的 Actor GameObject 上取坐标——在 `TryGetActorPos(Actor src)` 中封装，避免散点修改。

### 发布

无。VFXModule 纯消费侧，不向 EventBus 发布任何事件。

---

## 四、DataTable Schema

**当前决策：暂不引入 `VFXProfileConfig.json`，所有粒子参数保持代码内硬编码。**

理由：
- VFX 种类 = 7 Element × 8 ShapeName，但 `SpawnElementDecoration` 仅差异化处理 Frost / Holy / Pure 三种（`VFXModule.cs:612`）；实际参数组合数量可控
- 代码内参数修改可即时热重载（Domain Reload），而 DataTable 路径需要额外的 DataTableGenerator 步骤，收益不匹配
- 当 art-vfx 提出"每种元素都需要独立调参"时，可在该时间点迁入 DataTable

若将来确认引入，推荐 Schema：

```json
// VFXProfileConfig.json（预留草案，暂不实装）
{
  "table": "VFXProfile",
  "fields": ["Element", "Shape", "ParticleCount", "StartSpeed", "Duration", "StartSize"],
  "rows": [
    { "Element": "Frost", "Shape": "AOEBurst", "ParticleCount": 50, "StartSpeed": 3.5, "Duration": 0.4, "StartSize": 0.2 }
  ]
}
```

---

## 五、与其他模块的交互序列

```mermaid
sequenceDiagram
    participant TM as TattooModule
    participant EB as EventBus
    participant VM as VFXModule
    participant SM as SpawnerModule

    TM->>EB: Publish(VFXTriggerEvent { Source, ShapeName, ElementName, PrimaryTarget })
    EB->>VM: OnVFXTrigger(e)
    VM->>SM: Player.transform / Enemies[] 取坐标
    alt ShapeName == "SingleHit"
        VM->>VM: SpawnBeam(src, dst, color)
        VM->>VM: SpawnElementDecoration(elementName, dst, color)
    else ShapeName == "AOEBurst"
        VM->>VM: SpawnAOEBurst(center, color, radius)
        loop NearbyTargets
            VM->>VM: SpawnSpark(pos, color)
        end
    else ShapeName == "TrailZone"
        VM->>VM: SpawnTrailZone(center, color, radius, duration)
    else ShapeName == "SummonForm"
        VM->>VM: SpawnSummonForm(center, color, height, duration)
    else Intercepted == true
        VM->>VM: SpawnRing(source, color, 0.9f, 0.6f)
    end
    Note over VM: VFXInstance 入队 _instances
    loop OnUpdate(dt) 每帧
        VM->>VM: Elapsed+=dt; Tick委托; 超时 Destroy
    end
```

**后处理顺序备注**：VFX 视觉元素均为透明 Unlit 材质（renderQueue=3000），在 URP 的 Transparent 阶段绘制。Bloom 后处理（若启用）在 Transparent 之后的 Post-process 阶段叠加，VFX 自发光颜色会被 Bloom 放大——这是预期行为。ToneMap 在 Bloom 之后执行。

---

## 六、50 actor 性能预算

### 当前基线

| 指标 | 现有 v1 | 目标 v2 |
|---|---|---|
| 并存 VFX 实例上限 | 32（注释说明） | **64** |
| Material 种类 | 7（per-element 一份 + BuildTransparentUnlitMat 按需 new） | 7（共享池，LineRenderer 走 sharedMaterial） |
| 单次触发耗时 | <0.3ms | <0.3ms（队列削峰后保持） |
| GC alloc per trigger | 约 1 个 VFXInstance + 1 个闭包（Tick lambda） | 同上，无法消除（Unity GC.Alloc） |

### 50 actor 并发 spike 分析

50 actor 同帧触发（如 AOEBurst 全场）：
- 最坏情况：50 × AOEBurst → 50 个 ParticleSystem + 最多 50×N 个 Spark = 瞬时 200+ 实例
- `_instances.Count` 爆表将导致 `OnUpdate` 遍历耗时突增（每实例约 0.005ms，200 实例 = 1ms）

### 削峰策略（v2 补强方案）

**方案 A：全局上限 + 最旧优先淘汰**

```csharp
const int GlobalVFXCap = 64;

void Push(VFXInstance inst)
{
    if (_instances.Count >= GlobalVFXCap)
    {
        // 淘汰最旧的一个（_instances[0] 是最老的，倒序遍历不影响）
        var oldest = _instances[0];
        if (oldest.Go != null) UnityEngine.Object.Destroy(oldest.Go);
        _instances.RemoveAt(0);
    }
    _instances.Add(inst);
}
```

**方案 B：per-actor 预算（推荐与方案 A 结合）**

每个 `VFXTriggerEvent.Source` 最多允许同时存在 3 个活跃 VFX 实例；超过时掐掉该 actor 最旧的实例：

```csharp
const int PerActorCap = 3;

void Push(VFXInstance inst, Actor source)
{
    // 统计该 actor 当前实例数
    int count = 0;
    for (int i = 0; i < _instances.Count; i++)
        if (_instances[i].SourceActor == source) count++;

    if (count >= PerActorCap)
    {
        // 找最旧并销毁
        for (int i = 0; i < _instances.Count; i++)
        {
            if (_instances[i].SourceActor != source) continue;
            if (_instances[i].Go != null) UnityEngine.Object.Destroy(_instances[i].Go);
            _instances.RemoveAt(i);
            break;
        }
    }
    _instances.Add(inst);
}
```

**方案 C：距离裁剪**

在 `OnVFXTrigger` 入口判断，距玩家 >30m 的事件直接 return，不生成任何实例：

```csharp
// VFXModule.cs OnVFXTrigger 头部追加
if (e.Source != null)
{
    var actorPos = TryGetActorPos(e.Source);
    var playerPos = _spawner.Player.transform.position;
    if (actorPos.HasValue && Vector3.SqrMagnitude(actorPos.Value - playerPos) > 900f) // 30^2
        return;
}
```

### Material 共享池收益（SRP Batcher）

7 个 Material 实例 × 64 VFX 实例 → SRP Batcher 将同 Material 的 DrawCall 合批。最坏情况下 drawcall 数 = 7（而非 64），与 v1 相同。`LineRenderer.sharedMaterial` 是关键（`VFXModule.cs:311`，`VFXModule.cs:347`）；Zone/Pillar 使用 `new Material(shared)` 的 per-instance 拷贝会打断合批，需在 50 actor 场景下实测是否可接受。

### 内存估算

- 7 个共享 Material：约 7 × 0.5KB = 3.5KB
- 64 个 VFXInstance（对象头 + 3 字段 + 闭包）：约 64 × 200B = 12.8KB
- ParticleSystem 原生内存（maxParticles=60）：约 60 × 48B × 实例数

总 managed 堆增量 < 20KB，符合 CONTRACT §4「GC 总预算 <100KB/秒」（VFX 占比约 1/5）。

---

## 七、伪联机 → 真联机的迁移点

VFXModule 是**纯本地表现层**，无状态同步需求：

| 场景 | 当前（伪联机） | 真联机 |
|---|---|---|
| 事件来源 | 本地 EventBus | 网络层收到远端 actor 操作后，本地重新 Publish VFXTriggerEvent |
| VFXTriggerEvent 内容 | 本地 TattooModule 产生 | 网络层广播，client 侧 VFXModule 订阅相同事件 |
| Actor 坐标 | `SpawnerModule.Enemies[]` | NetworkActorModule 维护 actor 位置表，VFXModule 换一个坐标查询接口 |
| 时序 | 与战斗同帧 | 带网络延迟（<100ms），视觉上可接受（VFX 本身持续时间 >150ms） |

**唯一需要改动的接口**：`TryGetPos(Target t)`（`VFXModule.cs:215`）和 v2 新增的 `TryGetActorPos(Actor src)` 需从网络 actor 坐标表查询而非 `SpawnerModule`，这是唯一迁移点。VFXModule 其他代码不感知网络层。

---

## 八、测试策略

### EditMode 测试

目标：验证 Material 池预热 + 实例计数边界。

```csharp
[Test]
public void VFXModule_MaterialPool_Contains7Elements()
{
    // 通过反射读取 _matPool，验证 Count == 7
    // 不需要 Unity Player，无 GameObject 创建
}

[Test]
public void VFXInstance_Push_RespectsGlobalCap()
{
    // 构造 VFXModule 并模拟连续 Push 超过 64 次
    // 断言 _instances.Count <= 64
}
```

### PlayMode 测试（50 actor spike 压测）

```csharp
[UnityTest]
public IEnumerator VFXModule_50ActorConcurrentTrigger_FrameRateAbove50()
{
    // 1. 初始化 SpawnerModule + VFXModule
    // 2. 同帧发布 50 个 VFXTriggerEvent（ShapeName=AttackHit / AOEBurst 各半）
    // 3. yield return new WaitForSeconds(1f)
    // 4. 采样 Application.targetFrameRate / Time.deltaTime
    // 5. 断言 drawcall < 20 + 帧时间 < 20ms + GC.GetTotalMemory 增量 < 50KB
}
```

验收指标：
- 50 actor 同帧触发 → `_instances.Count` ≤ 64（GlobalVFXCap 生效）
- 帧率 ≥ 50fps（16.7ms 帧预算下 VFX tick 不超过 1ms）
- Drawcall 增量 ≤ 14（7 Material × 2 通道）

---

## 九、风险与开放问题

### 风险 1：并发 spike 时玩家视觉混乱

50 actor 同帧 AOEBurst 导致屏幕布满粒子，玩家看不清战场。

**推荐缓解**：屏幕中心区（玩家 ±5m 范围）VFX 优先级最高，正常渲染；外围（5m~30m）降级为粒子数减半（`main.maxParticles` 减半）；>30m 直接 skip（见方案 C）。优先级判断加在 `OnVFXTrigger` 路由层，不侵入各 Spawn 方法。

### 风险 2：per-instance Material 拷贝打断 SRP Batcher 合批

Zone / Pillar 类效果使用 `new Material(shared)` 做 alpha 动画（`VFXModule.cs:286`）。50 actor 并发下可能产生 >7 个 Material 实例，SRP Batcher 退化为逐实例 DrawCall。

**推荐缓解**：改用 `MaterialPropertyBlock` 驱动 alpha 动画（`SetColor` 到 mpb），避免 Material 拷贝。注意 URP Unlit 默认兼容 MPB，但需验证 SRP Batcher 是否对 LineRenderer 生效（LineRenderer 有已知的 SRP Batcher 兼容性限制）。

### 风险 3：LineRenderer.allowOcclusionWhenDynamic = false 的永久代价

当前所有 LineRenderer 关闭了 URP 动态遮挡剔除（`VFXModule.cs:316`）。50 actor 场景下大量 Beam 不被剔除，GPU overdraw 增加。

**推荐缓解**：仅对玩家发起的 Beam 关闭（视觉优先），AI 发起的 Beam 保持默认值（允许剔除）。

### 开放问题 1：URP Bloom 是否启用

当前 VFX 颜色均为线性色，`_BaseColor` 不含 HDR 值，Bloom 不会被触发。若 art-director 要求发光效果，需将 `_BaseColor` 颜色强度提升至 HDR（如 intensity=2.0），并在 URP Volume 中启用 Bloom。

**决策建议**：alpha 期（第一个可玩 build）决定是否开 Bloom，VFXModule 代码无需提前改动，只需调整颜色强度参数。

### 开放问题 2：`Actor Source` 字段迁移时机

CONTRACT §1.2 要求 `VFXTriggerEvent` 加 `Actor Source` 字段。现有 `TattooEvents.cs:95` 中尚无此字段。v2 实现时需与 client-unity 协调修改 `TattooEvents.cs`（不属于 VFXModule 自身改动范围，属于 CONTRACT §六「append」规则）。

---

## 引用

- `Assets/Scripts/Modules/VFX/VFXModule.cs`（反向工程基础）
- `Assets/Scripts/Events/TattooEvents.cs`（`VFXTriggerEvent` 当前签名）
- `openspec/changes/05-gdd-v2-full-design-docs/CONTRACT.md` §1.2 §4
- `openspec/changes/01-tattoo-framework-rewrite/`（URP 透明 + 共享池优化历史）
