## ADDED Requirements

### Requirement: Map terrain manifest
系统 SHALL 在生成地图前显式定义该地图允许使用的全部地貌 ID，并为每个地貌关联独立图块变体池。

#### Scenario: Pilot map declares grass and river
- **WHEN** 加载草地—河流试制地图
- **THEN** 地貌清单只包含 `grass` 与 `river`，且两者均能解析到各自的图块池

### Requirement: Eight variants per terrain
每个正式地貌池 MUST 包含 8 个稳定 ID 的绘图模型图块变体；首轮草地与河流各包含 8 个变体。

#### Scenario: Validate pilot variant counts
- **WHEN** 校验草地—河流试制资源
- **THEN** `grass` 与 `river` 的可用图块数量分别等于 8

### Requirement: Full-bleed terrain tile contract
每张地貌图块 MUST 是 256×256 PNG，内容 MUST 铺满整个正方形画布，四条边和四个角的 alpha MUST 为 255；系统 SHALL NOT 要求相邻图块纹理无缝。

#### Scenario: Reject transparent tile border
- **WHEN** 任一地貌图块边缘存在 alpha 小于 255 的像素或画布尺寸不是 256×256
- **THEN** 资源校验失败且该图块不得进入 pilot 图块池

#### Scenario: Accept non-seamless opaque tiles
- **WHEN** 两张 256×256 图块四边完全不透明但相接边纹理不连续
- **THEN** 资源校验通过，不因纹理不连续而拒收

### Requirement: Direct terrain adjacency
不同地貌 SHALL 直接以完整图块相邻渲染，系统 SHALL NOT 依赖方向边、角块、融合 mask、过渡带或 edge/socket 匹配资源。

#### Scenario: Grass touches river
- **WHEN** 草地格与河流格共享一条四邻域边
- **THEN** 两个完整图块直接相邻显示，且基础地貌渲染不请求特殊拼接图

### Requirement: Six decorations per terrain pair
每对已支持的不同地貌 MUST 配置 6 个稳定 ID 的透明交界装饰；草地—河流试制 MUST 提供 6 个画布尺寸或长宽比可不同的装饰，并记录统一像素密度下的实际占地。

#### Scenario: Validate grass river decorations
- **WHEN** 校验 `grass|river` 装饰池
- **THEN** 池中恰有 6 个可解析的 RGBA PNG，四角透明，并且每个条目包含画布像素尺寸与世界占地信息

### Requirement: Deterministic random selection
试制期系统 SHALL 使用 seed 对地貌变体和交界装饰进行可复现的加权随机选择；条目 MUST 保留权重和标签以支持未来规则过滤。

#### Scenario: Same seed reproduces visuals
- **WHEN** 对相同地貌布局使用相同 seed 生成两次
- **THEN** 每个格的图块变体 ID与每个边界段的装饰选择及位置一致

#### Scenario: Different seed can vary visuals
- **WHEN** 对相同地貌布局使用不同 seed 生成
- **THEN** 系统允许选择不同图块变体与装饰，而地貌布局本身保持不变

### Requirement: Variable-width water body
草地—水域试制地图 MUST 将水域作为一条连续的水域带生成，而非固定宽度的河流。生成器 MUST 接受最小和最大水域宽度；相邻行的水域 MUST 至少重叠一个格子，且宽度变化的单次步长 MUST 不超过一个格子。

#### Scenario: Water width varies within the configured range
- **WHEN** 使用最小宽度 2、最大宽度 5 和固定 seed 生成 16×12 草地—水域试制地图
- **THEN** 每一行水域宽度位于 2 至 5 格之间，至少出现两种宽度，且任意相邻两行的水域存在共享格子

### Requirement: Model-authored artwork only
地貌纹理与装饰主体 MUST 由绘图模型生成；运行时代码 SHALL 仅选择、放置和渲染已有资源，且 SHALL NOT 程序生成纹理、岸线、边缘或融合图。

#### Scenario: Runtime renders pilot assets
- **WHEN** Unity 渲染草地—河流试制地图
- **THEN** 所有可见地貌与交界装饰均能追溯到已登记的绘图模型 PNG 资源

### Requirement: Runnable Unity preview approval gate
系统 MUST 提供一个通过 Unity 实际 Sprite、Tilemap 与 MonoBehaviour 流程生成草地—河流地图的独立测试场景，场景 MUST 能直接打开并在 Play Mode 自动生成；在用户完成视觉效果测试并确认前 SHALL NOT 批量制作其他地貌的正式美术资源。

#### Scenario: Pilot test scene
- **WHEN** 22 张试制资源全部通过自动校验
- **THEN** `PCGGrassRiverPreview` 场景可加载 8 张草地、8 张河流和 6 张交界装饰，并使用固定 seed 完成一次无异常生成
