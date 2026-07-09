# Design: GF_X framework migration phase 1

## Context

当前项目已经有一套自研运行框架：`GameApp + ModuleRunner + EventBus + DataTableModule`。这套框架承担启动、模块生命周期、事件、配置表、输入、UI、场景和业务系统装配。用户希望以刚优化过的 `C:\Users\WIN10\Desktop\GF_X-master` 为新主框架，但第一阶段必须保证当前项目能继续打开、编译、启动，且现有业务代码和资源不丢。

GF_X 侧已经完成基础清理和增强：

- 活动目录统一为 `Assets/Game` 与 `GameData`。
- 示例项目集中到 `Examples`，不进入默认启动和运行流程。
- AI DataTable 工具支持 Json 校验、逆向更新 xlsx、备份、回滚和 diff 报告。
- 诊断工具支持运行链路、时序、结果、日志和迁移路径污染检查。
- 核心输入边界、预加载失败路径、启动链诊断和迁移路径契约已补强。

当前项目约束：

- 现有玩法、业务脚本和资源必须保留。
- 业务文件可以移动或重命名，但不能删除功能。
- 触及 `Assets/Scripts/Core/*`、启动入口、输入、配置表、UI、场景加载等旧框架绑定文件前，必须先向用户点名确认。
- 第一阶段不要求现有业务完全接入 GF_X 运行流程。

## Goals / Non-Goals

**Goals:**

- 将 GF_X 第一阶段框架、工具、诊断和文档入口迁入当前项目。
- 保持 GF_X 相对路径语义，避免新的硬编码绝对路径。
- 建立当前项目与 GF_X 的清晰边界，让后续重构有可验证入口。
- 迁入 AI DataTable 与诊断能力，使后续功能开发可以按测试点和诊断报告定位问题。
- 保证第一阶段结束时当前项目仍可打开、编译、启动。

**Non-Goals:**

- 不在第一阶段完成全部玩法业务的 GF_X 化。
- 不一次性替换 `GameApp`、`ModuleRunner`、`EventBus` 或所有业务模块。
- 不删除当前项目业务资源、场景、配置表或脚本。
- 不原样复制会与当前项目冲突的重复依赖。
- 不把 GF_X 示例项目重新混入默认启动流程。

## Decisions

### Decision 1: 采用框架先行的分阶段迁移

第一阶段迁入 GF_X 框架区、工具区、诊断区和迁移契约；业务接入在后续阶段逐块进行。

Alternatives considered:

- 一次性替换当前启动和业务框架：速度快，但会同时触发启动、依赖、DataTable、UI、场景和业务模块风险，不适合作为第一步。
- 只迁工具不迁框架：风险低，但无法形成以 GF_X 为主的后续架构。

Rationale:

分阶段迁移符合用户选择的“GF_X 为主、现有业务全保留、核心文件可改但先确认”。它允许先获得工具和诊断收益，再处理核心启动链和业务重构。

### Decision 2: GF_X 活动区固定为 `Assets/Game` 与 `GameData`

迁入的 GF_X 运行、Editor、工具、诊断和数据入口以 `Assets/Game`、`GameData` 为主路径。示例内容如果保留，必须集中在 `Examples`，不得进入默认启动或运行链路。

Alternatives considered:

- 保持 GF_X 原仓库目录之外的任意路径：会增加工具路径适配成本。
- 融入 `Assets/Scripts`：会让框架和现有业务再次混杂。

Rationale:

GF_X 已经完成 `AAAGame` 到 `Game` 的清理，沿用该路径能减少迁移变形，也方便后续诊断扫描硬编码污染。

### Decision 3: 依赖先盘点再迁入，UniTask 和 DOTween 以 GF_X 为准

迁移前必须对比 `Packages/manifest.json`、`Assets/Plugins`、`Assets/Demigiant` 和 GF_X 插件目录。用户已确认 UniTask 和 DOTween 以 GF_X 自带版本为准，因此当前项目的 `com.cysharp.unitask` 包入口与 `Assets/Demigiant` DOTween/DOTweenPro 入口需要从 Unity 编译路径移除。TextMesh Pro、URP、Newtonsoft 等已有或等价依赖不得重复导入。

Alternatives considered:

- 原样复制 GF_X 全部插件：实现简单，但最容易造成重复类型、asmdef 引用和 Unity 编译冲突。
- 复用当前项目 UniTask/DOTween：短期少改，但与“以 GF_X 为主框架”的依赖来源不一致。
- 全部依赖都不迁：工具和运行时可能缺失必要基础库。

