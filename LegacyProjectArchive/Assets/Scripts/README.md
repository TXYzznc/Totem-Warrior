# Scripts — C# 脚本根目录

> 所有 C# 脚本的根目录，按功能分区。

## 目录

| 目录 | 说明 | 文件数 |
|---|---|---|
| `Core/` | 框架核心（项目通用，不可删） | 10 |
| `Modules/` | 运行时模块集合（框架模块 + 当前游戏业务模块） | 24 个模块目录 |
| `DataTable/` | 配置表生成出的强类型 C# 类与注册表 | 29 |
| `Components/` | 可复用 MonoBehaviour 组件 | 2 |
| `Utils/` | 独立于业务的通用工具类 | 10 |
| `Templates/` | AI 开发时的参考模板（.cs.txt，不编译） | 5 |
| `ExternalAPI/` | 暴露给外部工具调用的 API 端点 | 1 |
| `Events/` | 跨模块的事件类型定义 | 9 |

## 使用规则

- 所有 C# 脚本必须放在上述某个子目录中，不要散落在根目录
- 运行时模块统一放在 `Modules/` 下，每个模块一个文件夹，复杂模块可含自己的 `Events/` / `Data/` / `UI/` 子目录
- 修改某个模块前，先读该模块的 `MODULE.md`
- 跨模块共享的类型放在对应的顶层目录（Events、Utils）
- 模板文件用 `.cs.txt` 后缀，避免被 Unity 编译

## AI 索引

- 项目总地图：`项目知识库（AI自行维护）/wiki/PROJECT_MAP.md`
- 当前上下文：`项目知识库（AI自行维护）/wiki/ACTIVE_CONTEXT.md`
- 模块 manifest：`项目知识库（AI自行维护）/wiki/manifests/modules.json`
- 生成命令：`python tools/ai_index/build_ai_manifests.py`
