美术素材状态: 待处理
处理日期: 2026-06-25（创建）
执行 SKILL: codex-image-gen
输出目录: art/raw/
生成记录: art/raw/生成记录.md

# Prompts — 06-v21-implementation v2.1 全套美术包

> 与 [requirements.md](./requirements.md) 配套，按子目录组织 91 条 prompt。
> 所有 prompt 走英文（imagegen 对英文响应更稳）。

## 全局基底前缀（所有图通用）

```
STYLE_BASE = "A 1024x1024 Hades-style 2.5D game asset, painterly thick brush strokes, vibrant saturated colors, strong silhouette with deep shadow outline, dramatic high-contrast lighting from upper-left at 45 degrees, rim light highlighting the subject, magical aura where applicable."
NEG_BASE = "no text, no letters, no numbers, no watermark, no signature, no logo, blurry, low contrast, flat vector, washed out, multiple subjects, frame border decoration"
```

每条 prompt 以 `STYLE_BASE` 起手，结尾追加 `NEG_BASE`（在 codex exec 的 JSON 里走 `negative` 字段）。

---

## 1. weapon/（5 张）

```json
[
  {
    "file": "raw/weapon/weapon_short_blade.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A short blade dagger as a game icon, single-edged steel blade with cold metallic gleam, black wrapped hilt with brass guard, blood drop on tip, centered square framing, subject occupies 75% of canvas, transparent background.",
    "negative": "NEG_BASE, sheath, hand holding, character"
  },
  {
    "file": "raw/weapon/weapon_heavy_hammer.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A two-handed heavy hammer as a game icon, rusty iron hammer head with cracks glowing dim red, thick wooden shaft wrapped in leather, centered square framing, transparent background.",
    "negative": "NEG_BASE, character holding"
  },
  {
    "file": "raw/weapon/weapon_pistol.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A semi-automatic pistol as a game icon, black steel finish with brass accents, faint muzzle smoke trailing from barrel, side profile view, centered square framing, transparent background.",
    "negative": "NEG_BASE, hand, ammunition magazine separated"
  },
  {
    "file": "raw/weapon/weapon_bow.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A compound bow drawn taut with a glowing arrow nocked, dark composite limbs, golden bowstring shimmer, arrow tip emits soft white energy glow, centered square framing, transparent background.",
    "negative": "NEG_BASE, quiver, archer"
  },
  {
    "file": "raw/weapon/weapon_energy_fist.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE An energy fist gauntlet as a game icon, armored knuckle plate wrapped in swirling blue-purple energy field, arcing electric streams around the knuckles, glowing core, centered square framing, transparent background.",
    "negative": "NEG_BASE, arm, character"
  }
]
```

---

## 2. skill/（8 张）

```json
[
  {
    "file": "raw/skill/skill_fireball.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A blazing fireball spell icon, swirling red-orange fire core with bright yellow center, ember sparks trailing, deep shadow outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_ice_field.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A hexagonal ice field spell icon, crystalline ice shards radiating from a central hexagon rune, frost-blue gradient, cold mist at base, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_chain_lightning.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A chain lightning spell icon, zigzag yellow electric bolt with branching forks, glowing white-yellow core, dark indigo aura behind, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_heal_aura.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A healing aura spell icon, glowing green ring with ascending white cross light from a stylized lotus base, gentle particle drift upward, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_shield.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE An octagonal energy shield icon, blue-cyan translucent shield plate with rim light glow and internal energy field swirls, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_stealth.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A brief invisibility spell icon, semi-transparent humanoid silhouette dissolving into purple-black smoke wisps with afterimage trails, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_summon.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A summon beast spell icon, an otherworldly summoning circle with arcane runes, a beast claw silhouette rising from purple energy, magical glow, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/skill/skill_time_slow.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A time slow spell icon, golden hourglass crossed with clock gears, soft golden trails of slow motion light, centered, transparent background.",
    "negative": "NEG_BASE"
  }
]
```

---

## 3. affix/（8 张, 候选 L2 合批）

```json
[
  {
    "file": "raw/affix/affix_fire_damage.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A fire damage plus affix icon, stylized red flame symbol with an upward red arrow overlay, deep red outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_cooldown.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A cooldown reduction affix icon, blue hourglass with a downward blue arrow overlay, cyan outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_attack_speed.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE An attack speed plus affix icon, two crossed yellow daggers with an upward yellow arrow overlay, golden outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_crit_chance.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A critical hit chance plus affix icon, purple diamond shape with bullseye target and an upward purple arrow overlay, violet outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_crit_damage.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A critical damage plus affix icon, golden explosion cross symbol with an upward golden arrow overlay, gold outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_range.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A range plus affix icon, a long-tailed arrow with an upward white arrow overlay, white-silver outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_accuracy.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE An accuracy plus affix icon, a precision crosshair reticle with an upward white arrow overlay, white outline, centered, transparent background.",
    "negative": "NEG_BASE"
  },
  {
    "file": "raw/affix/affix_lifesteal.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE A lifesteal on hit affix icon, a dripping blood cross with a small green heart at center, green outline, centered, transparent background.",
    "negative": "NEG_BASE"
  }
]
```

