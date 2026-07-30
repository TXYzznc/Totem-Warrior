# BF-02 彩色 ProBuilder 白模验收说明 V03

## 修正说明

V02 使用失效的网格尺寸写法，导致建筑被渲染为平铺切片，已废弃。V03 以场景中已经验证为真实体积的 ProBuilder 网格为基底重建，所有外墙、屋面、内隔墙、女儿墙、雨棚和功能家具均为可见实体。

## 当前 P2 结果

- 场景：`Assets/Game/Scenes/Blockout/BF02_Blockout.unity`
- 根节点：`BLD_BF02_ROOT`
- 结构：12×16m 单层深进式颜料与耗材商店；无楼梯、夹层或假二层。
- 流线：D01 → 前店 → 后走道 → D03/D04 → 西调制间 / 东储存间 → D02；外墙、隔墙与家具均有实体碰撞，门的绿色视觉标识不带碰撞。
- 语义色：灰为实体结构，黄为可走/展示面，蓝为功能家具与雨棚，绿为门洞标识。色块服务于结构阅读，不替代建筑体量。

## 复核图

- `Assets/Screenshots/BF02_P2_V03_Axon.png`
- `Assets/Screenshots/BF02_P2_V03_InteriorCutaway.png`（仅移除屋面用于内部复核，场景中屋面保持启用）

## 验证

`validate_scene`：0 errors，0 warnings。

## 确认结果

BF-02 的真实立体白模、外部结构、内部流线、归档资料和 P3 五视角效果图均已确认通过。
