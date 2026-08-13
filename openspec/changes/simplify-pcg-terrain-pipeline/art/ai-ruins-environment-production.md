# AI 遗迹环境立物：生产记录

> Completion audit (2026-07-16): 31 final PNG assets are present under `Assets/Game/Sprites/PCG/Props/AiRuins/`; the object catalog references all 31 unique assets (18 static objects + 14 anchor-visual entries, with one intentional asset reuse). Transparent-corner, import-setting, JSON, and PCG runtime-console checks passed.

> 状态：**进行中**（2026-07-16）。本主题以现有 AI 遗迹地形图为唯一视觉基线，不以病毒沼泽或通用概念图替代。

## 视觉对齐基线

- 参考地形：`RuinFloor/ruins_floor_01.png`、`ServiceMetal/ruins_service_metal_01.png`、`ReclaimedGrowth/ruins_reclaimed_growth_01.png`、`CoolantWater/ruins_coolant_water_01.png`。
- 锁定特征：低对比炭灰与蓝灰、细颗粒旧化、稀疏冷青亮点、无粗黑描边；环境立物必须保持同一冷色光照和材质密度。
- 技术规格：透明 RGBA PNG、底部中心 pivot、`Default` / `Point` / 关闭 Mipmap / `Clamp` / `Uncompressed`；地标导入为 512×512，静态物按 256×256 或 256×384。

## 已完成

| ID | 用途 | 原图 | 正式资源 | 验证 |
| --- | --- | --- | --- | --- |
| `ai_ruins_fallen_gate` | `player.spawn` 固定地标 | `art/raw/environment/ai-ruins/landmarks/ai_ruins_fallen_gate_chromakey.png` | `Assets/Game/Sprites/PCG/Props/AiRuins/Landmarks/ai_ruins_fallen_gate.png` | 512×512、RGBA 四角 alpha=0、catalog 可解析、运行时已生成 `PCG_Anchor_ai_ruins_fallen_gate_0` |
| `ai_ruins_command_spire` | `boss.spawn` 固定地标 | `art/raw/environment/ai-ruins/landmarks/ai_ruins_command_spire_chromakey.png` | `Assets/Game/Sprites/PCG/Props/AiRuins/Landmarks/ai_ruins_command_spire.png` | 512×512、RGBA 四角 alpha=0、catalog 可解析、运行时已生成 `PCG_Anchor_ai_ruins_command_spire_1` |
| `ai_ruins_power_relay` | `encounter.mid.center` 固定地标 | `art/raw/environment/ai-ruins/landmarks/ai_ruins_power_relay_chromakey.png` | `Assets/Game/Sprites/PCG/Props/AiRuins/Landmarks/ai_ruins_power_relay.png` | 512×512、RGBA 四角 alpha=0、catalog 可解析 |
| `ai_ruins_ink_terminal` | `npc.tattooist.base` 既有交互外观 | `art/raw/environment/ai-ruins/landmarks/ai_ruins_ink_terminal_chromakey.png` | `Assets/Game/Sprites/PCG/Props/AiRuins/Landmarks/ai_ruins_ink_terminal.png` | 512×512、RGBA 四角 alpha=0、catalog 可解析 |
| `ai_ruins_scrap_kiosk` | `npc.merchant.base` 既有交互外观 | `art/raw/environment/ai-ruins/landmarks/ai_ruins_scrap_kiosk_chromakey.png` | `Assets/Game/Sprites/PCG/Props/AiRuins/Landmarks/ai_ruins_scrap_kiosk.png` | 512×512、RGBA 四角 alpha=0、catalog 可解析 |

## 后续顺序

1. 继续完成另外 4 个固定地标；每项均以对应 AI 遗迹地形图作视觉基线。
2. 再按 `ruins_floor`、`ruins_metal`、`ruins_growth`、`ruins_coolant` 分批完成 20 个地貌立物，最后补齐 6 个低频地表点缀。
3. 每个资源保持源图、透明候选、导入设置与 catalog 条目同步；不得改动既有锚点的 payload、交互半径或奖励。

## 地貌立物进度

| ID | 地貌 | 类型 | 状态 |
| --- | --- | --- | --- |
| `ai_floor_cable_bloom` | `ruins_floor` | 静态装饰 | 已导入并加入随机 `objects` 池；待该批次一并运行时回归 |
