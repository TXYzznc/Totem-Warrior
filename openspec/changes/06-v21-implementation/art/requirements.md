美术素材状态: 待处理
处理日期: 2026-06-25（创建）
执行 SKILL: codex-image-gen
输出目录: art/raw/
生成记录: art/raw/生成记录.md
批量分档: 待 codex-image-gen 自动归类（预估 L1 主导 + L2 少量 ICON 合批）

# 美术需求 — 06-v21-implementation v2.1 全套美术包

> **状态**：待处理
> **创建日期**：2026-06-25
> **承担方**：`art-director`（本文档）+ `ai-art`（提示词）+ `codex-image-gen`（落盘）
> **目标用户**：v2.1 vertical slice 内所有图标、立绘、HUD、占位场景、角色 sprite，构成 10-15 分钟单局可玩 demo 的完整视觉资产
> **基线 GDD**：[00-总策划案 v2.1](../../../../项目知识库（AI自行维护）/GDD-v2/00-总策划案v2.md) + [13-UI 与 HUD](../../../../项目知识库（AI自行维护）/GDD-v2/systems/13-UI与HUD.md) + [11-怪物与 Boss](../../../../项目知识库（AI自行维护）/GDD-v2/systems/11-怪物与Boss.md) + [09-纹身师与商人 NPC](../../../../项目知识库（AI自行维护）/GDD-v2/systems/09-纹身师与商人NPC.md)

---

## 0. Art Bible（v2.1 整体风格统一约束）

### 0.1 风格定位

**「似 Hades 精致 2.5D」**（v2.1 §二设计基本盘锁死）：

- **画风层**：手绘描边 + 高对比光影 + 厚涂 + 鲜艳饱和色彩，单体形象在缩略尺寸仍能清晰辨识
- **色彩层**：每色单 hue ≥ 4 阶明暗渐变，轮廓加 deep shadow 描边（1-2px 等效）
- **构图层**：square framing（icon）/ 圆形 framing（头像）/ tile-friendly（环境）/ 8 方向独立姿态（角色）
- **光层**：主光源高位偏侧（30°-60°），背光 + rim light 强化轮廓
- **氛围层**：末日废墟基调，但 NPC / 颜料 / 配方等"希望"元素加 magical glow / inner light

### 0.2 Do / Don't（评审一票否决项）

| Do | Don't |
|---|---|
| 厚涂 + 描边强化轮廓 | 扁平 vector / flat design |
| 鲜艳饱和色（接近 #FF4500 / #00CFFF 强度）| 灰沉低饱和（除非环境贴图）|
| icon 单主体居中，留 8% padding | 多元素杂糅 / 满构图 |
| 透明背景 PNG（icon / sprite / 头像）| 不透明背景塞图 |
| NPC 头像圆形构图，肩部以上 | 全身像 / 多人合影 |
| 环境贴图可平铺无明显接缝 | 单一焦点 / 不可平铺 |
| 颜料瓶分档视觉差异显著（常见 1 渐变 / 稀有 2 层光晕 / 传说 magical aura 全发光）| 三档外观难分辨 |
| 角色 sprite 8 方向严格一致比例 + 服装 | 8 方向 idle 比例飘 / 服装变形 |

### 0.3 色彩规范（v2.1 颜料↔元素绑定）

| 色 | HEX 基准 | 元素绑定 | 用途 |
|---|---|---|---|
| 红 Red | `#E63946` | 火焰 / 攻击 | 颜料瓶 / 词缀图标火伤 / 火球技能 |
| 黄 Yellow | `#F4C430` | 雷电 / 速度 | 颜料瓶 / 词缀图标攻速 / 雷链技能 |
| 绿 Green | `#3DDC84` | 自然 / 治疗 | 颜料瓶 / 词缀图标命中回血 / 治疗光环 |
| 蓝 Blue | `#1FB6FF` | 冰霜 / 冷却 | 颜料瓶 / 词缀图标冷却减 / 寒冰阵 |
| 紫 Purple | `#9D4EDD` | 异变 / 暴击 | 颜料瓶（稀有档） / 词缀图标暴击 |
| 金 Gold | `#FFB400` | 神圣 / 暴伤 | 颜料瓶（稀有档） / 词缀图标暴伤 |
| 白 White | `#F8F9FA` | 纯能 / 距离 | 颜料瓶（传说档） / 词缀图标距离命中 |
| 暗灰描边 | `#22243A` | 通用描边 | 所有 icon 1-2px 等效描边 |