---

## 4. paint/（21 张 = 7 色 × 3 档）

通用结构：玻璃瓶居中略偏下，瓶占画幅 60-70%，软木塞 + 空白标签。三档差异严格遵守。

```json
[
  {"file":"raw/paint/paint_red_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common red paint bottle game icon, simple glass vial with cork stopper, filled with vibrant red liquid (#E63946), blank label on bottle, centered, transparent background.","negative":"NEG_BASE, glow, aura, particles, text on label"},
  {"file":"raw/paint/paint_red_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare red paint bottle game icon, ornate glass vial with cork stopper, filled with vibrant red liquid (#E63946) with inner orange-yellow swirl halo, floating ember particles inside, faint glow around bottle, centered, transparent background.","negative":"NEG_BASE, full body aura, text"},
  {"file":"raw/paint/paint_red_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary red paint bottle game icon, exquisite glass vial radiating intense magical aura, vibrant red liquid (#E63946) glowing from within, bright orange particles spiraling around, flame wisps overflowing the cork, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_yellow_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common yellow paint bottle game icon, simple glass vial with cork stopper, filled with vibrant yellow liquid (#F4C430) with faint lightning bolt pattern floating inside, blank label, centered, transparent background.","negative":"NEG_BASE, glow, text"},
  {"file":"raw/paint/paint_yellow_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare yellow paint bottle game icon, ornate glass vial, vibrant yellow liquid (#F4C430) with inner electric bolt swirls, floating spark particles inside, faint yellow glow, centered, transparent background.","negative":"NEG_BASE, full aura, text"},
  {"file":"raw/paint/paint_yellow_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary yellow paint bottle game icon, glass vial radiating intense magical aura, vibrant yellow liquid (#F4C430) glowing from within, lightning arcs escaping the cork, bright sparks spiraling, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_green_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common green paint bottle game icon, simple glass vial with cork, filled with vibrant green liquid (#3DDC84) with toxic bubbles on surface, blank label, centered, transparent background.","negative":"NEG_BASE, glow, text"},
  {"file":"raw/paint/paint_green_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare green paint bottle game icon, ornate glass vial, vibrant green liquid (#3DDC84) with inner emerald swirl, floating spores inside, faint green glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/paint/paint_green_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary green paint bottle game icon, vial radiating intense magical aura, vibrant green liquid (#3DDC84) glowing from within, emerald mist overflowing the cork, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_blue_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common blue paint bottle game icon, simple glass vial with cork, filled with vibrant blue liquid (#1FB6FF) with snowflake pattern floating inside, blank label, centered, transparent background.","negative":"NEG_BASE, glow, text"},
  {"file":"raw/paint/paint_blue_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare blue paint bottle game icon, ornate glass vial, vibrant blue liquid (#1FB6FF) with inner ice swirls, floating frost crystals inside, faint cyan glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/paint/paint_blue_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary blue paint bottle game icon, vial radiating intense magical aura, vibrant blue liquid (#1FB6FF) glowing from within, ice crystals radiating outward, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_purple_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common purple paint bottle game icon, simple glass vial with cork, filled with vibrant purple liquid (#9D4EDD) with mutation swirl pattern inside, blank label, centered, transparent background.","negative":"NEG_BASE, glow, text"},
  {"file":"raw/paint/paint_purple_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare purple paint bottle game icon, ornate glass vial, vibrant purple liquid (#9D4EDD) with inner violet swirls, floating dark particles, faint purple glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/paint/paint_purple_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary purple paint bottle game icon, vial radiating intense magical aura, vibrant purple liquid (#9D4EDD) glowing, distorted purple mist overflowing, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_gold_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common gold paint bottle game icon, simple glass vial with cork, filled with shimmering gold liquid (#FFB400) with small cross light and gold dust inside, blank label, centered, transparent background.","negative":"NEG_BASE, full glow, text"},
  {"file":"raw/paint/paint_gold_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare gold paint bottle game icon, ornate glass vial, shimmering gold liquid (#FFB400) with inner radiant cross, floating gold flecks, faint warm glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/paint/paint_gold_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary gold paint bottle game icon, vial radiating intense divine aura, shimmering gold liquid (#FFB400) glowing brilliantly, golden light rays emanating, holy ambience, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"},

  {"file":"raw/paint/paint_white_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common white paint bottle game icon, simple glass vial with cork, filled with pure white liquid (#F8F9FA) with polyhedral light crystal floating inside, blank label, centered, transparent background.","negative":"NEG_BASE, glow, text"},
  {"file":"raw/paint/paint_white_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare white paint bottle game icon, ornate glass vial, pure white liquid (#F8F9FA) with inner crystalline shards, floating light particles, faint pale-blue glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/paint/paint_white_legendary.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A legendary white paint bottle game icon, vial radiating intense pure energy aura, white liquid (#F8F9FA) pulsating with brilliant light, prismatic crystals orbiting, entire bottle radiates light, centered, transparent background.","negative":"NEG_BASE, text"}
]
```

