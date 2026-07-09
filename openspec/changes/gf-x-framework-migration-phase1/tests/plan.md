# Test Plan: GF_X framework migration phase 1

## 1. Static checks

- 扫描 `AAAGame`、旧本机绝对路径和 GF_X 示例混入。
- 对比当前项目与 GF_X 的 `Packages/manifest.json` 和插件目录。
- 检查 `Assets/Game`、`GameData`、诊断报告目录是否存在且路径相对。
- 检查受保护核心文件是否只在用户确认后修改。

## 2. Unity checks

- 运行 Unity 编译或等价 batchmode 验证。
- 运行迁入后的 GF_X 诊断菜单或 Editor 诊断入口。
- 运行 AI DataTable Json 校验和逆向导表的最小样例。
- 检查当前项目默认启动路径已切换为 `Assets/Game/Scene/Launch.unity`，旧启动场景和示例场景不再进入 BuildSettings。

## 3. Diagnostic report checks

诊断报告至少需要覆盖：

- 迁移路径契约。
- 依赖冲突或排除项。
- 当前启动链记录。
- AI DataTable 工具可用性。
- 示例内容隔离状态。

## 4. Regression notes

第一阶段不以“现有业务全部接入 GF_X”为验收条件。启动入口已经切到 GF_X；后续若发现业务运行问题，应先判断是否由迁移引入；如果涉及 DataTable、UI、Input、Scene 或业务脚本整体重构，需要回到确认门槛。
