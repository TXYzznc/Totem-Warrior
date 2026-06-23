---
name: character-sprite
description: 为Codex Office Visualizer的Agent生成完整的角色精灵图。创建所有动画帧（idle、行走、打字、递交文件、喝咖啡），确保所有精灵图的角色设计保持一致。采用迭代审批流程和基于参考图的生成方式来保证一致性。
triggers:
- create agent sprite
- create character sprite
- new office character
- generate agent animations
- make character for office
tags: sprite-sheet-generation, pixel-art-animation, game-asset-creation, imagemagick-workflow,
  Codex-office-visualizer
tags_cn: 精灵图生成, 像素画动画, 游戏资源制作, ImageMagick工作流, Codex Office Visualizer Agent
---

# 角色精灵图生成器

使用Nano Banana MCP和ImageMagick为Codex Office Visualizer Agent创建完整的动画角色精灵图。

## 项目背景

角色为办公室职员，采用**复古16位像素艺术风格**渲染。每个角色需要对应不同动作和方向的多组动画精灵图。

**艺术风格**：复古16位像素艺术（SNES/Genesis时代），像素清晰，配色有限。

【重要提示】禁止使用抗锯齿：所有精灵图必须拥有锐利清晰的像素边缘，**禁止使用抗锯齿、平滑处理、像素间混合**。每个像素应为纯色且边缘硬朗。带抗锯齿的精灵图在游戏中会显得模糊浑浊。

**角色约束**:
- 最大宽度：60px（与当前Agent半径×2匹配）
- 最大高度：75px（允许比宽度高25%）
- 洋红色（#FF00FF）抠像背景

## 精灵图技术规格

### 【重要提示】网格布局要求

游戏引擎通过将整张图片划分为固定网格来解析精灵图。**所有单元格必须大小统一，单元格之间无任何内边距/间距**。

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  单元格之间无间隙、边框或内边距。单元格计算方式为：                          │
│  cell_width = sheet_width / columns                                          │
│  cell_height = sheet_height / rows                                           │
│  位于(col, row)的帧起始像素为(col × cell_width, row × cell_height)           │
└──────────────────────────────────────────────────────────────────────────────┘
```

**禁止操作**:
- 添加可见的网格线或单元格边框
- 在单元格之间添加内边距或间距
- 使用不一致的单元格尺寸
- 保留空单元格（需填充洋红色背景）

### 精灵图尺寸与帧规格

| 精灵图类型 | 总尺寸 | 网格布局 | 单元格尺寸 | 帧数 |
|------------|------------|------|-----------|--------|
| **Idle** | 928 × 1152 px | 8列 × 8行 | 116 × 144 px | 共64帧 |
| **行走** | 928 × 1152 px | 8列 × 8行 | 116 × 144 px | 共64帧 |
| **打字** | 928 × 144 px | 8列 × 1行 | 116 × 144 px | 8帧 |
| **递交文件** | 928 × 1152 px | 4列 × 1行 | 232 × 411 px* | 4帧 |
| **喝咖啡** | 928 × 1152 px | 4列 × 1行 | 232 × 699 px* | 4帧 |

*递交文件/喝咖啡精灵图存在内容偏移 - 详见下方帧位置映射表。

### 帧位置映射表

**网格型精灵图（Idle/行走）- 8列 × 8行:**
```
第0行（y=0-143）:    朝南（正面朝向）     - 帧0-7位于x坐标：0, 116, 232, 348, 464, 580, 696, 812
第1行（y=144-287）:  西南方向               - 帧0-7
第2行（y=288-431）:  朝西（左侧轮廓）      - 帧0-7
第3行（y=432-575）:  西北方向               - 帧0-7
第4行（y=576-719）:  朝北（背面视角）        - 帧0-7
第5行（y=720-863）:  东北方向               - 帧0-7
第6行（y=864-1007）: 朝东（右侧轮廓）     - 帧0-7
第7行（y=1008-1151）: 东南方向              - 帧0-7
```

**条带型精灵图（单行）:**
```
打字:  第0行，8帧位于x坐标：0, 116, 232, 348, 464, 580, 696, 812（y=0-143）
递交文件: 第0行，4帧位于x坐标：0, 232, 464, 696（内容y=343-753）
喝咖啡:  第0行，4帧位于x坐标：0, 232, 464, 696（内容y=0-698）
```

### 动画要求

| 动画 | 帧数 | 时长 | 是否循环 | 备注 |
|-----------|--------|----------|--------|-------|
| **Idle** | 8帧 × 8方向 | 2000ms | 是 | 轻微的呼吸/重心转移动作 |
| **行走** | 8帧 × 8方向 | 800ms | 是 | 完整行走周期，包含手臂摆动 |
| **打字** | 8帧 | 400ms | 是 | 背面视角，双手在键盘上 |
| **递交文件** | 4帧 | 600ms | 否 | 侧面视角，递交文件夹 |
| **喝咖啡** | 4帧 | 400ms | 是 | 正面视角，喝咖啡动作 |

**8个方向**（行顺序）：南、西南、西、西北、北、东北、东、东南

## 工作流程

### 步骤1：生成基础角色设计

创建一个正面Idle姿态来确定角色外观：

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art game sprite of a [CHARACTER DESCRIPTION], front view facing camera, [CLOTHING DESCRIPTION], simple friendly face, small character suitable for top-down office game, retro SNES/Genesis style pixel art, standing idle pose with arms at sides, isolated on solid magenta background #FF00FF, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, centered composition, no text, no shadows on background, 64x80 pixels scale",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "model_tier": "pro"
}'
```

