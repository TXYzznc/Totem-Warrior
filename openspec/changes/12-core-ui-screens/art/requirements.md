# 美术需求 — 12-core-ui-screens

> 状态：批次 1-3（9 张）已出图，**SelfTattooForm 为复查后新增的第 10 个 Form，待出图**
> 范围：整理自 GDD `13-UI与HUD.md` + `01-纹身构筑系统.md` §2.8/§5.1/§5.2，不含 SettingsForm（已在 10-settings-form 单独处理）

## 表 A：页面清单（最小可用 UI）

| 页面 | 优先级 | 备注 |
|---|---|---|
| MainMenuForm | 必做 | 全屏，开始/设置/退出 |
| CharacterSelectForm | 必做 | 全屏，GridLayoutGroup 角色卡（本期 1 角色，骨架预留多角色） |
| CombatHUDForm | 必做 | 常驻战斗 HUD，含 HP 条/Buff/技能槽 Q/E/武器弹药/小地图/缩圈倒计时/战斗日志/Build 列表/被动条目/Boss HP 条 10 个子区块 |
| TattooStudioForm | 必做 | 800×600 居中覆盖层，含读条 UI + 死亡宝箱地图高亮 |
| TattooEnchantForm | 必做 | 700×500 居中覆盖层，嵌套于 TattooStudio 流程 |
| ShopForm | 必做 | 700×500 居中覆盖层，GridLayoutGroup 库存格 |
| ThreeChoiceForm | 必做 | 900×400 居中覆盖层，3 张 CardPanel 横排，强制选择 |
| PauseMenuForm | 必做 | 全屏遮罩，继续/设置/退出 |
| RunResultForm | 必做 | 全屏动画进场，杀敌/存活/Build 快照 |
| **SelfTattooForm**🆕 | **必做** | **任意地点 Tab 唤出，居中覆盖层。部位选择+颜色库存(按数量显示)+图案解锁状态(锁图标)+预览+风险提示+开始/取消。GDD 依据：`01-纹身构筑系统.md` §2.8/§5.1，后端 API 已实现（`TattooModule.StartSelfTattoo`），UI 一直缺失** |
| 读条悬浮 UI🆕 | 必做（子区块） | 不单独开 Form，并入 CombatHUDForm 常驻子区块（角色脚下圆环+屏幕中央进度条），仅自纹身进行时显示。GDD 依据：§5.2 |

## 表 B：复用组件清单（基于表 A 推算，引用 GDD 13-UI与HUD.md §四 表 B）

| 组件 | 目标数量 | 用途 / 下一步 |
|---|---|---|
| IconFrame（图标底框） | 约 40 个 | 纹身/技能/武器/Buff 图标统一视觉规格，圆角 r=4px + 描边 1px |
| ProgressBar（filled horizontal） | 5 个 | HP / 冷却 Q / 冷却 E / 读条 / Boss HP，fillAmount 复用 |
| ScrollListRow | 约 40 条 | Build 槽 + 被动 + 日志列表行，统一行高 24px@1080p |
| CardPanel | 3 张 | ThreeChoiceForm 选项卡，4 态样式复用 |
| ModalOverlay | 5 个 | 工作台/商人/三选一/暂停/结算覆盖层底板，alpha 0.6 |
| ControllerGlyph | 约 10 个 | 按键提示 Sprite（Xbox/PS/Switch 三套），仅手柄接入时显示 |

## 表 C：组件状态表（引用 GDD 13-UI与HUD.md §四 表 C）

| 组件 | 必备状态 | 备注 |
|---|---|---|
| HP 条 | normal(绿) / warning(黄,<50%) / critical(红,<25%) / dead(灰) | warning/critical 触发 DOTween 闪烁 |
| 技能槽 Q/E | ready / cooldown / no-skill / active | radial filled 冷却遮罩 |
| Buff 标签 | active / expiring(<3s) / consumed | alpha 淡出 |
| 武器图标 | equipped / switching / empty-ammo | sprite swap + alpha |
| 缩圈倒计时 | safe(绿) / warning(橙,<30s) / urgent(红,<10s) | urgent 时 scale 脉冲 |
| CardPanel | idle / hover / selected / locked(3s 内) | hover scale 1.03 |
| ModalOverlay | open / idle / close | CanvasGroup alpha DOTween 0.2s |
| Boss HP 条 | normal / phase-change / hidden | 仅 BossSpawnedEvent 后显示 |
| 读条（TattooEnchant） | idle / filling / success / fail | filling 期间按钮全部锁定 |

## 风格基线（已自决：沿用现有锁定风格）

继承 `06-v21-implementation/art/requirements.md §0` 与 `10-settings-form` 已出图确认的风格基线，不另起指南：

- **整体定位**：似 Hades 精致 2.5D——手绘描边 + 高对比光影 + 厚涂 + 鲜艳饱和色彩
- **色彩硬约束**：面板背景 `#1A1C2E`（90% 透明度）/ 主文字 `#F8F9FA` / 次级文字 `#A8A9C0` / Accent 金色 `#FFB400` / 描边 `#22243A` / 分割线 `#2E3050`
- 9 个新出图 Form 全部沿用此色板与描边规范，保证与已确认的 SettingsForm 视觉一致

## 待用户确认事项

- [ ] 三表范围与优先级是否准确（是否有遗漏区块）
- [ ] 确认后进入阶段 2（art-ui 撰写 `prompts.md`）
