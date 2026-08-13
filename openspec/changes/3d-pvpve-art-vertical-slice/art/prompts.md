# INT-PROP 可交互道具三视图提示词

美术素材状态：已处理
处理日期：2026-08-06
执行方式：imagegen 系统能力
最终输出目录：`artifacts/美术资源需求/模型/INT-PROP_可交互道具/<资源目录>/建模多视图/`
OpenSpec 原始归档：`art/raw/interactive-props/`
生成记录：`art/raw/interactive-props/生成记录.md`

## 生成方法

每个输出都以对应的 `INT-PROP-xxx_Axon.png` 作为唯一图像参考，并与下方“资源不可变约束”和“视图后缀”组合成一次独立生成请求。禁止一次生成三视图拼版；一个请求只生成一个视角、一个物体。

## 全局提示词

```text
Use case: stylized-concept
Asset type: game-ready 3D prop orthographic modeling reference
Input image: the supplied approved axonometric render is the identity reference. Preserve exactly the same object design, silhouette, proportions, component count, materials, color placement, edge treatment, wear level and stylized 3D rendering language.
Primary request: render the exact same prop from the requested strict orthographic direction. This is a new camera view of the approved object, not a redesign.
Scene/backdrop: clean neutral light studio background matching the approved axonometric reference, with only a subtle grounding shadow.
Composition/framing: exactly one complete object, centered, level camera, consistent baseline, generous even padding, no cropping. No perspective distortion, no elevated or lowered camera, no three-quarter angle.
Lighting/mood: soft neutral product lighting, high structural readability, restrained ambient occlusion, no dramatic colored light.
Constraints: obey every side-specific placement and count in the asset constraint block. Preserve the approved default closed/static state. No text, label, dimensions, arrows, UI, watermark, environment, character or extra prop.
Avoid: invented details, duplicated fittings, mirrored functional faces, open lids, active screens, glowing effects, particles, colorful liquids, damage not present in the reference, or a contact sheet.
```

## 视图后缀

### Front

```text
Requested direction: strict FRONT orthographic elevation, looking perpendicular to the documented functional front face. Show no right-side face and no top face except the minimum physically unavoidable rim thickness. Output filename suffix: _Front.png.
```

### Rear

```text
Requested direction: strict REAR orthographic elevation, exactly 180 degrees opposite the documented front. Show no left/right side face and no top face except the minimum physically unavoidable rim thickness. Output filename suffix: _Rear.png.
```

### Left

```text
Requested direction: strict LEFT orthographic elevation of the object's own left side when facing its documented front. This is not the object's right side and must not relocate front/rear functional parts. Show no front face except the minimum physically unavoidable edge thickness. Output filename suffix: _Left.png.
```

## 资源不可变约束

### INT-PROP-001 小型补给箱

- Front：单个主锁扣严格居中；连续水平盖缝；顶部居中的一体化提手；暖白壳体、深灰蓝底带，左右短脚落地。
- Rear：无锁扣和搬运槽；盖缝上方两个相同低矮铰链，位于约 1/3 和 2/3；提手仍居中。
- Left：中央偏上的横向搬运凹槽，深灰内腔、青绿上沿、暖白外框；无锁扣。
- 总量：锁扣 1、背部铰链 2、左右搬运凹槽各 1、顶部提手 1。

### INT-PROP-002 标准物资箱

- Front：两个相同锁扣在约 1/3 与 2/3 对称布置；宽浅凹面板；平整可堆叠顶盖，无顶部提手。
- Rear：无锁扣和把手；两个低矮铰链；宽浅凹面板。
- Left：上半部居中的横向搬运凹槽；前后暖白角柱和深灰角脚。
- 总量：前锁扣 2、背铰链 2、左右搬运凹槽各 1；比例明显比 001 更长更宽。

### INT-PROP-003 高价值加固箱

- Front：中央安全锁组件 1 套，由黑色空白显示槽、喷砂金属框和深灰机械锁体构成；珊瑚橙只作小安全件；左右暖白面板和四角护甲。
- Rear：无安全锁；两个宽厚铰链；中央纵向加强框；四角护甲和橙色密封线连续。
- Left：中央深灰搬运握位，厚金属保护框和一条珊瑚橙安全件；前后纵向护框闭合受力。
- 总量：中央锁 1、背铰链 2、左右握位各 1、四角护甲 4 组；禁止金色和发光宝石。

### INT-PROP-004 便携颜料罐

- Front：中轴细长八边形观察窗，深灰内框和静止中性灰蓝液位；顶部居中密封盖、青绿锁环和横跨左右的提手。
- Rear：连续暖白壳面，无观察窗、标签槽、阀门和管线。
- Left：中央浅凹矩形空白标签槽，无观察窗；显示提手支座与握把厚度但不得相互穿插。
- 总量：只有正面观察窗 1；左右各有空白标签槽 1；背面无标签；密封盖 1、提手 1。

### INT-PROP-005 标准颜料封存罐

