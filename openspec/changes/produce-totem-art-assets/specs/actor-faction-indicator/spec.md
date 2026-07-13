## ADDED Requirements

### Requirement: 共享角色的阵营识别不改变本体颜色
Player、SmartAI 和 LightAI 在共享 `actor_common` 本体资源时 SHALL 以原始 Sprite 颜色显示。系统 MUST 用角色脚点下方的扁平阵营环表达身份：玩家蓝、SmartAI 红、LightAI 黄。

#### Scenario: 实例化玩家角色
- **WHEN** runtime 通过 `actor.player` 或 `actor.player.1` 实例化通用角色
- **THEN** 角色身体保持原始 Sprite 颜色，脚下显示蓝色阵营环

#### Scenario: 实例化两类 AI
- **WHEN** runtime 通过 `actor.smartAi` 或 `actor.lightAi` 实例化通用角色
- **THEN** 角色身体保持原始 Sprite 颜色，脚下分别显示红色或黄色阵营环

#### Scenario: 非阵营 actor 不显示环
- **WHEN** runtime 实例化 Boss、纹身师或商人
- **THEN** 不创建玩家 / AI 阵营环
