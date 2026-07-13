# 成熟战士候选 M01–M05：生成与验证记录

- 日期：2026-07-13
- 范围：仅记录本轮生成的 `M01` 至 `M05`。目录内 `M06` 至 `M10`（含其 `v02`）由其它并行任务维护，未被本任务修改或重绘。
- 生成方式：内置 `image_gen`（`stylized-concept`），随后使用本地 chroma-key 工具转为透明 PNG。
- 工程状态：仅探索用原始美术交付，未导入 `Assets`，未改动 Unity、配置或运行时索引。
- 审阅总览：`mature_candidates_final_contact_sheet.png` 按 `M01`–`M10` 编号排列；其中 `M07`–`M10` 使用已验收的 `v02` 成品。

## 统一验收基准

- 角色为约 30 岁的成熟男性战士：方正成年脸、微胡茬或成熟眼神，宽厚肩背、结实四肢；不是少年脸、纤瘦青年、健美夸张体格或过度性感化造型。
- 全部为无固定纹身、无体绘、无符文和无武器的单人全身 Idle 图；头部、躯干、左右臂、左右腿保留可用于运行时贴花的干净肤色区。
- 全部采用中低精细度的 2D 半写实厚涂：大笔触、明确轮廓与 3–5 档主明暗；未采用照片级写实、3D 渲染、皮肤毛孔、微观材质或密集小配件。
- 图像源背景为平坦绿幕；成品均为透明 RGBA。

## 候选区别与人工内容检查

| 编号 | 画风与外观 | 成熟体态与贴花区检查 |
| --- | --- | --- |
| M01 | 暗蓝短披肩的海报厚涂斗士 | 披肩仅为小面积低饱和暗蓝；短胡茬、宽肩，胸腹使用整体块面而非六块腹肌；腕部仅细绳，短裤/护膝不遮蔽腿部主贴花区。 |
| M02 | 石墨灰开放无袖短外套的图形厚涂战士 | 轮廓最图形化；开放短外套、细腕绳、短裤和小护膝，双臂与双腿裸露区最完整。 |
| M03 | 沙褐短披风、短裤、粗水粉墨线荒原佣兵 | 披风置于单肩后方，双臂仍可见；粗水粉与墨线最强，肤色区无固定标记。 |
| M04 | 炭黑腰带/短裤、低细节 ARPG 手绘逃亡者 | 服装最少，贴花区最宽；以大块软阴影表现成熟结实体格，无分割腹肌。 |
| M05 | 暗红褐干刷、无袖马甲、护膝战术幸存者 | 暗红褐只服务服装本体，不代表阵营色；成熟脸和干刷旧化最明显，手臂与腿部贴花区保持可用。 |

## Alpha 验证

统一抠图命令参数：

```text
--auto-key border --soft-matte --transparent-threshold 12 --opaque-threshold 220 --despill
```

| 编号 | 成品尺寸 | 透明 / 半透明 / 不透明像素 | Alpha 外接框 | 四角 Alpha | 结果 |
| --- | --- | --- | --- | --- | --- |
| M01 | `1024 x 1536` | `1,128,500 / 5,535 / 438,829` | `(189, 59, 810, 1463)` | `0,0,0,0` | 通过 |
| M02 | `1024 x 1536` | `1,188,726 / 6,012 / 378,126` | `(241, 77, 785, 1448)` | `0,0,0,0` | 通过 |
| M03 | `919 x 1711` | `1,001,971 / 8,232 / 562,206` | `(66, 46, 807, 1670)` | `0,0,0,0` | 通过 |
| M04 | `1024 x 1536` | `1,168,909 / 5,966 / 397,989` | `(225, 28, 781, 1486)` | `0,0,0,0` | 通过 |
| M05 | `864 x 1821` | `1,046,584 / 7,559 / 519,201` | `(111, 73, 741, 1750)` | `0,0,0,0` | 通过 |

目视检查确认五张图没有明显绿边，发梢、手指、服装边缘和足部均完整。每张 Alpha 图均同时包含透明像素、不透明前景、有效前景外接框和透明角点。

## 交付文件与 SHA-256

- `M01_navy_mantle_poster_fighter.png`：`202C48C83D413FCE4B2F619E86D686B24C4FE23DAEC0C6E50D0C9FD1723B97DD`
- `M01_navy_mantle_poster_fighter_chromakey.png`：`0155B9EB05F44567ED1564FE4AC83F05DF190CFC17F4C57C1A7AA889340C2B7D`
- `M02_graphite_open_jacket_warrior.png`：`F35AD19BD99D9E1CDD9DDE72E97767BFEB7AD2FE59AA1755C2F944E4B8B18F0F`
- `M02_graphite_open_jacket_warrior_chromakey.png`：`601347755B65CBA59CABE6321064CCF81207A43838611A5D3FEA80D0CAED7919`
- `M03_sand_cloak_gouache_mercenary.png`：`A9AC6577DBAEDF1DA85B8EC13CC651AB3D6850BB906E8FC191FEDB94DABCBD10`
- `M03_sand_cloak_gouache_mercenary_chromakey.png`：`A0CB07B9658A3A7183BA03127E8F4F4CDAD50B3425131C7CFAEEDEE23DC85178`
- `M04_low_detail_arpg_escapee.png`：`99CF5CEE25782E79ABF8308D102D07A84CEA0A8659F2F4280F6AFFCA0FD96532`
- `M04_low_detail_arpg_escapee_chromakey.png`：`661E86109389C9BB8C0550F26A9B389EC5C0D634D44B29C5EB2E5D8EDAC3B521`
- `M05_redbrown_drybrush_survivor.png`：`292C92FD7F2352CF2131F270B53296E7E1A5F01BF069E72609DF6E12AC9A9E03`
- `M05_redbrown_drybrush_survivor_chromakey.png`：`07A7D21FB40F7DDB412DC2E109A8DE7B70B7892438F1353726AC31F24CCA6333`
