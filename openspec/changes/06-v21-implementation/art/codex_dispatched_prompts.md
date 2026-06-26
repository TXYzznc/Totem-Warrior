# Codex 实际收到的完整 Prompt 清单

> 从 `.codex-batch.log` 提取，共 **185 次 codex exec 调用记录**
> 每次 codex 接收的内容由 `.codex-batch.sh` 通过 `codex exec -s workspace-write "<prompt>" < /dev/null` 喂入

## 📊 Prompt 大小统计（codex 喂入端）

| 项 | 值 |
|---|---|
| 总调用次数 | 185 次（82 图 + 多次失败重试） |
| 单 prompt 最小 | 786 chars ≈ 196 tokens |
| 单 prompt 最大 | 58,536 chars ≈ 14,634 tokens（仅第 1 次，含 stdin 泄露 bug） |
| 单 prompt 中位 | 897 chars ≈ 224 tokens |
| 单 prompt 平均 | 1,218 chars ≈ 305 tokens |

## 🚨 第 1 次启动的 stdin 泄露 bug

**第 1 次 batch 启动（06-25 17:21）**：脚本里 `codex exec ...` 后面**没有** `< /dev/null` 隔离 stdin，导致整个 82 条 ITEMS_TSV 数据通过 stdin 流泄露到 codex 进程：

```
请使用 imagegen 系统 skill 生成一张图片。
...（正常 prompt 700 chars）

<stdin>
1|raw/weapon/weapon_heavy_hammer.png|1024x1024|A 1024x1024 Hades-style...
2|raw/weapon/weapon_pistol.png|1024x1024|...
... (82 行 TSV 数据)
```

**单次 token 从 215 暴涨到 14,634，多 68 倍。** 中断后用户立刻让我修复脚本（加 `< /dev/null`），第 2 次启动开始就正常了。

## 🔍 关键发现（实测数据驱动，推翻 `codex_usage_analysis.md` 部分推测）

### 1️⃣ 185 次调用 = 185 个独立 session（无任何复用）

| 维度 | 数据 | 含义 |
|---|---|---|
| `=== generating ===` 次数 | 185 | 启动了 185 次 codex exec |
| `session id:` 出现次数 | 185 | 每次启动 1 个新 session |
| **session id 去重后** | **185** | **没有任何 session 复用，全部独立 cold start** |

### 2️⃣ imagegen SKILL.md 实际只被读了 **1 次**（不是每次！）

| 项 | 实测 |
|---|---|
| `Get-Content imagegen/SKILL.md` 命令出现次数 | **1 次** |
| 唯一一次读取发生在 | 第 43 张图 |

**之前 `codex_usage_analysis.md` 推测"每次重读 SKILL.md 浪费 600K token" 是错的**。Codex CLI 实际上做了 SKILL 缓存，只有第 43 张图模型主动调了一次 PowerShell 读 SKILL.md（可能模型当时不确定流程）。

### 3️⃣ 真正元凶：每次 codex exec 内 **image_gen 工具被反复调用 7.6 次**

| cold start 固定开销 | 出现频次 | 平均/exec |
|---|---|---|
| OpenAI Codex 启动头 | 185 | 1.0/exec |
| `Skill descriptions shortened to fit 2% budget` | 185 | 1.0/exec |
| `failed to load skill .* SKILL.md`（12 个坏 frontmatter） | 2,220 | **12.0/exec** |
| `Ignoring malformed agent role`（实测 40 条/次，比之前推测的 19 多一倍） | 7,400 | **40.0/exec** |
| **`image_gen` 工具调用** | **1,403** | **7.6/exec** ← 真正大头 |

**每张图本该 1 次 image_gen 调用，结果调了 7-8 次**。原因：透明背景 + chroma-key 工作流要求模型反复生成、验证、重试。`image_gen` 单次（gpt-image-1 模型）就 3-5K token，**× 7.6 次 ≈ 30K token，占总消耗 63%**。

### 4️⃣ 修正后的 token 分布（单次 codex exec ≈ 47,200 token）

| 来源 | token | 占比 | 修复方法 |
|---|---|---|---|
| **image_gen 反复调用**（7.6 × ~4K） | ~30,000 | **63%** | 改 prompt 明确"只调 image_gen 一次，不要重试" |
| 40 条坏 toml + 12 个坏 SKILL.md 错误加载 | ~10,000 | 21% | 删 `.codex/agents/*.toml` + 修 SKILL.md frontmatter |
| codex CLI 内置 system prompt + tools schema | ~6,000 | 13% | 没法避免（cold start 固有） |
| **prompt 喂入端** | ~225 | **0.5%** | 已经最优，不用改 |
| SKILL.md 实际读取（1 次摊到 185 次） | ~50 | <0.1% | 几乎不影响 |

### 5️⃣ 性价比最高的修复

1. **改 prompt 显式要求"仅 1 次 image_gen 调用，不重试"** → 省 ~60% token
2. **删除 `.codex/agents/*.toml`** + 修 12 个坏 SKILL.md frontmatter → 省 ~21% token
3. （**不要修**）SKILL.md 读取已经被 codex 缓存，不是问题
4. （**不要修**）prompt 长度只占 0.5%，写短也省不了多少

详细原始分析见同目录 [`codex_usage_analysis.md`](./codex_usage_analysis.md)（其中"SKILL.md 重读"部分需按本文修正）。

## 📋 Prompt 模板（标准格式）

每次 codex 收到的 prompt 都是 `.codex-batch.sh` 第 60-72 行的 here-string，结构如下：

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
{STYLE_BASE 250 字} + {单图英文描述 150-200 字}

# 输出要求
- 尺寸：{SIZE}
- 透明背景：是
- 保存到：{OUT 项目相对路径}
- 负面：{NEG_BASE 35 字} + {单图负面提示}

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

> 从 `.codex-batch.log` 提取，共 185 次 codex exec 调用
> 每次 codex 接收的内容由 `.codex-batch.sh` 通过 `codex exec -s workspace-write "<prompt>" < /dev/null` 喂入

---

## [1/82] raw/weapon/weapon_short_blade.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A short blade dagger as a game icon, single-edged steel blade with cold metallic gleam, black wrapped hilt with brass guard, blood drop on tip, centered square framing, subject occupies 75% of canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/weapon/weapon_short_blade.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, sheath, hand holding, character

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。