**提示词变量:**
- `[CHARACTER DESCRIPTION]`: 例如，"male office worker", "female developer", "robot assistant"
- `[CLOTHING DESCRIPTION]`: 例如，"wearing blue dress shirt and tie, brown pants, black shoes, short brown hair"
- `[NAME]`: 角色标识符（例如，"agent1", "agent2", "boss"）

### 步骤2：验证与迭代

【重要提示】在生成所有精灵图之前，必须获得用户的批准。

1. 复制生成的图片:
   ```bash
   cp "/Users/probello/nanobanana-images/[FILENAME].png" \
      "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png"
   ```

2. 使用读取工具查看图片并展示给用户

3. 请用户验证以下内容:
   - 角色设计符合他们的预期
   - 艺术风格正确（16位像素艺术）
   - 比例适配游戏需求
   - 颜色和细节符合要求

4. **如果被拒绝**: 根据反馈调整提示词后重新生成

5. **如果获得批准**: 进入步骤3

### 步骤3：生成所有精灵图

每次生成时同时使用两张参考图:
- **参考图1**: 已批准的角色设计（用于保持外观一致）
- **参考图2**: 已有的Agent #1精灵图（用于保持帧布局一致）

#### 3a. Idle动画精灵图

**技术要求:**
- 总尺寸: 928 × 1152像素
- 网格: 8列 × 8行（共64个单元格）
- 单元格尺寸: 每个116 × 144像素
- 单元格之间无任何边框、内边距或间距
- 帧必须填满整个单元格区域

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art sprite sheet, EXACTLY 928x1152 pixels total, divided into 8 columns and 8 rows grid, each cell is EXACTLY 116x144 pixels with NO borders NO padding NO gaps between cells, character is [CHARACTER DESCRIPTION] (EXACTLY as shown in first reference image), 8 DIRECTIONS IN EXACT ORDER from top to bottom: ROW 0 south facing toward camera, ROW 1 south-west diagonal, ROW 2 west facing left profile, ROW 3 north-west diagonal, ROW 4 north facing away back view, ROW 5 north-east diagonal, ROW 6 east facing right profile, ROW 7 south-east diagonal, each row has 8 frames of subtle idle breathing animation, cells touch edge-to-edge with no visible grid lines, retro SNES Genesis 16-bit pixel art, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, consistent character in every cell matching reference, solid magenta #FF00FF background fills all empty space in each cell, game sprite sheet asset, no text no watermarks",
  "input_image_path_1": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "input_image_path_2": "/Users/probello/Repos/Codex-office/frontend/public/sprites/agent1_idle_sheet.png",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_idle_sheet_raw.png",
  "model_tier": "pro",
  "aspect_ratio": "4:5",
  "negative_prompt": "blurry, 3D, realistic, anti-aliasing, anti-aliased edges, smoothing, blending, soft edges, gradients, shadows on background, inconsistent character, different characters, text, watermark, grid lines, cell borders, padding between frames, gaps between cells"
}'
```

#### 3b. 行走动画精灵图

**技术要求:**
- 总尺寸: 928 × 1152像素
- 网格: 8列 × 8行（共64个单元格）
- 单元格尺寸: 每个116 × 144像素
- 单元格之间无任何边框、内边距或间距

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art sprite sheet for WALKING animation, EXACTLY 928x1152 pixels total, divided into 8 columns and 8 rows grid, each cell is EXACTLY 116x144 pixels with NO borders NO padding NO gaps between cells, character is [CHARACTER DESCRIPTION] (EXACTLY as shown in first reference image), 8 DIRECTIONS IN EXACT ORDER from top to bottom: ROW 0 walking south toward camera, ROW 1 walking south-west diagonal, ROW 2 walking west left profile, ROW 3 walking north-west diagonal, ROW 4 walking north away back view, ROW 5 walking north-east diagonal, ROW 6 walking east right profile, ROW 7 walking south-east diagonal, each row has 8 frames of walk cycle with alternating legs and natural arm swing, cells touch edge-to-edge with no visible grid lines, retro SNES Genesis 16-bit pixel art, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, consistent character in every cell matching reference, solid magenta #FF00FF background fills all empty space in each cell, game sprite sheet asset, no text no watermarks",
  "input_image_path_1": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "input_image_path_2": "/Users/probello/Repos/Codex-office/frontend/public/sprites/agent1_walk_sheet.png",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_walk_sheet_raw.png",
  "model_tier": "pro",
  "aspect_ratio": "4:5",
  "negative_prompt": "blurry, 3D, realistic, anti-aliasing, anti-aliased edges, smoothing, blending, soft edges, gradients, shadows on background, inconsistent character, different characters, text, watermark, standing still, static pose, grid lines, cell borders, padding between frames, gaps between cells"
}'
```

