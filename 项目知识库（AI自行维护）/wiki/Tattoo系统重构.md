---
title: Tattoo 系统重构（v1.0 → v2.0 框架化）
owner: client-lead
created: 2026-06-24
last_updated: 2026-06-24
status: active
related_specs:
  - openspec/changes/01-tattoo-framework-rewrite/
related_skills:
  - unity-architecture-di
  - unity-foundations
  - state-machine
  - unity-async-patterns
  - design-system
  - grill-me
---

# Tattoo 系统重构（v1.0 → v2.0 框架化）

## 一、背景

GameDesinger 项目 2026-06-24 完成 AI 协作框架集成（详见 [REFACTOR_REPORT.md](../../REFACTOR_REPORT.md)），引入 IGameModule / ModuleRunner / EventBus / DataTable / ResourceModule 体系。但原 Tattoo 业务代码（12 个 .cs，含 Composer/CombatRunner/21 SO 多态子类）完全脱离框架：

| 现状 | 框架要求 |
|---|---|
| `enum GameEvent` + `Composer.Fire` | `class XxxEvent` + `EventBus + [EventHandler]` |
| `CombatRunner.Awake` 程序化建场 | `GameApp → ModuleRunner.StartAsync` |
| `BodyPart/ColorSO/PatternSO/ElementBehavior/EffectShape` 21 SO 子类多态 | DataTable + 策略代码 |
| `Player.cs` 直接读 WASD/鼠标 | 所有按键走 `InputModule` |
| `Resources.Load` / `CreatePrimitive` | `ResourceModule + ResourceConfig` |
| `OnGUI` IMGUI | `UIModule + UI Toolkit` |

## 二、决策

**完全重构**——旧代码不复用，业务在框架下重写。grill-me 两轮反问后明确以下方向（2026-06-24 用户确认）：

1. **事件**：6 个 GameEvent enum 拆为 6 个 class 事件，全部走 EventBus
2. **启动**：CombatRunner 被砍，职责拆为 SpawnerModule + CombatModule + UIModule
3. **配置**：21 个 SO 子类的"数据部分"换 JSON DataTable；"行为部分"换策略代码
4. **模块**：拆为 3 个 IGameModule —— TattooModule（Composer/Passive）+ CombatModule（主循环）+ SpawnerModule（实体生成）
5. **旧代码**：移到 `Assets/_Legacy~/Tattoo/`（Unity 忽略 `~` 后缀目录），仅供参考
6. **UI**：IMGUI → UI Toolkit（UXML + USS），通过 UIModule 接入
7. **场景**：3 个旧场景（CombatScene/SampleScene/TattooDemo）删；新建 Launch.unity 为唯一启动场景
8. **执行节奏**：按模块逐个，Phase A → B → C，每个 phase 末跑测试用户确认

## 三、被否定的备选

| 备选 | 否定理由 |
|---|---|
| **保留 SO 多态 + 仅迁事件机制** | 违反"ScriptableObject 是配置不是数据库"戒律，且数值策划修改 SO 字段要重启 Unity，配置表更友好 |
| **保留 enum GameEvent，外套 class 适配层** | 双层事件机制长期维护成本高；agent 系统中所有 `[EventHandler]` 示例都基于 class 事件，混双层会迷惑 AI |
| **CombatRunner 仅改造为 IGameModule，不拆三模块** | CombatRunner 已经混入相机/灯光/Player/Enemy/UI 五种职责，单一模块违反 Single Responsibility |
| **保留 3 个旧场景** | 场景中 GameObject 挂的 MonoBehaviour 全部 missing reference，反而比新建复杂 |
| **IMGUI 演示版保留** | 长期项目要 UGUI 或 UI Toolkit；IMGUI 不适合产品化；Unity 6 推 UI Toolkit |
| **一次性大重构，中间不验证** | 风险过高，单点失败回滚成本大；分 phase 每阶段都可验证 |

## 四、影响范围

### 代码

- 删（移到 _Legacy~）：12 个 .cs（Assets/Scripts/Tattoo/*）
- 新增：~30 个 .cs（3 模块 + 21 策略 + 数据类 + UI Form + 测试）
- 配置：5 张 .xlsx + 5 张 .json（DataTable）
- UI：1 个 .uxml + 1 个 .uss

### Agent

主导：**client-lead**（架构）+ **client-unity**（实现）
辅助：**gd-system**（数值表 5 张）+ **art-ui**（UI 视觉，如果用户要 UGUI 而非 UI Toolkit 纯文本）+ **qa-engineer**（测试）

### Skill

- `unity-architecture-di` — Module / EventBus / DI 模式
- `unity-foundations` — Component / Prefab / ScriptableObject 用法
- `state-machine` — Module 内部状态机
- `unity-async-patterns` — InitializeAsync / 取消令牌
- `physics-collision` — Player/Enemy 碰撞（Phase C）
- `unity-ui` — UI Toolkit / uGUI

## 五、过时检查

- **完成时机**：Phase C 通过 PlayMode 集成测试 + 用户验收（预计 1-2 周）
- **过时条件**：
  - 框架核心 API 变化（IGameModule / EventBus / ModuleRunner 接口改）
  - Tattoo 玩法整体重做（不再是 6 部位 × 7 颜色 × 8 图案的三层原子）
  - 引擎升级到 Unity 7+ 或换 Godot/UE（重新评估架构）
- **review 周期**：完成后 3 个月内不动；之后随项目需求评估

## 六、详细链接

- 提案：[openspec/changes/01-tattoo-framework-rewrite/proposal.md](../../openspec/changes/01-tattoo-framework-rewrite/proposal.md)
- 设计：[openspec/changes/01-tattoo-framework-rewrite/design.md](../../openspec/changes/01-tattoo-framework-rewrite/design.md)
- 任务：[openspec/changes/01-tattoo-framework-rewrite/tasks.md](../../openspec/changes/01-tattoo-framework-rewrite/tasks.md)
- 规格：[openspec/changes/01-tattoo-framework-rewrite/specs/tattoo/spec.md](../../openspec/changes/01-tattoo-framework-rewrite/specs/tattoo/spec.md)
- 原 Tattoo 代码（参考）：[Assets/_Legacy~/Tattoo/](../../Assets/_Legacy~/Tattoo/)