---

## 5. consumable/（5 张, 候选 L2 合批）

```json
[
  {"file":"raw/consumable/consumable_antidote.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE An antidote vial game icon, small glass medicine bottle with cork, filled with bright green serum, antidote cross symbol on label, faint emerald glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/consumable/consumable_repair_kit.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A repair kit game icon, opened toolbox with wrench, screwdriver and blue patches visible, sturdy leather wrap, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/consumable/consumable_eraser.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A tattoo eraser tool game icon, metal scraper alongside a vial of solvent, faint purple corrosive mist, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/consumable/consumable_universal_paint.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A universal paint bottle game icon, ornate glass vial filled with swirling rainbow liquid showing all 7 colors blending, prismatic glow, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/consumable/consumable_gold_pile.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A gold coin pile game icon, 3 to 5 shiny gold coins stacked with one tilted on top, warm golden glow, centered, transparent background.","negative":"NEG_BASE, text, characters on coins"}
]
```

---

## 6. npc/（2 张, 圆形构图）

```json
[
  {"file":"raw/npc/npc_tattoo_artist.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular portrait of a middle-aged tattoo artist with mysterious aura, shoulders-up composition, weathered face with focused gaze, visible tattoos covering neck, collarbones and back of hands, dark vest over rolled-up sleeves, subtle purple magical glow emanating from tattoos, dramatic side lighting, transparent background, suitable for circular UI mask.","negative":"NEG_BASE, full body, multiple characters, text"},
  {"file":"raw/npc/npc_merchant.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular portrait of a wandering merchant peddler with cunning smile, shoulders-up composition, curly hair, weathered traveling coat, satchel straps visible across chest, holding a gold coin between fingers, warm amber lighting, transparent background, suitable for circular UI mask.","negative":"NEG_BASE, full body, multiple characters, text"}
]
```

---

## 7. boss/（3 张, 圆形构图）

```json
[
  {"file":"raw/boss/boss_ai_guardian.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular portrait of an AI Guardian boss, shoulders-up composition, mechanical humanoid head and torso, single glowing red cyclopean eye, polished metallic armor plates with embossed circuit patterns, exposed servo joints at neck, intimidating apocalyptic tech aesthetic, harsh rim light, transparent background, suitable for circular UI mask.","negative":"NEG_BASE, full body, text"},
  {"file":"raw/boss/boss_alien_consciousness.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular portrait of an Alien Consciousness boss, shoulders-up composition, otherworldly head with multiple glowing eyes and writhing tentacles around the face, iridescent purple and green skin patterns, deep alien aura, dramatic lighting, transparent background, suitable for circular UI mask.","negative":"NEG_BASE, full body, text"},
  {"file":"raw/boss/boss_virus_mutant.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular portrait of a Virus Mutant boss, shoulders-up composition, twisted humanoid form with grotesque tumor growths bursting from skin, exposed mutated muscle, sickly green-yellow pus oozing along virus vein patterns, horrific yet detailed, dramatic lighting, transparent background, suitable for circular UI mask.","negative":"NEG_BASE, full body, multiple characters, text"}
]
```

---

## 8. hud/（8 张, 部分需要 9-slice 安全区）