#### 3c. 打字动画精灵图

**技术要求:**
- 总尺寸: 928 × 144像素（单行）
- 网格: 8列 × 1行
- 单元格尺寸: 每个116 × 144像素
- 单元格之间无任何边框、内边距或间距
- **仅包含角色** - 不包含桌子、椅子或键盘（这些是独立的游戏资源）

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art sprite sheet for TYPING animation, EXACTLY 928x144 pixels total, horizontal strip with 8 equal cells of EXACTLY 116x144 pixels each with NO borders NO padding NO gaps between cells, character is [CHARACTER DESCRIPTION] (EXACTLY as shown in first reference image), character seen from behind (back view) in seated typing pose with arms extended forward making typing motions, CHARACTER ONLY no desk no chair no keyboard no furniture, 8 frame typing animation showing hands and arms making typing movements, frames show subtle arm position changes as if typing, cells touch edge-to-edge with no visible grid lines, retro SNES Genesis 16-bit pixel art, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, consistent character across all 8 frames matching reference, solid magenta #FF00FF background fills all empty space in each cell, game sprite sheet asset, no text no watermarks",
  "input_image_path_1": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "input_image_path_2": "/Users/probello/Repos/Codex-office/frontend/public/sprites/agent1_typing_sheet.png",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_typing_sheet_raw.png",
  "model_tier": "pro",
  "aspect_ratio": "21:9",
  "negative_prompt": "blurry, 3D, realistic, anti-aliasing, anti-aliased edges, smoothing, blending, soft edges, gradients, shadows on background, inconsistent character, different characters, text, watermark, front view, standing, grid lines, cell borders, padding between frames, gaps between cells, desk, chair, keyboard, furniture, computer, monitor"
}'
```

#### 3d. 递交文件夹动画精灵图

**技术要求:**
- 总尺寸: 928 × 1120像素（内容在单行）
- 网格: 4列 × 1行（可用内容区域）
- 单元格尺寸: 每个232 × 411像素
- 内容从y=343开始（垂直偏移）
- 单元格之间无任何边框、内边距或间距

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art sprite sheet for HANDING FOLDER animation, EXACTLY 928 pixels wide, horizontal strip with 4 equal cells of EXACTLY 232 pixels wide each with NO borders NO padding NO gaps between cells, character is [CHARACTER DESCRIPTION] (EXACTLY as shown in first reference image), character seen from side profile holding and handing over a manila folder document, 4 frame animation sequence: frame 1 holding folder at waist, frame 2 extending arm with folder, frame 3 arm fully extended offering folder, frame 4 releasing folder hand open, cells touch edge-to-edge with no visible grid lines, retro SNES Genesis 16-bit pixel art, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, consistent character across all 4 frames matching reference, solid magenta #FF00FF background fills all empty space in each cell, game sprite sheet asset, no text no watermarks",
  "input_image_path_1": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "input_image_path_2": "/Users/probello/Repos/Codex-office/frontend/public/sprites/agent1_handoff_sheet.png",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_handoff_sheet_raw.png",
  "model_tier": "pro",
  "aspect_ratio": "21:9",
  "negative_prompt": "blurry, 3D, realistic, anti-aliasing, anti-aliased edges, smoothing, blending, soft edges, gradients, shadows on background, inconsistent character, different characters, text, watermark, back view, grid lines, cell borders, padding between frames, gaps between cells"
}'
```