### 0.4 视觉层级（HUD 决策驱动）

参考 [13-UI 与 HUD §一](../../../../项目知识库（AI自行维护）/GDD-v2/systems/13-UI与HUD.md)：

- **瞬间扫层**：HP 条 / 技能槽 / 弹药数（最大色块、粗线条、最高对比）
- **慢看层**：buff 槽 / 武器图标 / 小地图 / 缩圈倒计时（中号图标 + TMP）
- **长时间观察层**：Build 列表 / 战斗日志 / 词缀面板（小字 / 滚动）

→ ICON 制作需考虑 32×32 缩略仍可辨识。

### 0.5 参考池（Reference Pool）

- Hades（Supergiant）— 厚涂描边 + 鲜艳 + 角色辨识度优先
- Dead Cells — 像素 + 高对比 + 末日基调（颜料瓶 / 武器 icon 可参考）
- Slay the Spire — icon 单主体居中 + 厚涂（技能 / 词缀 icon 可参考）
- Tribes of Midgard — 俯视角 BR 风格 + 鲜艳配色（地面 / 墙体平铺可参考）

---

## 1. UI 三表（HUD 元素强制门槛）

> **触发依据**：本期 8 个 HUD 元素属于 UI 类型，依 ai-art SKILL Step 0 必须先定三表。
> 注：本期不出 Form Prefab UI（那是 client-unity 的 Phase 3-F 工作），本表仅约束 HUD 装饰元素（边框 / 槽底 / 数字框 / 条目背景）的视觉规格。

### 表 A — HUD 装饰元素页面清单（8 项 + 1 复用）

| # | 元素 | 优先级 | 用途 | 出现位置 |
|---|---|---|---|---|
| 1 | HP 条边框 | 必做 | 装裹 Image filled 条 | CombatHUDForm 左上 |
| 2 | Buff 槽底 | 必做 | 装裹 Image buff 图标 | HP 条下方 |
| 3 | 技能槽 Q | 必做 | 装裹技能 icon + radial 冷却 | 底部居中左 |
| 4 | 技能槽 E | 必做 | 装裹技能 icon + radial 冷却 | 底部居中右（与 Q 镜像，可复用） |
| 5 | 弹药数字框 | 必做 | 装裹 TMP 弹药数 | 武器图标右侧 |
| 6 | 小地图框 | 必做 | 圆形 mask + 外环装饰 | 右上 |
| 7 | 缩圈倒计时框 | 必做 | TMP 倒计时背板 | 小地图正下 |
| 8 | 武器图标框 | 必做 | 装裹 weapon icon | 技能槽右侧 |
| 9 | Build 列表条目背景 | 必做 | ScrollListRow 子项底 | Sidebar 上半 |

### 表 B — 复用组件清单（v2.1 HUD 装饰）

| 组件类型 | 数量 | 用途 | 复用规则 |
|---|---|---|---|
| IconFrame（圆角描边底框）| 4 套 | 技能 / 武器 / Buff / Build 行 | 9-slice，单 sprite 4 配色变体 |
| ProgressBar 边框 | 2 套 | HP / Boss HP | 水平 + 圆角 + Hades 风装饰条 |
| 数字背板 | 2 套 | 弹药 / 倒计时 | 半透明深底 + 1px 金边 |
| 小地图外环装饰 | 1 套 | 仅小地图 | 圆环 + 8 方位刻度 |

### 表 C — 组件状态表（HUD 装饰元素）

| 组件 | 必备状态 | 视觉区分 |
|---|---|---|
| HP 条边框 | normal / warning / critical | 描边色 灰 → 橙 → 红（DOTween） |
| Buff 槽底 | empty / occupied | 灰底 / 彩底 |
| 技能槽 Q/E | ready / cooldown / no-skill | 完整描边 / 描边变暗 / 灰底无描边 |
| 弹药数字框 | normal / empty | 金边 / 红边 |
| 小地图框 | safe / shrinking | 静态 / 边缘脉冲红色 |
| 缩圈倒计时框 | safe / warning / urgent | 绿底 / 橙底 / 红底脉冲 |
| 武器图标框 | equipped / switching | 完整描边 / 淡入态半透明 |
| Build 列表条目背景 | normal / hover | 透明 0.4 / 高亮 0.7 |