```json
[
  {"file":"raw/hud/hud_hp_bar_frame.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A horizontal rounded HP bar frame for game HUD, Hades-style metallic engraved border with subtle filigree details, inner area completely transparent for filled bar overlay, 9-slice friendly with stretchable middle section, edges keep 16% padding, centered on canvas, transparent background.","negative":"NEG_BASE, fill content inside, text, characters"},
  {"file":"raw/hud/hud_buff_slot.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A small square buff slot frame for game HUD, rounded corners, subtle inner shadow giving recessed look, polished metal trim, central area completely transparent for icon overlay, centered, transparent background.","negative":"NEG_BASE, icon inside, text"},
  {"file":"raw/hud/hud_skill_slot.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular skill slot frame for game HUD, ornate metal outer ring with subtle Hades-style engraving, recessed central well completely transparent for icon overlay, small notch at top for key label area, centered, transparent background.","negative":"NEG_BASE, icon inside, Q letter, E letter, text"},
  {"file":"raw/hud/hud_ammo_box.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rectangular ammunition number plate for game HUD, semi-transparent dark background panel with 1px gold trim border, subtle embossed bullet symbol on left side, central area transparent for number overlay, centered, transparent background.","negative":"NEG_BASE, numbers, text"},
  {"file":"raw/hud/hud_minimap_frame.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A circular minimap frame for game HUD, ornate outer ring with 8 directional tick marks at N S E W and diagonals, polished metallic finish with Hades-style filigree, central area completely transparent for map content overlay, centered, transparent background.","negative":"NEG_BASE, map content inside, text labels"},
  {"file":"raw/hud/hud_shrink_timer.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rectangular rounded shrink countdown plate for game HUD, semi-transparent dark background with subtle embossed hourglass icon on left, gold trim, central area transparent for countdown overlay, centered, transparent background.","negative":"NEG_BASE, numbers, text"},
  {"file":"raw/hud/hud_weapon_frame.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A square weapon icon frame for game HUD, rounded corners with metallic trim, recessed central well completely transparent for weapon icon overlay, polished Hades-style detail, centered, transparent background.","negative":"NEG_BASE, weapon inside, text"},
  {"file":"raw/hud/hud_build_row_bg.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A horizontal rounded list row background for game HUD build list, semi-transparent dark fill with 1px dark outline, 9-slice friendly with stretchable middle, edges keep 16% padding, centered on canvas, transparent background.","negative":"NEG_BASE, content inside, text, icons"}
]
```

---

## 9. item/（5 张）

```json
[
  {"file":"raw/item/item_chest_common.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A common wooden chest game icon, sturdy oak planks with iron bands and lock, slight crack glowing faintly from lid, centered, transparent background.","negative":"NEG_BASE, opened wide, content inside, text"},
  {"file":"raw/item/item_chest_rare.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A rare silver-trimmed chest game icon, polished wood with silver bands, blue-violet gemstone embedded on front, strong magical glow leaking from edges, centered, transparent background.","negative":"NEG_BASE, opened wide, text"},
  {"file":"raw/item/item_chest_boss.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A boss-tier gold chest game icon, ornate gold-trimmed dark wood with engraved patterns, large red ruby gem on front, intense magical aura radiating, centered, transparent background.","negative":"NEG_BASE, opened wide, text"},
  {"file":"raw/item/item_chest_death.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A death chest game icon, worn gray adventurer backpack on ground, paint vial peeking from top, parchment scroll edge sticking out, semi-transparent ghostly soul flame hovering above, somber mood, centered, transparent background.","negative":"NEG_BASE, text"},
  {"file":"raw/item/item_recipe_book.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe book game icon, ancient leather-bound tome with gilded ornamental patterns on cover, flamboyant stickers and bookmarks, magical glow seeping from page edges, centered, transparent background.","negative":"NEG_BASE, opened pages, text"}
]
```

---

## 10. character/（1 张, 8 方向 grid）

```json
[
  {
    "file": "raw/character/character_player_8dir_idle.png",
    "size": "1024x1024",
    "transparent": true,
    "prompt": "STYLE_BASE An 8-directional idle sprite sheet for a top-down (slight 30-degree tilt) view game character. The canvas is divided into a clean 2-row by 4-column grid (each cell 256x256 with transparent 32px padding between cells). Row 1 left to right: facing North, facing North-East, facing East, facing South-East. Row 2 left to right: facing South, facing South-West, facing West, facing North-West. Character is an androgynous psionic warrior with athletic medium build, wearing a dark street jacket with rolled sleeves, visible tattoos on forearms and neck, dark cargo pants, combat boots. CRITICAL: body proportions, outfit, colors, lighting and shadow must be identical across all 8 directions. Idle pose: standing relaxed with weight slightly on one leg. Transparent background. Painterly Hades style.",
    "negative": "NEG_BASE, action poses, weapons drawn, walking, running, inconsistent body proportion, costume change between cells, text labels, grid lines, frame borders"
  }
]
```

---

## 11. env/（8 张, 地面墙体需平铺）