<stdin>
1|raw/weapon/weapon_heavy_hammer.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A two-handed heavy hammer as a game icon, rusty iron hammer head with cracks glowing dim red, thick wooden shaft wrapped in leather, centered square framing, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, character holding
2|raw/weapon/weapon_pistol.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A semi-automatic pistol as a game icon, black steel finish with brass accents, faint muzzle smoke trailing from barrel, side profile view, centered square framing, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, hand, ammunition magazine separated
3|raw/weapon/weapon_bow.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A compound bow drawn taut with a glowing arrow nocked, dark composite limbs, golden bowstring shimmer, arrow tip emits soft white energy glow, centered square framing, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, quiver, archer
4|raw/weapon/weapon_energy_fist.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An energy fist gauntlet as a game icon, armored knuckle plate wrapped in swirling blue-purple energy field, arcing electric streams around the knuckles, glowing core, centered square framing, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, arm, character
5|raw/skill/skill_fireball.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A blazing fireball spell icon, swirling red-orange fire core with bright yellow center, ember sparks trailing, deep shadow outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
6|raw/skill/skill_ice_field.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A hexagonal ice field spell icon, crystalline ice shards radiating from a central hexagon rune, frost-blue gradient, cold mist at base, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
7|raw/skill/skill_chain_lightning.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A chain lightning spell icon, zigzag yellow electric bolt with branching forks, glowing white-yellow core, dark indigo aura behind, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
8|raw/skill/skill_heal_aura.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A healing aura spell icon, glowing green ring with ascending white cross light from a stylized lotus base, gentle particle drift upward, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
9|raw/skill/skill_shield.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An octagonal energy shield icon, blue-cyan translucent shield plate with rim light glow and internal energy field swirls, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
10|raw/skill/skill_stealth.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A brief invisibility spell icon, semi-transparent humanoid silhouette dissolving into purple-black smoke wisps with afterimage trails, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
11|raw/skill/skill_summon.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A summon beast spell icon, an otherworldly summoning circle with arcane runes, a beast claw silhouette rising from purple energy, magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
12|raw/skill/skill_time_slow.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A time slow spell icon, golden hourglass crossed with clock gears, soft golden trails of slow motion light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
13|raw/affix/affix_fire_damage.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A fire damage plus affix icon, stylized red flame symbol with an upward red arrow overlay, deep red outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
14|raw/affix/affix_cooldown.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A cooldown reduction affix icon, blue hourglass with a downward blue arrow overlay, cyan outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
15|raw/affix/affix_attack_speed.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An attack speed plus affix icon, two crossed yellow daggers with an upward yellow arrow overlay, golden outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
16|raw/affix/affix_crit_chance.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A critical hit chance plus affix icon, purple diamond shape with bullseye target and an upward purple arrow overlay, violet outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
17|raw/affix/affix_crit_damage.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A critical damage plus affix icon, golden explosion cross symbol with an upward golden arrow overlay, gold outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
18|raw/affix/affix_range.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A range plus affix icon, a long-tailed arrow with an upward white arrow overlay, white-silver outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
19|raw/affix/affix_accuracy.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An accuracy plus affix icon, a precision crosshair reticle with an upward white arrow overlay, white outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
20|raw/affix/affix_lifesteal.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A lifesteal on hit affix icon, a dripping blood cross with a small green heart at center, green outline, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration
21|raw/paint/paint_red_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common red paint bottle game icon, simple glass vial with cork stopper, filled with vibrant red liquid (#E63946), blank label on bottle, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, aura, particles, text on label
22|raw/paint/paint_red_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare red paint bottle game icon, ornate glass vial with cork stopper, filled with vibrant red liquid (#E63946) with inner orange-yellow swirl halo, floating ember particles inside, faint glow around bottle, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body aura, text
23|raw/paint/paint_red_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary red paint bottle game icon, exquisite glass vial radiating intense magical aura, vibrant red liquid (#E63946) glowing from within, bright orange particles spiraling around, flame wisps overflowing the cork, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
24|raw/paint/paint_yellow_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common yellow paint bottle game icon, simple glass vial with cork stopper, filled with vibrant yellow liquid (#F4C430) with faint lightning bolt pattern floating inside, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text
25|raw/paint/paint_yellow_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare yellow paint bottle game icon, ornate glass vial, vibrant yellow liquid (#F4C430) with inner electric bolt swirls, floating spark particles inside, faint yellow glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full aura, text
26|raw/paint/paint_yellow_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary yellow paint bottle game icon, glass vial radiating intense magical aura, vibrant yellow liquid (#F4C430) glowing from within, lightning arcs escaping the cork, bright sparks spiraling, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
27|raw/paint/paint_green_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common green paint bottle game icon, simple glass vial with cork, filled with vibrant green liquid (#3DDC84) with toxic bubbles on surface, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text
28|raw/paint/paint_green_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare green paint bottle game icon, ornate glass vial, vibrant green liquid (#3DDC84) with inner emerald swirl, floating spores inside, faint green glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
29|raw/paint/paint_green_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary green paint bottle game icon, vial radiating intense magical aura, vibrant green liquid (#3DDC84) glowing from within, emerald mist overflowing the cork, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
30|raw/paint/paint_blue_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common blue paint bottle game icon, simple glass vial with cork, filled with vibrant blue liquid (#1FB6FF) with snowflake pattern floating inside, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text
31|raw/paint/paint_blue_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare blue paint bottle game icon, ornate glass vial, vibrant blue liquid (#1FB6FF) with inner ice swirls, floating frost crystals inside, faint cyan glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
32|raw/paint/paint_blue_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary blue paint bottle game icon, vial radiating intense magical aura, vibrant blue liquid (#1FB6FF) glowing from within, ice crystals radiating outward, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
33|raw/paint/paint_purple_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common purple paint bottle game icon, simple glass vial with cork, filled with vibrant purple liquid (#9D4EDD) with mutation swirl pattern inside, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text
34|raw/paint/paint_purple_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare purple paint bottle game icon, ornate glass vial, vibrant purple liquid (#9D4EDD) with inner violet swirls, floating dark particles, faint purple glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
35|raw/paint/paint_purple_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary purple paint bottle game icon, vial radiating intense magical aura, vibrant purple liquid (#9D4EDD) glowing, distorted purple mist overflowing, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
36|raw/paint/paint_gold_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common gold paint bottle game icon, simple glass vial with cork, filled with shimmering gold liquid (#FFB400) with small cross light and gold dust inside, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full glow, text
37|raw/paint/paint_gold_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare gold paint bottle game icon, ornate glass vial, shimmering gold liquid (#FFB400) with inner radiant cross, floating gold flecks, faint warm glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
38|raw/paint/paint_gold_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary gold paint bottle game icon, vial radiating intense divine aura, shimmering gold liquid (#FFB400) glowing brilliantly, golden light rays emanating, holy ambience, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
39|raw/paint/paint_white_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common white paint bottle game icon, simple glass vial with cork, filled with pure white liquid (#F8F9FA) with polyhedral light crystal floating inside, blank label, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text
40|raw/paint/paint_white_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare white paint bottle game icon, ornate glass vial, pure white liquid (#F8F9FA) with inner crystalline shards, floating light particles, faint pale-blue glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
41|raw/paint/paint_white_legendary.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary white paint bottle game icon, vial radiating intense pure energy aura, white liquid (#F8F9FA) pulsating with brilliant light, prismatic crystals orbiting, entire bottle radiates light, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
42|raw/consumable/consumable_antidote.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An antidote vial game icon, small glass medicine bottle with cork, filled with bright green serum, antidote cross symbol on label, faint emerald glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
43|raw/consumable/consumable_repair_kit.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A repair kit game icon, opened toolbox with wrench, screwdriver and blue patches visible, sturdy leather wrap, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
44|raw/consumable/consumable_eraser.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A tattoo eraser tool game icon, metal scraper alongside a vial of solvent, faint purple corrosive mist, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
45|raw/consumable/consumable_universal_paint.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A universal paint bottle game icon, ornate glass vial filled with swirling rainbow liquid showing all 7 colors blending, prismatic glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
46|raw/consumable/consumable_gold_pile.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A gold coin pile game icon, 3 to 5 shiny gold coins stacked with one tilted on top, warm golden glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, characters on coins
47|raw/npc/npc_tattoo_artist.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a middle-aged tattoo artist with mysterious aura, shoulders-up composition, weathered face with focused gaze, visible tattoos covering neck, collarbones and back of hands, dark vest over rolled-up sleeves, subtle purple magical glow emanating from tattoos, dramatic side lighting, transparent background, suitable for circular UI mask.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text
48|raw/npc/npc_merchant.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a wandering merchant peddler with cunning smile, shoulders-up composition, curly hair, weathered traveling coat, satchel straps visible across chest, holding a gold coin between fingers, warm amber lighting, transparent background, suitable for circular UI mask.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text
49|raw/boss/boss_ai_guardian.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an AI Guardian boss, shoulders-up composition, mechanical humanoid head and torso, single glowing red cyclopean eye, polished metallic armor plates with embossed circuit patterns, exposed servo joints at neck, intimidating apocalyptic tech aesthetic, harsh rim light, transparent background, suitable for circular UI mask.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text
50|raw/boss/boss_alien_consciousness.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an Alien Consciousness boss, shoulders-up composition, otherworldly head with multiple glowing eyes and writhing tentacles around the face, iridescent purple and green skin patterns, deep alien aura, dramatic lighting, transparent background, suitable for circular UI mask.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text
51|raw/boss/boss_virus_mutant.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a Virus Mutant boss, shoulders-up composition, twisted humanoid form with grotesque tumor growths bursting from skin, exposed mutated muscle, sickly green-yellow pus oozing along virus vein patterns, horrific yet detailed, dramatic lighting, transparent background, suitable for circular UI mask.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text
52|raw/hud/hud_hp_bar_frame.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded HP bar frame for game HUD, Hades-style metallic engraved border with subtle filigree details, inner area completely transparent for filled bar overlay, 9-slice friendly with stretchable middle section, edges keep 16% padding, centered on canvas, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, fill content inside, text, characters
53|raw/hud/hud_buff_slot.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A small square buff slot frame for game HUD, rounded corners, subtle inner shadow giving recessed look, polished metal trim, central area completely transparent for icon overlay, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, text
54|raw/hud/hud_skill_slot.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular skill slot frame for game HUD, ornate metal outer ring with subtle Hades-style engraving, recessed central well completely transparent for icon overlay, small notch at top for key label area, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, Q letter, E letter, text
55|raw/hud/hud_ammo_box.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular ammunition number plate for game HUD, semi-transparent dark background panel with 1px gold trim border, subtle embossed bullet symbol on left side, central area transparent for number overlay, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text
56|raw/hud/hud_minimap_frame.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular minimap frame for game HUD, ornate outer ring with 8 directional tick marks at N S E W and diagonals, polished metallic finish with Hades-style filigree, central area completely transparent for map content overlay, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, map content inside, text labels
57|raw/hud/hud_shrink_timer.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular rounded shrink countdown plate for game HUD, semi-transparent dark background with subtle embossed hourglass icon on left, gold trim, central area transparent for countdown overlay, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text
58|raw/hud/hud_weapon_frame.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A square weapon icon frame for game HUD, rounded corners with metallic trim, recessed central well completely transparent for weapon icon overlay, polished Hades-style detail, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, weapon inside, text
59|raw/hud/hud_build_row_bg.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded list row background for game HUD build list, semi-transparent dark fill with 1px dark outline, 9-slice friendly with stretchable middle, edges keep 16% padding, centered on canvas, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, content inside, text, icons
60|raw/item/item_chest_common.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common wooden chest game icon, sturdy oak planks with iron bands and lock, slight crack glowing faintly from lid, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, content inside, text
61|raw/item/item_chest_rare.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare silver-trimmed chest game icon, polished wood with silver bands, blue-violet gemstone embedded on front, strong magical glow leaking from edges, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text
62|raw/item/item_chest_boss.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A boss-tier gold chest game icon, ornate gold-trimmed dark wood with engraved patterns, large red ruby gem on front, intense magical aura radiating, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text
63|raw/item/item_chest_death.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A death chest game icon, worn gray adventurer backpack on ground, paint vial peeking from top, parchment scroll edge sticking out, semi-transparent ghostly soul flame hovering above, somber mood, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text
64|raw/item/item_recipe_book.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe book game icon, ancient leather-bound tome with gilded ornamental patterns on cover, flamboyant stickers and bookmarks, magical glow seeping from page edges, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened pages, text
65|raw/character/character_player_8dir_idle.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An 8-directional idle sprite sheet for a top-down (slight 30-degree tilt) view game character. The canvas is divided into a clean 2-row by 4-column grid (each cell 256x256 with transparent 32px padding between cells). Row 1 left to right: facing North, facing North-East, facing East, facing South-East. Row 2 left to right: facing South, facing South-West, facing West, facing North-West. Character is an androgynous psionic warrior with athletic medium build, wearing a dark street jacket with rolled sleeves, visible tattoos on forearms and neck, dark cargo pants, combat boots. CRITICAL: body proportions, outfit, colors, lighting and shadow must be identical across all 8 directions. Idle pose: standing relaxed with weight slightly on one leg. Transparent background. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, action poses, weapons drawn, walking, running, inconsistent body proportion, costume change between cells, text labels, grid lines, frame borders
66|raw/env/env_floor_metal.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal floor texture, dark steel plates with rivets and visible rust patches, oil stains, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style, dramatic top lighting.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges, single focal point
67|raw/env/env_floor_ruins.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins floor texture, cracked concrete with exposed rebar, scattered debris, small weeds in cracks, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges
68|raw/env/env_floor_blood_rock.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood rock floor texture, dark red rocky surface with pulsing blood vein cracks, mutated bio-organic accents, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges
69|raw/env/env_wall_metal.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal wall texture, dark steel panel with welded seams, faded graffiti, rust streaks, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges, single focal point
70|raw/env/env_wall_ruins.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins wall texture, broken brick with bullet holes, moss patches, exposed insulation, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges
71|raw/env/env_wall_blood.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood-rock wall texture, dark red rock face with blood vein patterns, bio-organic growths bulging outward, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges
72|raw/env/env_light_pillar_a.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An industrial fluorescent light pillar game prop, tall vertical fixture with blue-white glowing tubes inside, top metallic shade, base bolted to ground, painterly Hades style, transparent background. Single subject centered.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text
73|raw/env/env_light_pillar_b.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A broken neon light pillar game prop, weathered pink-red neon glass tubes partially cracked, faint flicker glow, dystopian feel, painterly Hades style, transparent background. Single subject centered.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text
74|raw/recipe/recipe_scroll_line.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll with both ends rolled up, central area unrolled showing a single bold straight line tattoo pattern in dark gold ink, gilded wax seal at bottom, faint magical glow around scroll edges, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
75|raw/recipe/recipe_scroll_ring.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a double concentric ring tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
76|raw/recipe/recipe_scroll_spiral.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing an Archimedean 5-loop spiral tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
77|raw/recipe/recipe_scroll_zigzag.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a 4-segment zigzag tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
78|raw/recipe/recipe_scroll_bolt.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a forked lightning bolt tattoo pattern in dark gold ink with branching forks, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
79|raw/recipe/recipe_scroll_star.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a five-pointed star tattoo pattern with surrounding dashed probability arcs in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
80|raw/recipe/recipe_scroll_stream.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a stream tattoo pattern with 3 parallel flowing lines in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
81|raw/recipe/recipe_scroll_beast.png|1024x1024|A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a beast claw silhouette tattoo pattern with summoning runes in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.|no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters
</stdin>
```

---

## [2/82] raw/weapon/weapon_heavy_hammer.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A two-handed heavy hammer as a game icon, rusty iron hammer head with cracks glowing dim red, thick wooden shaft wrapped in leather, centered square framing, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/weapon/weapon_heavy_hammer.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, character holding

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [3/82] raw/weapon/weapon_pistol.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A semi-automatic pistol as a game icon, black steel finish with brass accents, faint muzzle smoke trailing from barrel, side profile view, centered square framing, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/weapon/weapon_pistol.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, hand, ammunition magazine separated

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [4/82] raw/weapon/weapon_bow.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A compound bow drawn taut with a glowing arrow nocked, dark composite limbs, golden bowstring shimmer, arrow tip emits soft white energy glow, centered square framing, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/weapon/weapon_bow.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, quiver, archer

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [5/82] raw/weapon/weapon_energy_fist.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An energy fist gauntlet as a game icon, armored knuckle plate wrapped in swirling blue-purple energy field, arcing electric streams around the knuckles, glowing core, centered square framing, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/weapon/weapon_energy_fist.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, arm, character

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [6/82] raw/skill/skill_fireball.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A blazing fireball spell icon, swirling red-orange fire core with bright yellow center, ember sparks trailing, deep shadow outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_fireball.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [7/82] raw/skill/skill_ice_field.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A hexagonal ice field spell icon, crystalline ice shards radiating from a central hexagon rune, frost-blue gradient, cold mist at base, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_ice_field.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [8/82] raw/skill/skill_chain_lightning.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A chain lightning spell icon, zigzag yellow electric bolt with branching forks, glowing white-yellow core, dark indigo aura behind, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_chain_lightning.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [9/82] raw/skill/skill_heal_aura.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A healing aura spell icon, glowing green ring with ascending white cross light from a stylized lotus base, gentle particle drift upward, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_heal_aura.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [10/82] raw/skill/skill_shield.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An octagonal energy shield icon, blue-cyan translucent shield plate with rim light glow and internal energy field swirls, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_shield.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [11/82] raw/skill/skill_stealth.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A brief invisibility spell icon, semi-transparent humanoid silhouette dissolving into purple-black smoke wisps with afterimage trails, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_stealth.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [12/82] raw/skill/skill_summon.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A summon beast spell icon, an otherworldly summoning circle with arcane runes, a beast claw silhouette rising from purple energy, magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_summon.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [13/82] raw/skill/skill_time_slow.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A time slow spell icon, golden hourglass crossed with clock gears, soft golden trails of slow motion light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/skill/skill_time_slow.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [14/82] raw/affix/affix_fire_damage.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A fire damage plus affix icon, stylized red flame symbol with an upward red arrow overlay, deep red outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_fire_damage.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [15/82] raw/affix/affix_cooldown.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A cooldown reduction affix icon, blue hourglass with a downward blue arrow overlay, cyan outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_cooldown.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [16/82] raw/affix/affix_attack_speed.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An attack speed plus affix icon, two crossed yellow daggers with an upward yellow arrow overlay, golden outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_attack_speed.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [17/82] raw/affix/affix_crit_chance.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A critical hit chance plus affix icon, purple diamond shape with bullseye target and an upward purple arrow overlay, violet outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_crit_chance.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [18/82] raw/affix/affix_crit_damage.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A critical damage plus affix icon, golden explosion cross symbol with an upward golden arrow overlay, gold outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_crit_damage.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [19/82] raw/affix/affix_range.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A range plus affix icon, a long-tailed arrow with an upward white arrow overlay, white-silver outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_range.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [20/82] raw/affix/affix_accuracy.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An accuracy plus affix icon, a precision crosshair reticle with an upward white arrow overlay, white outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_accuracy.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [21/82] raw/affix/affix_lifesteal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A lifesteal on hit affix icon, a dripping blood cross with a small green heart at center, green outline, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/affix/affix_lifesteal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [22/82] raw/paint/paint_red_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common red paint bottle game icon, simple glass vial with cork stopper, filled with vibrant red liquid (#E63946), blank label on bottle, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, aura, particles, text on label

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [23/82] raw/paint/paint_red_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare red paint bottle game icon, ornate glass vial with cork stopper, filled with vibrant red liquid (#E63946) with inner orange-yellow swirl halo, floating ember particles inside, faint glow around bottle, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body aura, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [24/82] raw/paint/paint_red_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary red paint bottle game icon, exquisite glass vial radiating intense magical aura, vibrant red liquid (#E63946) glowing from within, bright orange particles spiraling around, flame wisps overflowing the cork, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [25/82] raw/paint/paint_yellow_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common yellow paint bottle game icon, simple glass vial with cork stopper, filled with vibrant yellow liquid (#F4C430) with faint lightning bolt pattern floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [26/82] raw/paint/paint_yellow_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare yellow paint bottle game icon, ornate glass vial, vibrant yellow liquid (#F4C430) with inner electric bolt swirls, floating spark particles inside, faint yellow glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full aura, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [27/82] raw/paint/paint_yellow_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary yellow paint bottle game icon, glass vial radiating intense magical aura, vibrant yellow liquid (#F4C430) glowing from within, lightning arcs escaping the cork, bright sparks spiraling, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [28/82] raw/paint/paint_green_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common green paint bottle game icon, simple glass vial with cork, filled with vibrant green liquid (#3DDC84) with toxic bubbles on surface, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [29/82] raw/paint/paint_green_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare green paint bottle game icon, ornate glass vial, vibrant green liquid (#3DDC84) with inner emerald swirl, floating spores inside, faint green glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [30/82] raw/paint/paint_green_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary green paint bottle game icon, vial radiating intense magical aura, vibrant green liquid (#3DDC84) glowing from within, emerald mist overflowing the cork, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [31/82] raw/paint/paint_blue_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common blue paint bottle game icon, simple glass vial with cork, filled with vibrant blue liquid (#1FB6FF) with snowflake pattern floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [32/82] raw/paint/paint_blue_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare blue paint bottle game icon, ornate glass vial, vibrant blue liquid (#1FB6FF) with inner ice swirls, floating frost crystals inside, faint cyan glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [33/82] raw/paint/paint_blue_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary blue paint bottle game icon, vial radiating intense magical aura, vibrant blue liquid (#1FB6FF) glowing from within, ice crystals radiating outward, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [34/82] raw/paint/paint_purple_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common purple paint bottle game icon, simple glass vial with cork, filled with vibrant purple liquid (#9D4EDD) with mutation swirl pattern inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [35/82] raw/paint/paint_purple_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare purple paint bottle game icon, ornate glass vial, vibrant purple liquid (#9D4EDD) with inner violet swirls, floating dark particles, faint purple glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [36/82] raw/paint/paint_purple_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary purple paint bottle game icon, vial radiating intense magical aura, vibrant purple liquid (#9D4EDD) glowing, distorted purple mist overflowing, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [37/82] raw/paint/paint_gold_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common gold paint bottle game icon, simple glass vial with cork, filled with shimmering gold liquid (#FFB400) with small cross light and gold dust inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [38/82] raw/paint/paint_gold_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare gold paint bottle game icon, ornate glass vial, shimmering gold liquid (#FFB400) with inner radiant cross, floating gold flecks, faint warm glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [39/82] raw/paint/paint_gold_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary gold paint bottle game icon, vial radiating intense divine aura, shimmering gold liquid (#FFB400) glowing brilliantly, golden light rays emanating, holy ambience, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [40/82] raw/paint/paint_white_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common white paint bottle game icon, simple glass vial with cork, filled with pure white liquid (#F8F9FA) with polyhedral light crystal floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [41/82] raw/paint/paint_white_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare white paint bottle game icon, ornate glass vial, pure white liquid (#F8F9FA) with inner crystalline shards, floating light particles, faint pale-blue glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [42/82] raw/paint/paint_white_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary white paint bottle game icon, vial radiating intense pure energy aura, white liquid (#F8F9FA) pulsating with brilliant light, prismatic crystals orbiting, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [43/82] raw/consumable/consumable_antidote.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An antidote vial game icon, small glass medicine bottle with cork, filled with bright green serum, antidote cross symbol on label, faint emerald glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_antidote.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [44/82] raw/consumable/consumable_repair_kit.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A repair kit game icon, opened toolbox with wrench, screwdriver and blue patches visible, sturdy leather wrap, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_repair_kit.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [45/82] raw/consumable/consumable_eraser.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A tattoo eraser tool game icon, metal scraper alongside a vial of solvent, faint purple corrosive mist, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_eraser.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [46/82] raw/consumable/consumable_universal_paint.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A universal paint bottle game icon, ornate glass vial filled with swirling rainbow liquid showing all 7 colors blending, prismatic glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_universal_paint.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [47/82] raw/consumable/consumable_gold_pile.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A gold coin pile game icon, 3 to 5 shiny gold coins stacked with one tilted on top, warm golden glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_gold_pile.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, characters on coins

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [48/82] raw/npc/npc_tattoo_artist.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a middle-aged tattoo artist with mysterious aura, shoulders-up composition, weathered face with focused gaze, visible tattoos covering neck, collarbones and back of hands, dark vest over rolled-up sleeves, subtle purple magical glow emanating from tattoos, dramatic side lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_tattoo_artist.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [49/82] raw/npc/npc_merchant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a wandering merchant peddler with cunning smile, shoulders-up composition, curly hair, weathered traveling coat, satchel straps visible across chest, holding a gold coin between fingers, warm amber lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_merchant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [50/82] raw/boss/boss_ai_guardian.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an AI Guardian boss, shoulders-up composition, mechanical humanoid head and torso, single glowing red cyclopean eye, polished metallic armor plates with embossed circuit patterns, exposed servo joints at neck, intimidating apocalyptic tech aesthetic, harsh rim light, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_ai_guardian.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [51/82] raw/boss/boss_alien_consciousness.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an Alien Consciousness boss, shoulders-up composition, otherworldly head with multiple glowing eyes and writhing tentacles around the face, iridescent purple and green skin patterns, deep alien aura, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_alien_consciousness.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [52/82] raw/boss/boss_virus_mutant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a Virus Mutant boss, shoulders-up composition, twisted humanoid form with grotesque tumor growths bursting from skin, exposed mutated muscle, sickly green-yellow pus oozing along virus vein patterns, horrific yet detailed, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_virus_mutant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [53/82] raw/hud/hud_hp_bar_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded HP bar frame for game HUD, Hades-style metallic engraved border with subtle filigree details, inner area completely transparent for filled bar overlay, 9-slice friendly with stretchable middle section, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_hp_bar_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, fill content inside, text, characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [54/82] raw/hud/hud_buff_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A small square buff slot frame for game HUD, rounded corners, subtle inner shadow giving recessed look, polished metal trim, central area completely transparent for icon overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_buff_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [55/82] raw/hud/hud_skill_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular skill slot frame for game HUD, ornate metal outer ring with subtle Hades-style engraving, recessed central well completely transparent for icon overlay, small notch at top for key label area, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_skill_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, Q letter, E letter, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [56/82] raw/hud/hud_ammo_box.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular ammunition number plate for game HUD, semi-transparent dark background panel with 1px gold trim border, subtle embossed bullet symbol on left side, central area transparent for number overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_ammo_box.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [57/82] raw/hud/hud_minimap_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular minimap frame for game HUD, ornate outer ring with 8 directional tick marks at N S E W and diagonals, polished metallic finish with Hades-style filigree, central area completely transparent for map content overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_minimap_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, map content inside, text labels

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [58/82] raw/hud/hud_shrink_timer.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular rounded shrink countdown plate for game HUD, semi-transparent dark background with subtle embossed hourglass icon on left, gold trim, central area transparent for countdown overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_shrink_timer.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [59/82] raw/hud/hud_weapon_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A square weapon icon frame for game HUD, rounded corners with metallic trim, recessed central well completely transparent for weapon icon overlay, polished Hades-style detail, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_weapon_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, weapon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [60/82] raw/hud/hud_build_row_bg.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded list row background for game HUD build list, semi-transparent dark fill with 1px dark outline, 9-slice friendly with stretchable middle, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_build_row_bg.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, content inside, text, icons

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [61/82] raw/item/item_chest_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common wooden chest game icon, sturdy oak planks with iron bands and lock, slight crack glowing faintly from lid, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, content inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [62/82] raw/item/item_chest_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare silver-trimmed chest game icon, polished wood with silver bands, blue-violet gemstone embedded on front, strong magical glow leaking from edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [63/82] raw/item/item_chest_boss.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A boss-tier gold chest game icon, ornate gold-trimmed dark wood with engraved patterns, large red ruby gem on front, intense magical aura radiating, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_boss.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [64/82] raw/item/item_chest_death.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A death chest game icon, worn gray adventurer backpack on ground, paint vial peeking from top, parchment scroll edge sticking out, semi-transparent ghostly soul flame hovering above, somber mood, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_death.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [65/82] raw/item/item_recipe_book.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe book game icon, ancient leather-bound tome with gilded ornamental patterns on cover, flamboyant stickers and bookmarks, magical glow seeping from page edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_recipe_book.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened pages, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [66/82] raw/character/character_player_8dir_idle.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An 8-directional idle sprite sheet for a top-down (slight 30-degree tilt) view game character. The canvas is divided into a clean 2-row by 4-column grid (each cell 256x256 with transparent 32px padding between cells). Row 1 left to right: facing North, facing North-East, facing East, facing South-East. Row 2 left to right: facing South, facing South-West, facing West, facing North-West. Character is an androgynous psionic warrior with athletic medium build, wearing a dark street jacket with rolled sleeves, visible tattoos on forearms and neck, dark cargo pants, combat boots. CRITICAL: body proportions, outfit, colors, lighting and shadow must be identical across all 8 directions. Idle pose: standing relaxed with weight slightly on one leg. Transparent background. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/character/character_player_8dir_idle.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, action poses, weapons drawn, walking, running, inconsistent body proportion, costume change between cells, text labels, grid lines, frame borders

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [67/82] raw/env/env_floor_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal floor texture, dark steel plates with rivets and visible rust patches, oil stains, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style, dramatic top lighting.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [68/82] raw/env/env_floor_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins floor texture, cracked concrete with exposed rebar, scattered debris, small weeds in cracks, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [69/82] raw/env/env_floor_blood_rock.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood rock floor texture, dark red rocky surface with pulsing blood vein cracks, mutated bio-organic accents, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_blood_rock.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [70/82] raw/env/env_wall_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal wall texture, dark steel panel with welded seams, faded graffiti, rust streaks, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [71/82] raw/env/env_wall_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins wall texture, broken brick with bullet holes, moss patches, exposed insulation, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [72/82] raw/env/env_wall_blood.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood-rock wall texture, dark red rock face with blood vein patterns, bio-organic growths bulging outward, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_blood.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [73/82] raw/env/env_light_pillar_a.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An industrial fluorescent light pillar game prop, tall vertical fixture with blue-white glowing tubes inside, top metallic shade, base bolted to ground, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_a.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [74/82] raw/env/env_light_pillar_b.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A broken neon light pillar game prop, weathered pink-red neon glass tubes partially cracked, faint flicker glow, dystopian feel, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_b.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [75/82] raw/recipe/recipe_scroll_line.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll with both ends rolled up, central area unrolled showing a single bold straight line tattoo pattern in dark gold ink, gilded wax seal at bottom, faint magical glow around scroll edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_line.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [76/82] raw/recipe/recipe_scroll_ring.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a double concentric ring tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_ring.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [77/82] raw/recipe/recipe_scroll_spiral.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing an Archimedean 5-loop spiral tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_spiral.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [78/82] raw/recipe/recipe_scroll_zigzag.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a 4-segment zigzag tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_zigzag.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [79/82] raw/recipe/recipe_scroll_bolt.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a forked lightning bolt tattoo pattern in dark gold ink with branching forks, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_bolt.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [80/82] raw/recipe/recipe_scroll_star.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a five-pointed star tattoo pattern with surrounding dashed probability arcs in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_star.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [81/82] raw/recipe/recipe_scroll_stream.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a stream tattoo pattern with 3 parallel flowing lines in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_stream.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [82/82] raw/recipe/recipe_scroll_beast.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a beast claw silhouette tattoo pattern with summoning runes in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_beast.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [22/82] raw/paint/paint_red_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common red paint bottle game icon, simple glass vial with cork stopper, filled with vibrant red liquid (#E63946), blank label on bottle, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, aura, particles, text on label

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [23/82] raw/paint/paint_red_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare red paint bottle game icon, ornate glass vial with cork stopper, filled with vibrant red liquid (#E63946) with inner orange-yellow swirl halo, floating ember particles inside, faint glow around bottle, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body aura, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [24/82] raw/paint/paint_red_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary red paint bottle game icon, exquisite glass vial radiating intense magical aura, vibrant red liquid (#E63946) glowing from within, bright orange particles spiraling around, flame wisps overflowing the cork, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_red_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [25/82] raw/paint/paint_yellow_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common yellow paint bottle game icon, simple glass vial with cork stopper, filled with vibrant yellow liquid (#F4C430) with faint lightning bolt pattern floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [26/82] raw/paint/paint_yellow_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare yellow paint bottle game icon, ornate glass vial, vibrant yellow liquid (#F4C430) with inner electric bolt swirls, floating spark particles inside, faint yellow glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full aura, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [27/82] raw/paint/paint_yellow_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary yellow paint bottle game icon, glass vial radiating intense magical aura, vibrant yellow liquid (#F4C430) glowing from within, lightning arcs escaping the cork, bright sparks spiraling, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_yellow_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [28/82] raw/paint/paint_green_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common green paint bottle game icon, simple glass vial with cork, filled with vibrant green liquid (#3DDC84) with toxic bubbles on surface, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [29/82] raw/paint/paint_green_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare green paint bottle game icon, ornate glass vial, vibrant green liquid (#3DDC84) with inner emerald swirl, floating spores inside, faint green glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [30/82] raw/paint/paint_green_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary green paint bottle game icon, vial radiating intense magical aura, vibrant green liquid (#3DDC84) glowing from within, emerald mist overflowing the cork, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_green_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [31/82] raw/paint/paint_blue_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common blue paint bottle game icon, simple glass vial with cork, filled with vibrant blue liquid (#1FB6FF) with snowflake pattern floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [32/82] raw/paint/paint_blue_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare blue paint bottle game icon, ornate glass vial, vibrant blue liquid (#1FB6FF) with inner ice swirls, floating frost crystals inside, faint cyan glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [33/82] raw/paint/paint_blue_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary blue paint bottle game icon, vial radiating intense magical aura, vibrant blue liquid (#1FB6FF) glowing from within, ice crystals radiating outward, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_blue_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [34/82] raw/paint/paint_purple_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common purple paint bottle game icon, simple glass vial with cork, filled with vibrant purple liquid (#9D4EDD) with mutation swirl pattern inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [35/82] raw/paint/paint_purple_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare purple paint bottle game icon, ornate glass vial, vibrant purple liquid (#9D4EDD) with inner violet swirls, floating dark particles, faint purple glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [36/82] raw/paint/paint_purple_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary purple paint bottle game icon, vial radiating intense magical aura, vibrant purple liquid (#9D4EDD) glowing, distorted purple mist overflowing, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_purple_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [37/82] raw/paint/paint_gold_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common gold paint bottle game icon, simple glass vial with cork, filled with shimmering gold liquid (#FFB400) with small cross light and gold dust inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [38/82] raw/paint/paint_gold_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare gold paint bottle game icon, ornate glass vial, shimmering gold liquid (#FFB400) with inner radiant cross, floating gold flecks, faint warm glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [39/82] raw/paint/paint_gold_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary gold paint bottle game icon, vial radiating intense divine aura, shimmering gold liquid (#FFB400) glowing brilliantly, golden light rays emanating, holy ambience, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_gold_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [40/82] raw/paint/paint_white_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common white paint bottle game icon, simple glass vial with cork, filled with pure white liquid (#F8F9FA) with polyhedral light crystal floating inside, blank label, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, glow, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [41/82] raw/paint/paint_white_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare white paint bottle game icon, ornate glass vial, pure white liquid (#F8F9FA) with inner crystalline shards, floating light particles, faint pale-blue glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [42/82] raw/paint/paint_white_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary white paint bottle game icon, vial radiating intense pure energy aura, white liquid (#F8F9FA) pulsating with brilliant light, prismatic crystals orbiting, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [43/82] raw/consumable/consumable_antidote.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An antidote vial game icon, small glass medicine bottle with cork, filled with bright green serum, antidote cross symbol on label, faint emerald glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_antidote.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [44/82] raw/consumable/consumable_repair_kit.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A repair kit game icon, opened toolbox with wrench, screwdriver and blue patches visible, sturdy leather wrap, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_repair_kit.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [45/82] raw/consumable/consumable_eraser.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A tattoo eraser tool game icon, metal scraper alongside a vial of solvent, faint purple corrosive mist, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_eraser.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [46/82] raw/consumable/consumable_universal_paint.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A universal paint bottle game icon, ornate glass vial filled with swirling rainbow liquid showing all 7 colors blending, prismatic glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_universal_paint.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [47/82] raw/consumable/consumable_gold_pile.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A gold coin pile game icon, 3 to 5 shiny gold coins stacked with one tilted on top, warm golden glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_gold_pile.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, characters on coins

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [48/82] raw/npc/npc_tattoo_artist.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a middle-aged tattoo artist with mysterious aura, shoulders-up composition, weathered face with focused gaze, visible tattoos covering neck, collarbones and back of hands, dark vest over rolled-up sleeves, subtle purple magical glow emanating from tattoos, dramatic side lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_tattoo_artist.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [49/82] raw/npc/npc_merchant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a wandering merchant peddler with cunning smile, shoulders-up composition, curly hair, weathered traveling coat, satchel straps visible across chest, holding a gold coin between fingers, warm amber lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_merchant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [50/82] raw/boss/boss_ai_guardian.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an AI Guardian boss, shoulders-up composition, mechanical humanoid head and torso, single glowing red cyclopean eye, polished metallic armor plates with embossed circuit patterns, exposed servo joints at neck, intimidating apocalyptic tech aesthetic, harsh rim light, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_ai_guardian.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [51/82] raw/boss/boss_alien_consciousness.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an Alien Consciousness boss, shoulders-up composition, otherworldly head with multiple glowing eyes and writhing tentacles around the face, iridescent purple and green skin patterns, deep alien aura, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_alien_consciousness.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [52/82] raw/boss/boss_virus_mutant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a Virus Mutant boss, shoulders-up composition, twisted humanoid form with grotesque tumor growths bursting from skin, exposed mutated muscle, sickly green-yellow pus oozing along virus vein patterns, horrific yet detailed, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_virus_mutant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [53/82] raw/hud/hud_hp_bar_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded HP bar frame for game HUD, Hades-style metallic engraved border with subtle filigree details, inner area completely transparent for filled bar overlay, 9-slice friendly with stretchable middle section, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_hp_bar_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, fill content inside, text, characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [54/82] raw/hud/hud_buff_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A small square buff slot frame for game HUD, rounded corners, subtle inner shadow giving recessed look, polished metal trim, central area completely transparent for icon overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_buff_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [55/82] raw/hud/hud_skill_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular skill slot frame for game HUD, ornate metal outer ring with subtle Hades-style engraving, recessed central well completely transparent for icon overlay, small notch at top for key label area, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_skill_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, Q letter, E letter, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [56/82] raw/hud/hud_ammo_box.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular ammunition number plate for game HUD, semi-transparent dark background panel with 1px gold trim border, subtle embossed bullet symbol on left side, central area transparent for number overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_ammo_box.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [57/82] raw/hud/hud_minimap_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular minimap frame for game HUD, ornate outer ring with 8 directional tick marks at N S E W and diagonals, polished metallic finish with Hades-style filigree, central area completely transparent for map content overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_minimap_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, map content inside, text labels

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [58/82] raw/hud/hud_shrink_timer.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular rounded shrink countdown plate for game HUD, semi-transparent dark background with subtle embossed hourglass icon on left, gold trim, central area transparent for countdown overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_shrink_timer.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [59/82] raw/hud/hud_weapon_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A square weapon icon frame for game HUD, rounded corners with metallic trim, recessed central well completely transparent for weapon icon overlay, polished Hades-style detail, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_weapon_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, weapon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [60/82] raw/hud/hud_build_row_bg.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded list row background for game HUD build list, semi-transparent dark fill with 1px dark outline, 9-slice friendly with stretchable middle, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_build_row_bg.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, content inside, text, icons

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [61/82] raw/item/item_chest_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common wooden chest game icon, sturdy oak planks with iron bands and lock, slight crack glowing faintly from lid, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, content inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [62/82] raw/item/item_chest_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare silver-trimmed chest game icon, polished wood with silver bands, blue-violet gemstone embedded on front, strong magical glow leaking from edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [63/82] raw/item/item_chest_boss.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A boss-tier gold chest game icon, ornate gold-trimmed dark wood with engraved patterns, large red ruby gem on front, intense magical aura radiating, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_boss.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [64/82] raw/item/item_chest_death.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A death chest game icon, worn gray adventurer backpack on ground, paint vial peeking from top, parchment scroll edge sticking out, semi-transparent ghostly soul flame hovering above, somber mood, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_death.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [65/82] raw/item/item_recipe_book.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe book game icon, ancient leather-bound tome with gilded ornamental patterns on cover, flamboyant stickers and bookmarks, magical glow seeping from page edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_recipe_book.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened pages, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [66/82] raw/character/character_player_8dir_idle.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An 8-directional idle sprite sheet for a top-down (slight 30-degree tilt) view game character. The canvas is divided into a clean 2-row by 4-column grid (each cell 256x256 with transparent 32px padding between cells). Row 1 left to right: facing North, facing North-East, facing East, facing South-East. Row 2 left to right: facing South, facing South-West, facing West, facing North-West. Character is an androgynous psionic warrior with athletic medium build, wearing a dark street jacket with rolled sleeves, visible tattoos on forearms and neck, dark cargo pants, combat boots. CRITICAL: body proportions, outfit, colors, lighting and shadow must be identical across all 8 directions. Idle pose: standing relaxed with weight slightly on one leg. Transparent background. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/character/character_player_8dir_idle.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, action poses, weapons drawn, walking, running, inconsistent body proportion, costume change between cells, text labels, grid lines, frame borders

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [67/82] raw/env/env_floor_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal floor texture, dark steel plates with rivets and visible rust patches, oil stains, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style, dramatic top lighting.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [68/82] raw/env/env_floor_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins floor texture, cracked concrete with exposed rebar, scattered debris, small weeds in cracks, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [69/82] raw/env/env_floor_blood_rock.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood rock floor texture, dark red rocky surface with pulsing blood vein cracks, mutated bio-organic accents, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_blood_rock.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [70/82] raw/env/env_wall_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal wall texture, dark steel panel with welded seams, faded graffiti, rust streaks, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [71/82] raw/env/env_wall_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins wall texture, broken brick with bullet holes, moss patches, exposed insulation, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [72/82] raw/env/env_wall_blood.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood-rock wall texture, dark red rock face with blood vein patterns, bio-organic growths bulging outward, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_blood.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [73/82] raw/env/env_light_pillar_a.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An industrial fluorescent light pillar game prop, tall vertical fixture with blue-white glowing tubes inside, top metallic shade, base bolted to ground, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_a.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [74/82] raw/env/env_light_pillar_b.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A broken neon light pillar game prop, weathered pink-red neon glass tubes partially cracked, faint flicker glow, dystopian feel, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_b.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [75/82] raw/recipe/recipe_scroll_line.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll with both ends rolled up, central area unrolled showing a single bold straight line tattoo pattern in dark gold ink, gilded wax seal at bottom, faint magical glow around scroll edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_line.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [76/82] raw/recipe/recipe_scroll_ring.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a double concentric ring tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_ring.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [77/82] raw/recipe/recipe_scroll_spiral.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing an Archimedean 5-loop spiral tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_spiral.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [78/82] raw/recipe/recipe_scroll_zigzag.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a 4-segment zigzag tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_zigzag.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [79/82] raw/recipe/recipe_scroll_bolt.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a forked lightning bolt tattoo pattern in dark gold ink with branching forks, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_bolt.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [80/82] raw/recipe/recipe_scroll_star.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a five-pointed star tattoo pattern with surrounding dashed probability arcs in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_star.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [81/82] raw/recipe/recipe_scroll_stream.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a stream tattoo pattern with 3 parallel flowing lines in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_stream.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [82/82] raw/recipe/recipe_scroll_beast.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a beast claw silhouette tattoo pattern with summoning runes in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_beast.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [41/82] raw/paint/paint_white_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare white paint bottle game icon, ornate glass vial, pure white liquid (#F8F9FA) with inner crystalline shards, floating light particles, faint pale-blue glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [42/82] raw/paint/paint_white_legendary.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A legendary white paint bottle game icon, vial radiating intense pure energy aura, white liquid (#F8F9FA) pulsating with brilliant light, prismatic crystals orbiting, entire bottle radiates light, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/paint/paint_white_legendary.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [43/82] raw/consumable/consumable_antidote.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An antidote vial game icon, small glass medicine bottle with cork, filled with bright green serum, antidote cross symbol on label, faint emerald glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_antidote.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [44/82] raw/consumable/consumable_repair_kit.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A repair kit game icon, opened toolbox with wrench, screwdriver and blue patches visible, sturdy leather wrap, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_repair_kit.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [45/82] raw/consumable/consumable_eraser.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A tattoo eraser tool game icon, metal scraper alongside a vial of solvent, faint purple corrosive mist, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_eraser.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [46/82] raw/consumable/consumable_universal_paint.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A universal paint bottle game icon, ornate glass vial filled with swirling rainbow liquid showing all 7 colors blending, prismatic glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_universal_paint.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [47/82] raw/consumable/consumable_gold_pile.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A gold coin pile game icon, 3 to 5 shiny gold coins stacked with one tilted on top, warm golden glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/consumable/consumable_gold_pile.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, characters on coins

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [48/82] raw/npc/npc_tattoo_artist.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a middle-aged tattoo artist with mysterious aura, shoulders-up composition, weathered face with focused gaze, visible tattoos covering neck, collarbones and back of hands, dark vest over rolled-up sleeves, subtle purple magical glow emanating from tattoos, dramatic side lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_tattoo_artist.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [49/82] raw/npc/npc_merchant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a wandering merchant peddler with cunning smile, shoulders-up composition, curly hair, weathered traveling coat, satchel straps visible across chest, holding a gold coin between fingers, warm amber lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/npc/npc_merchant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [50/82] raw/boss/boss_ai_guardian.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an AI Guardian boss, shoulders-up composition, mechanical humanoid head and torso, single glowing red cyclopean eye, polished metallic armor plates with embossed circuit patterns, exposed servo joints at neck, intimidating apocalyptic tech aesthetic, harsh rim light, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_ai_guardian.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [51/82] raw/boss/boss_alien_consciousness.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of an Alien Consciousness boss, shoulders-up composition, otherworldly head with multiple glowing eyes and writhing tentacles around the face, iridescent purple and green skin patterns, deep alien aura, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_alien_consciousness.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [52/82] raw/boss/boss_virus_mutant.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular portrait of a Virus Mutant boss, shoulders-up composition, twisted humanoid form with grotesque tumor growths bursting from skin, exposed mutated muscle, sickly green-yellow pus oozing along virus vein patterns, horrific yet detailed, dramatic lighting, transparent background, suitable for circular UI mask.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/boss/boss_virus_mutant.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, full body, multiple characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [53/82] raw/hud/hud_hp_bar_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded HP bar frame for game HUD, Hades-style metallic engraved border with subtle filigree details, inner area completely transparent for filled bar overlay, 9-slice friendly with stretchable middle section, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_hp_bar_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, fill content inside, text, characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [54/82] raw/hud/hud_buff_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A small square buff slot frame for game HUD, rounded corners, subtle inner shadow giving recessed look, polished metal trim, central area completely transparent for icon overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_buff_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [55/82] raw/hud/hud_skill_slot.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular skill slot frame for game HUD, ornate metal outer ring with subtle Hades-style engraving, recessed central well completely transparent for icon overlay, small notch at top for key label area, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_skill_slot.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, icon inside, Q letter, E letter, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [56/82] raw/hud/hud_ammo_box.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular ammunition number plate for game HUD, semi-transparent dark background panel with 1px gold trim border, subtle embossed bullet symbol on left side, central area transparent for number overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_ammo_box.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [57/82] raw/hud/hud_minimap_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A circular minimap frame for game HUD, ornate outer ring with 8 directional tick marks at N S E W and diagonals, polished metallic finish with Hades-style filigree, central area completely transparent for map content overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_minimap_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, map content inside, text labels

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [58/82] raw/hud/hud_shrink_timer.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rectangular rounded shrink countdown plate for game HUD, semi-transparent dark background with subtle embossed hourglass icon on left, gold trim, central area transparent for countdown overlay, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_shrink_timer.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, numbers, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [59/82] raw/hud/hud_weapon_frame.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A square weapon icon frame for game HUD, rounded corners with metallic trim, recessed central well completely transparent for weapon icon overlay, polished Hades-style detail, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_weapon_frame.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, weapon inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [60/82] raw/hud/hud_build_row_bg.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A horizontal rounded list row background for game HUD build list, semi-transparent dark fill with 1px dark outline, 9-slice friendly with stretchable middle, edges keep 16% padding, centered on canvas, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/hud/hud_build_row_bg.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, content inside, text, icons

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [61/82] raw/item/item_chest_common.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A common wooden chest game icon, sturdy oak planks with iron bands and lock, slight crack glowing faintly from lid, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_common.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, content inside, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [62/82] raw/item/item_chest_rare.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A rare silver-trimmed chest game icon, polished wood with silver bands, blue-violet gemstone embedded on front, strong magical glow leaking from edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_rare.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [63/82] raw/item/item_chest_boss.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A boss-tier gold chest game icon, ornate gold-trimmed dark wood with engraved patterns, large red ruby gem on front, intense magical aura radiating, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_boss.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened wide, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [64/82] raw/item/item_chest_death.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A death chest game icon, worn gray adventurer backpack on ground, paint vial peeking from top, parchment scroll edge sticking out, semi-transparent ghostly soul flame hovering above, somber mood, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_chest_death.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [65/82] raw/item/item_recipe_book.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe book game icon, ancient leather-bound tome with gilded ornamental patterns on cover, flamboyant stickers and bookmarks, magical glow seeping from page edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/item/item_recipe_book.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, opened pages, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [66/82] raw/character/character_player_8dir_idle.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An 8-directional idle sprite sheet for a top-down (slight 30-degree tilt) view game character. The canvas is divided into a clean 2-row by 4-column grid (each cell 256x256 with transparent 32px padding between cells). Row 1 left to right: facing North, facing North-East, facing East, facing South-East. Row 2 left to right: facing South, facing South-West, facing West, facing North-West. Character is an androgynous psionic warrior with athletic medium build, wearing a dark street jacket with rolled sleeves, visible tattoos on forearms and neck, dark cargo pants, combat boots. CRITICAL: body proportions, outfit, colors, lighting and shadow must be identical across all 8 directions. Idle pose: standing relaxed with weight slightly on one leg. Transparent background. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/character/character_player_8dir_idle.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, action poses, weapons drawn, walking, running, inconsistent body proportion, costume change between cells, text labels, grid lines, frame borders

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [67/82] raw/env/env_floor_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal floor texture, dark steel plates with rivets and visible rust patches, oil stains, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style, dramatic top lighting.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [68/82] raw/env/env_floor_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins floor texture, cracked concrete with exposed rebar, scattered debris, small weeds in cracks, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [69/82] raw/env/env_floor_blood_rock.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood rock floor texture, dark red rocky surface with pulsing blood vein cracks, mutated bio-organic accents, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_floor_blood_rock.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, characters, props, walls, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [70/82] raw/env/env_wall_metal.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable industrial metal wall texture, dark steel panel with welded seams, faded graffiti, rust streaks, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_metal.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges, single focal point

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [71/82] raw/env/env_wall_ruins.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable post-apocalyptic ruins wall texture, broken brick with bullet holes, moss patches, exposed insulation, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_ruins.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [72/82] raw/env/env_wall_blood.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A seamless tileable blood-rock wall texture, dark red rock face with blood vein patterns, bio-organic growths bulging outward, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_wall_blood.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, floor, characters, visible seam at edges

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [73/82] raw/env/env_light_pillar_a.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. An industrial fluorescent light pillar game prop, tall vertical fixture with blue-white glowing tubes inside, top metallic shade, base bolted to ground, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_a.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [74/82] raw/env/env_light_pillar_b.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A broken neon light pillar game prop, weathered pink-red neon glass tubes partially cracked, faint flicker glow, dystopian feel, painterly Hades style, transparent background. Single subject centered.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/env/env_light_pillar_b.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, tileable texture, floor, walls, characters, text

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [75/82] raw/recipe/recipe_scroll_line.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll with both ends rolled up, central area unrolled showing a single bold straight line tattoo pattern in dark gold ink, gilded wax seal at bottom, faint magical glow around scroll edges, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_line.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [76/82] raw/recipe/recipe_scroll_ring.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a double concentric ring tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_ring.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [77/82] raw/recipe/recipe_scroll_spiral.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing an Archimedean 5-loop spiral tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_spiral.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [78/82] raw/recipe/recipe_scroll_zigzag.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a 4-segment zigzag tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_zigzag.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [79/82] raw/recipe/recipe_scroll_bolt.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a forked lightning bolt tattoo pattern in dark gold ink with branching forks, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_bolt.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [80/82] raw/recipe/recipe_scroll_star.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a five-pointed star tattoo pattern with surrounding dashed probability arcs in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_star.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [81/82] raw/recipe/recipe_scroll_stream.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a stream tattoo pattern with 3 parallel flowing lines in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_stream.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

## [82/82] raw/recipe/recipe_scroll_beast.png

```
请使用 imagegen 系统 skill 生成一张图片。

# 画面描述（英文提示词）
A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable. A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a beast claw silhouette tattoo pattern with summoning runes in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.

# 输出要求
- 尺寸：1024x1024
- 透明背景：是
- 保存到：openspec/changes/06-v21-implementation/art/raw/recipe/recipe_scroll_beast.png
- 负面：no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration, text, written characters

# 完成标准
生成成功后输出文件相对路径与字节大小。失败请说明具体错误，不要重试无限次。
```

---