---

## 2. 资源清单（91 张 / 含 8 方向 idle 算 98）

### 2.1 武器图标（5 张）

| ID | 文件名 | 主体描述 | 关键视觉 |
|---|---|---|---|
| W01 | weapon_short_blade.png | 短刀 | 单刃匕首 + 黑色握柄 + 寒光刀身 |
| W02 | weapon_heavy_hammer.png | 重锤 | 双手大锤 + 锈铁锤头 + 龟裂红光 |
| W03 | weapon_pistol.png | 手枪 | 半自动手枪 + 黑钢 + 枪口余烟 |
| W04 | weapon_bow.png | 弓 | 复合弓 + 满弦 + 闪光箭头 |
| W05 | weapon_energy_fist.png | 能量拳 | 拳套 + 蓝紫能量场环绕 + 电流 |

**规格**：1024×1024 → 256×256，透明背景，正方形 framing，主体占 70-80% 画幅，主体居中略偏左下（持握感）。

### 2.2 技能图标（8 张）

| ID | 文件名 | 主体 | 关键视觉 |
|---|---|---|---|
| S01 | skill_fireball.png | 火球术 | 燃烧火球 + 火花轨迹 + 红橙渐变 |
| S02 | skill_ice_field.png | 寒冰阵 | 六边形冰阵 + 冰晶刺 + 寒蓝 |
| S03 | skill_chain_lightning.png | 雷链 | 锯齿闪电链 + 黄电光 + 多分叉 |
| S04 | skill_heal_aura.png | 治疗光环 | 绿色光环 + 上升十字光 + 莲花底 |
| S05 | skill_shield.png | 护盾 | 八边形蓝盾 + rim light + 内能量场 |
| S06 | skill_stealth.png | 短暂隐身 | 半透明人形剪影 + 紫黑烟雾 + 残影 |
| S07 | skill_summon.png | 召唤兽 | 异界召唤阵 + 兽爪剪影 + 紫光 |
| S08 | skill_time_slow.png | 时间慢放 | 沙漏 + 时钟齿轮 + 金色慢动光 |

**规格**：1024×1024 → 256×256，透明背景，square framing，主体居中，强化 magical aura。

### 2.3 词缀图标（8 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| A01 | affix_fire_damage.png | 火伤+ | 火焰符 + 上箭头 + 红边 |
| A02 | affix_cooldown.png | 冷却- | 沙漏 + 下箭头 + 蓝边 |
| A03 | affix_attack_speed.png | 攻速+ | 双刀十字 + 上箭头 + 黄边 |
| A04 | affix_crit_chance.png | 暴击+ | 菱形+靶心 + 上箭头 + 紫边 |
| A05 | affix_crit_damage.png | 暴伤+ | 爆炸十字 + 上箭头 + 金边 |
| A06 | affix_range.png | 距离+ | 长尾箭 + 上箭头 + 白边 |
| A07 | affix_accuracy.png | 命中+ | 准星 + 上箭头 + 白边 |
| A08 | affix_lifesteal.png | 命中回血 | 滴血十字 + 心形 + 绿边 |

**规格**：1024×1024 → 256×256，透明背景，square framing，主体居中。**注意箭头方向**：增益用上箭头，减益用下箭头（冷却-属于"数值减"但效果"增益"，箭头按下，但底色蓝以示有利）。

### 2.4 颜料瓶（7 色 × 3 档 = 21 张）

| ID 前缀 | 色 | 常见档（_common）| 稀有档（_rare）| 传说档（_legendary）|
|---|---|---|---|---|
| paint_red | 红 | 简单玻璃瓶 + 红液 | + 内层光晕（橙黄）+ 浮粒 | + magical aura 整瓶发光 + 粒子轨迹 |
| paint_yellow | 黄 | 同上换色（雷电图样浮于液面）| 同上 | 同上 + 雷弧外溢 |
| paint_green | 绿 | 同上（毒气泡浮于液面）| 同上 | 同上 + 翠绿雾溢出 |
| paint_blue | 蓝 | 同上（雪花浮于液面）| 同上 | 同上 + 冰晶辐射 |
| paint_purple | 紫 | 同上（异变漩涡）| 同上 | 同上 + 紫雾扭曲 |
| paint_gold | 金 | 同上（十字光 +金粉）| 同上 | 同上 + 金光神圣感 |
| paint_white | 白 | 同上（多边光晶）| 同上 | 同上 + 纯能脉冲发光 |