```json
[
  {"file":"raw/env/env_floor_metal.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable industrial metal floor texture, dark steel plates with rivets and visible rust patches, oil stains, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style, dramatic top lighting.","negative":"NEG_BASE, characters, props, walls, visible seam at edges, single focal point"},
  {"file":"raw/env/env_floor_ruins.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable post-apocalyptic ruins floor texture, cracked concrete with exposed rebar, scattered debris, small weeds in cracks, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.","negative":"NEG_BASE, characters, props, walls, visible seam at edges"},
  {"file":"raw/env/env_floor_blood_rock.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable blood rock floor texture, dark red rocky surface with pulsing blood vein cracks, mutated bio-organic accents, top-down view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.","negative":"NEG_BASE, characters, props, walls, visible seam at edges"},
  {"file":"raw/env/env_wall_metal.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable industrial metal wall texture, dark steel panel with welded seams, faded graffiti, rust streaks, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.","negative":"NEG_BASE, floor, characters, visible seam at edges, single focal point"},
  {"file":"raw/env/env_wall_ruins.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable post-apocalyptic ruins wall texture, broken brick with bullet holes, moss patches, exposed insulation, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.","negative":"NEG_BASE, floor, characters, visible seam at edges"},
  {"file":"raw/env/env_wall_blood.png","size":"1024x1024","transparent":false,"prompt":"STYLE_BASE A seamless tileable blood-rock wall texture, dark red rock face with blood vein patterns, bio-organic growths bulging outward, frontal view, edge continuity ensured for tiling, no visible seam at edges. Painterly Hades style.","negative":"NEG_BASE, floor, characters, visible seam at edges"},
  {"file":"raw/env/env_light_pillar_a.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE An industrial fluorescent light pillar game prop, tall vertical fixture with blue-white glowing tubes inside, top metallic shade, base bolted to ground, painterly Hades style, transparent background. Single subject centered.","negative":"NEG_BASE, tileable texture, floor, walls, characters, text"},
  {"file":"raw/env/env_light_pillar_b.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A broken neon light pillar game prop, weathered pink-red neon glass tubes partially cracked, faint flicker glow, dystopian feel, painterly Hades style, transparent background. Single subject centered.","negative":"NEG_BASE, tileable texture, floor, walls, characters, text"}
]
```

---

## 12. recipe/（8 张）

```json
[
  {"file":"raw/recipe/recipe_scroll_line.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll with both ends rolled up, central area unrolled showing a single bold straight line tattoo pattern in dark gold ink, gilded wax seal at bottom, faint magical glow around scroll edges, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_ring.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a double concentric ring tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_spiral.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing an Archimedean 5-loop spiral tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_zigzag.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a 4-segment zigzag tattoo pattern in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_bolt.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a forked lightning bolt tattoo pattern in dark gold ink with branching forks, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_star.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a five-pointed star tattoo pattern with surrounding dashed probability arcs in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_stream.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a stream tattoo pattern with 3 parallel flowing lines in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"},
  {"file":"raw/recipe/recipe_scroll_beast.png","size":"1024x1024","transparent":true,"prompt":"STYLE_BASE A recipe scroll game icon, ancient parchment scroll, central area unrolled showing a beast claw silhouette tattoo pattern with summoning runes in dark gold ink, gilded wax seal, faint magical glow, centered, transparent background.","negative":"NEG_BASE, text, written characters"}
]
```

---

## 13. 批次执行计划（给 codex-image-gen 的建议）

按子目录分批，每批 ≤ 12 张：

| 批次 | 内容 | 数量 | 档位 |
|---|---|---|---|
| Batch 1 | weapon/ 5 + skill/ 8 前 7 | 12 | L1 |
| Batch 2 | skill/ 8 最后 1 + affix/ 8 + consumable/ 5（13 张超限，拆）| — | — |
| Batch 2a | skill/ S08 + affix/ A01-A08 + consumable/ C01-C03 | 12 | L1（或 affix+consumable 走 L2 合批 1 张大画布） |
| Batch 2b | consumable/ C04-C05 + paint/ 红 3 档 + 黄 3 档 + 绿 2 档 | 10 | L1 |
| Batch 3 | paint/ 绿 1 + 蓝 3 + 紫 3 + 金 3 + 白 2 | 12 | L1 |
| Batch 4 | paint/ 白 1 + npc/ 2 + boss/ 3 + hud/ 6 | 12 | L1 |
| Batch 5 | hud/ 2 + item/ 5 + recipe/ 5 | 12 | L1 |
| Batch 6 | recipe/ 3 + env/ 8 + character/ 1 | 12 | L1 |

注：codex-image-gen 会自动归类与分批，本表为指导参考；实际执行以 SKILL 自动决策为准。
