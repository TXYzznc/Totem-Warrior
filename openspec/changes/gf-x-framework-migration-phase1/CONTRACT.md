# CONTRACT: GF_X framework migration phase 1

## 1. 目标边界

本 change 的第一目标是把 GF_X 作为后续主框架迁入当前项目，并先跑通框架、工具、诊断和迁移约束。当前项目业务层必须保留，第一阶段不要求所有业务完全改用 GF_X 生命周期。

## 2. 活动目录

GF_X 迁入后的主活动区：

```text
Assets/Game/
GameData/
Docs/
```

当前项目业务保留区：

```text
Assets/Scripts/
Assets/Resources/
Assets/Scenes/
Assets/Settings/
Assets/Tests/
```

共享或敏感区：

```text
Packages/manifest.json
ProjectSettings/EditorBuildSettings.asset
Assets/Demigiant/
Assets/Plugins/
```

共享或敏感区不得在未盘点依赖和启动影响前直接覆盖。

## 3. 核心文件确认门槛

以下文件可以在后续阶段修改，但每次修改前必须先向用户说明“为什么改、改什么、如何验证”，得到确认后再执行：

| 文件/区域 | 原因 |
|---|---|
| `Assets/Scripts/Core/GameApp.cs` | 当前启动入口和模块注册中心 |
| `Assets/Scripts/Core/ModuleRunner.cs` | 当前模块生命周期和依赖图 |
| `Assets/Scripts/Core/EventBus.cs` | 当前跨模块事件通信 |
| `Assets/Scripts/Core/IGameModule.cs` | 当前模块契约 |
| `Assets/Scripts/Core/GameTickDriver.cs` | 当前 Tick/LateTick 驱动 |
| `Assets/Scripts/Modules/Input/InputModule.cs` | 项目输入唯一入口 |
| `Assets/Scripts/Modules/DataTable/DataTableModule.cs` | 当前配置表加载入口 |
| `Assets/Scripts/Modules/UI/UIModule.cs` | 当前 UI 加载和生命周期入口 |
| `Assets/Scripts/Modules/Scene/SceneModule.cs` | 当前场景加载入口 |
| `ProjectSettings/EditorBuildSettings.asset` | 启动场景和构建场景顺序 |
| `Packages/manifest.json` | Unity 包依赖和版本 |

## 4. 依赖迁移规则

- 不得同时保留当前项目与 GF_X 的重复依赖。
- UniTask 以 GF_X `Assets/Plugins/UniTask` 为准；当前项目 `Packages/manifest.json` 中的 `com.cysharp.unitask` 入口必须移除。
- DOTween 以 GF_X `Assets/Plugins/DOTween` 为准；当前项目 `Assets/Demigiant` 下的 DOTween/DOTweenPro 入口必须从 Unity 编译路径移除。
- 当前业务脚本若只依赖普通 `DG.Tweening`，应改用 GF_X DOTween 继续编译；若发现 DOTweenPro 专有 API，再单独确认替代方案。
- URP、TextMesh Pro、Newtonsoft 等包必须先对比版本和引用来源。
- GF_X 必需依赖如果必须迁入，应记录来源、目标路径、冲突检查结果和验证结果。

## 5. 示例内容规则

- GF_X 示例项目可以保留在 `Examples` 目录下。
- 示例内容不得进入默认启动流程、构建场景、运行初始化链或当前业务注册链。
- 示例内容不得散落到当前项目业务目录中。

## 6. AI DataTable 与诊断规则

- AI DataTable 工具必须支持 Json 校验、Json 逆向更新 xlsx、备份、回滚和 diff 报告。
- 诊断报告必须写入可追踪位置，默认使用 `GameData/Diagnostics/Reports`。
- 新增功能脚本后，如果效果不对，应优先补充测试点或诊断场景，而不是只依赖断点。
- 诊断报告应尽量包含时序、输入、状态变化、事件、资源路径、结果和错误上下文。

## 7. 验收口径

第一阶段完成时必须满足：

1. 当前项目能打开、编译、启动。
2. GF_X 框架、工具、诊断和文档入口已迁入。
3. 当前业务脚本和资源不丢。
4. 不存在新的 `AAAGame` 活动路径污染。
5. 不存在新的硬编码本机绝对路径污染。
6. 示例内容不进入默认启动或运行流程。
7. 核心文件修改已按本契约完成确认。