- Front：中轴窄长八边形观察窗；中性灰蓝静止液位；完整青绿上锁环和下密封环；上下短锁止块与窗口同轴。
- Rear：无观察窗；上半部中心、紧邻上锁环下方只有一个带封帽的压力接口，无表盘和软管。
- Left：连续圆筒曲面，无窗口和接口；完整上下环与居中顶盖。
- 总量：正面窗口 1、背面压力接口 1；圆筒直径、高度和环厚不变。

### INT-PROP-006 大型颜料储罐

- Front：上部中轴观察窗；下部两个封闭管口，观察者左侧较大、右侧较小且高度错开；骨架和柱脚完整接地。
- Rear：无窗口和管口；两条平行竖向维护安装轨，各有三组对齐的空白踏点安装座；绝不生成完整梯子。
- Left：与正面同尺寸的纵向观察窗；侧骨架为单组 X 斜撑且不遮挡窗口；没有正面管口。
- 总量：正、左、右各 1 窗，背面无窗；两个管口都只在正面；高度 2.40m、直径 1.80m。

### INT-PROP-007 泄漏危险颜料罐

- Front：上半部中轴深色观察窗，下半部同轴封闭风险接口；接口为深灰基座、珊瑚橙八边外环和圆形封盖；正面唯一集液托盘。
- Rear：无窗口、接口和托盘；左右对称斜撑；连续暖白罐体。
- Left：前后立柱之间一条对角斜撑；罐体连续无窗无接口；托盘仅在前缘显示侧面厚度。
- 总量：窗口、风险接口、托盘都仅在正面；默认无彩色液池、滴流、烟雾和粒子。

### INT-PROP-008 木质货箱

- Front：四条等高横向宽木板；第一条木板中央一个真实贯穿的圆角矩形搬运孔；左右青灰竖向加固条和角护块。
- Rear：结构数量与正面相同，木纹可自然变化但不镜像；无铰链、盖缝和锁。
- Left：四条横板；第一条中央同尺寸搬运孔；前后竖向加固条和连续底梁。
- 总量：四个竖直面各有搬运孔 1；每面横板 4；顶面无孔；不是可开盖箱。

### INT-PROP-009 陶制储罐

- Front：两个相对把手左右对称且完整可见；厚圆口、短颈、宽腹、窄底；肩部 2 条和底部 2 条连续釉带；罐口敞开且内部为空。
- Rear：整体曲线和两个把手轮廓与正面一致；无附加纹样、裂纹和附件。
- Left：只显示左侧外轮廓上的一个完整椭圆把手，另一把手被罐体遮挡；釉带水平连续。
- 总量：把手 2，肩部釉带 2、底部釉带 2；无盖、内容物、裂口和出水口。

### INT-PROP-010 轻金属运输箱

- Front：单个锁止件严格居中；窄水平盖缝；薄板大面、浅压边和少量铆点；顶盖两条前后走向压筋，无提手。
- Rear：无锁止件；两个低矮扁平铰链；薄金属大面。
- Left：上半部中央横向搬运握位，深灰凹腔、青绿上沿、薄金属折边框；无锁扣。
- 总量：前锁止件 1、背铰链 2、左右握位各 1、顶盖压筋 2；保持薄板感，禁止厚装甲框。

### INT-PROP-011 玩家死亡箱

- Front：短边中央唯一领取接口，默认关闭；顶面大面积身份槽保持空白；低矮圆角胶囊轮廓和深灰缓冲环。
- Rear：无领取接口和玩家信息；两个隐藏式低矮铰链；身份槽后缘可见但仍空白。
- Left：连续圆角胶囊壳面；只显示分缝和缓冲环；身份槽低矮嵌入；正面接口只可见极小边缘厚度。
- 总量：前领取接口 1、背铰链 2、顶面身份槽 1；不得生成姓名、头像、阵营、纹身或编号。

### INT-PROP-012 撤离点装置

- Front：倾斜黑色空白屏幕和屏幕下方中央读取模块；四个锚座中的两个前锚座可见；顶部中央轴与四片开放框臂，不发光。
- Rear：无屏幕和读取模块；中央封闭维护面板及下方一个带封盖维护接口；两根斜撑连接两个后锚座。
- Left：明确的楔形交互台侧面，屏幕后倾，读取模块从前下方突出；前后两个左侧锚座和连接后锚座的斜撑完整可见。
- 总量：四个锚座固定为 1.20m 方形，高度 2.80m；信号框臂 4；屏幕、灯带和信号面关闭；无光束、范围环、倒计时和粒子。

## 文件路径映射

每个 `INT-PROP-xxx` 的三张输出路径为：

```text
artifacts/美术资源需求/模型/INT-PROP_可交互道具/<对应资源目录>/建模多视图/INT-PROP-xxx_Front.png
artifacts/美术资源需求/模型/INT-PROP_可交互道具/<对应资源目录>/建模多视图/INT-PROP-xxx_Rear.png
artifacts/美术资源需求/模型/INT-PROP_可交互道具/<对应资源目录>/建模多视图/INT-PROP-xxx_Left.png
```

生成后必须实际复制或移动到以上路径，并检查图片可解码；不得只保留在 Codex 默认生成目录。
