## ADDED Requirements

### Requirement: 第一阶段 UI 必须覆盖完整三轮闭环
美术交付 MUST 覆盖主菜单/本地对局确认、纹身与元素档案、玩法帮助、设置、制作人员、退出确认、常驻 HUD、构筑阶段、六人情报、颜料请求、倒地/救援/淘汰、观战、三轮结算和开发模式提示。

#### Scenario: 对照 UI 清单验收
- **WHEN** 检查第一阶段 UI 交付索引
- **THEN** 每个必需界面都有稳定 ID、layout、状态清单和对应素材计划
- **AND** 不要求 Boss、撤离、局外熟练度或第 4/5 轮页面

### Requirement: 每组 UI 必须先完成 prefab layout
任何效果图生成前 MUST 先输出含 Canvas、anchor、pivot、sizeDelta、anchoredPosition、层级、交互状态和安全区规则的 `prefab-layout.md`，并经过用户确认。

#### Scenario: layout 尚未确认
- **WHEN** 某界面只有需求文字或草图
- **THEN** 不得开始正式效果图生成或素材拆分

### Requirement: UI 必须适配四类屏幕与两类输入
layout MUST 提供 720p、1080p、4K 和掌机的缩放/fallback；控制器焦点、glyph 和返回路径优先定义，同时保留键鼠 hover、点击和滚动行为。

#### Scenario: 720p 六人情报界面
- **WHEN** 六人情报在 1280×720 显示
- **THEN** 关键构筑和成果信息不超出安全区
- **AND** 允许通过分页/滚动查看而不是缩小到不可读字号

### Requirement: 三元素与三层状态不能只靠颜色表达
火、冰、雷 MUST 具有独立图形轮廓或纹理节奏；弱/标准/强 MUST 具有可数的层级符号。所有关键图标在 32×32 下可辨识，并通过红绿、蓝黄和全色盲模拟。

#### Scenario: 去色查看元素状态
- **WHEN** UI 以灰度或全色盲模拟显示
- **THEN** 仍能区分火、冰、雷和三个层级

### Requirement: 图案使用临时 ID 而非虚构名称
第一阶段 UI MUST 使用 P01/P02，档案可以预览 P01～P08 的现有视觉，但不得给八个图案添加未经确认的正式名称或标签化流派名称。

#### Scenario: 打开纹身档案
- **WHEN** 玩家浏览图案视觉库
- **THEN** 图案显示 P01～P08 临时编号
- **AND** 只有 P01/P02 标记为第一阶段可用

### Requirement: 动态文字和数值不得烘焙进素材
玩家名、效果文本、属性、统计、倒计时、颜料数量和版本信息 MUST 由程序渲染；PNG/矢量素材只能包含可复用视觉结构。

#### Scenario: 素材拆分检查
- **WHEN** 检查导出的 HUD 或情报面板素材
- **THEN** 不存在写死的玩家名、数值或中文业务文本
