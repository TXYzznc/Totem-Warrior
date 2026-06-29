# Tasks — 12-core-ui-screens

> ✅ = 已交付；🔲 = 未完成。按 CLAUDE.md §六 UI 制作 5 阶段流程执行，禁止跳阶段。

## Phase 0 — 需求设计（阶段 1）

- [x] 重新审视 9 GDD Form + SettingsForm 清单，排查遗漏页面（见 design.md §一）→ 结论：不新增页面
- [x] `art/requirements.md` 三表起草（页面清单/复用组件清单/组件状态表）
- [x] 用户确认三表 + 风格基线沿用决定

## Phase 1 — 效果图设计与生成（阶段 2-3，批次执行）

### 批次 1：CombatHUDForm / MainMenuForm
- [x] art-ui 撰写 `art/prompts.md` 中 CombatHUDForm / MainMenuForm 效果图提示词
- [x] codex-image-gen 出图（1 轮通过），写入 `art/mockups/`
- [x] 用户确认批次 1 效果图

### 批次 2：CharacterSelectForm / PauseMenuForm / RunResultForm
- [x] art-ui 撰写对应提示词
- [x] codex-image-gen 出图（1 轮通过）
- [x] 用户确认批次 2 效果图

### 批次 3：TattooStudioForm / TattooEnchantForm / ShopForm / ThreeChoiceForm
- [x] art-ui 撰写对应提示词
- [x] codex-image-gen 出图（1 轮通过）
- [x] 用户确认批次 3 效果图

### 批次 4（复查新增）：SelfTattooForm
- [x] 复查 GDD `01-纹身构筑系统.md` §2.8/§5.1/§5.2，发现"玩家自纹身"机制无 UI 承载的缺口
- [x] design.md / art/requirements.md 同步更新（清单 10→11，含归入 CombatHUDForm 的读条子区块）
- [x] 线框文字稿确认（部位默认选中/库存示例/解锁示例/预览文案/人形风格/关闭按钮，移除风险提示文字）
- [x] art-ui 撰写提示词
- [x] codex-image-gen 出图（1 轮通过）
- [x] 用户确认 SelfTattooForm 效果图
- [x] CombatHUDForm 读条悬浮子区块（角色脚下圆环+屏幕中央进度条）后续在 Phase 2 实现时补充设计（不单独出图，跟随 CombatHUDForm 联调时一起做视觉）

## Phase 2 — Prefab 视觉调整（阶段 4）

- [x] 按效果图逐个调整 9 个 Form 的现有 Prefab（间距/字号/配色/Sprite），不改 RectTransform 层级结构
  - 7 个 Form（CombatHUD / MainMenu / CharacterSelect / PauseMenu / RunResult / TattooStudio / Shop）视觉与 mockup 对齐
  - 4 个 Form（Settings / SelfTattoo / ThreeChoice / TattooEnchant）因 unity-skills MCP 中文编码 bug 出现乱码 + 未绑 Sprite，移交 follow-up change 13-fix-broken-prefabs 处理
- [x] art-ui 出标注稿，与 client-unity 的 Prefab 调整并行（Fan-Out，WhenAll 汇合）
- [x] 编译通过 + 运行时打开各 Form 截图自检

## Phase 3 — 完整可玩联调（阶段 5）

- [x] 端到端跑一局：主菜单 → 角色选择 → 战斗 HUD → 纹身师/商人/三选一交互 → 暂停 → 结算 → 返回主菜单
- [x] 验证 Form 触发链路（见 design.md §2.2 表）
- [x] 验证 Form 关闭链路（ESC/B 键、3s 防误触锁、读条按钮锁定）
- [x] 验证 Sort Order 层级遮挡关系（0/10/20/30）
- [x] 验证跨 Form 状态同步（如金币数）
- [x] 验证异常路径（战斗中死亡时覆盖层强制关闭）
- [x] SettingsForm 从 MainMenu / PauseMenu 两个入口打开验证（与 10-settings-form 联调对接）
- [x] 运行时截图 vs 效果图对比，记录偏差清单（4 个 Prefab 偏差移交 13-fix-broken-prefabs）
- [x] 发现的 UI 显示/交互 bug 修复；非 UI 范畴问题记录到 `tests/bugs.md` 转其他系统 owner

## Phase 4 — 收尾

- [x] `tests/results.md` 汇总联调结果（移交段含 4 个返工 Prefab 清单）
- [x] 同步更新 `项目知识库（AI自行维护）/INDEX.md`
- [x] `openspec archive 12-core-ui-screens`
