## ADDED Requirements

### Requirement: 结构台账先于素材生产

系统 MUST 在为任一已确认 UI 效果图生成素材前，为其建立结构台账。台账 MUST 对每个视觉节点标记 `ui-shell`、`runtime-slot`、`data-asset` 或 `text`，并列出其 Unity 使用方式、尺寸/拉伸规则和交互状态。没有 `ui-shell` 标记的节点不得作为页面 UI Sprite 生成。

#### Scenario: 角色与纹身预览不被生成成页面组件
- **WHEN** 台账分析纹身工作台中的角色、纹身贴花和前后材质预览
- **THEN** 它们 MUST 标记为 `runtime-slot` 或 `data-asset`，而不是页面 UI Sprite

#### Scenario: 普通文案不进入素材清单
- **WHEN** 台账分析标题、按钮文案、数量和百分比
- **THEN** 它们 MUST 标记为 `text`，并声明由 TMP_Text 与本地化数据提供

### Requirement: 通用组件集中且按可变形规则复用

形状相同、仅颜色变化或可由 Unity 九宫格/拉伸复现的 UI 壳体 MUST 只生成一份，并保存到 `artifacts/美术资源需求/通用UI组件/`。通用清单 MUST 记录其 Sprite 类型、边框、推荐尺寸、允许的 Tint/拉伸范围和可复用状态。

#### Scenario: 同形卡片不重复生成
- **WHEN** 两个页面使用轮廓相同但颜色不同的卡片、槽位或选择框
- **THEN** 系统 MUST 仅生成一个通用 Sprite，并在清单中说明颜色由 Unity Tint 或覆盖层处理

#### Scenario: 页面语义装饰保持专属
- **WHEN** 某组件含有稀有度缺口、撤离事件、图案成长或纹身工作台专属语义
- **THEN** 系统 MUST 将其保留在对应页面的专属素材目录，而不得错误归入通用库

### Requirement: 交互状态覆盖完整

每个交互 `ui-shell` MUST 声明 normal、focused、pressed 和 disabled 状态；具有权限条件的项 MUST 声明 locked 状态；存在异步提交、读条或结果反馈的页面 MUST 声明 loading、success 和 failure 的壳体或遮罩资源。仅颜色变化的状态 MUST 复用同一 Sprite 并在 Unity 侧着色。

#### Scenario: 效果图只展示选中态
- **WHEN** 效果图只呈现一个已选中的按钮或卡片
- **THEN** 台账 MUST 仍列出 normal、focused、pressed 和 disabled 状态，并标记未在效果图中出现的状态为“补充制作”

#### Scenario: 锁定样式卡
- **WHEN** 样式、构筑或熟练度项存在未解锁条件
- **THEN** 台账 MUST 具备 locked 遮罩、锁图标或禁用边框的资源策略

### Requirement: 透明素材生成与验收可追溯

所有带材质、描边、高光或纹理的 `ui-shell` MUST 通过纯 `#00ff00` 绿幕重生成、整图去绿和后续切图生成；纯色几何件可以程序化生成透明 PNG。最终组件 MUST 为 RGBA、四角 alpha 为 0、包含透明像素、文件大于 1KB，并有来源和验证记录。

#### Scenario: 禁止从效果图直接裁切组件
- **WHEN** 系统需要获得某个面板、按钮、图标或边框素材
- **THEN** 系统 MUST NOT 从效果图直接裁取矩形，并 MUST 在生成记录中保留其绿幕或程序化来源

#### Scenario: Alpha 验收失败
- **WHEN** 组件任何一个采样角的 alpha 不为 0，或图像不存在透明像素
- **THEN** 系统 MUST 将该组件标记为失败且不得列入最终素材清单

### Requirement: 新旧素材互不覆盖

新版结构化素材 MUST 写入效果图同级的 `结构重制` 目录；既有拆分产物 MUST 保留为审计资料。新版清单 MUST 明确标记来源效果图、生成日期、组件职责和旧版不适用原因。

#### Scenario: 重制前已有拆分目录
- **WHEN** 页面已有上一轮拆分出的文件
- **THEN** 系统 MUST 不删除、不覆盖或移动该目录，并 MUST 将新版输出隔离在 `结构重制` 中
