## MODIFIED Requirements

### Requirement: 角色 sprite 资源组织
系统 SHALL 在新的非旧目录路径下管理 2D 角色资源：`Assets/Game/Sprites/Actors/<character_id>/<action>/<direction>/` 用于可动画角色的切分帧，`Assets/Game/Sprites/NPC/<npc_id>/` 用于静态世界 Sprite，`Assets/Game/Sprites/UI/CharacterSelectForm/Portraits/` 用于角色选择立绘。旧 `Assets/Game/Sprites/Character` 和 `Assets/Game/Sprites/Characters` MUST NOT 被重新使用。

#### Scenario: 通用玩家与 Boss 的完整动作目录
- **WHEN** 导入 `actor_common` 或 `boss_ai_ruins_warden`
- **THEN** 各自均存在 `idle`、`walk`、`attack`、`death` 的 `down`、`up`、`left`、`right` 切分帧目录

#### Scenario: 玩家 2/3 仅有角色选择立绘
- **WHEN** 导入 `player_2` 或 `player_3` 首批资源
- **THEN** 仅在 CharacterSelectForm Portraits 目录存在对应立绘，且不要求战斗动画目录

### Requirement: AnimatorController 参数契约
通用玩家角色与 Boss 的 AnimatorController SHALL 暴露以下参数：`Direction` (Int，0=Down、1=Up、2=Left、3=Right)、`IsMoving` (Bool)、`AttackTrigger` (Trigger)、`Die` (Trigger)、`Dead` (Bool)。

#### Scenario: 参数可由运行时读取和写入
- **WHEN** Actor runtime 设置方向、移动、攻击或死亡状态
- **THEN** AnimatorController 包含对应参数，且不会因缺失参数导致诊断失败
