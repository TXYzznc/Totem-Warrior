# WPN-FP-001 多视角生成提示词

> 状态：供网页版图像生成使用的概念设计辅助文档，不是正式模型、贴图或 Unity 资源。
>
> 参考图：`art/production/weapon/WPN-FP-001/preview/WPN-FP-001_isometric-reference-user-approved-v02.png`

## 使用方式

1. 每次生成都上传上述轴测参考图，并将它设为**外观参考 / image reference**。
2. 优先按下面的“单视角提示词”逐张生成；单图比一次五视角更容易保持比例并方便重抽。
3. 所有视角通过后，再用“可选：五视角转面图”检查整体一致性。
4. 生成结果仅用于建模参考。不要把图中的透视、阴影或细小接缝直接当作模型结构。

## 全局锁定条件

每条单视角提示词都已包含核心约束；若网页支持 Negative Prompt，可额外粘贴以下内容：

```text
do not redesign, do not add accessories, no person, no hands, no character,
no scene, no logos, no readable text, no serial numbers, no brand marks,
no camouflage, no military insignia, no tactical attachments, no rails,
no new glowing parts, no extra cables, no watermark, no perspective distortion
```

外观必须锁定为：暖灰白 / 浅灰蓝的方盒式外壳、深石板灰结构件、烟熏青灰半透明弹匣 / 状态模块、一体式桥架及其大矩形开口、切角护木和唯一的小型琥珀色维护指示灯。除必要的枪口孔洞外，不新增圆形装饰或连续圆润曲面。

---

## 1. 左侧正交视图

```text
Use the uploaded reference image as the only visual source of truth. Create a strict orthographic LEFT SIDE VIEW of the exact same original fictional civilian modular patrol rifle from the reference image.

Lock the silhouette and all existing parts: compact rectangular warm off-white / pale gray-blue upper shell, dark graphite faceted lower structure, trapezoid carry-handle bridge with one large rectangular cutout, angular front handguard, short exposed barrel, smoky translucent teal magazine/status module leaning slightly forward, rear shell cap, and one tiny amber maintenance indicator. Preserve every existing part's relative position, proportion, color blocking, seam rhythm, and hard-edged low-poly-conscious design language. Do not redesign or add parts.

Camera: perfectly orthographic side elevation, muzzle pointing left, no perspective, no three-quarter angle, no crop. Center the complete object on a plain light neutral-gray studio background with a faint baseline. Crisp stylized hard-surface game concept render; flat, readable material separation; no dramatic lighting.
```

## 2. 右侧正交视图

```text
Use the uploaded reference image as the only visual source of truth. Create a strict orthographic RIGHT SIDE VIEW of the exact same original fictional civilian modular patrol rifle. This is the mirrored physical side of the same object, not a redesigned mirror copy.

Keep the exact overall proportions, rectangular warm off-white / pale gray-blue upper shell, dark graphite faceted underframe, integrated trapezoid carry-handle bridge with its large rectangular opening, angular front handguard, short exposed barrel, smoky translucent teal magazine/status module, rear shell cap, and tiny amber maintenance indicator. Infer only the hidden-side construction needed for a plausible matching counterpart; preserve the same panel hierarchy and do not invent extra controls, accessories, rails, labels, or ornament.

Camera: perfectly orthographic side elevation, muzzle pointing right, no perspective, no three-quarter angle, no crop. Center the complete object on a plain light neutral-gray studio background with a faint baseline. Crisp stylized hard-surface game concept render, broad clean planes and hard edges.
```

## 3. 顶部正交视图

