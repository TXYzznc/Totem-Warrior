# HUD 美术资源说明

## 基本信息
通用 HUD 装饰元件库（非 Form 特定）
来源：openspec/changes/06-v21-implementation/art/requirements.md（§1 UI 三表）
规划日期：2026-06-25，生成时间：2026-06-26

## 功能说明
全局通用 HUD 框架组件，用于装饰各类 UI 元素的底框、边框、槽位。与 CombatHUDForm 联合使用，但独立于特定页面。

| 文件名 | 用途 | 对应表项 | 复用范围 |
|---|---|---|---|
| hud_ammo_box.png | 弹药数字背框 | UI 三表项 5 | CombatHUDForm 左下 |
| hud_buff_slot.png | Buff 图标底槽 | UI 三表项 2 | Buff 槽行 |
| hud_build_row_bg.png | Build 列表行背景 | UI 三表项 9 | Sidebar ScrollListRow |
| hud_hp_bar_frame.png | HP 条框（含描边+装饰） | UI 三表项 1 | CombatHUDForm + CombatHUDForm |
| hud_minimap_frame.png | 小地图圆形框+装饰环 | UI 三表项 6 | 右上小地图 |
| hud_shrink_timer.png | 缩圈倒计时背框 | UI 三表项 7 | 右下倒计时文字底板 |
| hud_skill_slot.png | 技能槽通用底框（Q/E 可复用） | UI 三表项 3/4 | 底部技能槽 |
| hud_weapon_frame.png | 武器图标框 | UI 三表项 8 | 技能槽右侧武器图 |

## 出处追溯
- 原始需求：openspec/changes/06-v21-implementation/art/requirements.md（表 A 9 项）
- 生成方式：codex-image-gen L1 batch（样式化 icon frame + progress bar frame）
- 确认人：art-director（本文档）
- 设计风格：Hades 精致 2.5D，描边 1-2px，深色背景 #1A1C2E + 金色 #FFB400 描边

## 架构设计
所有 frame 均采用 **9-slice 可扩展**规范：
- 中心 80% 为内容区（可填充 Buff/技能 icon）
- 四周 2-3px 边框（深阴影 #22243A + 金色高光）
- 圆角半径 r=4px（icon 类）或 r=2px（条形）

## 备注
- 这 8 个文件是 v2.1 HUD 的"核心骨架"，CombatHUDForm 中的同名文件系拷贝/变体
- 无 Buff 类型的具体图标（如火/冰/暴击），仅提供 slot 底框，icon 由上层业务逻辑动态加载
- 所有文件均已在 Prefab 中引用，无孤儿资源