#### 3e. 喝咖啡动画精灵图

**技术要求:**
- 总尺寸: 928 × 1120像素（内容在单行）
- 网格: 4列 × 1行（可用内容区域）
- 单元格尺寸: 每个232 × 699像素
- 内容从y=0开始
- 单元格之间无任何边框、内边距或间距

```bash
mcpl call nanobanana generate_image '{
  "prompt": "16-bit pixel art sprite sheet for DRINKING COFFEE animation, EXACTLY 928 pixels wide, horizontal strip with 4 equal cells of EXACTLY 232 pixels wide each with NO borders NO padding NO gaps between cells, character is [CHARACTER DESCRIPTION] (EXACTLY as shown in first reference image), character seen from front holding and drinking from a coffee cup mug, 4 frame animation sequence: frame 1 holding coffee cup at chest, frame 2 raising cup toward face, frame 3 cup at lips drinking, frame 4 lowering cup with satisfied expression, cells touch edge-to-edge with no visible grid lines, retro SNES Genesis 16-bit pixel art, SHARP CRISP PIXEL EDGES WITH ABSOLUTELY NO ANTI-ALIASING NO SMOOTHING NO BLENDING, each pixel is a solid color with hard edges, consistent character across all 4 frames matching reference, solid magenta #FF00FF background fills all empty space in each cell, game sprite sheet asset, no text no watermarks",
  "input_image_path_1": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_front_idle_raw.png",
  "input_image_path_2": "/Users/probello/Repos/Codex-office/frontend/public/sprites/agent1_coffee_sheet.png",
  "output_path": "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_coffee_sheet_raw.png",
  "model_tier": "pro",
  "aspect_ratio": "21:9",
  "negative_prompt": "blurry, 3D, realistic, anti-aliasing, anti-aliased edges, smoothing, blending, soft edges, gradients, shadows on background, inconsistent character, different characters, text, watermark, back view, sitting, grid lines, cell borders, padding between frames, gaps between cells"
}'
```

### 步骤4：验证每个生成的精灵图

【重要提示】生成每个精灵图后：

1. 复制到精灵图文件夹:
   ```bash
   cp "/Users/probello/nanobanana-images/[FILENAME].png" \
      "/Users/probello/Repos/Codex-office/frontend/public/sprites/[NAME]_[TYPE]_sheet_raw.png"
   ```