**统一构图**：玻璃瓶占画幅 60-70%，居中略偏下，瓶口软木塞 + 标签可见。
**规格**：1024×1024 → 256×256，透明背景。
**关键档差**：常见档=简单 + 单色；稀有档=+ 光晕 + 浮粒；传说档=+ magical aura + 全瓶发光 + 粒子轨迹。**三档外观必须可在 32×32 缩略下区分**。

### 2.5 消耗品图标（5 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| C01 | consumable_antidote.png | 解药 | 小药瓶 + 绿液 + 解毒符 |
| C02 | consumable_repair_kit.png | 修复包 | 工具包 + 扳手 + 蓝补丁 |
| C03 | consumable_eraser.png | 刮除剂 | 刮刀 + 颜料溶剂瓶 + 紫雾 |
| C04 | consumable_universal_paint.png | 万能颜料 | 七色彩虹液瓶 + 全光谱 |
| C05 | consumable_gold_pile.png | 金币堆 | 3-5 枚金币堆叠 + 金光 |

**规格**：1024×1024 → 256×256，透明背景，square framing，主体居中。

### 2.6 NPC 头像（2 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| N01 | npc_tattoo_artist.png | 纹身师 | 中年异能者 + 满身纹身可见（颈部 / 锁骨 / 手背）+ 神秘表情 + 紫色 magical glow |
| N02 | npc_merchant.png | 商人 | 卷发流浪商人 + 背包小贩装束 + 怀揣金币 + 狡黠笑容 + 暖光 |

**规格**：1024×1024 → 256×256（圆形 mask 时），圆形构图，肩部以上，**透明背景圆形 framing**。

### 2.7 Boss 头像（3 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| B01 | boss_ai_guardian.png | AI 守卫 | 机械人形 + 红色单眼 + 金属护甲 + 电路纹 + 末日科技感 |
| B02 | boss_alien_consciousness.png | 外星意识体 | 触手 + 多眼 + 异形头颅 + 紫绿配色 + 异界 aura |
| B03 | boss_virus_mutant.png | 病毒变异体 | 扭曲人形 + 增生肿瘤 + 暴露肌肉 + 绿黄脓液 + 病毒纹 |

**规格**：1024×1024，圆形构图（肩部以上），**透明背景**，强化威慑感（高对比 + rim light）。

### 2.8 HUD 装饰元素（8 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| H01 | hud_hp_bar_frame.png | HP 条边框 | 横条圆角边框 + Hades 风金属雕花 + 9-slice 可拉伸 |
| H02 | hud_buff_slot.png | Buff 槽底 | 小正方形圆角底 + 内凹光感 + 透明中央 |
| H03 | hud_skill_slot.png | 技能槽 | 圆形外环 + 内凹槽 + 顶部 Q/E 标识位 + 9-slice |
| H04 | hud_ammo_box.png | 弹药数字框 | 矩形深底 + 1px 金边 + 子弹符浮雕 |
| H05 | hud_minimap_frame.png | 小地图框 | 圆环外饰 + 8 方位刻度（N/S/E/W + 对角线）+ 内圆透明 |
| H06 | hud_shrink_timer.png | 缩圈倒计时背板 | 矩形圆角 + 半透明 + 沙漏浮雕 |
| H07 | hud_weapon_frame.png | 武器图标框 | 方形圆角 + 金属外饰 + 内凹槽 |
| H08 | hud_build_row_bg.png | Build 列表条目背景 | 横条圆角 + 半透明深底 + 1px 暗描边 + 9-slice |

**规格**：1024×1024 → 适配 256/128/64（按用途），透明背景，**主体必须居中且边缘留 9-slice 安全区**（外 16% padding 可拉伸）。

