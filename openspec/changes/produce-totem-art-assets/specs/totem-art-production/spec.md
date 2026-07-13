## ADDED Requirements

### Requirement: 首批角色主体和可见纹身区
系统 SHALL 生产一个无预烘焙纹身的 `actor_common` 角色主体，供 `actor.player`、`actor.smartAi` 和 `actor.lightAi` 共用。主体在正、背、左、右四视图中 MUST 为头、躯干、左臂、右臂、左腿、右腿分别保留可见且不被衣物遮挡的贴花主区域。

#### Scenario: 通用角色不带固定纹身
- **WHEN** 检查 `actor_common` 概念图、四视图和任一动画帧
- **THEN** 不存在绘入皮肤的固定纹身，且六个贴花部位均有可用裸露区域

#### Scenario: 三类 actor 复用同一主体
- **WHEN** 检查 Player、SmartAI 和 LightAI 的资源绑定
- **THEN** 三者使用同一套 `actor_common` Sprite 与 AnimatorController，而非各自复制一份绘制资产

### Requirement: 角色动画源文件与切分帧
通用角色与 AI 遗迹执政官 SHALL 先有概念立绘和四视图，再生产 `idle`、`walk`、`attack`、`death` 四个动作的 `down`、`up`、`left`、`right` 帧动画。每个源画布 MUST 只含一个角色、一个动作和一个方向，单帧最大为 512×512，脚底锚点一致。

#### Scenario: 标准帧数完整
- **WHEN** 检查任一角色的完整动画交付
- **THEN** `idle` 有 4 帧、`walk` 有 6 帧、`attack` 有 6 帧、`death` 有 8 帧，且每个动作均有四个方向

#### Scenario: 帧文件可由 Unity 导入
- **WHEN** 切分一个方向的源画布
- **THEN** 输出文件名为 `<character_id>_<action>_<direction>_<frame:02>.png`，背景为透明，所有帧具有一致的尺寸与脚底锚点

### Requirement: Boss 与静态角色视觉
系统 SHALL 生产 AI 遗迹执政官 Boss 的半写实厚涂主体，以及纹身师、商人的透明背景三分之四正面全身静态 Sprite；并 SHALL 生产 `player_2` 荒原讯号猎手和 `player_3` 失控改造者的半身占位立绘。

#### Scenario: Boss 传达配置技能
- **WHEN** 检查 Boss 概念图和四视图
- **THEN** 胸腔能量核心、厚重双足和肩背召唤构件分别可读为 beam、stomp、summon 的身体前摇基础

#### Scenario: NPC 不要求帧动画
- **WHEN** 检查纹身师和商人的首批交付
- **THEN** 每个 NPC 均有一张透明背景的全身 Idle 站立姿势 Sprite 可用于场景摆放，且不存在要求四方向帧动画的交付项

### Requirement: 生产记录与美术边界
所有本 change 生成的项目绑定图片 SHALL 保存到 `openspec/changes/produce-totem-art-assets/art/` 的原始或处理目录，并在生成记录中记录提示词摘要、参考图、输出文件和处理结果。VFX 位图、Shader、粒子系统和预烘焙纹身 MUST NOT 被作为本 change 的绘制交付。

#### Scenario: 可追溯的最终资源
- **WHEN** 任一新最终 PNG 被导入项目
- **THEN** change 的生成记录包含对应资源名、源输出、处理状态与最终导入路径
