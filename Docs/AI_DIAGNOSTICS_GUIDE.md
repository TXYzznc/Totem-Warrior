# GF_X AI Diagnostics Guide

本文档记录 GF_X 后续开发时的诊断、测试点、日志规范。目标不是做一次性脚本，而是让每个功能都能留下 AI 可读、可复跑、能定位因果的诊断信息。

## 目标

- 固定体检：检查项目结构、启动工作区、AppConfigs、BuildSettings、资源规则、AI DataTable JSON 等基础健康状态。
- 功能场景：每个新功能可以注册自己的 `IGFDiagnosticScenario`，由诊断 Runner 自动发现并写入同一份报告。
- 时序追踪：功能关键路径使用 `GFTrace` 记录输入、条件、状态、资源、结果，方便从报告 timeline 还原发生顺序。
- AI 友好：报告必须包含明确的错误、警告、细节字段和最近 timeline，避免只靠人工打断点。

## 运行入口

Unity 菜单：

- `Game Framework/GameTools/Diagnostics/Run All`
- `Game Framework/GameTools/Diagnostics/Export Snapshot`
- `Game Framework/GameTools/Diagnostics/Open Latest Report`

批处理命令：

```powershell
& "<UnityEditor.exe>" -batchmode -quit -projectPath "<project-root>" -executeMethod UGF.EditorTools.GFDiagnosticRunner.RunAll -logFile "<project-root>\Temp\GFDiagnostics.log"
```

报告输出目录：

```text
GameData/Diagnostics/Reports
```

## 新功能测试点规范

新增功能时，按以下顺序决定测试点：

1. 纯算法、纯数据转换：优先写普通 EditMode 测试或诊断场景。
2. 配置表、资源、Prefab、ScriptableObject、BuildSettings：写 `EditMode` 诊断场景。
3. Procedure、UI、Entity、输入、资源加载、战斗流程等有时序的功能：写 `PlayMode` 或 `Any` 诊断场景，并在关键路径打 `GFTrace`。
4. 只在人工游玩中出现的问题：先补 `GFTrace`，再把复现路径沉淀成场景。

每个功能至少要覆盖：

- Arrange：依赖是否存在，配置是否可读，资源路径是否正确。
- Act：执行了哪个动作，输入来自哪里，触发了哪个状态变化。
- Assert：预期结果和实际结果是什么，失败时能直接看到差异。
- Cleanup：场景创建的临时对象、事件监听、状态修改要恢复。

## GFTrace 规范

`system` 建议格式：

```text
模块.功能
```

示例：

```text
Procedure.Preload
UI.Inventory
Combat.Damage
DataTable.Item
```

`action` 建议格式：

```text
阶段.动作
```

常用动作：

- `Input.Accepted`
- `Condition.Failed`
- `State.Enter`
- `State.Exit`
- `Resource.Load.Begin`
- `Resource.Load.Success`
- `Resource.Load.Failure`
- `Config.Read`
- `Entity.Spawn`
- `UI.Open`
- `Result.Applied`

`data` 必须尽量放可定位字段：

- 配置：表名、行 ID、字段名、期望值、实际值。
- 资源：assetName、assetPath、resourceGroup、loadMode。
- 状态：currentState、nextState、procedureName、entityId。
- 输入：inputAction、source、targetId。所有按键输入仍必须走 `InputModule`。
- 错误：reason、exceptionType、stackTrace 摘要。

不要在 `Update` 中逐帧刷日志。只记录状态变化、用户输入、资源请求、关键判断失败、最终结果。

## 诊断场景规范

新增场景实现 `IGFDiagnosticScenario` 或继承 `GFDiagnosticScenarioBase`。Runner 会自动发现，不需要修改 `GFDiagnosticRunner.RunAll`。

```csharp
using System.IO;

public sealed class ItemTableDiagnosticScenario : GFDiagnosticScenarioBase
{
    public override string Name => "Item Table Contract";
    public override string Category => "DataTable";
    public override GFDiagnosticScenarioMode Mode => GFDiagnosticScenarioMode.EditMode;

    public override void Run(GFDiagnosticScenarioContext context)
    {
        context.TraceInfo("Arrange", "Check ItemTable source files.");
        context.RequireFile("GameData/DataTables/ItemTable.xlsx");

        bool hasJson = File.Exists("GameData/AIData/DataTables/ItemTable.json");
        context.Assert(hasJson, "ItemTable AI json does not exist.");

        context.Detail("source", "GameData/DataTables/ItemTable.xlsx");
        context.Detail("aiJson", hasJson);
    }
}
```

要求：

- `Name` 使用人能读懂的功能名，不要只写类名。
- `Category` 使用模块名，便于报告聚合，例如 `Core`、`DataTable`、`UI`、`Combat`。
- `Mode` 明确运行环境：只查文件和配置用 `EditMode`，需要运行场景用 `PlayMode`，两者都能跑用 `Any`。
- 用 `context.Detail` 写上下文，用 `context.Assert` 写验收点，用 `context.Trace*` 写时序。
- 一个场景只验证一个功能契约。复杂功能拆成多个小场景。

## AI 读报告顺序

定位问题时按这个顺序读：

1. 先看 `failureCount` 和失败的 `items[].errors`。
2. 再看失败项的 `details`，确认输入、资源、配置和实际状态。
3. 再看 `timeline`，按 seq 还原输入、条件、状态、资源、结果的先后关系。
4. 最后看 `snapshot`，确认当前 Procedure、加载场景、Unity 日志、运行环境。

排查时优先判断问题落在哪一段：

- Input：输入是否被接收，是否走 `InputModule`。
- Condition：条件判断是否提前失败。
- State：状态是否进入、离开或切换到错误目标。
- Resource：资源路径、表行、Prefab、UIForm 是否存在。
- Output：结果是否应用到实体、UI、存档、事件或配置。

## 当前内置场景

- `Scenario/Core/Clean Workspace Contract`：保证当前启动工作区干净，GF_X DemoGame 示例内容不再保留在当前工程，且不会混入默认启动流程。