### 2.9 物品占位（5 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| I01 | item_chest_common.png | 普通宝箱 | 木质宝箱 + 铁锁 + 微启缝光 |
| I02 | item_chest_rare.png | 精品宝箱 | 镶银宝箱 + 蓝紫宝石 + 强光 |
| I03 | item_chest_boss.png | Boss 宝箱 | 镶金宝箱 + 红宝石 + magical aura |
| I04 | item_chest_death.png | 死亡宝箱 | 灰旧背包 + 颜料瓶外露 + 拓本卷轴外露 + 半透明灵魂火 |
| I05 | item_recipe_book.png | 配方书 | 古旧书本 + 烫金图案 + 浮夸贴纸 + magical glow |

**规格**：1024×1024 → 256×256，透明背景，square framing。

### 2.10 角色 placeholder（1 张含 8 方向）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| P01 | character_player_8dir_idle.png | 玩家 + Bot 共用顶视角异能者 sprite（俯视）8 方向 idle | 异能者中性体型 + 街头夹克 + 部分纹身可见 + 8 方向 idle 静态站立 |

**规格**：1024×1024 单张包含 8 方向（2×4 grid 排布：N / NE / E / SE / S / SW / W / NW），透明背景，每方向占 256×256 cell（含 padding ≥ 32px），俯视角度（顶视偏 30°）。**严格保持比例 + 服装一致**。

**布局指令**：2 行 4 列 grid，从左上到右下：[N, NE, E, SE] / [S, SW, W, NW]。每 cell 250×250 主体 + 32px 透明 padding。

### 2.11 Hades 风环境贴图（8 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| E01 | env_floor_metal.png | 金属地面 | 工业金属板 + 铆钉 + 锈蚀 + 可平铺 |
| E02 | env_floor_ruins.png | 废墟地面 | 碎裂混凝土 + 钢筋 + 杂草 + 可平铺 |
| E03 | env_floor_blood_rock.png | 血岩地面 | 暗红岩面 + 血脉裂纹 + 异变 + 可平铺 |
| E04 | env_wall_metal.png | 金属墙体 | 工业墙板 + 焊缝 + 涂鸦 + 可平铺 |
| E05 | env_wall_ruins.png | 废墟墙体 | 砖墙断裂 + 弹孔 + 苔藓 + 可平铺 |
| E06 | env_wall_blood.png | 血岩墙体 | 暗红岩墙 + 血纹 + 异形增生 + 可平铺 |
| E07 | env_light_pillar_a.png | 灯光柱 A | 工业荧光柱 + 蓝白光 + 顶部灯罩 |
| E08 | env_light_pillar_b.png | 灯光柱 B | 残破霓虹柱 + 红粉光 + 玻璃碎裂 |

**规格**：1024×1024，**可无缝平铺**（顶部=底部、左=右），整图风格与 Hades 末日科技基调一致。**地面和墙体必须 tileable**（用 prompt 强调 "seamless tileable texture, edge continuity"）。灯光柱不需要平铺，单体即可。

### 2.12 配方卷轴（8 张）

| ID | 文件名 | 主体 | 视觉 |
|---|---|---|---|
| R01 | recipe_scroll_line.png | 直线配方卷轴 | 古旧卷轴 + 中央直线图案 + 烫金封印 + magical glow |
| R02 | recipe_scroll_ring.png | 圆环配方卷轴 | 同上 + 圆环图案 |
| R03 | recipe_scroll_spiral.png | 螺旋配方卷轴 | 同上 + 螺旋图案 |
| R04 | recipe_scroll_zigzag.png | 锯齿配方卷轴 | 同上 + 锯齿图案 |
| R05 | recipe_scroll_bolt.png | 闪电配方卷轴 | 同上 + 闪电图案 |
| R06 | recipe_scroll_star.png | 星形配方卷轴 | 同上 + 五角星图案 |
| R07 | recipe_scroll_stream.png | 流线配方卷轴 | 同上 + 流线图案 |
| R08 | recipe_scroll_beast.png | 兽形配方卷轴 | 同上 + 兽爪图案 |

**规格**：1024×1024 → 256×256，透明背景，square framing，卷轴上下卷起呈现 + 中央展开露出图案。

---

## 3. 总计与落盘

### 3.1 数量汇总