Rationale:

当前项目已有 UniTask 和 DOTween 来源，GF_X 也带同类依赖。以 GF_X 为准可以减少后续框架差异，但需要同步调整 asmdef 和包清单，避免重复类型与缺失引用。

### Decision 4: 核心文件设置确认门槛

以下文件或系统在第一阶段可以修改，但执行前必须先列出原因并得到用户确认：

- `Assets/Scripts/Core/GameApp.cs`
- `Assets/Scripts/Core/ModuleRunner.cs`
- `Assets/Scripts/Core/EventBus.cs`
- `Assets/Scripts/Core/IGameModule.cs`
- `Assets/Scripts/Core/GameTickDriver.cs`
- `Assets/Scripts/Modules/Input/InputModule.cs`
- `Assets/Scripts/Modules/DataTable/DataTableModule.cs`
- `Assets/Scripts/Modules/UI/UIModule.cs`
- `Assets/Scripts/Modules/Scene/SceneModule.cs`
- `ProjectSettings/EditorBuildSettings.asset`
- `Packages/manifest.json`

Alternatives considered:

- 完全禁止改核心文件：无法完成从旧框架向 GF_X 的实质迁移。
- 直接修改核心文件：容易破坏当前项目启动和业务保留要求。

Rationale:

这些文件是当前旧框架绑定点。它们确实需要逐步改，但每次改动都应有目的、边界和回滚口径。

### Decision 5: 诊断工具作为长期开发基础设施

GF_X 诊断工具不是一次性脚本。迁入后应作为后续功能开发的测试点、日志、时序和因果分析入口。新增功能脚本如果效果不对，应优先补充诊断场景或测试点，让报告指出输入、状态变化、事件、资源加载和输出结果。

Alternatives considered:

- 每个功能临时写测试脚本：短期快，但积累后难复用。
- 只依赖 Unity Console 和人工断点：定位成本高，AI 也难以稳定理解上下文。

Rationale:

用户明确希望“整体一块解决”测试诊断问题。把诊断做成框架能力，后续开发才能持续受益。

## Risks / Trade-offs

- [Risk] 第一阶段会短期存在旧框架和 GF_X 框架并行边界。Mitigation: 用 `CONTRACT.md` 固定目录、依赖、启动和核心文件门槛。
- [Risk] 依赖冲突导致 Unity 编译失败。Mitigation: 先做依赖清单和排除列表，再迁入插件。
- [Risk] GF_X 工具存在旧项目路径假设。Mitigation: 迁入后运行路径污染诊断，扫描绝对路径、`AAAGame` 和示例混入。
- [Risk] 直接切换启动链会破坏现有业务。Mitigation: 第一阶段不强制切换业务启动链，核心文件修改前单独确认。
- [Risk] 诊断报告覆盖不足。Mitigation: 将测试点规范写入 spec，后续每个新增功能按诊断点补充。

## Migration Plan

1. 完成 openspec 方案、设计、规格、契约和任务清单。
2. 盘点当前项目和 GF_X 的目录、依赖、asmdef、包版本、场景和启动入口。
3. 制定迁入排除列表，移除当前项目 UniTask/DOTween 入口，迁入 GF_X UniTask/DOTween，并排除示例默认启动、旧绝对路径和旧 `AAAGame` 污染。
4. 迁入 `Assets/Game`、`GameData`、GF_X Editor 工具、AI DataTable 和诊断工具。
5. 根据当前项目 asmdef 和包结构做最小适配。
6. 增加当前项目迁移诊断：路径契约、依赖冲突、业务保留、输入边界、启动链记录。
7. 运行 Unity 编译、Editor 测试或诊断菜单，保存结果。
8. 输出需要进入下一阶段的核心文件修改清单，并逐项请求确认。

Rollback strategy:

- 第一阶段优先采用新增文件和隔离目录，回滚时可按迁入清单移除新增 GF_X 文件。
- 如果已确认并修改核心文件，则每次修改必须记录对应文件、目的和验证结果，回滚以对应补丁为单位执行。
- 不通过删除当前业务资源作为回滚手段。

## Open Questions

- GF_X 的 `UnityGameFramework` 依赖是否在第一阶段完整迁入，还是先迁入工具与诊断所需最小集合。
- 当前项目启动场景最终是否切换到 GF_X `Launch` 流程，还是先保留现有 `MainMenu/Launch` 并建立桥接层。
- 当前 `Resources/DataTable` 与 GF_X `GameData` 配置表流程的合并顺序需要在下一阶段确认。
