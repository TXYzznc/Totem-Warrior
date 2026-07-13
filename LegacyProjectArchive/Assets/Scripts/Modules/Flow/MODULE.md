---
module: Flow
owner: client-lead
generated_at: 2026-07-10
source: tools/ai_index/build_ai_manifests.py
---

# Flow Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

流程编排、启动/运行阶段切换和模块间流程上下文。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- 无

## 关联 OpenSpec

- `openspec/specs/main-menu-flow/spec.md`

## 关联 DataTable

- 无

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/Flow/FlowContext.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Flow/FlowModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/Flow/IFlow.cs`

## 注意事项

- `流程层只编排，不承载具体业务规则。`

