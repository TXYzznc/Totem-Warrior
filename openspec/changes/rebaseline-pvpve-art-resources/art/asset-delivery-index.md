# 第一阶段美术资源交付索引

> Change：`rebaseline-pvpve-art-resources`  
> 台账唯一状态源：`artifacts/美术资源需求/美术资源需求表.xlsx`  
> 建立日期：2026-08-11  
> 责任边界：本索引只定义美术交付与程序消费合同；不修改 C#、DataTable 或 GF_X。

## 状态门槛

| 状态 | 允许的证据 | 禁止的误用 |
|---|---|---|
| 详细设计待确认 | 已有 brief、layout 或规格，待用户确认 | 以此状态生成正式效果图、模型或最终素材 |
| 设计完成-待制作 | 已确认概念/规格，尚无正式资产 | 标为已完成-符合 |
| 待验收 | 正式源文件或导出文件已存在 | 以效果图代替导入、可读性或性能证据 |
| 已完成-需返工 | 历史资产仍可追溯，但与当前范围冲突 | 覆盖、删除或作为当前第一阶段交付 |
| 暂不需要 | 已移出第一阶段，保留历史记录 | 删除原文件或隐式恢复到 runtime |

## 交付登记

| 稳定 ID | 责任方 | 当前阶段 | 目标路径 | 消费合同/依赖 | 验收证据 |
|---|---|---|---|---|---|
| ART-RET-ENV-001 | 美术窗口 | A1 审计中 | `artifacts/美术资源需求/模型/ENV-End-绿洲新城/` | 首图场景、建筑与装饰为 retained 基线；主玩法仅读取，不复制生产 | 路径存在；导入/Prefab 兼容性待审计 |
| ART-RET-CHR-001 | 美术窗口 | A1 审计中 | `artifacts/美术资源需求/模型/CHR-003_轻机能服装与局部机械组件/` | 保留角色服装与局部机械方向；需验证统一骨骼、第一/第三人称和六部位可见性 | 路径存在；正式 FBX/PBR/Prefab 状态待核对 |
| ART-RET-TAT-001 | 美术窗口 | A1 已入库 | `artifacts/美术资源需求/通用/GEN-001_纹身贴花与遮罩/` | 已确认的 S01 八图案板已按 `P01`～`P08` 透明底切图，入库至 `Assets/Game/Sprites/Tattoo/`；仅 P01/P02 进入第一阶段功能消费 | 运行时 Key 为 `tattoo.pattern.p01`～`tattoo.pattern.p08`；贴花映射与颜色适配待运行时验收 |
| ART-UI-HIST-001 | 美术窗口 | A0 已登记 | `artifacts/美术资源需求/UI/` | 旧 UI-001～UI-005 及其效果图仅作历史参考；禁止同名覆盖 | 目录快照见 `retained-asset-audit.md` |
| ART-UI-HIST-002 | 美术窗口 | A0 已登记 | `artifacts/美术资源需求/通用UI组件/` | 历史通用组件可参考或候选复用；须经第一阶段 layout 与台账重新确认 | 目录快照见 `retained-asset-audit.md` |
| ART-UI-FP-001 | art-ui / 主玩法 change | A2–A4 详细设计待确认 | `openspec/changes/rebaseline-pvpve-art-resources/art/prefab-layout.md` | 13 组 form/overlay、稳定节点/slot ID、焦点、安全区、fallback；替换素材不得改变结构合同 | 用户确认 layout 后才进入效果图与切片 |
| ART-CMB-WPN-001 | art-3d / 主玩法 change | A6 详细设计待确认 | `openspec/changes/rebaseline-pvpve-art-resources/art/weapon/` | 唯一“民用模块化巡防步枪”；稳定 key 建议 `weapon.rifle.patrol.v1`；完整设计确认后才允许 FBX/PBR/LOD | `WPN-FP-001_rifle-design-brief.md` 的第一/第三人称、材质、LOD 与挂点合同 |
| ART-CMB-ENM-001 | 后续独立 change | 已移出范围 | 不在本 change 登记 | 敌人造型、材质、动画、弱点和 Prefab 后续单独设计 | 不计入本 change 完成度 |
| ART-CMB-VFX-001 | art-vfx / client-ta / 主玩法 change | A8 详细设计待确认 | `openspec/changes/rebaseline-pvpve-art-resources/art/vfx/` | 火/冰/雷三层、三反应与 P01/P02 仅消费已结算事件；使用通用目标锚点，不新增规则 | `first-playable-vfx-design.md` 的 key、时序、性能、遮挡与降级合同 |
| ART-TEST-SCENE-001 | art-vfx / client-unity / client-ta | A8 待制作 | `Assets/Game/Scene/ArtResourceTest.unity` | 独立美术资源测试场景；明暗背景、第一/第三人称、通用方盒目标、六人压力、画质与性能验证；交互全部走 InputModule | 编辑器截图、Profiler/Overdraw 记录、目标平台构建复测与晋级签署 |
| ART-PROMPT-FP-001 | art-director / 各美术责任方 | A5/A6/A8 提示词待确认 | `openspec/changes/rebaseline-pvpve-art-resources/art/prompts.md` | 只定义下一阶段生成输入；当前不得生成图片或正式资产 | 用户明确批准提示词后分批生成并记录 provenance |

## 文件命名与交接要求

1. 所有正式导出文件以台账资源 ID 开头；本索引 ID 用于 change 内追踪，不替代 Excel 的资源行 ID。
2. UI 交付须提供：尺寸/比例、透明度、色彩空间、切片、九宫格拉伸区、状态变体、目标导入路径和 fallback。
3. 模型/特效交付须提供：格式、尺寸/比例、挂点或参数合同、状态变体、目标导入路径和 fallback。
4. 动态文本、数值、倒计时、玩家名和版本信息只能由程序注入，禁止烘焙进 PNG 或矢量图。
5. 发现 runtime 接口缺口时，只更新 `handoff-log.md`；由 `rebuild-six-player-pvpve-first-playable` 决定实现。
