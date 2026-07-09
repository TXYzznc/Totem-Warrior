---
module: DataTable
owner: client-unity
generated_at: 2026-07-09
source: tools/ai_index/build_ai_manifests.py
---

# DataTable Module

> 这是 AI 读取卡。修改本模块前，先读本文件，再按下面的关联入口补上下文。

## 职责

配置表加载、注册表消费、JSON 到强类型表的运行时入口。

## AI 读取顺序

1. 本文件
2. 关联 GDD / wiki
3. 关联 OpenSpec
4. 关联 DataTable
5. 模块源码与测试

## 关联 GDD / Wiki

- 无

## 关联 OpenSpec

- 无

## 关联 DataTable

- 无

## 关联资源

- 无

## 主要脚本

- `LegacyProjectArchive/Assets/Scripts/Modules/DataTable/DataTableFile.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/DataTable/DataTableModule.cs`
- `LegacyProjectArchive/Assets/Scripts/Modules/DataTable/IDataTable.cs`

## 注意事项

- `旧表结构源已归档为证据；当前业务数据先改 GameData/AIData/DataTables/Business/*.json，再逆向生成 GameData/DataTables/Business/*.xlsx 和 runtime catalog。`

