# Proposal — 01-tattoo-framework-rewrite

> **范围**：把现有 `Assets/Scripts/Tattoo/` 业务整体重写为符合 AI_Friendly 框架（IGameModule / ModuleRunner / EventBus / DataTable / ResourceModule）的实现。
> **决策日期**：2026-06-24
> **决策方式**：grill-me 两轮反问（共 8 问）+ 用户明确「完全重构」回答
> **预计 phases**：3 阶段（A 框架与配置 / B 核心模块 / C 战斗与 UI）

---

## 为什么做

GameDesinger 项目刚刚集成了 AI 协作框架与 IGameModule 架构（[REFACTOR_REPORT.md](../../../REFACTOR_REPORT.md)），但原有 Tattoo 业务代码完全脱离框架：

| 现状 | 框架规范 | 冲突 |
|---|---|---|
| `enum GameEvent` + `Composer.Fire(ev)` 直接分发 | `class XxxEvent` + `EventBus.Publish` + `[EventHandler]` | 事件机制完全不同 |
| `CombatRunner.Awake` 程序化创建 Camera/光/Player/Enemy | `GameApp.Start() → ModuleRunner.StartAsync → Modules` | 启动入口冲突 |
| `BodyPart 6 + ElementBehavior 7 + EffectShape 8 = 21 SO 子类多态` | DataTable + 策略代码 | 数据载体不符 |
| `Player.cs` 直接处理 WASD / 鼠标输入 | 「所有按键输入必须走 `InputModule`」 | 输入规则违反 |
| `Resources.Load` / `GameObject.CreatePrimitive` 散落 | `ResourceModule + ResourceConfig` 统一加载 | 资源加载违反 |
| `OnGUI` IMGUI 散落到 Runner 中 | `UIModule` + UI Toolkit | UI 入口违反 |

不解决 → 框架戒律永远没有真实代码可参照；24 agents 的 system prompt 中所有"框架戒律"段对 GameDesinger 现状毫无意义；CLAUDE.md 的死链长期存在。

## 目标（DoD）

- 现有 `Assets/Scripts/Tattoo/` 12 个 .cs 全部迁出（已暂存 `Assets/_Legacy~/Tattoo/`，Unity 忽略 `~` 后缀目录）
- 21 个 SO 子类的"数据部分" → JSON DataTable（`tattoo_part / tattoo_color / tattoo_pattern / tattoo_element / tattoo_shape`）
- 21 个 SO 子类的"行为部分" → 策略代码（按 ID dispatch），写入 `TattooModule` 内部
- 3 个新 IGameModule：**TattooModule / CombatModule / SpawnerModule**（参 [design.md](./design.md)）
- 所有 `GameEvent` 枚举值拆为独立 `class XxxEvent`（6 个）
- 所有按键输入走 `InputModule`（新增方法 / 新的 `[EventHandler]`）
- UI 改 UI Toolkit（UXML + USS）+ 通过 `UIModule` 接入
- `Assets/Scenes/Launch.unity` 作为唯一启动场景，仅挂 `GameApp`
- 测试覆盖：每个新模块至少 1 个 Edit-mode 测试 + 1 个 Play-mode 集成测试
- 文档：每个模块带 `README.md` + 更新 `项目知识库（AI自行维护）/wiki/` 条目

## 非目标（明确不做）

- 不保留 IMGUI（演示版也不留——CLAUDE 用户决策已确认改 UI Toolkit）
- 不保留 3 个旧场景（CombatScene / SampleScene / TattooDemo 已删）
- 不引入新依赖（继续用 UniTask / Newtonsoft.Json / DOTween）
- 不做反作弊 / 网络同步（这是单机原型）
- 不做 Addressables 远程热更（本地 Resources/ 足够）

## Phase 拆分

详见 [tasks.md](./tasks.md)。

| Phase | 主体 | 完成标志 |
|---|---|---|
| **A** | 配置 schema + DataTable 生成 + 公共骨架 | 5 张 JSON 表 + 对应 `.cs` 由 generator 产出；`STGEvents` 风格的 `TattooEvents.cs` 写入 `Assets/Scripts/Events/` |
| **B** | TattooModule 实现 + Composer/Passive/Build 移入 | TattooModule 通过 EditMode 单元测试；EventBus 上能正确发出/接收 OnAttack/OnCrit/OnDamaged 等 6 个事件 |
| **C** | CombatModule + SpawnerModule + UI Toolkit 接入 | Launch.unity 可跑通完整战斗流程；UI Toolkit 面板能切换 Build 与查看日志 |

## 风险与回滚

| 风险 | 缓解 |
|---|---|
| JSON 不能表达 ScriptableObject 多态行为 | 用 `Type` 字段 + 策略 switch；策略代码在 `TattooModule` 内 |
| 输入接 InputModule 时序问题 | 严格遵循 §四 多 Agent「骨架先行」——先定 `Events/TattooEvents.cs` 接口签名 |
| UI Toolkit 学习曲线 | 第一版做最小可用面板，Phase C 末期再优化视觉 |
| 3 个 IGameModule 互相引用 | 走「骨架先行+并行填充」：先定空壳 + 全局事件总表 |

回滚路径：
1. 把 `Assets/_Legacy~` 改回 `Assets/_Legacy/`（去掉 `~`）让 Unity 识别
2. 删除新写的 TattooModule/CombatModule/SpawnerModule
3. 在 GameApp 中跳过这 3 个 module 的 AddModule
4. 旧 Tattoo 代码回归编译，CombatRunner 重新工作

## 引用

- [.claude/CLAUDE.md](../../../.claude/CLAUDE.md) — 框架戒律与路由
- [.claude/SKILL_MATRIX.md](../../../.claude/SKILL_MATRIX.md) — 涉及 agents：client-lead（架构）/ client-unity（实现）/ gd-system（数值）/ art-ui（UI 视觉）/ qa-engineer（测试）
- [.claude/agents/](../../../.claude/agents/) — 各 agent system prompt
- [AI友好型项目探讨/03-模块系统详细设计.md](../../../AI友好型项目探讨/03-模块系统详细设计.md)
- [AI友好型项目探讨/04-事件系统详细设计.md](../../../AI友好型项目探讨/04-事件系统详细设计.md)