```text
Use the uploaded reference image as the only visual source of truth. Create a strict orthographic TOP VIEW of the exact same original fictional civilian modular patrol rifle.

Preserve the same compact length, narrow central maintenance spine, broad rectangular upper shell, faceted shoulder protection, trapezoid carry-handle / sight bridge with its clearly visible rectangular opening, centered angular front handguard, rear shell cap, and the existing dark graphite structural frame. The smoky translucent teal magazine/status module must be visible below the body only where physically appropriate. Do not add top rails, optics, labels, cables, vents, accessories, or rounded decorative geometry.

Camera: true top-down orthographic plan, muzzle pointing left, no perspective and no crop. Center the complete object on a plain light neutral-gray studio background. Use neutral even lighting that clearly separates the warm shell, dark structure, metal barrel, and translucent module.
```

## 4. 前方 / 枪口正交视图

```text
Use the uploaded reference image as the only visual source of truth. Create a strict orthographic FRONT VIEW looking directly into the front end of the exact same original fictional civilian modular patrol rifle.

Lock the faceted rectangular handguard outline, the low-segment functional barrel opening centered inside it, the dark graphite frame, the warm off-white outer shell, and the bridge's visible upper silhouette. The circular or polygonal barrel aperture is allowed only because it is functional; every surrounding form must remain square, trapezoidal, wedge-shaped, and sharply chamfered. Show enough of the translucent teal lower module and grip silhouette to establish their centered attachment, without introducing new parts.

Camera: perfectly orthographic front elevation, centered on the muzzle axis, no perspective, no crop, no dramatic foreshortening. Plain light neutral-gray studio background; clean game-concept material rendering; no effects or visible projectile.
```

## 5. 后方 / 枪托正交视图

```text
Use the uploaded reference image as the only visual source of truth. Create a strict orthographic REAR VIEW looking directly at the rear shell of the exact same original fictional civilian modular patrol rifle.

Preserve the compact rectangular rear shell cap, dark graphite edge frame, warm off-white body shell, centered top bridge silhouette, and the correctly aligned lower grip and smoky translucent teal magazine/status module. Infer only the rear-facing panels necessary to complete the existing design; maintain the same sparse broad surfaces, hard single-step bevels, trapezoids, and asymmetry only where the reference already suggests it. Do not add a realistic stock mechanism, logos, labels, accessories, extra lights, or new rounded details.

Camera: perfectly orthographic rear elevation, centered on the longitudinal axis, no perspective and no crop. Plain light neutral-gray studio background with even neutral lighting. Keep all shapes readable for low-cost hard-surface modeling.
```

---

## 可选：五视角转面图

若网页版生成器能稳定处理参考图和多物体布局，可使用这一条作整体复核；它不是单视角输出的替代品。

```text
Use the uploaded reference image as the only visual source of truth. Produce a clean orthographic turnaround sheet of the exact same original fictional civilian modular patrol rifle. Arrange five separate, equal-scale, fully visible views on one wide landscape board: LEFT SIDE, RIGHT SIDE, TOP, FRONT, and REAR. Every view must be strict orthographic with no perspective and must preserve the reference image's exact silhouette, proportions, part placement, color blocking, rectangular upper shell, dark graphite faceted frame, trapezoid bridge with a large rectangular opening, angular handguard, smoky translucent teal lower module, rear cap, and tiny amber maintenance indicator.

Use only small non-decorative view labels: LEFT, RIGHT, TOP, FRONT, REAR. Plain light neutral-gray background, faint shared baseline, even studio lighting, crisp stylized hard-surface game concept rendering. Preserve the low-cost language of boxes, trapezoids, wedges and hard chamfers. Do not redesign, add parts, use perspective, add characters, logos, readable maintenance text, accessories, camouflage, military insignia, effects, watermarks, or UI framing.
```

## 验收重点

- 五张图的总长度、桥架开口、机匣高度、护木长度、透明模块位置必须一致。
- 右侧图允许补全不可见面，但不得凭空增加操作机构、导轨或装饰。
- 正面与后面用于确认横向截面；不应为了好看而把方盒轮廓改成圆滑曲面。
- 若单图与轴测图矛盾，以这张用户确认的轴测图为准；冲突部分标注后再重生成。
