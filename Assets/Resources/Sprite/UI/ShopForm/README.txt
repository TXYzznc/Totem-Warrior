# ShopForm 美术资源说明

## 基本信息
对应 Form：ShopForm（Assets/Scripts/Modules/NPC/UI/ShopForm.cs）
对应 Prefab：Assets/Resources/Prefab/UI/Shop.prefab
原始需求出处：openspec/changes/12-core-ui-screens/art/requirements.md（表 A，必做）

## 功能说明
居中覆盖层商店界面，包含：
- 背景
- 库存网格（GridLayoutGroup）内各类物品的图标（武器/药水/配方/纹身等）
- 关闭按钮
- 刷新按钮

关键 SerializeField：物品图标（dagger/potion/ring/scroll/shield/tattoo_ink 等）。

## 文件清单

| 文件名 | 用途 | 类型 | 状态 |
|---|---|---|---|
| ShopForm_bg.png | 背景 | 背景 | ✓ OK |
| ShopForm_btn_close.png | 关闭按钮 | 按钮 | ✓ OK |
| ShopForm_btn_refresh_bg.png | 刷新按钮背景 | 按钮 | ✓ OK |
| ShopForm_icon_dagger.png | 匕首图标 | 物品 | ✓ OK |
| ShopForm_icon_gold.png | 金币图标 | 物品 | ✓ OK |
| ShopForm_icon_potion.png | 药水图标 | 物品 | ✓ OK |
| ShopForm_icon_ring.png | 戒指图标 | 物品 | ✓ OK |
| ShopForm_icon_scroll.png | 卷轴图标 | 物品 | ✓ OK |
| ShopForm_icon_shield.png | 盾牌图标 | 物品 | ✓ OK |
| ShopForm_icon_tattoo_ink.png | 纹身墨水图标 | 物品 | ✓ OK |

## 出处追溯
- 来源：openspec/changes/12-core-ui-screens （批次 3，2026-06-28）
- 生成记录：openspec/changes/12-core-ui-screens/art/raw/ShopForm/生成记录.md
- 设计依据：GDD 09-纹身师与商人 NPC

## 备注
- 物品图标为示例素材，实际商店库存由后端动态驱动
- 物品类型可能需要扩展（如符文、材料等），建议预留图标库
- 此处仅提供 UI 框架和示例图标，不含所有可能的物品类型