| 类别 | 数量 | 子目录 |
|---|---|---|
| 武器图标 | 5 | `raw/weapon/` |
| 技能图标 | 8 | `raw/skill/` |
| 词缀图标 | 8 | `raw/affix/` |
| 颜料瓶（7×3） | 21 | `raw/paint/` |
| 消耗品图标 | 5 | `raw/consumable/` |
| NPC 头像 | 2 | `raw/npc/` |
| Boss 头像 | 3 | `raw/boss/` |
| HUD 装饰 | 8 | `raw/hud/` |
| 物品占位 | 5 | `raw/item/` |
| 角色 sprite | 1（含 8 方向）| `raw/character/` |
| 环境贴图 | 8 | `raw/env/` |
| 配方卷轴 | 8 | `raw/recipe/` |
| **总计** | **91 张文件**（含 8 方向 sprite 算 1 文件）| — |

### 3.2 落盘路径

| 阶段 | 路径 |
|---|---|
| 原始生成 | `openspec/changes/06-v21-implementation/art/raw/<category>/<filename>.png` |
| 工程使用（主对话后续做）| `Assets/Resources/Sprite/v21/{Weapon,Skill,Affix,Paint,Consumable,NPC,Boss,HUD,Item,Character,Env,Recipe}/` |

art-director 与 codex-image-gen **只产出 raw/**，不碰 Assets/。

### 3.3 批次规划（codex-image-gen 自动归类参考）

- **L2（合并画布）候选**：A01-A08 词缀 8 张 + C01-C05 消耗品 5 张 = 13 张小尺寸 ICON 可合 1-2 张大画布
- **L1（进程内并行）主力**：其余资源全部 1024×1024 单独画作，按 ≤12 张/批分批
- **预估批次**：L1 约 6-7 批 + L2 约 1-2 批

### 3.4 失败容错

失败的图记入 `art/raw/生成记录.md`，状态 `failed` + 原因，等 Codex 配额恢复后整批重试 1 次。仍失败不再重试，由主对话决定降级 / 补做。

---

## 4. 验收 DoD

- [ ] 91 张 PNG 全部生成至对应 `art/raw/<category>/` 子目录
- [ ] 单文件 size > 1KB（验证非空 / 非占位错误）
- [ ] `art/raw/生成记录.md` 完整覆盖（ok / failed 都记）
- [ ] 风格一致性（人工抽检 10 张，全部符合 Hades 厚涂描边基调）
- [ ] 颜料三档外观可区分（缩到 32×32 仍能分辨档位）
- [ ] 8 方向 sprite 比例 + 服装一致
- [ ] 环境贴图可无缝平铺（Photoshop 偏移测试）
- [ ] 头像（NPC + Boss）圆形构图，肩部以上
- [ ] HUD 装饰元素中央留 9-slice 安全区
- [ ] `requirements.md` 与 `prompts.md` 头部更新状态字段为 `已处理` 或 `部分已处理`

---

## 5. 与代码的关联

- 工程落点（后续 client-unity 处理）：`Assets/Resources/Sprite/v21/{Weapon,Skill,Affix,Paint,Consumable,NPC,Boss,HUD,Item,Character,Env,Recipe}/`
- ResourceConfig.json 新增条目：约 91 条 ResourceItem（Key 形如 `Sprite.v21.Weapon.ShortBlade`）
- CombatHUDForm / ShopForm / TattooStudioForm 等通过 `ResourceModule.Load<Sprite>` 引用
- BotControllerModule 共用 character_player_8dir_idle.png（玩家 + 20 SmartBot + 29 LightBot 共用骨架）

---

## 6. 风险与回退

| 风险 | 缓解 |
|---|---|
| Codex 配额限制 | 走 chatgpt auth + 每批 ≤ 12 张 + 失败重试 1 次后等配额 |
| 风格不统一（91 张多批次）| 每批同 prompt 前缀 `A 1024x1024 Hades-style game icon, vibrant colors, painterly brush strokes, strong silhouette, deep shadow outline, magical aura.` 锁基调 |
| 颜料三档外观难区分 | prompt 明确档差描述（常见/稀有/传说三段独立 prompt 模板）|
| 8 方向 sprite 比例飘 | 单图 grid 排布 + prompt 强调"consistent body proportion across all 8 directions"|
| 环境贴图接缝可见 | prompt 加 "seamless tileable texture, edge continuity, no visible seam" + 后期 PS 检查 |
| AI 生成中含文字 | 所有 prompt 加 negative "no text, no letters, no watermark" |
