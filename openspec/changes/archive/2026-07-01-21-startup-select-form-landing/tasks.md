# Tasks — 21-startup-select-form-landing

> **Phase 0 盘点后重写**：素材复用率 100%，全程跳过 codex-image-gen。核心工作转到 code + prefab。

## Phase 0 · 盘点结果（已完成 ✓）

- ✅ 现有 CharacterSelectForm 素材完整（bg / button_idle / button_primary / card_frame_unlocked / lock_icon）
- ✅ 5 武器 sprite 齐（weapon_short_blade / heavy_hammer / pistol / bow / energy_fist）对应 5 个 WeaponId
- ✅ 颜料 sprite 齐（paint_red/yellow/blue_common 对应 Id 1/2/4）
- ✅ 图案 sprite 齐（Line / Ring）
- ✅ 角色 sprite：Player1/2/3 各有 Idle/Down.png 可作 icon
- ✅ MainMenuForm.OnStartClicked **当前直接调 gs.StartGame()**（跳过了 CharSel/StartupSel）— **必须改**

## Phase 1 · 需求文档（精简）

- [ ] 1.1 art/requirements.md：写「素材映射清单」代替常规三表（复用无需出图）
- [ ] 1.2 art/annotations.md：写各 Form 层级 + 元素位置说明

## Phase 2 · 代码接线（主对话直接改，工作量小时序关键）

- [ ] 2.1 改 `StartupSelectForm.cs`：
  - CreateCard 加 sprite 参数，Resources.Load<Sprite> 挂到 Image
  - BuildOptions 用 sprite 路径映射表（颜料→paint_*_common，武器→weapon_*，图案→pattern_*）
  - OnConfirm 末尾追加 `_runner.GetModule<GameStateModule>().StartGame()`
- [ ] 2.2 改 `CharacterSelectForm.cs`：从空壳→实体
  - 加 3 张角色卡片动态生成（Player1/2/3 Idle sprite）
  - 加 Next 按钮接线（打开 StartupSelectForm）
  - Bootstrap 拿 EventBus / ModuleRunner（走 GameApp.TryGetRuntime 已有模式）
- [ ] 2.3 改 `MainMenuForm.cs.OnStartClicked`：
  - **删除** `gs.StartGame()` 直调
  - **改为** `_runner.GetModule<UIModule>().Open<CharacterSelectForm>()`（或直接 FindObjectOfType 兜底）
- [ ] 2.4 UIModule.Register：确认 CharacterSelectForm / StartupSelectForm 都被注册（缺则补）

## Phase 3 · Prefab 构建（client-unity 主导，unity-skills MCP）

- [ ] 3.1 unity-skills MCP 更新 `Assets/Resources/Prefab/UI/CharacterSelect.prefab`：
  - Canvas Panel → Background(Image, 用 CharacterSelectForm_bg) → Title(Text) → CharacterRoot(HorizontalLayoutGroup) → NextButton(Button, 用 button_primary)
- [ ] 3.2 unity-skills MCP 新建 `Assets/Resources/Prefab/UI/StartupSelect.prefab`：
  - Canvas Panel → Background → Title → ColorRoot / WeaponRoot / PatternRoot（3 个 HorizontalLayoutGroup） → ConfirmButton + CancelButton

## Phase 4 · 联调 Loop（核心，反复迭代）

- [ ] 4.1 editor_stop → asset_refresh → editor_get_state isCompiling=false → editor_play
- [ ] 4.2 从 MainMenu 「开始」 → 验证跳 CharacterSelect
- [ ] 4.3 CharSel 选 Player1 卡片 → Next 亮 → 点击 → 跳 StartupSelect
- [ ] 4.4 StartupSel 选 red + short_blade + Line → Confirm 亮 → 点击 → 进 InGame
- [ ] 4.5 验证 `SpawnerModule Action=OnStartupSelected Weapon=knife_basic` 日志
- [ ] 4.6 战斗到 PlayerDied → GoToMainMenu → 再走一遍换 pistol → 进 InGame → 验证换武器生效
- [ ] 4.7 console 0 Error，仅剩 MainMixer/StartupSelect 无关的启动 warning（若有 21 相关 warning 修掉）
- [ ] 4.8 loop：任一步失败 → 修 → 重跑 4.1

## Phase 5 · 归档

- [ ] 5.1 写 `tools/playtest/reports/2026-07-01-XXXX-startup-select-landing.md`
- [ ] 5.2 `openspec archive-change 21-startup-select-form-landing`
- [ ] 5.3 更新 `项目知识库（AI自行维护）/INDEX.md`
