# core-ui-screens Specification

## Purpose
TBD - created by archiving change 12-core-ui-screens. Update Purpose after archive.
## Requirements
### Requirement: 核心 UI 清单覆盖完整游戏循环
第一阶段 MUST 包含：MainMenu、本地对局确认、纹身与元素档案、玩法帮助、Settings、Credits、退出确认、CombatHUD、构筑界面、六人情报、颜料请求、倒地/救援/淘汰反馈、观战和五轮结果。旧 CharacterSelect、StartupSelect、Shop、ThreeChoice、SelfTattoo 和熟练度界面不得进入主流程。

#### Scenario: 枚举第一阶段 UI 注册表
- **WHEN** 读取当前 UI form catalog
- **THEN** 所有第一阶段界面具有稳定 form/slot ID 和占位资源
- **AND** 旧主流程 Form 未被自动打开

### Requirement: 每个 Form 必须有效果图作准绳
程序实现 MUST 先消费 `rebaseline-pvpve-art-resources` 提供的 `prefab-layout.md` 接口；在美术效果图未确认前 MUST 使用可替换占位视觉，不得伪造“视觉已验收”。最终视觉验收只在美术 change 中完成。

#### Scenario: 美术尚未交付时运行 smoke
- **WHEN** 只存在占位 sprite/material
- **THEN** 所有必要 Form 仍可完整交互并通过 smoke

### Requirement: Form 间交互链路与时序
所有 Form MUST 通过 GF_X UI service 管理层级与异步打开/关闭；输入必须由 InputModule 提供。构筑阶段 UI 使用 unscaled 倒计时，暂停菜单不得推进战斗模拟。

#### Scenario: 构筑界面倒计时结束
- **WHEN** 45 秒构筑倒计时归零
- **THEN** 未关闭的弹窗和请求安全收尾
- **AND** UI 切换到 HUD 后才恢复战斗模拟

### Requirement: 联调发现 bug 修复路径
UI 业务接线、状态和输入 bug MUST 在本 change 修复；视觉、切片或布局偏差 MUST 记录到美术 change，不得在程序 tasks 中修改美术源文件。

#### Scenario: HUD 数据正确但视觉间距错误
- **WHEN** 联调确认数据与交互正确而布局不符合已确认稿
- **THEN** 主 change 记录美术 handoff 问题
- **AND** 不改变战斗服务或统计公式

