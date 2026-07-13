## MODIFIED Requirements

### Requirement: Main menu flow MUST progress through GF_X UI services
主菜单到战斗 HUD 的流程 MUST 由 GF_X UI form 和 runtime service 驱动：`MainMenu -> CharacterSelect -> StartupSelect -> CombatHUD`。打开 CombatHUD MUST NOT 直接把本地 Participant 设为 Active；本地 Ready 必须等待场景、HUD、相机、InputModule 和一个渲染帧全部就绪。

#### Scenario: 开始游戏进入角色选择
- **WHEN** 玩家在 MainMenu 点击开始按钮
- **THEN** TotemUIService MUST 打开 CharacterSelect
- **AND** TotemGameFlowService MUST 进入 CharacterSelect 状态

#### Scenario: 角色选择进入启动选择
- **WHEN** 玩家选择角色并继续
- **THEN** TotemUIService MUST 打开 StartupSelect
- **AND** 选择结果 MUST 写入启动状态

#### Scenario: 启动选择打开战斗 HUD 但尚未激活
- **WHEN** 玩家确认初始颜色、武器和图案
- **THEN** TotemUIService MUST 打开 CombatHUD
- **AND** 本地 Participant MUST 保持 Loading，直到 Readiness 条件全部满足

#### Scenario: HUD 相机和 InputModule 就绪后提交 Ready
- **WHEN** CombatHUD 已渲染至少一帧且相机与 InputModule 均可用
- **THEN** 本地 Readiness provider MUST 提交 Ready
- **AND** Participant MUST 进入最长 5 秒 Protected，再按输入或超时进入 Active

