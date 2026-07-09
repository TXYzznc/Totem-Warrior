# Templates/ — AI 参考脚本

> 本目录所有 `.cs.txt` **不参与 Unity 编译**。它们是写给 AI 与开发者的"参考样板"。
>
> 复制 → 改名（去 `.txt`）→ 改内容，才会被 Unity 识别为代码。

## 模板清单

| 模板 | 用途 | 目标位置 |
|---|---|---|
| [ModuleTemplate.cs.txt](./ModuleTemplate.cs.txt) | `IGameModule` 标准实现：Category / Dependencies / Init / Shutdown / EventHandler | `Assets/Scripts/Modules/<YourModule>/<YourModule>.cs` |
| [EventTemplate.cs.txt](./EventTemplate.cs.txt) | 广播事件类的标准写法（class + 只读字段） | `Assets/Scripts/Events/<XxxEvent>.cs`（跨模块）或模块内 `Events/` |
| [RequestTemplate.cs.txt](./RequestTemplate.cs.txt) | 请求-响应模式（`RequestAsync<TReq, TResp>` + `[RequestHandler]`） | 同上 |
| [DataTableTemplate.cs.txt](./DataTableTemplate.cs.txt) | `IDataTable` schema 生成结果参考（由 [DataTableGenerator.cs](../Modules/DataTable/Editor/DataTableGenerator.cs) 从 JSON 产出） | 仅供参考，不复制 |

## 使用规则

1. **AI 不要在 Templates/ 下创建实际业务文件** —— 业务文件去对应目录（Modules/Events/Utils）。
2. **`.cs.txt` 不能 import 到 Unity 编译** —— 这样占位类型（`SomeDependencyModule` / `TestEvent` 等）不会与你的真实代码冲突。
3. **DataTable 必须走 JSON 直写流程**：用户先在 `Assets/Resources/DataTable/<Name>.json` 编辑 JSON → 在 Unity 菜单 `Tools/DataTable/生成全部配置表代码` → 自动生成 `Assets/Scripts/DataTable/<Name>.cs` → 再写读取代码。**不要用 Excel / .xlsx / .bytes**，那是历史污染。`DataTableTemplate.cs.txt` 仅展示 generator 产物的结构，不要手抄。
4. **新增模板时**：保持文件名结构 `<Pattern>Template.cs.txt`，并在本表格添加一行。