2. 验证尺寸是否符合规格:
   ```bash
   cd /Users/probello/Repos/Codex-office/frontend/public/sprites

   # 检查精灵图尺寸
   magick "[NAME]_[TYPE]_sheet_raw.png" -format "Size: %wx%h" info:

   # 预期尺寸:
   # idle/行走精灵图: 928x1152
   # 打字精灵图: 928x144（裁剪后）或 928x1152（完整生成）
   # 递交文件/喝咖啡精灵图: 928x1152
   ```

3. 使用读取工具查看以验证:
   - 角色与已批准的设计一致
   - 所有帧都存在且排列正确
   - 动画进度看起来正常
   - 洋红色背景为纯色
   - **无可见的网格线或单元格边框**
   - **单元格之间无内边距/间距**
   - **像素边缘锐利，无抗锯齿**（放大检查）
   - **对于方向型精灵图（idle/行走）**: 验证方向顺序是否正确:
     - 第0行：南（朝向镜头）
     - 第1行：西南
     - 第2行：西（左侧轮廓）
     - 第3行：西北
     - 第4行：北（背面视角）
     - 第5行：东北
     - 第6行：东（右侧轮廓）
     - 第7行：东南

4. 检查常见问题:
   - 如果网格线可见：重新生成，强调“no grid lines”
   - 如果单元格数量错误：重新生成，使用正确的列/行规格
   - 如果单元格尺寸不一致：重新生成，在提示词中明确像素尺寸
   - **如果边缘看起来柔和/模糊（抗锯齿）**: 重新生成，加强“NO ANTI-ALIASING”的强调
   - **如果方向顺序错误**: 重新生成，明确指定“ROW 0 south, ROW 1 south-west...”的顺序

5. 如果发现问题，重新生成该精灵图

### 步骤5：处理所有精灵图

使用改进的多阶段工作流程移除所有精灵图的洋红色背景:

```bash
cd /Users/probello/Repos/Codex-office/frontend/public/sprites

# 使用共享脚本处理所有精灵图
SCRIPT="/Users/probello/Repos/Codex-office/.Codex/skills/shared/scripts/remove_magenta.sh"

for sheet in [NAME]_idle_sheet [NAME]_walk_sheet [NAME]_typing_sheet [NAME]_handoff_sheet [NAME]_coffee_sheet; do
  INPUT="${sheet}_raw.png"
  OUTPUT="${sheet}.png"

  # 使用多阶段移除（--skip-trim保留精灵图尺寸）
  "$SCRIPT" "$INPUT" "$OUTPUT" --skip-trim

  echo "Processed: $OUTPUT"
  magick "$OUTPUT" -format "  Size: %wx%h, Opaque: %[opaque]" info:
done
```

多阶段工作流程:
1. **FFmpeg geq滤镜**: 移除R≈B且G值低的像素（紫色/洋红色调）
2. **ImageMagick多阶段处理**: 捕获剩余的亮洋红色调
3. **ImageMagick深紫色清理**: 移除深色边缘像素如rgb(32,0,31)

同时处理基础角色精灵图:
```bash
INPUT="[NAME]_front_idle_raw.png"
OUTPUT="[NAME]_front_idle.png"

"$SCRIPT" "$INPUT" "$OUTPUT"
```

**备选方案**（如果脚本不可用）:
```bash
magick "$INPUT" \
  -fuzz 40% -transparent "#FF00FF" \
  -fuzz 15% -transparent "#CC00CC" \
  -fuzz 15% -transparent "#880088" \
  -strip \
  "$OUTPUT"
```

### 步骤6：最终验证

查看每个处理后的精灵图以确认:
- **角色边缘无洋红色/粉色毛边**（放大检查）
- 透明度处理干净
- 角色边缘完整（未被透明度移除操作侵蚀）
- 所有帧可见

