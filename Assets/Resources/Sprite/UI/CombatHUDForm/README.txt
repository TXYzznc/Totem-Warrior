# CombatHUDForm 美术资源说明

## 基本信息
对应 Form：CombatHUDForm（Assets/Scripts/Modules/UI/CombatHUDForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/CombatHUD.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
常驻战斗 HUD，包含以下功能区块：
1. HP 条（绿/黄/红/灰多态）
2. Buff 图标槽
3. 技能 Q/E 冷却槽（含 radial 冷却遮罩）
4. 弹药显示
5. 小地图框
6. 缩圈倒计时
7. Build 列表（ScrollListRow）
8. 战斗日志
9. Boss HP 条

关键 SerializeField：_hpBar, _skillQ, _skillE, _weaponIcon, _minimapImage, _bossHpBar, _buildListRoot, _logListRoot 等。

## 文件清单

| 文件名 | 用途 | 所属模块 | 状态 |
|---|---|---|---|
| CombatHUDForm_bg.png | HUD 背景板 | 背景 | ✓ OK |
| boss_hp_bar.png | Boss HP 条框（装饰边框） | Boss 面板 | ✓ OK |
| boss_phase_icon.png | Boss 阶段指示图标 | Boss 面板 | ✓ OK |
| buff_icon_crit.png | Buff 图标（暴击） | Buff 槽 | ✓ OK |
| buff_icon_fire.png | Buff 图标（火焰） | Buff 槽 | ✓ OK |
| buff_icon_ice.png | Buff 图标（冰霜） | Buff 槽 | ✓ OK |
| hp_bar_normal.png | HP 条框（装饰边框） | HP 面板 | ✓ OK |
| minimap_frame.png | 小地图圆形框 | 小地图 | ✓ OK |
| sidebar_panel_bg.png | Build/日志列表背板 | Sidebar | ✓ OK |
| skill_slot_e_cooldown.png | E 技能冷却遮罩（radial） | 技能槽 | ✓ OK |
| skill_slot_q_cooldown.png | Q 技能冷却遮罩（radial） | 技能槽 | ✓ OK |
| shrink_timer_bg.png | 缩圈倒计时背板 | 倒计时 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 2，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/CombatHUDForm/生成记录.md
- 出图方式：codex-image-gen + 手工切割组件

## 备注
- 区别于 HUD/ 目录：HUD/ 为通用 HUD 元件库，CombatHUDForm/ 为战斗专用面板组件
- Buff 图标仅示例 3 个（火/冰/暴击），实际游戏中的 Buff 类型由 TattooElementConfig 驱动，可能需要动态加载或扩展
- 冷却遮罩采用 radial filled 策略，与 Image 组件 fillAmount 关联，此处仅提供静态 UI 框架
