# Proposal: 实现框架核心脚本

## 变更概述

根据已完成的设计文档，实现新项目框架核心层的所有 C# 脚本。

## 设计依据

- [01-框架核心设计概述](../../../../../AI友好型项目探讨/01-框架核心设计概述.md)
- [03-模块系统详细设计](../../../../../AI友好型项目探讨/03-模块系统详细设计.md)
- [04-事件系统详细设计](../../../../../AI友好型项目探讨/04-事件系统详细设计.md)
- [02-AI友好型日志规范](../../../../../AI友好型项目探讨/02-AI友好型日志规范.md)
- [conventions.md](../../../../../conventions.md)
- [framework-core spec](specs/framework-core/spec.md)

## 实现范围

### 框架核心（Core/）
1. **IGameModule.cs** — 模块接口
2. **ModuleRunner.cs** — 模块生命周期管理
3. **EventBus.cs** — 事件系统
4. **FrameworkLogger.cs** — AI 友好型日志
5. **EventHandlerAttribute.cs** — 属性定义
6. **RequestHandlerAttribute.cs** — 请求响应处理器属性定义
7. **GameApp.cs** — 启动入口

### 工具类（Utils/）
8. **StateMachine.cs** — 通用状态机
9. **CompositeDisposable.cs** — 聚合 IDisposable

### 模板（Templates/）
10. **ModuleTemplate.cs.txt** — 模块标准写法（非编译参考）
11. **EventTemplate.cs.txt** — 事件定义模板（非编译参考）

## 不在本次范围

- Modules/ 下各功能模块的实现（后续按需开发）
- ExternalAPI 实现
- Unity 场景和 Prefab 创建（需用户在 Unity Editor 中操作）
- `Assets/Scripts/**/README.md` 文档骨架（后续文件结构变更中补齐）

## 预期结果

新项目拥有一个完整可用的框架核心，可以在此基础上开发游戏功能模块。