**如果处理后仍有粉色边缘:**
1. 尝试增加模糊值: `-fuzz 45%` 或 `-fuzz 50%`（但模糊值超过50%时要注意角色边缘被侵蚀）
2. 如果这会侵蚀角色边缘，降低模糊值并手动检查原始图片
3. 原始图片中角色边缘与洋红色背景存在抗锯齿的，总会出现毛边问题 - 重新生成原始图片，加强“NO ANTI-ALIASING”的强调

## 参考精灵图

以下Agent #1精灵图作为布局参考:

| 精灵图 | 位置 |
|-------|----------|
| Idle | `/frontend/public/sprites/agent1_idle_sheet.png` |
| 行走 | `/frontend/public/sprites/agent1_walk_sheet.png` |
| 打字 | `/frontend/public/sprites/agent1_typing_sheet.png` |
| 递交文件 | `/frontend/public/sprites/agent1_handoff_sheet.png` |
| 喝咖啡 | `/frontend/public/sprites/agent1_coffee_sheet.png` |

## 输出位置

所有精灵图保存至: `/Users/probello/Repos/Codex-office/frontend/public/sprites/`

- `[name]_front_idle_raw.png` - 原始角色设计（作为参考保留）
- `[name]_front_idle.png` - 已处理的角色设计，带透明度
- `[name]_[type]_sheet_raw.png` - 原始精灵图（作为备份保留）
- `[name]_[type]_sheet.png` - 已处理的精灵图，可用于游戏

## 角色创意

示例角色描述以增加多样性:

| 角色 | 描述 |
|-----------|-------------|
| Agent 2 | 女开发者，穿紫色衬衫、黑裤子、戴眼镜、扎马尾辫 |
| Agent 3 | 男资深开发者，穿灰色毛衣、卡其裤、留胡子、发际线后移 |
| Agent 4 | 非二元性别的实习生，穿绿色连帽衫、牛仔裤、发色鲜艳 |
| Boss/Codex | 尊贵形象，采用橙色/棕褐色配色（与Codex品牌匹配） |

## 反模式与常见问题

### 网格布局问题（重要）

**禁止操作:**
- 在单元格之间添加可见的网格线或边框
- 在单元格之间添加内边距/外边距/间距
- 生成单元格尺寸不一致的精灵图
- 使用错误的行列数（网格型精灵图必须为8×8，详见上方规格）
- 在打字精灵图中包含桌子、椅子、键盘（这些是独立的游戏资源）

**需要拒绝的常见生成错误:**
- 单元格之间可见网格线 → 重新生成，强调“no grid lines”
- 单元格尺寸不一 → 重新生成，明确像素尺寸
- 存在空/缺失的单元格 → 重新生成，填满所有单元格
- 角色包含家具 → 重新生成，强调“CHARACTER ONLY”

### 抗锯齿问题（重要）

**禁止操作:**
- 接受边缘柔和/模糊的精灵图
- 接受像素间颜色混合的精灵图
- 接受边缘有平滑渐变的精灵图

**如何检测抗锯齿:**
- 放大角色边缘 - 每个像素应为单一纯色
- 查看角色轮廓周围是否有“光晕”或“毛边”颜色
- 检查边缘是否平滑而非锯齿状/阶梯状

**需要拒绝的常见抗锯齿错误:**
- 角色轮廓边缘柔和 → 重新生成，加强“NO ANTI-ALIASING”的强调
- 像素边界存在颜色渐变 → 重新生成，强调“each pixel is a solid color”
- 外观模糊/浑浊 → 重新生成，强调“SHARP CRISP PIXEL EDGES”

### 工作流程反模式

**禁止操作:**
- 跳过初始设计审批 - 迭代直到用户满意
- 不使用两张参考图就生成精灵图
- 跳过每个生成精灵图的验证步骤
- 使用填充工具移除背景（对精灵图使用`-transparent`）
- 精灵图之间角色设计不一致

**推荐操作:**
- 始终先获得用户对基础设计的批准
- 每个精灵图都使用角色参考图+布局参考图
- 生成后验证每个精灵图再继续
- 保留原始图片作为备份
- 验证完成后批量处理所有精灵图
- 生成后验证单元格尺寸是否符合规格