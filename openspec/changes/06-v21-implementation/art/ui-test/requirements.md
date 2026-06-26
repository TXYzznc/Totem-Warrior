# UI 美术资源测试样本 — 需求

> 状态：未处理（测试 codex-art-gen MCP 端到端工作流）
> 范围：从 GDD-v2 §13 复用组件清单选 16 个高复用 UI 组件
> 用途：验证 ai-art 三表 + codex-art-gen L2 合图流程

---

## 一、风格基线（与 v2.1 主美术对齐）

- **art bible**：Hades-style 2.5D painterly，厚涂笔触，强轮廓深色描边
- **光照**：左上方 45° 强光 + rim light
- **配色**：饱和但不刺眼，HUD 元素深暖色调（铜/铁/血红/暗金）
- **格框规范**：圆角 4px（9-slice 友好），描边 1px
- **可读性**：32×32px 最小尺寸下轮廓清晰可识别（GDD §7.2 多分辨率 + §7.4 色盲约束）

## 二、表 A — 页面清单（本次测试涉及的载体）

| # | 载体 | 用到的组件 |
|---|---|---|
| A1 | CombatHUDForm | HP 条 + 技能槽 + 武器图标 + Buff 标签（用到 IconFrame、ProgressBar） |
| A2 | ThreeChoiceForm | 3 张 CardPanel + ModalOverlay 底板 |
| A3 | ShopForm / TattooStudioForm | 通用按钮 + ModalOverlay |
| A4 | PauseMenuForm | 通用按钮 + ModalOverlay |

> 本次**仅出复用组件**，不直接出 Form 整体效果（Form 由组件拼装）。

## 三、表 B — 复用组件清单（16 张，本次测试目标）

| 组 | # | 组件名 | 用途 | 目标尺寸（256×256 切格内） | 透明 |
|---|---|---|---|---|---|
| **按钮（4 态）** | 1 | `button_normal` | 默认态通用按钮 | 220×72 占格中心 | ✓ |
| | 2 | `button_hover` | hover 高亮态 | 同上 + 边缘发光 | ✓ |
| | 3 | `button_pressed` | 按下凹陷态 | 同上 + 内阴影 + 暗化 | ✓ |
| | 4 | `button_disabled` | 不可用态 | 同上 + 灰度 + 透明度 60% | ✓ |
| **图标底框（4 档稀有度）** | 5 | `iconframe_common` | 灰色铁质底框 | 192×192 占格中心 | ✓ |
| | 6 | `iconframe_rare` | 蓝色 + 微弱蓝光 | 同上 | ✓ |
| | 7 | `iconframe_epic` | 紫色 + 紫色辉光 | 同上 | ✓ |
| | 8 | `iconframe_legendary` | 金色 + 强金光 + 火花粒子 | 同上 | ✓ |
| **进度条（4 类）** | 9 | `progressbar_bg` | 进度条底（凹槽） | 220×32 占格中心 | ✓ |
| | 10 | `progressbar_fill_hp_green` | HP 填充 绿态（normal） | 220×24 同位置 | ✓ |
| | 11 | `progressbar_fill_hp_red` | HP 填充 红态（critical） | 同上 | ✓ |
| | 12 | `progressbar_fill_cd_radial` | 技能冷却 radial 遮罩（半圆扇形动效起始帧） | 192×192 圆形 | ✓ |
| **覆盖层 & 卡片（4 态）** | 13 | `modaloverlay_panel_bg` | 通用面板底板（9-slice） | 240×240 占格中心 | ✓（半透明） |
| | 14 | `cardpanel_idle` | 三选一卡片 idle 态 | 200×240 占格 | ✓ |
| | 15 | `cardpanel_hover` | 三选一卡片 hover 边缘高亮 | 同上 + 边光 | ✓ |
| | 16 | `cardpanel_selected` | 三选一卡片 selected 确认填充 | 同上 + 内辉光 | ✓ |

## 四、表 C — 组件状态表（关键差异点）

| 组件组 | 状态间视觉差异手段 | 美术呈现关键 |
|---|---|---|
| **按钮 4 态** | normal → hover：**边缘加发光**<br>hover → pressed：**整体下沉 + 内阴影 + 颜色暗 10%**<br>pressed → disabled：**饱和度归零 + 透明度 60%** | 4 张同一木质底，**仅修改光效层与饱和度**（保证状态切换时不感觉换了按钮） |
| **图标底框 4 档** | common（灰）→ rare（蓝）→ epic（紫）→ legendary（金）<br>稀有度越高：**金属质感强 + 辉光半径加大 + 顶部加宝石/纹饰** | 4 张同一框架结构，**纹饰复杂度递增** |
| **进度条 4 类** | bg 是凹槽底（深色硬边）<br>fill_green/red 同一外形差颜色（饱和绿 vs 警示红，**红色加破裂裂纹纹理**）<br>cd_radial 是圆形遮罩（半透明白） | bg + fill 分层设计，**fill 必须能 9-slice 横拉** |
| **覆盖层 & 卡片 4 态** | modaloverlay：**深暖色背景 + 边角金属铆钉装饰**<br>cardpanel idle → hover：**边缘金光线条**<br>hover → selected：**整张内部辉光填充** | 卡片底板尺寸完全一致，**仅光效层叠加** |

## 五、不做

- 不出整 Form 效果（由 16 个组件拼）
- 不出文字（中文文字交字体层渲染，不让 imagegen 生）
- 不出图标本体（图标用 v2.1 已生成的 weapon/skill/affix）
- 不出 9-slice 切片标记（生成原图，9-slice 在 Unity Inspector 配置）

## 六、验收

- 16 张 PNG 全部生成（透明背景）
- 32×32 缩略下轮廓可辨
- 按钮 4 态色彩饱和度变化符合 §四 状态表
- 图标底框稀有度递进视觉强度合理
- 进度条 fill 可见横拉（左右色彩均匀）
